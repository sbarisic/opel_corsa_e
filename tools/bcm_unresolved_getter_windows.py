#!/usr/bin/env python3
"""Dump byte windows for BCM Group B getters that are not simple RAM loads."""

from __future__ import annotations

import csv
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
FLASH = REPO_ROOT / "data" / "raw" / "bcm_p_flash.bin"
GETTERS = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_wrapper_getters.csv"
OUT_CSV = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_unresolved_getter_windows.csv"
SHORT_GP_BASE = 0x04000000
WINDOW = 0x80


def u16le(data: bytes, offset: int) -> int:
    return data[offset] | (data[offset + 1] << 8)


def s16(value: int) -> int:
    return value - 0x10000 if value >= 0x8000 else value


def ram_reads_in_window(data: bytes, start: int, end: int) -> list[str]:
    reads: list[str] = []
    pos = start
    while pos + 8 <= end:
        if data[pos] == 0x40 and data[pos + 1] == 0x56:
            high = u16le(data, pos + 2)
            op = data[pos + 4 : pos + 6].hex().upper()
            raw_offset = u16le(data, pos + 6)
            address = (high << 16) + s16(raw_offset)
            reads.append(f"0x{pos:06X}:op={op}:ram=0x{address:08X}")
            pos += 2
            continue

        if data[pos] in (0x84, 0xA4) and data[pos + 1] == 0x57:
            op = data[pos : pos + 2].hex().upper()
            raw_offset = u16le(data, pos + 2)
            address = SHORT_GP_BASE + s16(raw_offset)
            reads.append(f"0x{pos:06X}:op={op}:ram=0x{address:08X}")
        pos += 1
    return reads


def main() -> int:
    data = FLASH.read_bytes()
    rows: list[dict[str, str]] = []

    with GETTERS.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            if row["source_kind"] != "computed_branchy":
                continue
            getter = int(row["getter"], 16)
            start = max(0, getter)
            end = min(len(data), getter + WINDOW)
            window = data[start:end]
            rows.append(
                {
                    "frame_id": row["frame_id"],
                    "block": row["block"],
                    "getter": row["getter"],
                    "window_start": f"0x{start:06X}",
                    "window_end": f"0x{end:06X}",
                    "bytes": window.hex(" ").upper(),
                    "ram_reads_in_window": "; ".join(ram_reads_in_window(data, start, end)),
                }
            )

    OUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=[
                "frame_id",
                "block",
                "getter",
                "window_start",
                "window_end",
                "bytes",
                "ram_reads_in_window",
            ],
        )
        writer.writeheader()
        writer.writerows(rows)

    print(f"wrote {len(rows)} unresolved getter windows to {OUT_CSV}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
