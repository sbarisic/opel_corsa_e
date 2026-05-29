#!/usr/bin/env python3
"""Trace BCM Group A entries that match the real IPC low-speed capture."""

from __future__ import annotations

import argparse
import csv
import re
import subprocess
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
FLASH = REPO_ROOT / "data" / "raw" / "bcm_p_flash.bin"
GENERATED = REPO_ROOT / "analysis" / "generated"
GROUP_A_TRACE = GENERATED / "bcm_group_a_tx_trace.csv"
IPC_PROJECTION = GENERATED / "ipc_lowspeed_bcm_projection.csv"
OUT_CSV = GENERATED / "bcm_ipc_live_tx_trace.csv"
OUT_MD = REPO_ROOT / "analysis" / "BCM_IPC_LIVE_REVERSE.md"
MAPPED_FLASH_BASE = 0x80000000
GP_BASE = 0x03FF0578
TP_BASE = 0x0013E248

WRITERS = {
    0x12F06C: ("write_u16_be", "writes r8 as two payload bytes, MSB first, index += 2"),
    0x12F10C: ("or_bit_if_one", "if r8 == 1, ORs r9 bit mask into payload[index]"),
    0x12F124: ("write_u8", "writes r8 low byte to payload[index], index += 1"),
    0x12F208: ("write_u32_be", "writes r8 as four payload bytes, MSB first, index += 4"),
}


@dataclass(frozen=True)
class HandlerTrace:
    handler: int
    getter: int
    writer: int
    getter_source: str
    getter_disasm: str
    handler_disasm: str


def windows_path_to_wsl(path: Path) -> str:
    resolved = path.resolve()
    drive = resolved.drive.rstrip(":").lower()
    rest = str(resolved)[3:].replace("\\", "/")
    return f"/mnt/{drive}/{rest}"


def run_r2(commands: list[str], timeout: int = 120) -> str:
    r2_command = ";".join(commands)
    flash_wsl = windows_path_to_wsl(FLASH)
    shell = (
        "r2 -q -a v850 -b 32 -e cfg.bigendian=false -e scr.color=false "
        f"-e asm.lines=false -c \"{r2_command}\" -c q '{flash_wsl}'"
    )
    result = subprocess.run(
        ["wsl", "sh", "-lc", shell],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=timeout,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or result.stdout.strip())
    return result.stdout


def parse_hex(value: str) -> int:
    return int(value.strip(), 16)


def load_csv(path: Path) -> list[dict[str, str]]:
    with path.open(newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f))


def unique_handlers(group_rows: list[dict[str, str]]) -> list[int]:
    handlers: list[int] = []
    for row in group_rows:
        for match in re.finditer(r"(0x[0-9A-Fa-f]+)@", row.get("direct_dispatch_handlers", "")):
            handler = parse_hex(match.group(1))
            if handler not in handlers:
                handlers.append(handler)
    return handlers


def split_blocks(text: str, label: str) -> dict[int, list[str]]:
    blocks: dict[int, list[str]] = {}
    current: int | None = None
    lines: list[str] = []
    marker = re.compile(rf"=== {label} (0x[0-9A-Fa-f]+) ===")
    for line in text.splitlines():
        match = marker.match(line)
        if match:
            if current is not None:
                blocks[current] = lines
            current = parse_hex(match.group(1))
            lines = []
            continue
        if current is not None:
            lines.append(line)
    if current is not None:
        blocks[current] = lines
    return blocks


def disassemble_handlers(handlers: list[int]) -> dict[int, list[str]]:
    commands: list[str] = []
    for handler in handlers:
        commands.append(f"?e === handler 0x{handler:X} ===")
        commands.append(f"pd 48 @ 0x{handler:X}")
    return split_blocks(run_r2(commands), "handler")


def first_handler_function(lines: list[str]) -> list[str]:
    out: list[str] = []
    for line in lines:
        out.append(line)
        if "callt 31" in line:
            break
    return out


def extract_pairs(lines: list[str]) -> list[tuple[int, int]]:
    calls: list[int] = []
    for line in first_handler_function(lines):
        match = re.search(r"jarl 0x([0-9A-Fa-f]+), lp", line)
        if match:
            calls.append(parse_hex("0x" + match.group(1)))
    pairs: list[tuple[int, int]] = []
    i = 0
    while i < len(calls) - 1:
        getter, writer = calls[i], calls[i + 1]
        if writer in WRITERS:
            pairs.append((getter, writer))
            i += 2
        else:
            i += 1
    return pairs


