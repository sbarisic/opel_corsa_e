#!/usr/bin/env python3
"""BCM CAN gateway analysis helpers for the Corsa E BCM program flash.

The script intentionally stays table/xref oriented. It does not try to
auto-disassemble the target CPU; instead it emits repeatable anchors for manual
V850/RH850-style reversing around the CAN gateway paths.
"""

from __future__ import annotations

import argparse
import csv
import re
import statistics
import subprocess
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_FLASH = REPO_ROOT / "data" / "raw" / "bcm_p_flash.bin"
DEFAULT_LOG_GLOB = REPO_ROOT / "data" / "can_logs" / "*"
DEFAULT_OUTPUT_DIR = REPO_ROOT / "analysis" / "generated"
MAPPED_FLASH_BASE = 0x80000000

TABLES = [
    {
        "name": "Group A",
        "inference": "BCM transmit / periodic output / body-base table",
        "offset": 0x01DE74,
        "count": 131,
    },
    {
        "name": "Group B",
        "inference": "BCM receive filter / input table",
        "offset": 0x01E830,
        "count": 148,
    },
]

DISPATCH_TABLES = [
    {
        "name": "Dispatch/wrapper table",
        "offset": 0x021A40,
        "count": 514,
        "stride": 12,
        "inference": "message-object key to handler wrapper/function and class",
    },
]

GROUP_A_DLC_OFFSET = 0x01E080
GROUP_A_PAYLOAD_PTR_OFFSET = 0x01E104
GROUP_A_AUX_FUNC_A_OFFSET = 0x01E310
GROUP_A_AUX_FUNC_B_OFFSET = 0x01E51C
GROUP_A_SCHED_BUCKET_OFFSET = 0x01E730
GROUP_A_SCHED_MASK_OFFSET = 0x01E7B0
GROUP_A_COUNT = 131

FILTER_MASK_TABLES = [
    {
        "name": "Filter/mask table A",
        "offset": 0x0176CA,
        "count": 13,
        "stride": 8,
        "inference": "CAN encoded value plus mask/class word",
    },
    {
        "name": "Filter/mask table B",
        "offset": 0x025D12,
        "count": 11,
        "stride": 8,
        "inference": "CAN encoded value plus mask/class word",
    },
]

TARGET_IDS = [
    0x0C9,
    0x3D1,
    0x3E9,
    0x4C1,
    0x4D1,
    0x4F1,
    0x160,
    0x1F1,
    0x241,
    0x641,
]

LOW_SPEED_TX_FOCUS_IDS = {
    0x0F1,
    0x12A,
    0x135,
    0x137,
    0x139,
    0x160,
    0x1F1,
    0x451,
    0x4E1,
    0x514,
}

RX_HANDLER_TARGET_IDS = {0x0C9, 0x3D1, 0x3E9, 0x4C1, 0x4D1}
IPC_BODY_TARGET_IDS = {
    0x0F1,
    0x12A,
    0x135,
    0x137,
    0x139,
    0x160,
    0x1F1,
    0x451,
    0x4E1,
    0x514,
}

ECU_STREAM_FILES = {
    "corsa_e_no_ecu_streams.candump",
}

NATIVE_BODY_FILES = {
    "corsa_e_opc_ecumaster_read.candump",
    "corsa_e_opc_ecumaster_read_engine_running.candump",
}

COMBINED_FILES = {
    "corsa_e_opc_ecumaster_bcm_combined_keyoff.candump",
    "corsa_e_opc_ecumaster_bcm_combined_keyon.candump",
    "corsa_e_opc_ecumaster_bcm_combined_keyon_startcar.candump",
}

KNOWN_ECU_STREAM = {
    0x0C9,
    0x1BC,
    0x1BD,
    0x1C1,
    0x1F5,
    0x2C5,
    0x3C9,
    0x3D1,
    0x3E9,
    0x3F9,
    0x4C1,
    0x4D1,
}

KNOWN_NATIVE_BODY = {
    0x0F1,
    0x120,
    0x12A,
    0x135,
    0x137,
    0x139,
    0x140,
    0x160,
    0x1E1,
    0x1F1,
    0x1F3,
    0x32A,
    0x3C9,
    0x3CB,
    0x3E7,
    0x3F1,
    0x451,
    0x4C5,
    0x4D7,
    0x4E1,
    0x4E9,
    0x514,
    0x52A,
    0x530,
}

CAN_DUMP_RE = re.compile(r"\)\s+\S+\s+([0-9A-Fa-f]{1,8})#([0-9A-Fa-f]*)")
TEXT_CAPTURE_RE = re.compile(
    r"Standard ID:\s*0x([0-9A-Fa-f]+)\s+DLC:\s*(\d+)\s+Data:\s*(.*)"
)
HEX_BYTE_RE = re.compile(r"0x([0-9A-Fa-f]{2})")
CAN_DUMP_EVENT_RE = re.compile(
    r"^\((?P<timestamp>[0-9]+(?:\.[0-9]+)?)\)\s+\S+\s+"
    r"(?P<frame_id>[0-9A-Fa-f]{1,8})#(?P<payload>[0-9A-Fa-f]*)"
)
R2_LINE_RE = re.compile(
    r"0x(?P<offset>[0-9A-Fa-f]+)\s+(?P<bytes>[0-9A-Fa-f]+)\s+(?P<op>.*)"
)

HELPER_TARGETS = {
    0x34BD2: "sfr_register_helper",
    0x34AAE: "can_command_queue_helper",
}

R2_FOCUSED_RANGES = [
    (0x02E000, 0x1200, "rx_buffer_or_can_service_path"),
    (0x034A00, 0x0300, "can_helper_implementations"),
    (0x130000, 0x6000, "payload_wrapper_region"),
]


@dataclass(frozen=True)
class TableEntry:
    group: str
    inference: str
    index: int
    offset: int
    raw: int
    frame_id: int
    flags: int


@dataclass(frozen=True)
class Occurrence:
    frame_id: int
    raw: int
    offset: int
    context: str
    table_group: str
    table_index: str
    mapped_address: int


@dataclass(frozen=True)
class DispatchEntry:
    table: str
    inference: str
    index: int
    offset: int
    object_key: int
    handler: int
    entry_class: int


@dataclass(frozen=True)
class FilterMaskEntry:
    table: str
    inference: str
    index: int
    offset: int
    raw: int
    frame_id: int
    flags: int
    mask_class: int


@dataclass(frozen=True)
class HelperCallsite:
    helper: int
    helper_name: str
    callsite: int
    instruction_bytes: str
    instruction: str
    scan_range: str


def hex_id(value: int) -> str:
    return f"0x{value:03X}"


def hex_addr(value: int) -> str:
    return f"0x{value:06X}"


def hex_raw(value: int) -> str:
    return f"0x{value:08X}"


def u32le(data: bytes, offset: int) -> int:
    return int.from_bytes(data[offset : offset + 4], "little")


def decode_tables(data: bytes) -> list[TableEntry]:
    entries: list[TableEntry] = []
    for table in TABLES:
        start = table["offset"]
        for index in range(table["count"]):
            offset = start + index * 4
            raw = u32le(data, offset)
            entries.append(
                TableEntry(
                    group=table["name"],
                    inference=table["inference"],
                    index=index,
                    offset=offset,
                    raw=raw,
                    frame_id=(raw >> 18) & 0x7FF,
                    flags=raw & 0x3FFFF,
                )
            )
    return entries


