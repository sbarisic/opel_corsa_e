#!/usr/bin/env python3
"""Capture, compare, and experimentally transmit IPC low-speed CAN traffic.

This tool talks directly to candleLight/gs_usb adapters via the low-level
``gs_usb`` package.  The standard python-can gs_usb wrapper is not used because
it cannot currently derive the 33.333 kbit/s timing for this CANable2 clock.
"""

from __future__ import annotations

import argparse
import csv
import datetime as dt
import os
import re
import statistics
import sys
import time
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LIVE_DIR = REPO_ROOT / "data" / "can_logs" / "live"
DEFAULT_ANALYSIS_DIR = REPO_ROOT / "analysis" / "generated"
DEFAULT_BCM_TABLE = DEFAULT_ANALYSIS_DIR / "bcm_can_tables_decoded.csv"
DEFAULT_NOTE = REPO_ROOT / "analysis" / "IPC_LOW_SPEED_CAN.md"

CAN_TIMING = {
    "fclk": 170_000_000,
    "bitrate": 33_333.333,
    "brp": 255,
    "prop_seg": 1,
    "phase_seg1": 13,
    "phase_seg2": 5,
    "sjw": 4,
}

CAN_ID_RE = re.compile(
    r"^\((?P<ts>[^)]+)\)\s+\S+\s+(?P<id>[0-9A-Fa-f]+)#(?P<data>[0-9A-Fa-f]*)"
)
_LIBUSB_PATH_READY = False

DEFAULT_GAUGE_BASELINE = [
    # ID, payload, period_ms, note
    (0x10244060, "06", 5000.0, "live IPC-origin candidate; BCM projection 0x409"),
    (0x10424060, "079964", 1000.0, "live IPC-origin candidate; BCM projection 0x410"),
    (0x10ACE060, "1E00000000000000", 110.0, "live IPC-origin candidate; BCM projection 0x42B"),
    (0x10AE8060, "030E010104050000", 30.0, "live IPC-origin candidate; BCM projection 0x42B"),
    (0x10B0A060, "001E04", 110.0, "live IPC-origin candidate"),
    (0x10AFC060, "38FFFFFFFFFFFF2B", 5000.0, "live IPC-origin candidate; BCM projection 0x42B"),
    (0x10600060, "01609370015B00", 5000.0, "live IPC-origin candidate; BCM projection 0x418"),
]

STANDARD_BODY_PROBE = [
    (0x160, "0000000000", 100.0, "older static BCM/body candidate"),
    (0x1F1, "0000000000000000", 100.0, "older static BCM/body candidate"),
    (0x0F1, "000000000000", 15.0, "older static BCM/body candidate"),
    (0x12A, "0000000000000000", 100.0, "older static BCM/body candidate"),
    (0x451, "000000000000", 20.0, "older static BCM/body candidate"),
    (0x4E1, "0000000000000000", 1000.0, "older static BCM/body candidate"),
    (0x514, "0000000000000000", 1000.0, "older static BCM/body candidate"),
]

BCM_HEARTBEAT_PROBE = [
    # Captured IPC-origin frames appear to use source address 0x60.  These
    # candidates swap destination/source for frames addressed to 0x40 and add a
    # likely BCM-source broadcast heartbeat.
    (0x13FFE040, "", 250.0, "BCM-source broadcast heartbeat candidate"),
    (0x10246040, "06", 500.0, "response to IPC 0x10244060 with DA/SA swapped"),
    (0x10426040, "079964", 100.0, "response to IPC 0x10424060 with DA/SA swapped"),
    (0x10446040, "00", 1000.0, "response to IPC 0x10444060 with DA/SA swapped"),
    (0x10776040, "00", 750.0, "response to IPC 0x10774060 with DA/SA swapped"),
]

BCM_EXACT_TABLE_PROBE = [
    (0x13FFE040, "", 250.0, "BCM-source broadcast heartbeat candidate"),
    (0x10424040, "079964", 100.0, "BCM-source version of exact table raw 0x90424000"),
    (0x10600040, "01609370015B00", 1000.0, "BCM-source version of exact table raw 0x90600000"),
    (0x10244040, "06", 500.0, "BCM-source variant of projected 0x409 family"),
]

FIRMWARE_WAKE_PROFILE = [
    (0x100, "", 500.0, "firmware-confirmed standard DLC0 network wake object"),
    (0x13FFE040, "", 500.0, "firmware-confirmed extended DLC0 IPC heartbeat candidate"),
    (0x621, "0140000000000000", 1000.0, "firmware-shaped DLC8 network-management initiate candidate"),
    (0x621, "0040000000000000", 500.0, "firmware-shaped DLC8 network-management continue candidate"),
]