def disassemble_getters(getters: list[int]) -> dict[int, list[str]]:
    commands: list[str] = []
    for getter in getters:
        commands.append(f"?e === getter 0x{getter:X} ===")
        commands.append(f"pd 14 @ 0x{getter:X}")
    return split_blocks(run_r2(commands), "getter")


def summarize_getter(lines: list[str]) -> str:
    text = "\n".join(lines[:8])
    gp_match = re.search(r"ld\.(?:bu|b|hu|h|w)\s+(-?\d+)\[gp\]", text)
    if gp_match:
        offset = int(gp_match.group(1))
        return f"RAM gp{offset:+d} -> 0x{GP_BASE + offset:08X}"
    tp_match = re.search(r"ld\.(?:bu|b|hu|h|w)\s+(-?\d+)\[tp\]", text)
    if tp_match:
        offset = int(tp_match.group(1))
        return f"RAM tp{offset:+d} -> 0x{TP_BASE + offset:08X}"
    abs_match = re.search(
        r"movhi\s+(\d+), r0, r10.*?ld\.(?:bu|b|hu|h|w)\s+(-?\d+)\[r10\]",
        text,
        re.S,
    )
    if abs_match:
        high = int(abs_match.group(1))
        offset = int(abs_match.group(2))
        address = (high << 16) + offset
        return f"RAM/SFR 0x{high << 16:08X}{offset:+#x} -> 0x{address:08X}"
    if "jarl" in text:
        calls = "; ".join(re.findall(r"jarl 0x[0-9A-Fa-f]+", text))
        return f"computed getter via {calls}"
    if "mov 0, r10" in text:
        return "constant 0"
    return "unclassified getter"


def build_traces(handler_blocks: dict[int, list[str]]) -> dict[int, list[HandlerTrace]]:
    getter_ids = sorted({getter for lines in handler_blocks.values() for getter, _ in extract_pairs(lines)})
    getter_blocks = disassemble_getters(getter_ids)
    traces: dict[int, list[HandlerTrace]] = defaultdict(list)
    for handler, lines in handler_blocks.items():
        handler_disasm = "\n".join(first_handler_function(lines))
        for getter, writer in extract_pairs(lines):
            getter_lines = getter_blocks.get(getter, [])
            traces[handler].append(
                HandlerTrace(
                    handler=handler,
                    getter=getter,
                    writer=writer,
                    getter_source=summarize_getter(getter_lines),
                    getter_disasm="\n".join(getter_lines[:8]),
                    handler_disasm=handler_disasm,
                )
            )
    return traces


def live_rows() -> list[dict[str, str]]:
    projection = load_csv(IPC_PROJECTION)
    live_projected: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in projection:
        if row.get("projected_bcm_frame_id") and "Group A" in row.get("bcm_groups", ""):
            live_projected[row["projected_bcm_frame_id"]].append(row)
    group_a = load_csv(GROUP_A_TRACE)
    rows = []
    for row in group_a:
        for live in live_projected.get(row["frame_id"], []):
            merged = dict(row)
            merged.update({f"live_{key}": value for key, value in live.items()})
            rows.append(merged)
    return rows