def decode_dispatch_tables(data: bytes) -> list[DispatchEntry]:
    entries: list[DispatchEntry] = []
    for table in DISPATCH_TABLES:
        start = table["offset"]
        stride = table["stride"]
        for index in range(table["count"]):
            offset = start + index * stride
            entries.append(
                DispatchEntry(
                    table=table["name"],
                    inference=table["inference"],
                    index=index,
                    offset=offset,
                    object_key=u32le(data, offset),
                    handler=u32le(data, offset + 4),
                    entry_class=u32le(data, offset + 8),
                )
            )
    return entries


def decode_filter_mask_tables(data: bytes) -> list[FilterMaskEntry]:
    entries: list[FilterMaskEntry] = []
    for table in FILTER_MASK_TABLES:
        start = table["offset"]
        stride = table["stride"]
        for index in range(table["count"]):
            offset = start + index * stride
            raw = u32le(data, offset)
            entries.append(
                FilterMaskEntry(
                    table=table["name"],
                    inference=table["inference"],
                    index=index,
                    offset=offset,
                    raw=raw,
                    frame_id=(raw >> 18) & 0x7FF,
                    flags=raw & 0x3FFFF,
                    mask_class=u32le(data, offset + 4),
                )
            )
    return entries


def table_lookup(entries: Iterable[TableEntry]) -> dict[int, TableEntry]:
    return {entry.offset: entry for entry in entries}


def table_range_lookup() -> list[tuple[int, int, str]]:
    ranges = []
    for table in TABLES:
        start = table["offset"]
        end = start + table["count"] * 4
        ranges.append((start, end, table["name"]))
    for table in DISPATCH_TABLES:
        start = table["offset"]
        end = start + table["count"] * table["stride"]
        ranges.append((start, end, table["name"]))
    for table in FILTER_MASK_TABLES:
        start = table["offset"]
        end = start + table["count"] * table["stride"]
        ranges.append((start, end, table["name"]))
    return ranges


def classify_file(path: Path) -> str:
    name = path.name
    if name in ECU_STREAM_FILES:
        return "ecu_stream"
    if name in NATIVE_BODY_FILES:
        return "native_body"
    if name in COMBINED_FILES:
        return "combined"
    return "misc"


def parse_log_line(line: str) -> tuple[int, int] | None:
    match = CAN_DUMP_RE.search(line)
    if match:
        frame_id = int(match.group(1), 16)
        payload_hex = match.group(2)
        return frame_id, len(payload_hex) // 2

    match = TEXT_CAPTURE_RE.search(line)
    if match:
        frame_id = int(match.group(1), 16)
        dlc = int(match.group(2))
        data_bytes = HEX_BYTE_RE.findall(match.group(3))
        return frame_id, dlc if dlc else len(data_bytes)

    return None


def parse_log_event(line: str) -> tuple[int, int, float | None] | None:
    match = CAN_DUMP_EVENT_RE.search(line)
    if match:
        payload_hex = match.group("payload")
        return (
            int(match.group("frame_id"), 16),
            len(payload_hex) // 2,
            float(match.group("timestamp")),
        )

    parsed = parse_log_line(line)
    if parsed:
        frame_id, dlc = parsed
        return frame_id, dlc, None
    return None


def parse_logs(paths: Iterable[Path]) -> dict[int, dict[str, object]]:
    per_id: dict[int, dict[str, object]] = defaultdict(
        lambda: {
            "count": 0,
            "files": Counter(),
            "classes": Counter(),
            "dlcs": Counter(),
            "period_samples_ms": defaultdict(list),
        }
    )
    last_timestamp: dict[tuple[int, str], float] = {}

    for path in sorted(paths):
        if not path.is_file():
            continue
        file_class = classify_file(path)
        with path.open("r", encoding="utf-8", errors="ignore") as handle:
            for line in handle:
                parsed = parse_log_event(line)
                if not parsed:
                    continue
                frame_id, dlc, timestamp = parsed
                item = per_id[frame_id]
                item["count"] += 1
                item["files"][path.name] += 1
                item["classes"][file_class] += 1
                item["dlcs"][dlc] += 1
                if timestamp is not None:
                    key = (frame_id, path.name)
                    previous = last_timestamp.get(key)
                    if previous is not None and timestamp > previous:
                        samples = item["period_samples_ms"][file_class]
                        if len(samples) < 5000:
                            samples.append((timestamp - previous) * 1000.0)
                    last_timestamp[key] = timestamp
    return per_id


def period_hint(frame_id: int, log_info: dict[int, dict[str, object]]) -> tuple[str, str]:
    info = log_info.get(frame_id)
    if not info:
        return "", ""
    samples_by_class = info.get("period_samples_ms", {})
    samples = []
    for preferred in ("native_body", "ecu_stream", "combined", "misc"):
        if preferred in samples_by_class and samples_by_class[preferred]:
            samples = samples_by_class[preferred]
            break
    if not samples:
        return "", ""
    median_ms = statistics.median(samples)
    hz = 1000.0 / median_ms if median_ms else 0.0
    return f"{median_ms:.2f}", f"{hz:.2f}"


def log_presence(frame_id: int, log_info: dict[int, dict[str, object]]) -> str:
    if frame_id not in log_info:
        return "not_observed"
    classes = sorted(log_info[frame_id]["classes"].keys())
    return "+".join(classes)


def classify_known_sets(frame_id: int) -> str:
    labels = []
    if frame_id in KNOWN_ECU_STREAM:
        labels.append("known_ecu_stream")
    if frame_id in KNOWN_NATIVE_BODY:
        labels.append("known_native_body")
    if frame_id in RX_HANDLER_TARGET_IDS:
        labels.append("rx_handler_target")
    if frame_id in IPC_BODY_TARGET_IDS:
        labels.append("ipc_body_target")
    if frame_id in TARGET_IDS:
        labels.append("priority_xref_target")
    return ";".join(labels)


def classify_message_role(frame_id: int) -> str:
    if frame_id in RX_HANDLER_TARGET_IDS:
        return "ecu_rx_priority"
    if frame_id in {0x4E1, 0x514, 0x52A, 0x530}:
        return "vin_or_config_periodic"
    if frame_id in {0x160, 0x1F1, 0x0F1, 0x451}:
        return "wake_keepalive_or_sync"
    if frame_id in {0x12A, 0x135, 0x137, 0x139, 0x140}:
        return "body_display_state"
    if frame_id == 0x641:
        return "diagnostic_response"
    if frame_id == 0x241:
        return "diagnostic_request"
    return "other_can_object"


def dispatch_matches_for_entry(
    entry: TableEntry, dispatch_entries: list[DispatchEntry]
) -> list[DispatchEntry]:
    object_key = entry.raw >> 16
    return [item for item in dispatch_entries if item.object_key == object_key]


def first_or_blank(values: Iterable[str]) -> str:
    for value in values:
        return value
    return ""