BCM_TO_IPC_TEMPLATES = [
    # base_id_without_source, payload, period_ms, note. Destination stays 0x60.
    (0x13FFE000, "", 250.0, "PF=0xFF broadcast/group heartbeat"),
    (0x10246000, "06", 500.0, "PF=0x24 destination=IPC 0x60"),
    (0x10426000, "079964", 100.0, "PF=0x42 destination=IPC 0x60"),
    (0x10446000, "00", 1000.0, "PF=0x44 destination=IPC 0x60"),
    (0x10776000, "00", 750.0, "PF=0x77 destination=IPC 0x60"),
]

GMLAN_VNMF_WAKE_PROFILE = [
    # Public GMLAN notes describe $100 as a wake-up request and $620-$63F as
    # standard-ID VNMFs.  Byte 0: 0x01 initiate, 0x00 continue; following bytes
    # are virtual-network bit masks.  Keep the continue frame below 3 seconds.
    (0x100, "", 1000.0, "GMLAN wake-up request / no data"),
    (0x621, "01FFFFFFFF000000", 1000.0, "VNMF initiate all common VNs from BCM-like source"),
    (0x621, "00FFFFFFFF000000", 500.0, "VNMF continue all common VNs from BCM-like source"),
]

DIAGNOSTIC_WAKE_PROFILE = [
    (0x100, "", 500.0, "GMLAN wake-up request / no data"),
    (0x101, "023E000000000000", 1000.0, "functional tester-present candidate"),
    (0x101, "0210010000000000", 2000.0, "functional default-session candidate"),
    (0x241, "023E000000000000", 1000.0, "BCM-style tester-present candidate from static notes"),
]

PROFILE_CHOICES = [
    "gauge-sweep",
    "standard-body-probe",
    "bcm-heartbeat-probe",
    "bcm-primary",
    "bcm-exact-table",
    "firmware-wake",
    "vnmf-wake",
    "diagnostic-wake",
]


@dataclass(frozen=True)
class CanRecord:
    timestamp: float
    can_id: int
    data: bytes
    is_extended: bool
    source: Path

    @property
    def dlc(self) -> int:
        return len(self.data)

    @property
    def data_hex(self) -> str:
        return self.data.hex().upper()

    @property
    def candump_id(self) -> str:
        return f"{self.can_id:08X}" if self.is_extended else f"{self.can_id:03X}"

    def candump_line(self, channel: str = "can0") -> str:
        return f"({self.timestamp:.6f}) {channel} {self.candump_id}#{self.data_hex}"


@dataclass
class ScheduledFrame:
    can_id: int
    data: bytes
    period: float
    is_extended: bool
    note: str = ""
    next_due: float = 0.0


def parse_int(value: str) -> int:
    text = value.strip().replace("_", "")
    return int(text, 0 if text.lower().startswith("0x") else 16)


def parse_hex_data(value: str) -> bytes:
    text = value.strip().replace(" ", "").replace("_", "")
    if not text:
        return b""
    if len(text) % 2:
        raise ValueError(f"payload hex must have an even number of digits: {value!r}")
    data = bytes.fromhex(text)
    if len(data) > 8:
        raise ValueError("classic CAN payloads must be 8 bytes or fewer")
    return data


def parse_values(value: str) -> list[int]:
    values: list[int] = []
    for part in value.split(","):
        part = part.strip()
        if not part:
            continue
        if "-" in part and not part.lower().startswith("0x"):
            start_s, end_s = part.split("-", 1)
            start = int(start_s, 0)
            end = int(end_s, 0)
            step = 1 if end >= start else -1
            values.extend(range(start, end + step, step))
        else:
            values.append(int(part, 0))
    for item in values:
        if item < 0 or item > 0xFF:
            raise ValueError(f"sweep value outside byte range: {item}")
    return values


def parse_candump(path: Path) -> list[CanRecord]:
    records: list[CanRecord] = []
    for line in path.read_text(errors="ignore").splitlines():
        match = CAN_ID_RE.match(line.strip())
        if not match:
            continue
        can_id_text = match.group("id").upper()
        data_text = match.group("data").upper()
        records.append(
            CanRecord(
                timestamp=float(match.group("ts")),
                can_id=int(can_id_text, 16),
                data=bytes.fromhex(data_text) if data_text else b"",
                is_extended=len(can_id_text) > 3,
                source=path,
            )
        )
    return records


