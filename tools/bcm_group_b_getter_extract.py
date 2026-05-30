#!/usr/bin/env python3
"""Extract BCM Group B wrapper getter chains from the program flash.

This is intentionally byte-pattern based. The handlers use a compact and
regular V850 block template:

  callt 9 ... jarl getter ... jarl writer ... callt 31

The script decodes the relative jarl targets and classifies simple getter
functions that begin with an absolute RAM load pattern.
"""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
FLASH = REPO_ROOT / "data" / "raw" / "bcm_p_flash.bin"
OUT_CSV = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_wrapper_getters.csv"
SHORT_GP_BASE = 0x04000000


TARGETS = [
    ("0x0C9", 145, 0x131BBC),
    ("0x3E9", 95, 0x13142E),
    ("0x3D1", 97, 0x131CDA),
    ("0x4C1", 92, 0x131D42),
    ("0x4D1", 90, 0x131D5C),
]


@dataclass(frozen=True)
class Block:
    frame_id: str
    group_b_index: int
    handler: int
    block_index: int
    block_address: int
    getter: int
    writer: int


def u16le(data: bytes, offset: int) -> int:
    return data[offset] | (data[offset + 1] << 8)


def s16(value: int) -> int:
    return value - 0x10000 if value >= 0x8000 else value


def is_block(data: bytes, pos: int) -> bool:
    return (
        data[pos] == 0x09
        and data[pos + 1] == 0x02
        and data[pos + 8] == 0xBE
        and data[pos + 9] == 0xFF
        and data[pos + 18] == 0xBF
        and data[pos + 19] == 0xFF
    )


def extract_blocks(data: bytes, frame_id: str, group_b_index: int, handler: int) -> list[Block]:
    blocks: list[Block] = []
    pos = handler
    end = min(handler + 0x120, len(data) - 24)
    while pos < end:
        if is_block(data, pos):
            getter_disp = u16le(data, pos + 10)
            writer_disp = s16(u16le(data, pos + 20))
            blocks.append(
                Block(
                    frame_id=frame_id,
                    group_b_index=group_b_index,
                    handler=handler,
                    block_index=len(blocks),
                    block_address=pos,
                    getter=pos + 8 - 0x20000 + getter_disp,
                    writer=pos + 18 + writer_disp,
                )
            )
            pos += 26
            continue
        pos += 2
    return blocks


def classify_getter(data: bytes, address: int) -> tuple[str, str, str, str]:
    """Return source_kind, op, ram_address, note."""
    if address + 8 >= len(data):
        return "out_of_range", "", "", ""

    # Observed absolute pattern, e.g.:
    # 40 56 FF 03 AA 57 BB 84 7F 00
    # high is little-endian after 40 56, offset is little-endian after load op.
    if data[address] == 0x40 and data[address + 1] == 0x56:
        high = u16le(data, address + 2)
        op = f"{data[address + 4]:02X}{data[address + 5]:02X}"
        raw_offset = u16le(data, address + 6)
        offset = s16(raw_offset)
        ram_address = (high << 16) + offset
        return "absolute", op, f"0x{ram_address:08X}", ""

    # Short/gp-like load pattern. In this BCM image these negative offsets
    # line up with the same 0x03FFxxxx RAM window when resolved against the
    # implicit 0x04000000 base used by the absolute signed-negative getters.
    if data[address] in (0x84, 0xA4) and data[address + 1] == 0x57:
        op = f"{data[address]:02X}{data[address + 1]:02X}"
        raw_offset = u16le(data, address + 2)
        offset = s16(raw_offset)
        ram_address = SHORT_GP_BASE + offset
        return "short_gp_like", op, f"0x{ram_address:08X}", f"offset 0x{raw_offset:04X} / {offset}; base 0x{SHORT_GP_BASE:08X}"

    return "computed_branchy", "", "", ""


def main() -> int:
    data = FLASH.read_bytes()
    OUT_CSV.parent.mkdir(parents=True, exist_ok=True)

    rows = []
    for frame_id, group_b_index, handler in TARGETS:
        for block in extract_blocks(data, frame_id, group_b_index, handler):
            source_kind, op, ram_address, note = classify_getter(data, block.getter)
            rows.append(
                {
                    "frame_id": frame_id,
                    "group_b_index": group_b_index,
                    "handler": f"0x{block.handler:X}",
                    "block": block.block_index,
                    "block_address": f"0x{block.block_address:X}",
                    "getter": f"0x{block.getter:X}",
                    "writer": f"0x{block.writer:X}",
                    "source_kind": source_kind,
                    "op": op,
                    "ram_address": ram_address,
                    "note": note,
                }
            )

    with OUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=[
                "frame_id",
                "group_b_index",
                "handler",
                "block",
                "block_address",
                "getter",
                "writer",
                "source_kind",
                "op",
                "ram_address",
                "note",
            ],
        )
        writer.writeheader()
        writer.writerows(rows)

    print(f"wrote {len(rows)} rows to {OUT_CSV}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