def find_occurrences(data: bytes, entries: list[TableEntry]) -> list[Occurrence]:
    entry_by_offset = table_lookup(entries)
    ranges = table_range_lookup()
    occurrences: list[Occurrence] = []

    for frame_id in TARGET_IDS:
        raw = frame_id << 18
        pattern = raw.to_bytes(4, "little")
        start = 0
        while True:
            offset = data.find(pattern, start)
            if offset == -1:
                break
            entry = entry_by_offset.get(offset)
            table_group = ""
            table_index = ""
            context = "non_table"
            if entry:
                context = "table_entry"
                table_group = entry.group
                table_index = str(entry.index)
            else:
                for range_start, range_end, group_name in ranges:
                    if range_start <= offset < range_end:
                        context = "inside_structured_table"
                        table_group = group_name
                        break

            occurrences.append(
                Occurrence(
                    frame_id=frame_id,
                    raw=raw,
                    offset=offset,
                    context=context,
                    table_group=table_group,
                    table_index=table_index,
                    mapped_address=MAPPED_FLASH_BASE + offset,
                )
            )
            start = offset + 1

    return sorted(occurrences, key=lambda item: (item.offset, item.frame_id))


def hexdump(data: bytes, base_offset: int, width: int = 16) -> str:
    lines = []
    for row in range(0, len(data), width):
        chunk = data[row : row + width]
        hex_part = " ".join(f"{byte:02X}" for byte in chunk)
        ascii_part = "".join(chr(byte) if 32 <= byte < 127 else "." for byte in chunk)
        lines.append(f"{base_offset + row:06X}  {hex_part:<47}  {ascii_part}")
    return "\n".join(lines)


def words16le(data: bytes, base_offset: int) -> str:
    lines = []
    width = 16
    for row in range(0, len(data), width):
        chunk = data[row : row + width]
        words = [
            int.from_bytes(chunk[index : index + 2], "little")
            for index in range(0, len(chunk) - 1, 2)
        ]
        rendered = " ".join(f"{word:04X}" for word in words)
        lines.append(f"{base_offset + row:06X}  {rendered}")
    return "\n".join(lines)


def find_nearby_constants(
    data: bytes, start: int, end: int, decoded_raws: dict[int, set[str]]
) -> list[str]:
    hits: list[str] = []
    window = data[start:end]
    for rel in range(0, max(0, len(window) - 3)):
        raw = int.from_bytes(window[rel : rel + 4], "little")
        if raw in decoded_raws:
            labels = ",".join(sorted(decoded_raws[raw]))
            frame_id = (raw >> 18) & 0x7FF
            flags = raw & 0x3FFFF
            hits.append(
                f"{hex_addr(start + rel)} {hex_id(frame_id)} raw={hex_raw(raw)} "
                f"flags=0x{flags:05X} source={labels}"
            )
    return sorted(set(hits))


def code_window_markdown(
    data: bytes, entries: list[TableEntry], occurrences: list[Occurrence]
) -> str:
    decoded_raws: dict[int, set[str]] = defaultdict(set)
    for entry in entries:
        decoded_raws[entry.raw].add(f"{entry.group}[{entry.index}]")
    for frame_id in TARGET_IDS:
        decoded_raws[frame_id << 18].add("priority_target")

    non_table = [item for item in occurrences if item.context == "non_table"]
    lines = [
        "# BCM Candidate Code Windows",
        "",
        "Generated by `tools/bcm_gateway_analysis.py`.",
        "",
        "These windows are centered on exact little-endian `CAN_ID << 18` constants",
        "that occur outside the known structured table ranges. Mapped addresses assume",
        f"`0x{MAPPED_FLASH_BASE:08X} + file_offset` and are only a working import base.",
        "",
    ]

    for item in non_table:
        start = max(0, item.offset - 0x40)
        end = min(len(data), item.offset + 0x44)
        chunk = data[start:end]
        nearby = find_nearby_constants(data, start, end, decoded_raws)
        lines.extend(
            [
                f"## {hex_id(item.frame_id)} at {hex_addr(item.offset)}",
                "",
                f"- Raw constant: `{hex_raw(item.raw)}`",
                f"- Mapped address guess: `0x{item.mapped_address:08X}`",
                f"- Window: `{hex_addr(start)}..{hex_addr(end)}`",
                f"- Nearby decoded CAN constants: `{len(nearby)}`",
                "",
            ]
        )
        if nearby:
            for hit in nearby:
                lines.append(f"- `{hit}`")
            lines.append("")
        lines.extend(
            [
                "```text",
                hexdump(chunk, start),
                "```",
                "",
                "16-bit little-endian words:",
                "",
                "```text",
                words16le(chunk, start),
                "```",
                "",
            ]
        )

    if not non_table:
        lines.append(
            "No priority ID constants were found outside known structured tables."
        )

    for item in non_table:
        marker = f"{hex_id(item.frame_id)} at {hex_addr(item.offset)}"
        assert marker in "\n".join(lines), f"missing code window for {marker}"

    return "\n".join(lines).rstrip() + "\n"


def write_table_csv(
    path: Path, entries: list[TableEntry], log_info: dict[int, dict[str, object]]
) -> None:
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "group",
                "inference",
                "index",
                "offset",
                "raw",
                "frame_id",
                "flags",
                "clean_standard_encoding",
                "log_presence",
                "known_classification",
                "observed_count",
                "observed_dlcs",
                "observed_files",
            ],
        )
        writer.writeheader()
        for entry in entries:
            info = log_info.get(entry.frame_id)
            writer.writerow(
                {
                    "group": entry.group,
                    "inference": entry.inference,
                    "index": entry.index,
                    "offset": hex_addr(entry.offset),
                    "raw": hex_raw(entry.raw),
                    "frame_id": hex_id(entry.frame_id),
                    "flags": f"0x{entry.flags:05X}",
                    "clean_standard_encoding": "yes" if entry.flags == 0 else "no",
                    "log_presence": log_presence(entry.frame_id, log_info),
                    "known_classification": classify_known_sets(entry.frame_id),
                    "observed_count": info["count"] if info else 0,
                    "observed_dlcs": ";".join(
                        f"{dlc}:{count}"
                        for dlc, count in sorted(info["dlcs"].items())
                    )
                    if info
                    else "",
                    "observed_files": ";".join(sorted(info["files"].keys()))
                    if info
                    else "",
                }
            )


def write_occurrences_csv(path: Path, occurrences: list[Occurrence]) -> None:
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "frame_id",
                "raw",
                "offset",
                "mapped_address_guess",
                "context",
                "table_group",
                "table_index",
            ],
        )
        writer.writeheader()
        for item in occurrences:
            writer.writerow(
                {
                    "frame_id": hex_id(item.frame_id),
                    "raw": hex_raw(item.raw),
                    "offset": hex_addr(item.offset),
                    "mapped_address_guess": f"0x{item.mapped_address:08X}",
                    "context": item.context,
                    "table_group": item.table_group,
                    "table_index": item.table_index,
                }
            )


