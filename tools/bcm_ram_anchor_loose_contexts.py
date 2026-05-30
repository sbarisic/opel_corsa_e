#!/usr/bin/env python3
"""Scan loose byte contexts for Group B RAM-anchor low offsets.

The structured xref pass only records access encodings we already understand.
This helper is intentionally broader: it finds every occurrence of each RAM
anchor's low 16-bit offset in flash, marks the ones already explained by the
structured pass, and keeps the surrounding bytes for manual disassembly.
"""

from __future__ import annotations

import csv
from collections import defaultdict
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
FLASH = REPO_ROOT / "data" / "raw" / "bcm_p_flash.bin"
GETTERS = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_wrapper_getters.csv"
STRUCTURED = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_ram_anchor_xrefs.csv"
OUT_CSV = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_ram_anchor_loose_contexts.csv"
MAX_CONTEXTS_PER_ANCHOR = 80


def u16le(value: int) -> bytes:
    return bytes([value & 0xFF, (value >> 8) & 0xFF])


def load_anchor_addresses() -> dict[str, set[int]]:
    by_frame: dict[str, set[int]] = defaultdict(set)
    with GETTERS.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            ram = row.get("ram_address", "")
            if not ram:
                continue
            by_frame[row["frame_id"]].add(int(ram, 16))
    return by_frame


def load_known_offset_positions() -> set[tuple[str, int, int]]:
    known: set[tuple[str, int, int]] = set()
    if not STRUCTURED.exists():
        return known
    with STRUCTURED.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            frame_id = row["frame_id"]
            ram = int(row["ram_address"], 16)
            xref = int(row["xref_offset"], 16)
            if row["encoding"].startswith("short-gp"):
                offset_pos = xref + 2
            else:
                offset_pos = xref + 6
            known.add((frame_id, ram, offset_pos))
    return known


def classify_preceding_op(op: bytes) -> str:
    if len(op) != 2:
        return "unknown"
    if op[1] == 0x57:
        return "known-load-family"
    if op[1] in (0x5F, 0x67, 0x6F, 0x77, 0x7F, 0x87, 0x8F):
        return "possible-write-or-bitop-family"
    return "other"


def scan(data: bytes, by_frame: dict[str, set[int]]) -> list[dict[str, str]]:
    known = load_known_offset_positions()
    rows: list[dict[str, str]] = []

    for frame_id, addresses in sorted(by_frame.items()):
        for address in sorted(addresses):
            needle = u16le(address & 0xFFFF)
            start = 0
            count = 0
            while True:
                idx = data.find(needle, start)
                if idx < 0:
                    break
                context_start = max(0, idx - 8)
                context_end = min(len(data), idx + 10)
                preceding_op = data[idx - 2 : idx] if idx >= 2 else b""
                rows.append(
                    {
                        "frame_id": frame_id,
                        "ram_address": f"0x{address:08X}",
                        "offset_bytes": needle.hex().upper(),
                        "offset_position": f"0x{idx:06X}",
                        "preceding_op": preceding_op.hex().upper(),
                        "preceding_op_family": classify_preceding_op(preceding_op),
                        "known_structured_load": "yes" if (frame_id, address, idx) in known else "no",
                        "context_start": f"0x{context_start:06X}",
                        "context_bytes": data[context_start:context_end].hex(" ").upper(),
                    }
                )
                count += 1
                if count >= MAX_CONTEXTS_PER_ANCHOR:
                    break
                start = idx + 1
    return rows


def main() -> int:
    data = FLASH.read_bytes()
    rows = scan(data, load_anchor_addresses())

    OUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=[
                "frame_id",
                "ram_address",
                "offset_bytes",
                "offset_position",
                "preceding_op",
                "preceding_op_family",
                "known_structured_load",
                "context_start",
                "context_bytes",
            ],
        )
        writer.writeheader()
        writer.writerows(rows)

    unexplained = sum(1 for row in rows if row["known_structured_load"] == "no")
    print(f"wrote {len(rows)} loose contexts ({unexplained} unexplained) to {OUT_CSV}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
