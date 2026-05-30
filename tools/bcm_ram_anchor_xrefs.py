#!/usr/bin/env python3
"""Find byte-pattern xrefs for RAM anchors used by BCM Group B getter chains.

This is not a full V850 disassembler. It searches for the recurring absolute
RAM access encoding shape observed in the BCM flash:

  40 56 <high16-le> <op16-le> <offset16-le>

The second opcode byte is often 0x57 for load-like getters in the current
traces. Other op values are retained as possible writer or computed access
leads for manual disassembly.
"""

from __future__ import annotations

import csv
from collections import defaultdict
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
FLASH = REPO_ROOT / "data" / "raw" / "bcm_p_flash.bin"
ANCHORS = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_wrapper_getters.csv"
OUT_CSV = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_ram_anchor_xrefs.csv"
SHORT_GP_BASE = 0x04000000


def u16le(value: int) -> bytes:
    return bytes([value & 0xFF, (value >> 8) & 0xFF])


def encodings_for_ram(address: int) -> list[tuple[int, int, str]]:
    """Return high, unsigned offset, encoding note."""
    out: list[tuple[int, int, str]] = []
    high = address >> 16
    offset = address & 0xFFFF
    out.append((high, offset, f"direct high=0x{high:04X} off=0x{offset:04X}"))

    # Addresses below a round high boundary can also appear as high+1 with a
    # signed negative 16-bit offset. This is common for 0x03FF8xxx addresses.
    if offset >= 0x8000:
        out.append((high + 1, offset, f"signed-negative high=0x{high + 1:04X} off=0x{offset:04X}"))

    return list(dict.fromkeys(out))


def load_anchor_addresses() -> dict[str, set[int]]:
    by_frame: dict[str, set[int]] = defaultdict(set)
    with ANCHORS.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            ram = row.get("ram_address", "")
            if not ram:
                continue
            by_frame[row["frame_id"]].add(int(ram, 16))
    return by_frame


def scan(data: bytes, by_frame: dict[str, set[int]]) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    for frame_id, addresses in sorted(by_frame.items()):
        for address in sorted(addresses):
            for high, offset, note in encodings_for_ram(address):
                prefix = b"\x40\x56" + u16le(high)
                off_bytes = u16le(offset)
                start = 0
                while True:
                    idx = data.find(prefix, start)
                    if idx < 0:
                        break
                    if idx + 8 <= len(data) and data[idx + 6 : idx + 8] == off_bytes:
                        op = data[idx + 4 : idx + 6]
                        rows.append(
                            {
                                "frame_id": frame_id,
                                "ram_address": f"0x{address:08X}",
                                "xref_offset": f"0x{idx:06X}",
                                "encoding": note,
                                "op": op.hex().upper(),
                                "op_family": classify_op(op),
                            }
                        )
                    start = idx + 1

            short_offset = (address - SHORT_GP_BASE) & 0xFFFF
            if SHORT_GP_BASE - 0x8000 <= address < SHORT_GP_BASE:
                for op0 in (0x84, 0xA4):
                    pattern = bytes([op0, 0x57]) + u16le(short_offset)
                    start = 0
                    while True:
                        idx = data.find(pattern, start)
                        if idx < 0:
                            break
                        rows.append(
                            {
                                "frame_id": frame_id,
                                "ram_address": f"0x{address:08X}",
                                "xref_offset": f"0x{idx:06X}",
                                "encoding": f"short-gp base=0x{SHORT_GP_BASE:08X} off=0x{short_offset:04X}",
                                "op": pattern[:2].hex().upper(),
                                "op_family": classify_op(pattern[:2]),
                            }
                        )
                        start = idx + 1
    return rows


def classify_op(op: bytes) -> str:
    if len(op) != 2:
        return "unknown"
    if op[1] == 0x57:
        if op[0] in (0x84, 0xA4):
            return "short-gp-load-like-observed"
        return "load-like-observed"
    if op[1] in (0x67, 0x77):
        return "store-like-candidate"
    return "other"


def main() -> int:
    data = FLASH.read_bytes()
    by_frame = load_anchor_addresses()
    rows = scan(data, by_frame)
    OUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=["frame_id", "ram_address", "xref_offset", "encoding", "op", "op_family"],
        )
        writer.writeheader()
        writer.writerows(rows)

    print(f"wrote {len(rows)} xrefs to {OUT_CSV}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