def write_dispatch_csv(
    path: Path,
    entries: list[DispatchEntry],
    table_entries: list[TableEntry],
    filter_entries: list[FilterMaskEntry],
) -> None:
    table_by_key: dict[int, list[TableEntry]] = defaultdict(list)
    for entry in table_entries:
        table_by_key[entry.raw >> 16].append(entry)

    filter_by_key: dict[int, list[FilterMaskEntry]] = defaultdict(list)
    for entry in filter_entries:
        filter_by_key[entry.raw >> 16].append(entry)

    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "table",
                "index",
                "offset",
                "object_key",
                "handler",
                "entry_class",
                "matching_primary_can_entries",
                "matching_filter_entries",
                "inference",
            ],
        )
        writer.writeheader()
        for entry in entries:
            primary_matches = [
                f"{item.group}[{item.index}] {hex_id(item.frame_id)} "
                f"flags=0x{item.flags:05X}"
                for item in table_by_key.get(entry.object_key, [])
            ]
            filter_matches = [
                f"{item.table}[{item.index}] {hex_id(item.frame_id)} "
                f"flags=0x{item.flags:05X} mask={hex_raw(item.mask_class)}"
                for item in filter_by_key.get(entry.object_key, [])
            ]
            writer.writerow(
                {
                    "table": entry.table,
                    "index": entry.index,
                    "offset": hex_addr(entry.offset),
                    "object_key": f"0x{entry.object_key:04X}",
                    "handler": hex_addr(entry.handler),
                    "entry_class": str(entry.entry_class),
                    "matching_primary_can_entries": "; ".join(primary_matches),
                    "matching_filter_entries": "; ".join(filter_matches),
                    "inference": entry.inference,
                }
            )


def write_filter_mask_csv(path: Path, entries: list[FilterMaskEntry]) -> None:
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "table",
                "index",
                "offset",
                "raw",
                "object_key",
                "frame_id",
                "flags",
                "mask_class",
                "inference",
            ],
        )
        writer.writeheader()
        for entry in entries:
            writer.writerow(
                {
                    "table": entry.table,
                    "index": entry.index,
                    "offset": hex_addr(entry.offset),
                    "raw": hex_raw(entry.raw),
                    "object_key": f"0x{entry.raw >> 16:04X}",
                    "frame_id": hex_id(entry.frame_id),
                    "flags": f"0x{entry.flags:05X}",
                    "mask_class": hex_raw(entry.mask_class),
                    "inference": entry.inference,
                }
            )


def windows_path_to_wsl(path: Path) -> str:
    resolved = path.resolve()
    drive = resolved.drive.rstrip(":").lower()
    if drive:
        rest = resolved.as_posix().split(":", 1)[1].lstrip("/")
        return f"/mnt/{drive}/{rest}"
    return resolved.as_posix()


def run_wsl_r2_command(flash_path: Path, command: str, timeout: int = 45) -> str:
    repo_wsl = windows_path_to_wsl(REPO_ROOT)
    flash_wsl = windows_path_to_wsl(flash_path)
    shell = (
        f"cd '{repo_wsl}' && "
        "r2 -q -a v850 -b 32 -e cfg.bigendian=false -e scr.color=false "
        "-e asm.lines=false "
        f"-c \"{command}\" -c q '{flash_wsl}'"
    )
    try:
        result = subprocess.run(
            ["wsl", "sh", "-lc", shell],
            check=False,
            capture_output=True,
            text=True,
            timeout=timeout,
        )
    except (FileNotFoundError, subprocess.TimeoutExpired):
        return ""
    if result.returncode != 0:
        return ""
    return result.stdout.strip()


def disassemble_snippet(flash_path: Path, offset: int, count: int = 14) -> str:
    return run_wsl_r2_command(flash_path, f"pd {count} @ 0x{offset:X}", timeout=30)


def scan_helper_callsites(flash_path: Path) -> list[HelperCallsite]:
    callsites: list[HelperCallsite] = []
    for start, length, label in R2_FOCUSED_RANGES:
        text = run_wsl_r2_command(
            flash_path, f"pD 0x{length:X} @ 0x{start:X}", timeout=90
        )
        if not text:
            continue
        for line in text.splitlines():
            for helper, helper_name in HELPER_TARGETS.items():
                needle = f"jarl 0x{helper:x}"
                if needle not in line.lower():
                    continue
                match = R2_LINE_RE.search(line)
                if not match:
                    continue
                callsites.append(
                    HelperCallsite(
                        helper=helper,
                        helper_name=helper_name,
                        callsite=int(match.group("offset"), 16),
                        instruction_bytes=match.group("bytes"),
                        instruction=match.group("op").strip(),
                        scan_range=label,
                    )
                )
    return sorted(callsites, key=lambda item: (item.helper, item.callsite))


def write_helper_callsites_csv(path: Path, callsites: list[HelperCallsite]) -> None:
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "helper",
                "helper_name",
                "callsite",
                "instruction_bytes",
                "instruction",
                "scan_range",
                "confidence",
            ],
        )
        writer.writeheader()
        for item in callsites:
            writer.writerow(
                {
                    "helper": hex_addr(item.helper),
                    "helper_name": item.helper_name,
                    "callsite": hex_addr(item.callsite),
                    "instruction_bytes": item.instruction_bytes,
                    "instruction": item.instruction,
                    "scan_range": item.scan_range,
                    "confidence": "confirmed-by-code-path",
                }
            )


def write_group_a_tx_trace_csv(
    path: Path,
    entries: list[TableEntry],
    dispatch_entries: list[DispatchEntry],
    log_info: dict[int, dict[str, object]],
) -> None:
    group_a = [entry for entry in entries if entry.group == "Group A"]
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "frame_id",
                "table_index",
                "table_offset",
                "raw",
                "object_key",
                "message_role",
                "focus_low_speed_ipc",
                "direct_dispatch_handlers",
                "dispatch_entry_classes",
                "observed_presence",
                "observed_count",
                "observed_dlcs",
                "median_period_ms",
                "approx_hz",
                "state_source_status",
                "confidence",
            ],
        )
        writer.writeheader()
        for entry in group_a:
            matches = dispatch_matches_for_entry(entry, dispatch_entries)
            info = log_info.get(entry.frame_id)
            median_ms, hz = period_hint(entry.frame_id, log_info)
            if matches:
                state_status = "direct dispatch wrapper(s) found; inspect handler getter chain"
                confidence = "confirmed-by-code-path"
            elif entry.frame_id in LOW_SPEED_TX_FOCUS_IDS:
                state_status = "standard-ID table entry confirmed; dispatch/scheduler link unresolved"
                confidence = "confirmed-by-table"
            else:
                state_status = "not prioritized for low-speed IPC trace"
                confidence = "confirmed-by-table"
            writer.writerow(
                {
                    "frame_id": hex_id(entry.frame_id),
                    "table_index": entry.index,
                    "table_offset": hex_addr(entry.offset),
                    "raw": hex_raw(entry.raw),
                    "object_key": f"0x{entry.raw >> 16:04X}",
                    "message_role": classify_message_role(entry.frame_id),
                    "focus_low_speed_ipc": "yes"
                    if entry.frame_id in LOW_SPEED_TX_FOCUS_IDS
                    else "no",
                    "direct_dispatch_handlers": "; ".join(
                        f"{hex_addr(item.handler)}@{hex_addr(item.offset)}"
                        for item in matches
                    ),
                    "dispatch_entry_classes": "; ".join(
                        str(item.entry_class) for item in matches
                    ),
                    "observed_presence": log_presence(entry.frame_id, log_info),
                    "observed_count": info["count"] if info else 0,
                    "observed_dlcs": ";".join(
                        f"{dlc}:{count}"
                        for dlc, count in sorted(info["dlcs"].items())
                    )
                    if info
                    else "",
                    "median_period_ms": median_ms,
                    "approx_hz": hz,
                    "state_source_status": state_status,
                    "confidence": confidence,
                }
            )