def summarize_records(records: Iterable[CanRecord]) -> list[dict[str, str]]:
    by_id: dict[tuple[int, bool], list[CanRecord]] = defaultdict(list)
    for record in records:
        by_id[(record.can_id, record.is_extended)].append(record)

    rows: list[dict[str, str]] = []
    for (can_id, is_extended), items in sorted(
        by_id.items(), key=lambda item: (-len(item[1]), item[0][0], item[0][1])
    ):
        timestamps = [item.timestamp for item in items]
        gaps = [b - a for a, b in zip(timestamps, timestamps[1:]) if b >= a]
        median_ms = statistics.median(gaps) * 1000.0 if gaps else None
        approx_hz = 1000.0 / median_ms if median_ms else None
        dlcs = Counter(item.dlc for item in items)
        payloads = Counter(item.data_hex for item in items)
        candump_id = f"{can_id:08X}" if is_extended else f"{can_id:03X}"
        rows.append(
            {
                "can_id": "0x" + candump_id,
                "id_format": "extended" if is_extended else "standard",
                "count": str(len(items)),
                "dlcs": ";".join(f"{dlc}:{count}" for dlc, count in sorted(dlcs.items())),
                "median_period_ms": "" if median_ms is None else f"{median_ms:.3f}",
                "approx_hz": "" if approx_hz is None else f"{approx_hz:.3f}",
                "first_payload": items[0].data_hex,
                "top_payloads": ";".join(
                    f"{payload or '<empty>'}:{count}" for payload, count in payloads.most_common(5)
                ),
            }
        )
    return rows