def write_outputs(
    rows: list[dict[str, str]],
    traces: dict[int, list[HandlerTrace]],
    out_csv: Path,
    out_md: Path,
) -> None:
    out_csv.parent.mkdir(parents=True, exist_ok=True)
    fields = [
        "live_can_id",
        "live_low_18_flags_variant",
        "projected_bcm_frame_id",
        "table_wire_id_source_60",
        "exact_wire_id_match",
        "table_index",
        "table_offset",
        "raw",
        "object_key",
        "handler",
        "writer",
        "getter",
        "getter_source",
        "first_payload",
        "median_period_ms",
        "trace_status",
    ]
    with out_csv.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fields)
        writer.writeheader()
        for row in rows:
            handlers = [parse_hex(match.group(1)) for match in re.finditer(r"(0x[0-9A-Fa-f]+)@", row["direct_dispatch_handlers"])]
            table_wire_id = (parse_hex(row["raw"]) & 0x1FFFFFFF) | 0x60
            exact_wire_id_match = "yes" if parse_hex(row["live_live_can_id"]) == table_wire_id else "no"
            base_row = {
                "live_can_id": row["live_live_can_id"],
                "live_low_18_flags_variant": row["live_low_18_flags_variant"],
                "projected_bcm_frame_id": row["frame_id"],
                "table_wire_id_source_60": f"0x{table_wire_id:08X}",
                "exact_wire_id_match": exact_wire_id_match,
                "table_index": row["table_index"],
                "table_offset": row["table_offset"],
                "raw": row["raw"],
                "object_key": row["object_key"],
                "first_payload": row["live_first_payload"],
                "median_period_ms": row["live_median_period_ms"],
            }
            if not handlers:
                out_row = dict(base_row)
                out_row.update(
                    {
                        "trace_status": "table-only; no direct dispatch entry",
                    }
                )
                writer.writerow(out_row)
                continue
            for handler in handlers:
                handler_traces = traces.get(handler) or []
                if not handler_traces:
                    out_row = dict(base_row)
                    out_row.update(
                        {
                            "handler": f"0x{handler:06X}",
                            "trace_status": "dispatch handler found; no payload writer pair parsed",
                        }
                    )
                    writer.writerow(out_row)
                    continue
                for trace in handler_traces:
                    out_row = dict(base_row)
                    out_row.update(
                        {
                            "handler": f"0x{handler:06X}",
                            "writer": WRITERS[trace.writer][0],
                            "getter": f"0x{trace.getter:06X}",
                            "getter_source": trace.getter_source,
                            "trace_status": "payload builder traced",
                        }
                    )
                    writer.writerow(out_row)

    with out_md.open("w", encoding="utf-8") as out:
        out.write("# BCM IPC Live-ID Reverse Engineering\n\n")
        out.write("Generated by `tools/bcm_ipc_live_trace.py`.\n\n")
        out.write("## Findings\n\n")
        out.write(
            "- Real IPC capture IDs map into BCM Group A by `(extended_id >> 18) & 0x7ff`.\n"
        )
        out.write(
            "- Exact wire IDs often match `(table_raw & 0x1fffffff) | 0x60`, where `0x60` is the IPC/source byte seen on the direct cluster capture.\n"
        )
        out.write(
            "- Most matching Group A rows have direct dispatch handlers in the `0x021A40` table.\n"
        )
        out.write(
            "- Dispatch wrappers call a RAM getter, move the result to `r8`, then call a payload writer.\n"
        )
        out.write(
            "- Payload writers now identified: `0x12F06C` writes u16 big-endian, `0x12F10C` ORs bit masks, `0x12F124` writes u8, and `0x12F208` writes u32 big-endian.\n"
        )
        out.write(
            "- This confirms the early Group A block is a real low-speed IPC/body payload-builder region, not a table artifact.\n\n"
        )
        out.write("## Trace Table\n\n")
        out.write("| Live ID | BCM ID | Variant | Table wire | Exact | Raw | Handler | Writer | Getter source | Payload seen |\n")
        out.write("|---:|---:|---:|---:|---|---:|---:|---|---|---|\n")
        with out_csv.open(newline="", encoding="utf-8") as f:
            for row in csv.DictReader(f):
                out.write(
                    f"| {row['live_can_id']} | {row['projected_bcm_frame_id']} | {row['live_low_18_flags_variant']} | "
                    f"{row['table_wire_id_source_60']} | {row['exact_wire_id_match']} | `{row['raw']}` | "
                    f"{row['handler']} | {row['writer']} | {row['getter_source']} | `{row['first_payload']}` |\n"
                )
        out.write("\n## Practical Implication\n\n")
        out.write(
            "The IPC-only frames are not enough to synthesize gauge inputs, but the BCM firmware shows how this "
            "low-speed object family is built: table row -> dispatch handler -> RAM getter -> payload writer. "
            "The next reversing step is to trace the getter RAM locations back to their writers, especially the "
            "ECU-side Group B receive handlers for `0x0C9`, `0x3D1`, `0x3E9`, `0x4C1`, and `0x4D1`.\n"
        )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--csv", default=str(OUT_CSV))
    parser.add_argument("--markdown", default=str(OUT_MD))
    args = parser.parse_args()
    out_csv = Path(args.csv)
    out_md = Path(args.markdown)
    rows = live_rows()
    handlers = unique_handlers(rows)
    handler_blocks = disassemble_handlers(handlers)
    traces = build_traces(handler_blocks)
    write_outputs(rows, traces, out_csv, out_md)
    print(f"wrote {out_csv}")
    print(f"wrote {out_md}")
    print(f"matched Group A rows: {len(rows)}")
    print(f"unique handlers: {len(handlers)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