def clean_standard_entry(entry: TableEntry) -> bool:
    return entry.flags == 0 and entry.raw == entry.frame_id << 18


def group_a_wire_id(entry: TableEntry, source: int) -> int:
    if clean_standard_entry(entry):
        return entry.frame_id
    return (entry.raw & 0x1FFFFFFF) | source


def group_a_pointer(data: bytes, table_offset: int, index: int) -> int | None:
    offset = table_offset + index * 4
    if offset + 4 > len(data):
        return None
    return int.from_bytes(data[offset : offset + 4], "little")


def group_a_byte(data: bytes, table_offset: int, index: int) -> int | None:
    offset = table_offset + index
    if offset >= len(data):
        return None
    return data[offset]


def read_live_projection(path: Path) -> dict[int, dict[str, str]]:
    if not path.exists():
        return {}
    rows: dict[int, dict[str, str]] = {}
    with path.open(newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            value = row.get("live_can_id", "")
            if not value:
                continue
            try:
                rows[int(value, 16)] = row
            except ValueError:
                continue
    return rows


def classify_group_a_object(entry: TableEntry, dlc: int | None, exact_live: bool) -> str:
    if entry.raw == 0x93FFE000 and dlc == 0:
        return "best_dlc0_heartbeat_or_wake_candidate"
    if entry.frame_id == 0x100 and dlc == 0:
        return "standard_dlc0_network_wake_candidate"
    if entry.frame_id == 0x621:
        return "standard_network_management_candidate"
    if entry.frame_id in {0x7F1, 0x641}:
        return "diagnostic_or_diagnostic_response"
    if exact_live:
        return "ipc_origin_live_object"
    if entry.frame_id in {0x409, 0x410, 0x42B, 0x4FF}:
        return "low_speed_ipc_projected_family"
    if entry.frame_id in LOW_SPEED_TX_FOCUS_IDS:
        return "legacy_standard_body_tx_candidate"
    return classify_message_role(entry.frame_id)


def write_group_a_object_metadata_csv(
    path: Path,
    data: bytes,
    entries: list[TableEntry],
    dispatch_entries: list[DispatchEntry],
    live_projection_path: Path,
) -> None:
    live_rows = read_live_projection(live_projection_path)
    group_a = [entry for entry in entries if entry.group == "Group A"]
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "index",
                "table_offset",
                "raw",
                "frame_id",
                "flags_or_variant",
                "clean_standard_encoding",
                "dlc",
                "payload_ram_ptr",
                "aux_func_a",
                "aux_func_b",
                "scheduler_bucket",
                "scheduler_mask",
                "object_key",
                "direct_dispatch_handlers",
                "wire_id_ipc_source_60",
                "wire_id_bcm_source_40",
                "exact_live_source_60",
                "live_count",
                "live_period_ms",
                "live_payload",
                "role",
                "wake_power_confidence",
            ],
        )
        writer.writeheader()
        for entry in group_a:
            dlc = group_a_byte(data, GROUP_A_DLC_OFFSET, entry.index)
            payload_ptr = group_a_pointer(data, GROUP_A_PAYLOAD_PTR_OFFSET, entry.index)
            aux_a = group_a_pointer(data, GROUP_A_AUX_FUNC_A_OFFSET, entry.index)
            aux_b = group_a_pointer(data, GROUP_A_AUX_FUNC_B_OFFSET, entry.index)
            bucket = group_a_byte(data, GROUP_A_SCHED_BUCKET_OFFSET, entry.index)
            mask = (
                group_a_byte(data, GROUP_A_SCHED_MASK_OFFSET, entry.index)
                if GROUP_A_SCHED_MASK_OFFSET + entry.index < TABLES[1]["offset"]
                else None
            )
            wire_60 = group_a_wire_id(entry, 0x60)
            wire_40 = group_a_wire_id(entry, 0x40)
            live = live_rows.get(wire_60, {})
            exact_live = bool(live)
            role = classify_group_a_object(entry, dlc, exact_live)
            if role == "best_dlc0_heartbeat_or_wake_candidate":
                confidence = "strong: table+DLC0+null-payload+exact-live-IPC-origin"
            elif role == "standard_dlc0_network_wake_candidate":
                confidence = "medium: table+DLC0+live-standard-ID; bus direction unresolved"
            elif role == "standard_network_management_candidate":
                confidence = "medium: table+DLC8; payload/sender direction unresolved"
            elif exact_live:
                confidence = "medium: exact live IPC-origin object; direction to IPC unresolved"
            else:
                confidence = "low-for-wake"
            matches = dispatch_matches_for_entry(entry, dispatch_entries)
            writer.writerow(
                {
                    "index": entry.index,
                    "table_offset": hex_addr(entry.offset),
                    "raw": hex_raw(entry.raw),
                    "frame_id": hex_id(entry.frame_id),
                    "flags_or_variant": f"0x{entry.flags:05X}",
                    "clean_standard_encoding": "yes" if clean_standard_entry(entry) else "no",
                    "dlc": "" if dlc is None else dlc,
                    "payload_ram_ptr": hex_addr(payload_ptr) if payload_ptr else "",
                    "aux_func_a": hex_addr(aux_a) if aux_a else "",
                    "aux_func_b": hex_addr(aux_b) if aux_b else "",
                    "scheduler_bucket": "" if bucket is None else bucket,
                    "scheduler_mask": "" if mask is None else f"0x{mask:02X}",
                    "object_key": f"0x{entry.raw >> 16:04X}",
                    "direct_dispatch_handlers": "; ".join(
                        f"{hex_addr(item.handler)}@{hex_addr(item.offset)}"
                        for item in matches
                    ),
                    "wire_id_ipc_source_60": hex(wire_60),
                    "wire_id_bcm_source_40": hex(wire_40),
                    "exact_live_source_60": "yes" if exact_live else "no",
                    "live_count": live.get("count", ""),
                    "live_period_ms": live.get("median_period_ms", ""),
                    "live_payload": live.get("first_payload", ""),
                    "role": role,
                    "wake_power_confidence": confidence,
                }
            )