def write_csv(path: Path, rows: list[dict[str, str]], fieldnames: list[str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as out:
        writer = csv.DictWriter(out, fieldnames=fieldnames)
        writer.writeheader()
        for row in rows:
            writer.writerow(row)


def load_bcm_table(path: Path) -> dict[int, list[dict[str, str]]]:
    table: dict[int, list[dict[str, str]]] = defaultdict(list)
    if not path.exists():
        return table
    with path.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            try:
                frame_id = int(row["frame_id"], 16)
            except (KeyError, ValueError):
                continue
            table[frame_id].append(row)
    return table


def load_old_log_counts(paths: Iterable[Path]) -> Counter[int]:
    counts: Counter[int] = Counter()
    for path in paths:
        if not path.is_file() or path.parent.name == "live":
            continue
        for record in parse_candump(path):
            counts[record.can_id] += 1
    return counts


def projection_rows(
    records: list[CanRecord], bcm_table: dict[int, list[dict[str, str]]], old_counts: Counter[int]
) -> list[dict[str, str]]:
    summary_by_id = summarize_records(records)
    count_by_id = {(parse_int(row["can_id"]), row["id_format"] == "extended"): row for row in summary_by_id}
    rows: list[dict[str, str]] = []
    for (can_id, is_extended), summary in sorted(count_by_id.items()):
        projected = (can_id >> 18) & 0x7FF if is_extended else ""
        flags = can_id & 0x3FFFF if is_extended else ""
        bcm_hits = bcm_table.get(projected, []) if isinstance(projected, int) else []
        groups = sorted({hit.get("group", "") for hit in bcm_hits if hit.get("group")})
        offsets = sorted({hit.get("offset", "") for hit in bcm_hits if hit.get("offset")})
        rows.append(
            {
                "live_can_id": summary["can_id"],
                "id_format": summary["id_format"],
                "count": summary["count"],
                "dlcs": summary["dlcs"],
                "median_period_ms": summary["median_period_ms"],
                "first_payload": summary["first_payload"],
                "projected_bcm_frame_id": "" if projected == "" else f"0x{projected:03X}",
                "low_18_flags_variant": "" if flags == "" else f"0x{flags:05X}",
                "exact_old_log_count": str(old_counts.get(can_id, 0)),
                "projected_old_log_count": "" if projected == "" else str(old_counts.get(projected, 0)),
                "bcm_groups": ";".join(groups),
                "bcm_offsets": ";".join(offsets[:16]),
                "projection_confidence": "bcm-group-a-projection" if "Group A" in groups else "unresolved",
            }
        )
    return rows


def add_libusb_dll_path() -> None:
    global _LIBUSB_PATH_READY
    if _LIBUSB_PATH_READY:
        return
    try:
        import libusb_package
    except ImportError:
        return
    libdir = Path(libusb_package.__path__[0])
    if hasattr(os, "add_dll_directory"):
        os.add_dll_directory(str(libdir))
    path_parts = os.environ.get("PATH", "").split(os.pathsep)
    if str(libdir) not in path_parts:
        os.environ["PATH"] = str(libdir) + os.pathsep + os.environ.get("PATH", "")
    _LIBUSB_PATH_READY = True


def open_gs_usb(listen_only: bool):
    add_libusb_dll_path()
    from gs_usb.constants import GS_CAN_MODE_HW_TIMESTAMP, GS_CAN_MODE_LISTEN_ONLY
    from gs_usb.gs_usb import GsUsb

    devices = GsUsb.scan()
    if not devices:
        raise RuntimeError("no gs_usb/candleLight devices found")
    dev = devices[0]
    try:
        dev.stop()
    except Exception:
        pass
    dev.set_timing(
        prop_seg=CAN_TIMING["prop_seg"],
        phase_seg1=CAN_TIMING["phase_seg1"],
        phase_seg2=CAN_TIMING["phase_seg2"],
        sjw=CAN_TIMING["sjw"],
        brp=CAN_TIMING["brp"],
    )
    flags = GS_CAN_MODE_HW_TIMESTAMP
    if listen_only:
        flags |= GS_CAN_MODE_LISTEN_ONLY
    dev.start(flags=flags)
    flush_stale_rx(dev, 0.25)
    return dev


def flush_stale_rx(dev, seconds: float) -> int:
    from gs_usb.gs_usb_frame import GsUsbFrame

    deadline = time.monotonic() + seconds
    frame = GsUsbFrame()
    count = 0
    while time.monotonic() < deadline:
        if dev.read(frame, 10):
            count += 1
    if count:
        print(f"flushed {count} stale RX frame(s)")
    return count


def frame_from_record(record: CanRecord):
    add_libusb_dll_path()
    from gs_usb.constants import CAN_EFF_FLAG
    from gs_usb.gs_usb_frame import GsUsbFrame

    can_id = record.can_id | (CAN_EFF_FLAG if record.is_extended else 0)
    return GsUsbFrame(can_id=can_id, data=record.data)


def frame_from_scheduled(scheduled: ScheduledFrame):
    add_libusb_dll_path()
    from gs_usb.constants import CAN_EFF_FLAG
    from gs_usb.gs_usb_frame import GsUsbFrame

    can_id = scheduled.can_id | (CAN_EFF_FLAG if scheduled.is_extended else 0)
    return GsUsbFrame(can_id=can_id, data=scheduled.data)


def write_capture_header(out) -> None:
    out.write(
        "# gs_usb direct capture "
        f"bitrate={CAN_TIMING['bitrate']} fclk={CAN_TIMING['fclk']} "
        f"brp={CAN_TIMING['brp']} prop={CAN_TIMING['prop_seg']} "
        f"phase1={CAN_TIMING['phase_seg1']} phase2={CAN_TIMING['phase_seg2']} "
        f"sjw={CAN_TIMING['sjw']}\n"
    )


def drain_rx(dev, until: float, out=None, printed: int = 0) -> int:
    from gs_usb.gs_usb_frame import GsUsbFrame

    start_wall = time.time()
    start_mono = time.monotonic()
    count = 0
    frame = GsUsbFrame()
    while time.monotonic() < until:
        if not dev.read(frame, 100):
            continue
        record = CanRecord(
            timestamp=start_wall + (time.monotonic() - start_mono),
            can_id=frame.arbitration_id,
            data=bytes(frame.data[: frame.can_dlc]),
            is_extended=frame.is_extended_id,
            source=Path("<live>"),
        )
        count += 1
        line = record.candump_line()
        if out:
            out.write(line + "\n")
        total = printed + count
        if total <= 40 or total % 100 == 0:
            print(f"[rx {total:05d}] {line}")
    return count


def receive_records(dev, until: float, out_path: Path | None = None) -> int:
    if not out_path:
        return drain_rx(dev, until)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("w", encoding="ascii", newline="") as out:
        write_capture_header(out)
        return drain_rx(dev, until, out=out)


def default_live_output(prefix: str, suffix: str = ".candump") -> Path:
    stamp = dt.datetime.now().strftime("%Y%m%d_%H%M%S")
    return DEFAULT_LIVE_DIR / f"{prefix}_{stamp}{suffix}"


def cmd_capture(args: argparse.Namespace) -> int:
    out_path = Path(args.out) if args.out else default_live_output("ipc_lowspeed_gsusb_33333")
    dev = open_gs_usb(listen_only=args.listen_only)
    try:
        print(f"capturing {args.seconds:.1f}s to {out_path}")
        count = receive_records(dev, time.monotonic() + args.seconds, out_path)
    finally:
        dev.stop()
    print(f"done: {count} frames")
    return 0


def cmd_summarize(args: argparse.Namespace) -> int:
    path = Path(args.log)
    records = parse_candump(path)
    rows = summarize_records(records)
    if args.out:
        fieldnames = [
            "can_id",
            "id_format",
            "count",
            "dlcs",
            "median_period_ms",
            "approx_hz",
            "first_payload",
            "top_payloads",
        ]
        write_csv(Path(args.out), rows, fieldnames)
        print(f"wrote {args.out}")
    print(f"{path}: {len(records)} frames / {len(rows)} unique IDs")
    for row in rows[:30]:
        period = row["median_period_ms"] or "single"
        print(
            f"{row['can_id']:>12} {row['id_format']:<8} count={row['count']:>5} "
            f"period_ms={period:>10} dlcs={row['dlcs']} payload={row['first_payload']}"
        )
    return 0


def cmd_compare(args: argparse.Namespace) -> int:
    log_path = Path(args.log)
    records = parse_candump(log_path)
    bcm_table = load_bcm_table(Path(args.bcm_table))
    old_counts = load_old_log_counts(Path(p) for p in args.old_logs)
    summary = summarize_records(records)
    projections = projection_rows(records, bcm_table, old_counts)

    out_dir = Path(args.out_dir)
    summary_path = out_dir / "ipc_lowspeed_live_summary.csv"
    projection_path = out_dir / "ipc_lowspeed_bcm_projection.csv"
    write_csv(
        summary_path,
        summary,
        [
            "can_id",
            "id_format",
            "count",
            "dlcs",
            "median_period_ms",
            "approx_hz",
            "first_payload",
            "top_payloads",
        ],
    )
    write_csv(
        projection_path,
        projections,
        [
            "live_can_id",
            "id_format",
            "count",
            "dlcs",
            "median_period_ms",
            "first_payload",
            "projected_bcm_frame_id",
            "low_18_flags_variant",
            "exact_old_log_count",
            "projected_old_log_count",
            "bcm_groups",
            "bcm_offsets",
            "projection_confidence",
        ],
    )
    write_note(Path(args.note), log_path, summary, projections)
    exact_hits = [row for row in projections if row["exact_old_log_count"] != "0"]
    group_a_hits = [row for row in projections if "Group A" in row["bcm_groups"]]
    print(f"wrote {summary_path}")
    print(f"wrote {projection_path}")
    print(f"wrote {args.note}")
    print(f"exact old-log overlaps: {len(exact_hits)}")
    print(f"BCM Group A projected hits: {len(group_a_hits)}")
    return 0


def write_note(note_path: Path, log_path: Path, summary: list[dict[str, str]], projections: list[dict[str, str]]) -> None:
    note_path.parent.mkdir(parents=True, exist_ok=True)
    group_a = [row for row in projections if "Group A" in row["bcm_groups"]]
    exact = [row for row in projections if row["exact_old_log_count"] != "0"]
    with note_path.open("w", encoding="utf-8") as out:
        out.write("# IPC Low-Speed CAN\n\n")
        out.write("Generated by `tools/ipc_lowspeed_gsusb.py compare`.\n\n")
        out.write("## Capture Context\n\n")
        out.write(f"- Source log: `{log_path}`\n")
        out.write(f"- Frames: {sum(int(row['count']) for row in summary)}\n")
        out.write(f"- Unique IDs: {len(summary)}\n")
        out.write("- Topology assumption: IPC-only capture; frames are treated as IPC-origin until proven otherwise.\n")
        out.write("- Timing: 33.333 kbit/s via candleLight/gs_usb direct timing, not python-can gs_usb.\n\n")
        out.write("## Main Findings\n\n")
        out.write(f"- Exact overlap with previous high-speed logs: {len(exact)} IDs.\n")
        out.write(f"- BCM Group A projected matches via `(extended_id >> 18) & 0x7ff`: {len(group_a)} IDs.\n")
        out.write(
            "- Live extended IDs such as `0x10244060`, `0x10424060`, `0x10ACE060`, "
            "`0x10AE8060`, and `0x13FFE060` project into the early extended/flagged "
            "BCM Group A region.\n"
        )
        out.write(
            "- This makes the IPC wire format look closer to the BCM Group A flagged entries "
            "than to the older standard 11-bit high-speed replay hypothesis.\n\n"
        )
        out.write("## Top Live IDs\n\n")
        out.write("| ID | Format | Count | Period ms | DLCs | First payload |\n")
        out.write("|---:|---|---:|---:|---|---|\n")
        for row in summary[:20]:
            out.write(
                f"| {row['can_id']} | {row['id_format']} | {row['count']} | "
                f"{row['median_period_ms'] or ''} | {row['dlcs']} | `{row['first_payload']}` |\n"
            )
        out.write("\n## BCM Projection\n\n")
        out.write("| Live ID | Projected BCM ID | Flags/Variant | BCM Groups | Exact old-log count |\n")
        out.write("|---:|---:|---:|---|---:|\n")
        for row in projections:
            out.write(
                f"| {row['live_can_id']} | {row['projected_bcm_frame_id']} | "
                f"{row['low_18_flags_variant']} | {row['bcm_groups']} | "
                f"{row['exact_old_log_count']} |\n"
            )
        out.write("\n## Next Work\n\n")
        out.write("- Use `send-profile --profile gauge-sweep --tx-confirm` for guarded byte-sweep experiments.\n")
        out.write("- Watch the IPC while sweeping one byte at a time; do not treat changes as decoded until repeated.\n")
        out.write("- Keep logging RX frames during TX to distinguish IPC-origin chatter from responses to injected frames.\n")


def records_to_schedule(records: list[CanRecord]) -> list[ScheduledFrame]:
    by_key: dict[tuple[int, bool, str], list[CanRecord]] = defaultdict(list)
    for record in records:
        by_key[(record.can_id, record.is_extended, record.data_hex)].append(record)
    schedule: list[ScheduledFrame] = []
    for (can_id, is_extended, data_hex), items in by_key.items():
        timestamps = [item.timestamp for item in items]
        gaps = [b - a for a, b in zip(timestamps, timestamps[1:]) if b >= a]
        if gaps:
            period = max(0.005, statistics.median(gaps))
        else:
            period = 1.0
        schedule.append(
            ScheduledFrame(
                can_id=can_id,
                data=parse_hex_data(data_hex),
                period=period,
                is_extended=is_extended,
                note="replay",
            )
        )
    return schedule


def ensure_tx_confirm(args: argparse.Namespace) -> None:
    if not args.tx_confirm:
        raise SystemExit("refusing to transmit without --tx-confirm")


def cmd_replay(args: argparse.Namespace) -> int:
    ensure_tx_confirm(args)
    records = parse_candump(Path(args.log))
    if not records:
        raise SystemExit(f"no replayable frames found in {args.log}")
    records.sort(key=lambda item: item.timestamp)
    first_ts = records[0].timestamp
    out_path = Path(args.rx_log) if args.rx_log else default_live_output("ipc_lowspeed_replay_rx")
    dev = open_gs_usb(listen_only=False)
    sent = 0
    received = 0
    try:
        deadline = time.monotonic() + args.duration if args.duration else None
        out_path.parent.mkdir(parents=True, exist_ok=True)
        with out_path.open("w", encoding="ascii", newline="") as out:
            write_capture_header(out)
            for record in records:
                if deadline and time.monotonic() >= deadline:
                    break
                due_delay = max(0.0, record.timestamp - first_ts)
                target_time = time.monotonic() + due_delay if sent == 0 else replay_start + due_delay
                if sent == 0:
                    replay_start = time.monotonic()
                    target_time = replay_start
                while time.monotonic() < target_time:
                    received += drain_rx(dev, min(target_time, time.monotonic() + 0.05), out=out, printed=received)
                dev.send(frame_from_record(record))
                sent += 1
                if sent <= 40 or sent % 100 == 0:
                    print(f"[tx {sent:05d}] {record.candump_line()}")
            received += drain_rx(dev, time.monotonic() + args.post_rx_seconds, out=out, printed=received)
    finally:
        dev.stop()
    print(f"done: sent={sent} rx_frames={received} rx_log={out_path}")
    return 0


def make_profile_schedule(args: argparse.Namespace) -> list[ScheduledFrame]:
    profiles = {
        "gauge-sweep": DEFAULT_GAUGE_BASELINE,
        "standard-body-probe": STANDARD_BODY_PROBE,
        "bcm-heartbeat-probe": BCM_HEARTBEAT_PROBE,
        "bcm-primary": BCM_HEARTBEAT_PROBE,
        "bcm-exact-table": BCM_EXACT_TABLE_PROBE,
        "firmware-wake": FIRMWARE_WAKE_PROFILE,
        "vnmf-wake": GMLAN_VNMF_WAKE_PROFILE,
        "diagnostic-wake": DIAGNOSTIC_WAKE_PROFILE,
    }
    source = profiles[args.profile]
    schedule = [
        ScheduledFrame(
            can_id=can_id,
            data=parse_hex_data(payload),
            period=period_ms / 1000.0,
            is_extended=can_id > 0x7FF,
            note=note,
        )
        for can_id, payload, period_ms, note in source
    ]
    if args.target_id:
        target_id = parse_int(args.target_id)
        target = next((item for item in schedule if item.can_id == target_id), None)
        if target is None:
            payload = parse_hex_data(args.payload or "0000000000000000")
            target = ScheduledFrame(
                can_id=target_id,
                data=payload,
                period=args.period_ms / 1000.0,
                is_extended=target_id > 0x7FF,
                note="user target",
            )
            schedule.append(target)
        else:
            if args.payload:
                target.data = parse_hex_data(args.payload)
            target.period = args.period_ms / 1000.0
    return schedule


def send_fixed_schedule(
    dev,
    schedule: list[ScheduledFrame],
    hold_seconds: float,
    out_path: Path,
) -> tuple[int, int]:
    sent = 0
    received = 0
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("w", encoding="ascii", newline="") as out:
        write_capture_header(out)
        now = time.monotonic()
        for item in schedule:
            item.next_due = now
        hold_until = time.monotonic() + hold_seconds
        while time.monotonic() < hold_until:
            now = time.monotonic()
            for item in schedule:
                if now >= item.next_due:
                    dev.send(frame_from_scheduled(item))
                    sent += 1
                    item.next_due = now + item.period
                    if sent <= 60 or sent % 100 == 0:
                        ident = f"{item.can_id:08X}" if item.is_extended else f"{item.can_id:03X}"
                        print(f"[tx {sent:05d}] {ident}#{item.data.hex().upper()}")
            received += drain_rx(dev, min(hold_until, time.monotonic() + 0.02), out=out, printed=received)
    return sent, received


def make_source_sweep_schedule(source_address: int) -> list[ScheduledFrame]:
    schedule = []
    for base_id, payload, period_ms, note in BCM_TO_IPC_TEMPLATES:
        schedule.append(
            ScheduledFrame(
                can_id=base_id | source_address,
                data=parse_hex_data(payload),
                period=period_ms / 1000.0,
                is_extended=True,
                note=f"{note}; source=0x{source_address:02X}",
            )
        )
    return schedule


def cmd_source_sweep(args: argparse.Namespace) -> int:
    ensure_tx_confirm(args)
    sources = parse_values(args.sources)
    out_path = Path(args.rx_log) if args.rx_log else default_live_output("ipc_lowspeed_source_sweep_rx")
    dev = open_gs_usb(listen_only=False)
    sent = 0
    received = 0
    try:
        out_path.parent.mkdir(parents=True, exist_ok=True)
        with out_path.open("w", encoding="ascii", newline="") as out:
            write_capture_header(out)
            for source in sources:
                schedule = make_source_sweep_schedule(source)
                now = time.monotonic()
                for item in schedule:
                    item.next_due = now
                hold_until = time.monotonic() + (args.hold_ms / 1000.0)
                print(f"source sweep source=0x{source:02X}")
                while time.monotonic() < hold_until:
                    now = time.monotonic()
                    for item in schedule:
                        if now >= item.next_due:
                            dev.send(frame_from_scheduled(item))
                            sent += 1
                            item.next_due = now + item.period
                            if sent <= 60 or sent % 100 == 0:
                                print(f"[tx {sent:05d}] {item.can_id:08X}#{item.data.hex().upper()}")
                    received += drain_rx(dev, min(hold_until, time.monotonic() + 0.02), out=out, printed=received)
    finally:
        dev.stop()
    print(f"done: sent={sent} rx_frames={received} rx_log={out_path}")
    return 0


def cmd_vnmf_sweep(args: argparse.Namespace) -> int:
    ensure_tx_confirm(args)
    start_id = parse_int(args.start_id)
    end_id = parse_int(args.end_id)
    if start_id > end_id:
        start_id, end_id = end_id, start_id
    out_path = Path(args.rx_log) if args.rx_log else default_live_output("ipc_lowspeed_vnmf_sweep_rx")
    dev = open_gs_usb(listen_only=False)
    sent = 0
    received = 0
    try:
        out_path.parent.mkdir(parents=True, exist_ok=True)
        with out_path.open("w", encoding="ascii", newline="") as out:
            write_capture_header(out)
            for vnmf_id in range(start_id, end_id + 1):
                wake = ScheduledFrame(0x100, b"", 1.0, False, "wake request")
                init = ScheduledFrame(vnmf_id, parse_hex_data(args.initiate_payload), 0.35, False, "VNMF initiate")
                cont = ScheduledFrame(vnmf_id, parse_hex_data(args.continue_payload), 0.35, False, "VNMF continue")
                schedule = [wake, init, cont]
                now = time.monotonic()
                for item in schedule:
                    item.next_due = now
                hold_until = time.monotonic() + (args.hold_ms / 1000.0)
                print(f"VNMF sweep id=0x{vnmf_id:03X}")
                while time.monotonic() < hold_until:
                    now = time.monotonic()
                    for item in schedule:
                        if now >= item.next_due:
                            dev.send(frame_from_scheduled(item))
                            sent += 1
                            item.next_due = now + item.period
                            if sent <= 80 or sent % 100 == 0:
                                print(f"[tx {sent:05d}] {item.can_id:03X}#{item.data.hex().upper()}")
                    received += drain_rx(dev, min(hold_until, time.monotonic() + 0.02), out=out, printed=received)
    finally:
        dev.stop()
    print(f"done: sent={sent} rx_frames={received} rx_log={out_path}")
    return 0


def cmd_send_profile(args: argparse.Namespace) -> int:
    ensure_tx_confirm(args)
    if args.profile not in PROFILE_CHOICES:
        raise SystemExit(f"unknown profile: {args.profile}")

    schedule = make_profile_schedule(args)
    out_path = Path(args.rx_log) if args.rx_log else default_live_output(f"ipc_lowspeed_{args.profile}_rx")
    dev = open_gs_usb(listen_only=False)
    if args.fixed:
        try:
            sent, received = send_fixed_schedule(dev, schedule, args.hold_ms / 1000.0, out_path)
        finally:
            dev.stop()
        print(f"done: sent={sent} rx_frames={received} rx_log={out_path}")
        return 0

    sweep_values = parse_values(args.values) if args.values else [0x00, 0x20, 0x40, 0x60, 0x80, 0xA0, 0xC0, 0xE0, 0xFF]
    target_id = parse_int(args.target_id or "0x10424060")
    target = next((item for item in schedule if item.can_id == target_id), None)
    if target is None:
        raise SystemExit(f"target ID 0x{target_id:X} is not in the profile schedule")
    if args.byte < 0 or args.byte >= len(target.data):
        raise SystemExit(f"--byte {args.byte} is outside target payload length {len(target.data)}")

    sent = 0
    received = 0
    try:
        out_path.parent.mkdir(parents=True, exist_ok=True)
        with out_path.open("w", encoding="ascii", newline="") as out:
            write_capture_header(out)
            for item in schedule:
                item.next_due = time.monotonic()
            for value in sweep_values:
                mutable = bytearray(target.data)
                mutable[args.byte] = value
                target.data = bytes(mutable)
                hold_until = time.monotonic() + (args.hold_ms / 1000.0)
                print(f"sweep target=0x{target.can_id:X} byte={args.byte} value=0x{value:02X}")
                while time.monotonic() < hold_until:
                    now = time.monotonic()
                    for item in schedule:
                        if now >= item.next_due:
                            dev.send(frame_from_scheduled(item))
                            sent += 1
                            item.next_due = now + item.period
                            if sent <= 40 or sent % 100 == 0:
                                ident = f"{item.can_id:08X}" if item.is_extended else f"{item.can_id:03X}"
                                print(f"[tx {sent:05d}] {ident}#{item.data.hex().upper()}")
                    received += drain_rx(dev, min(hold_until, time.monotonic() + 0.02), out=out, printed=received)
    finally:
        dev.stop()
    print(f"done: sent={sent} rx_frames={received} rx_log={out_path}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    capture = sub.add_parser("capture", help="capture live IPC low-speed traffic")
    capture.add_argument("--seconds", type=float, default=30.0)
    capture.add_argument("--out")
    capture.add_argument("--listen-only", action="store_true")
    capture.set_defaults(func=cmd_capture)

    summarize = sub.add_parser("summarize", help="summarize a candump log")
    summarize.add_argument("--log", required=True)
    summarize.add_argument("--out")
    summarize.set_defaults(func=cmd_summarize)

    compare = sub.add_parser("compare", help="compare IPC low-speed IDs against BCM tables and old logs")
    compare.add_argument("--log", required=True)
    compare.add_argument("--out-dir", default=str(DEFAULT_ANALYSIS_DIR))
    compare.add_argument("--bcm-table", default=str(DEFAULT_BCM_TABLE))
    compare.add_argument("--note", default=str(DEFAULT_NOTE))
    compare.add_argument(
        "--old-logs",
        nargs="*",
        default=[str(path) for path in sorted((REPO_ROOT / "data" / "can_logs").glob("*.candump"))],
    )
    compare.set_defaults(func=cmd_compare)

    replay = sub.add_parser("replay", help="replay a candump log with original relative timing")
    replay.add_argument("--log", required=True)
    replay.add_argument("--duration", type=float, help="optional maximum replay duration in seconds")
    replay.add_argument("--post-rx-seconds", type=float, default=2.0)
    replay.add_argument("--rx-log")
    replay.add_argument("--tx-confirm", action="store_true")
    replay.set_defaults(func=cmd_replay)

    send = sub.add_parser("send-profile", help="send guarded experimental IPC profiles")
    send.add_argument(
        "--profile",
        choices=PROFILE_CHOICES,
        default="gauge-sweep",
    )
    send.add_argument("--target-id")
    send.add_argument("--payload", help="override/add target payload hex")
    send.add_argument("--byte", type=int, default=0)
    send.add_argument("--values", default="0,32,64,96,128,160,192,224,255")
    send.add_argument("--period-ms", type=float, default=100.0)
    send.add_argument("--hold-ms", type=float, default=1500.0)
    send.add_argument("--rx-log")
    send.add_argument("--tx-confirm", action="store_true")
    send.add_argument("--fixed", action="store_true", help="send the selected profile as-is instead of sweeping a byte")
    send.set_defaults(func=cmd_send_profile)

    source_sweep = sub.add_parser("source-sweep", help="sweep likely BCM source addresses to IPC destination 0x60")
    source_sweep.add_argument("--sources", default="0,16,32,48,64,80,96,112,128,160,192,224,255")
    source_sweep.add_argument("--hold-ms", type=float, default=2500.0)
    source_sweep.add_argument("--rx-log")
    source_sweep.add_argument("--tx-confirm", action="store_true")
    source_sweep.set_defaults(func=cmd_source_sweep)

    vnmf_sweep = sub.add_parser("vnmf-sweep", help="sweep standard GMLAN VNMF IDs 0x620-0x63F")
    vnmf_sweep.add_argument("--start-id", default="0x620")
    vnmf_sweep.add_argument("--end-id", default="0x63F")
    vnmf_sweep.add_argument("--hold-ms", type=float, default=800.0)
    vnmf_sweep.add_argument("--initiate-payload", default="01FFFFFFFF000000")
    vnmf_sweep.add_argument("--continue-payload", default="00FFFFFFFF000000")
    vnmf_sweep.add_argument("--rx-log")
    vnmf_sweep.add_argument("--tx-confirm", action="store_true")
    vnmf_sweep.set_defaults(func=cmd_vnmf_sweep)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