def wake_power_candidates_markdown(
    data: bytes,
    entries: list[TableEntry],
    dispatch_entries: list[DispatchEntry],
    live_projection_path: Path,
) -> str:
    live_rows = read_live_projection(live_projection_path)
    group_a = [entry for entry in entries if entry.group == "Group A"]
    by_index = {entry.index: entry for entry in group_a}

    def row(index: int) -> dict[str, object]:
        entry = by_index[index]
        wire_60 = group_a_wire_id(entry, 0x60)
        live = live_rows.get(wire_60, {})
        return {
            "entry": entry,
            "dlc": group_a_byte(data, GROUP_A_DLC_OFFSET, index),
            "ptr": group_a_pointer(data, GROUP_A_PAYLOAD_PTR_OFFSET, index),
            "aux_a": group_a_pointer(data, GROUP_A_AUX_FUNC_A_OFFSET, index),
            "aux_b": group_a_pointer(data, GROUP_A_AUX_FUNC_B_OFFSET, index),
            "bucket": group_a_byte(data, GROUP_A_SCHED_BUCKET_OFFSET, index),
            "mask": group_a_byte(data, GROUP_A_SCHED_MASK_OFFSET, index)
            if GROUP_A_SCHED_MASK_OFFSET + index < TABLES[1]["offset"]
            else None,
            "wire_60": wire_60,
            "wire_40": group_a_wire_id(entry, 0x40),
            "live": live,
            "dispatch": dispatch_matches_for_entry(entry, dispatch_entries),
        }

    focus_indices = [0, 1, 2, 3, 4, 5, 52, 53, 76, 77, 98, 129]
    lines = [
        "# BCM Wake / Power Candidate Notes",
        "",
        "Generated by `tools/bcm_gateway_analysis.py`.",
        "",
        "## Main Finding",
        "",
        "`0x13FFE0xx` is now the strongest firmware-backed wake/keepalive candidate.",
        "It is Group A index 5, raw `0x93FFE000`, DLC `0`, with no payload RAM",
        "pointer and no direct payload-builder dispatch wrapper. The direct IPC-only",
        "capture saw the source-`0x60` form `0x13FFE060` at about 1.2 s. The likely",
        "BCM-origin trial form is therefore `0x13FFE040` as an extended DLC0 frame.",
        "",
        "This does not prove the physical transmitter yet; a normal CANable may still",
        "be unable to drive true single-wire GMLAN even when it can passively sniff it.",
        "",
        "## Candidate Objects",
        "",
        "| Index | Raw | Frame | DLC | Payload RAM | Aux A | Aux B | Bucket | Mask | IPC/source 0x60 | BCM/source 0x40 | Live | Interpretation |",
        "|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|",
    ]
    for index in focus_indices:
        item = row(index)
        entry = item["entry"]
        live = item["live"]
        exact_live = "yes" if live else "no"
        role = classify_group_a_object(entry, item["dlc"], bool(live))
        lines.append(
            f"| {index} | `{hex_raw(entry.raw)}` | {hex_id(entry.frame_id)} | "
            f"{item['dlc']} | {hex_addr(item['ptr']) if item['ptr'] else ''} | "
            f"{hex_addr(item['aux_a']) if item['aux_a'] else ''} | "
            f"{hex_addr(item['aux_b']) if item['aux_b'] else ''} | "
            f"{item['bucket']} | "
            f"{f'0x{item['mask']:02X}' if item['mask'] is not None else ''} | "
            f"{hex(item['wire_60'])} | {hex(item['wire_40'])} | {exact_live} | {role} |"
        )

    lines.extend(
        [
            "",
            "## `0x621` Aux Function Notes",
            "",
            "`0x100` is also present as a standard DLC0 Group A object at indices",
            "98 and 129, both with aux entries `0x101EEE` and `0x101F7A`. It was",
            "seen once in the direct IPC-only capture. Treat `0x100#` as a real",
            "firmware-backed network wake candidate, but less IPC-specific than",
            "`0x13FFE040#`. The aux functions do not build payload bytes; they set",
            "per-object RAM flags/counters in the `0x03FFA61C + 4*object` state",
            "block. `0x101EEE` sets init/pending bits and counter bytes, while",
            "`0x101F7A` sets the high pending/transmit flag at object-state byte 2.",
            "",
            "Group A index 3 (`0x621`, DLC8) has aux entries at `0x101D64` and",
            "`0x101C7C`. The `0x101C7C` function looks like a variable payload",
            "builder: it writes an initial status byte to the caller buffer, copies",
            "bytes from RAM-backed arrays, and updates per-object RAM flags around",
            "`0x03FFA61C`. The `0x101D64` function walks indexed byte/mask tables",
            "and calls `0x103ADE` when state bits change. That is stronger evidence",
            "that `0x621` is a real network-management/state object, but not enough",
            "yet to claim the exact wake payload.",
            "",
            "## Generic Sender Xrefs",
            "",
            "An r2 `-A` xref pass found the generic Group A sender at `0x102836`.",
            "This function reads the DLC byte from `0x01E080` (`ld.bu -8064[...]`)",
            "and the payload RAM pointer from `0x01E104` (`ld.w -7932[...]`).",
            "Its caller around `0x103054` passes object index/state into this sender.",
            "For Group A index 5 the DLC table returns `0` and the payload pointer",
            "table returns null, so the generic sender can still emit a valid empty",
            "CAN object. A second setup/refresh path at `0x104D7C` also references",
            "the same DLC and payload-pointer tables.",
            "",
            "## Transmit Implications",
            "",
            "- First wake/keepalive trial should remain extended `0x13FFE040#` at",
            "  roughly 250 ms to 1200 ms. The firmware evidence says it is a DLC0",
            "  scheduled object, not a payload builder.",
            "- Standard `0x100#` is also a firmware-confirmed DLC0 Group A object and",
            "  remains the best generic network-wake prelude.",
            "- Standard `0x621` is also a table-confirmed Group A DLC8 object. It is a",
            "  plausible VNMF/network-management candidate, but the firmware has not yet",
            "  exposed the exact payload in this pass.",
            "- `0x641`/`0x7F1` are more likely diagnostic response objects than cluster",
            "  power wake frames.",
            "- `0x10424040`, `0x10600040`, and `0x10244040` are useful state-object probes,",
            "  but they look like payload/status messages rather than the primary wake",
            "  frame.",
            "",
            "## Next Static Targets",
            "",
            "1. Find code that iterates Group A index/bucket/mask metadata around",
            "   `0x01E730` and `0x01E7B0`.",
            "2. Trace Group A index 5 through the generic scheduler path, because it has",
            "   no direct payload handler.",
            "3. Trace standard `0x621` payload sources from its DLC8 object metadata and",
            "   compare against observed IPC-origin `0x62C` network-management traffic.",
        ]
    )
    return "\n".join(lines).rstrip() + "\n"


def write_group_b_rx_trace_csv(
    path: Path,
    entries: list[TableEntry],
    dispatch_entries: list[DispatchEntry],
    occurrences: list[Occurrence],
    log_info: dict[int, dict[str, object]],
) -> None:
    occurrence_by_id: dict[int, list[Occurrence]] = defaultdict(list)
    for occurrence in occurrences:
        occurrence_by_id[occurrence.frame_id].append(occurrence)

    group_b = [
        entry
        for entry in entries
        if entry.group == "Group B" and entry.frame_id in RX_HANDLER_TARGET_IDS
    ]
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "frame_id",
                "table_index",
                "table_offset",
                "raw",
                "object_key",
                "message_role",
                "direct_dispatch_handlers",
                "occurrence_contexts",
                "observed_presence",
                "observed_count",
                "observed_dlcs",
                "median_period_ms",
                "approx_hz",
                "rx_consumer_status",
                "confidence",
            ],
        )
        writer.writeheader()
        for entry in group_b:
            matches = dispatch_matches_for_entry(entry, dispatch_entries)
            info = log_info.get(entry.frame_id)
            median_ms, hz = period_hint(entry.frame_id, log_info)
            contexts = [
                f"{item.context}:{item.table_group}@{hex_addr(item.offset)}"
                for item in occurrence_by_id.get(entry.frame_id, [])
            ]
            if matches:
                rx_status = "direct dispatch wrapper(s) found"
                confidence = "confirmed-by-code-path"
            else:
                rx_status = (
                    "RX filter table entry confirmed; consumer likely reached through "
                    "interrupt/buffer path, not the 0x021A40 TX wrapper table"
                )
                confidence = "confirmed-by-table"
            writer.writerow(
                {
                    "frame_id": hex_id(entry.frame_id),
                    "table_index": entry.index,
                    "table_offset": hex_addr(entry.offset),
                    "raw": hex_raw(entry.raw),
                    "object_key": f"0x{entry.raw >> 16:04X}",
                    "message_role": classify_message_role(entry.frame_id),
                    "direct_dispatch_handlers": "; ".join(
                        f"{hex_addr(item.handler)}@{hex_addr(item.offset)}"
                        for item in matches
                    ),
                    "occurrence_contexts": "; ".join(contexts),
                    "observed_presence": log_presence(entry.frame_id, log_info),
                    "observed_count": info["count"] if info else 0,
                    "observed_dlcs": ";".join(
                        f"{dlc}:{count}"
                        for dlc, count in sorted(info["dlcs"].items())
                    )
                    if info
                    else "",
                    "median_period_ms": median_ms,
                    "approx_hz": hz,
                    "rx_consumer_status": rx_status,
                    "confidence": confidence,
                }
            )


def low_speed_map_markdown(
    flash_path: Path,
    entries: list[TableEntry],
    dispatch_entries: list[DispatchEntry],
    callsites: list[HelperCallsite],
    log_info: dict[int, dict[str, object]],
) -> str:
    lines = [
        "# BCM Low-Speed Static Message Map",
        "",
        "Generated by `tools/bcm_gateway_analysis.py`.",
        "",
        "This is a static-only map. It identifies firmware tables and code paths",
        "that likely feed BCM/body/IPC-side CAN traffic, but it does not prove the",
        "final low-speed physical bus without a direct BCM-to-IPC capture.",
        "",
        "## Current TX Model",
        "",
        "```text",
        "Group A standard-ID table entries",
        "  -> scheduler/object metadata, not fully linked yet for all standard IDs",
        "  -> some object-key variants map into the 0x021A40 dispatch table",
        "  -> dispatch wrappers read gp/RAM state and write payload bytes",
        "  -> CAN helpers 0x34BD2 and 0x34AAE touch controller/register queues",
        "```",
        "",
        "## Focus IPC/Body TX IDs",
        "",
        "| ID | Group A | DLC(s) | Period ms | Hz | Role | Dispatch | Confidence |",
        "|---:|---|---|---:|---:|---|---|---|",
    ]

    group_a_by_id: dict[int, list[TableEntry]] = defaultdict(list)
    for entry in entries:
        if entry.group == "Group A":
            group_a_by_id[entry.frame_id].append(entry)

    for frame_id in sorted(LOW_SPEED_TX_FOCUS_IDS):
        group_entries = group_a_by_id.get(frame_id, [])
        if group_entries:
            table_ref = "; ".join(
                f"{entry.index}@{hex_addr(entry.offset)}" for entry in group_entries
            )
            dispatch = []
            for entry in group_entries:
                for match in dispatch_matches_for_entry(entry, dispatch_entries):
                    dispatch.append(hex_addr(match.handler))
            confidence = "confirmed-by-code-path" if dispatch else "confirmed-by-table"
        else:
            table_ref = ""
            dispatch = []
            confidence = "unresolved"
        info = log_info.get(frame_id)
        dlcs = (
            "; ".join(f"{dlc}:{count}" for dlc, count in sorted(info["dlcs"].items()))
            if info
            else ""
        )
        median_ms, hz = period_hint(frame_id, log_info)
        lines.append(
            f"| {hex_id(frame_id)} | {table_ref} | {dlcs} | {median_ms} | {hz} | "
            f"{classify_message_role(frame_id)} | {'; '.join(dispatch) or 'unresolved'} | "
            f"{confidence} |"
        )

    lines.extend(
        [
            "",
            "## Priority ECU RX IDs",
            "",
            "| ID | Group B | DLC(s) | Period ms | Hz | RX consumer | Confidence |",
            "|---:|---|---|---:|---:|---|---|",
        ]
    )

    group_b_by_id: dict[int, list[TableEntry]] = defaultdict(list)
    for entry in entries:
        if entry.group == "Group B":
            group_b_by_id[entry.frame_id].append(entry)

    for frame_id in sorted(RX_HANDLER_TARGET_IDS):
        group_entries = group_b_by_id.get(frame_id, [])
        table_ref = "; ".join(
            f"{entry.index}@{hex_addr(entry.offset)}" for entry in group_entries
        )
        info = log_info.get(frame_id)
        dlcs = (
            "; ".join(f"{dlc}:{count}" for dlc, count in sorted(info["dlcs"].items()))
            if info
            else ""
        )
        median_ms, hz = period_hint(frame_id, log_info)
        lines.append(
            f"| {hex_id(frame_id)} | {table_ref} | {dlcs} | {median_ms} | {hz} | "
            "interrupt/buffer path unresolved | confirmed-by-table |"
        )

    lines.extend(
        [
            "",
            "## CAN Helper Callsites",
            "",
            "| Helper | Name | Callsites in focused scan |",
            "|---|---|---|",
        ]
    )
    by_helper: dict[int, list[HelperCallsite]] = defaultdict(list)
    for item in callsites:
        by_helper[item.helper].append(item)
    for helper, helper_name in HELPER_TARGETS.items():
        sites = "; ".join(hex_addr(item.callsite) for item in by_helper.get(helper, []))
        lines.append(f"| {hex_addr(helper)} | {helper_name} | {sites or 'not found'} |")

    lines.extend(
        [
            "",
            "## Representative Handler Snippets",
            "",
            "The focus standard IDs currently do not directly map into the dispatch table by",
            "`raw_value >> 16`. The snippets below document confirmed dispatch-wrapper",
            "behavior for nearby Group A object variants and the CAN helper path.",
            "",
        ]
    )
    for title, offset in [
        ("Dispatch wrapper example 0x00131344", 0x131344),
        ("Payload byte writer 0x0012F124", 0x12F124),
        ("CAN command queue helper 0x00034AAE", 0x34AAE),
        ("CAN register helper 0x00034BD2", 0x34BD2),
    ]:
        snippet = disassemble_snippet(flash_path, offset, 18)
        lines.extend([f"### {title}", "", "```text", snippet or "r2 output unavailable", "```", ""])

    lines.extend(
        [
            "## Remaining Static Gaps",
            "",
            "- The standard Group A IPC/body IDs are table-confirmed and log-correlated,",
            "  but most are not directly linked to `0x021A40` dispatch wrappers.",
            "- Group B ECU RX IDs are table-confirmed, but their consumers still need to",
            "  be traced through the RX interrupt/buffer path.",
            "- Scheduler/rate metadata is inferred from logs at this stage, not decoded",
            "  from the BCM scheduler tables.",
        ]
    )
    return "\n".join(lines).rstrip() + "\n"


def write_xref_csv(
    path: Path,
    entries: list[TableEntry],
    occurrences: list[Occurrence],
    log_info: dict[int, dict[str, object]],
) -> None:
    entries_by_id: dict[int, list[TableEntry]] = defaultdict(list)
    for entry in entries:
        entries_by_id[entry.frame_id].append(entry)

    non_table_by_id: dict[int, list[Occurrence]] = defaultdict(list)
    for item in occurrences:
        if item.context == "non_table":
            non_table_by_id[item.frame_id].append(item)

    candidate_ids = sorted(set(TARGET_IDS) | RX_HANDLER_TARGET_IDS | IPC_BODY_TARGET_IDS)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "frame_id",
                "role_hint",
                "table_entries",
                "non_table_offsets",
                "mapped_address_guesses",
                "log_presence",
                "known_classification",
                "manual_reversing_note",
            ],
        )
        writer.writeheader()
        for frame_id in candidate_ids:
            role = []
            if frame_id in RX_HANDLER_TARGET_IDS:
                role.append("ECU-side RX handler target")
            if frame_id in IPC_BODY_TARGET_IDS:
                role.append("IPC/body TX candidate")
            if frame_id in {0x241, 0x641}:
                role.append("diagnostic anchor")
            table_entries = [
                f"{entry.group}[{entry.index}]@{hex_addr(entry.offset)} flags=0x{entry.flags:05X}"
                for entry in entries_by_id.get(frame_id, [])
            ]
            offsets = [hex_addr(item.offset) for item in non_table_by_id.get(frame_id, [])]
            mapped = [
                f"0x{item.mapped_address:08X}"
                for item in non_table_by_id.get(frame_id, [])
            ]
            note = ""
            if offsets:
                note = "Open corresponding window in bcm_candidate_code_windows.md."
            elif table_entries:
                note = "Only table entries found so far; follow table consumers."
            else:
                note = "No exact CAN_ID<<18 constant found; search payload/scheduler code."
            writer.writerow(
                {
                    "frame_id": hex_id(frame_id),
                    "role_hint": "; ".join(role),
                    "table_entries": "; ".join(table_entries),
                    "non_table_offsets": "; ".join(offsets),
                    "mapped_address_guesses": "; ".join(mapped),
                    "log_presence": log_presence(frame_id, log_info),
                    "known_classification": classify_known_sets(frame_id),
                    "manual_reversing_note": note,
                }
            )


def architecture_probe(data: bytes) -> list[str]:
    first_words_be = [
        int.from_bytes(data[offset : offset + 4], "big") for offset in range(0, 0x80, 4)
    ]
    mapped_be = [
        value for value in first_words_be if 0x80000000 <= value <= 0x80180000
    ]
    nonzero_even_halfwords = 0
    for offset in range(0, min(len(data), 0x10000) - 1, 2):
        if int.from_bytes(data[offset : offset + 2], "little") not in (0, 0xFFFF):
            nonzero_even_halfwords += 1

    notes = [
        "Architecture is not proven by this script alone; WSL/radare2 confirms V850-family decoding at the known vector stubs.",
        f"First vector/header words contain {len(mapped_be)} big-endian values in the 0x80000000..0x80180000 range.",
        "CAN table values and exact CAN_ID<<18 constants are little-endian 32-bit values.",
        f"The first 64 KiB contains {nonzero_even_halfwords} non-empty 16-bit little-endian words, consistent with dense embedded code/data.",
        "Working part-level hypothesis remains Renesas/NEC V850E2/FK4, likely near uPD70F3558M1 until chip markings confirm it.",
    ]
    return notes


def assert_expected_anchors(entries: list[TableEntry]) -> None:
    by_group: dict[str, set[int]] = defaultdict(set)
    for entry in entries:
        by_group[entry.group].add(entry.frame_id)

    assert 0x641 in by_group["Group A"], "Group A must contain diagnostic response 0x641"
    for required in (0x241, 0x0C9, 0x3E9, 0x4C1, 0x4D1):
        assert required in by_group["Group B"], (
            f"Group B must contain expected RX/filter anchor {hex_id(required)}"
        )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--flash", type=Path, default=DEFAULT_FLASH)
    parser.add_argument("--logs", type=Path, default=DEFAULT_LOG_GLOB)
    parser.add_argument("--out", type=Path, default=DEFAULT_OUTPUT_DIR)
    args = parser.parse_args()

    flash_path = args.flash
    output_dir = args.out
    output_dir.mkdir(parents=True, exist_ok=True)

    data = flash_path.read_bytes()
    entries = decode_tables(data)
    dispatch_entries = decode_dispatch_tables(data)
    filter_entries = decode_filter_mask_tables(data)
    assert_expected_anchors(entries)

    log_paths = list(args.logs.parent.glob(args.logs.name))
    log_info = parse_logs(log_paths)

    known_ecu_seen = KNOWN_ECU_STREAM & set(log_info)
    known_body_seen = KNOWN_NATIVE_BODY & set(log_info)
    assert {0x0C9, 0x3E9, 0x4C1, 0x4D1}.issubset(known_ecu_seen), (
        "known ECU stream frames missing from parsed logs"
    )
    assert {0x0F1, 0x160, 0x1F1, 0x514}.issubset(known_body_seen), (
        "known native body frames missing from parsed logs"
    )

    occurrences = find_occurrences(data, entries)
    helper_callsites = scan_helper_callsites(flash_path)

    write_table_csv(output_dir / "bcm_can_tables_decoded.csv", entries, log_info)
    write_occurrences_csv(output_dir / "bcm_can_id_occurrences.csv", occurrences)
    write_dispatch_csv(
        output_dir / "bcm_dispatch_table_candidates.csv",
        dispatch_entries,
        entries,
        filter_entries,
    )
    write_filter_mask_csv(
        output_dir / "bcm_filter_mask_table_candidates.csv", filter_entries
    )
    write_xref_csv(
        output_dir / "bcm_gateway_xref_candidates.csv", entries, occurrences, log_info
    )
    write_group_a_tx_trace_csv(
        output_dir / "bcm_group_a_tx_trace.csv", entries, dispatch_entries, log_info
    )
    write_group_a_object_metadata_csv(
        output_dir / "bcm_group_a_object_metadata.csv",
        data,
        entries,
        dispatch_entries,
        output_dir / "ipc_lowspeed_bcm_projection.csv",
    )
    write_group_b_rx_trace_csv(
        output_dir / "bcm_group_b_rx_trace.csv",
        entries,
        dispatch_entries,
        occurrences,
        log_info,
    )
    write_helper_callsites_csv(
        output_dir / "bcm_can_helper_callsites.csv", helper_callsites
    )
    (output_dir / "bcm_candidate_code_windows.md").write_text(
        code_window_markdown(data, entries, occurrences), encoding="utf-8"
    )
    (output_dir / "bcm_low_speed_static_message_map.md").write_text(
        low_speed_map_markdown(
            flash_path, entries, dispatch_entries, helper_callsites, log_info
        ),
        encoding="utf-8",
    )
    (output_dir.parent / "BCM_WAKE_POWER_CANDIDATES.md").write_text(
        wake_power_candidates_markdown(
            data,
            entries,
            dispatch_entries,
            output_dir / "ipc_lowspeed_bcm_projection.csv",
        ),
        encoding="utf-8",
    )

    print(f"flash: {flash_path}")
    print(f"decoded table entries: {len(entries)}")
    print(f"decoded dispatch records: {len(dispatch_entries)}")
    print(f"decoded filter/mask records: {len(filter_entries)}")
    print(f"parsed log IDs: {len(log_info)} from {len(log_paths)} files")
    print(f"priority ID occurrences: {len(occurrences)}")
    print(f"focused helper callsites: {len(helper_callsites)}")
    print("architecture probe:")
    for note in architecture_probe(data):
        print(f"- {note}")
    print(f"wrote: {output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
