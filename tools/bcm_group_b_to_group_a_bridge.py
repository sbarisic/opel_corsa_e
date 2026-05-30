#!/usr/bin/env python3
"""Look for direct references from Group A builders to Group B RAM structs."""

from __future__ import annotations

import csv
from collections import defaultdict
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
FLASH = REPO_ROOT / "data" / "raw" / "bcm_p_flash.bin"
GROUP_A = REPO_ROOT / "analysis" / "generated" / "bcm_group_a_object_metadata.csv"
RAM_MAP = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_window_ram_map.csv"
OUT_CSV = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_to_group_a_bridge.csv"
OUT_MD = REPO_ROOT / "analysis" / "generated" / "bcm_group_b_to_group_a_bridge.md"
SHORT_GP_BASE = 0x04000000
BUILDER_RADIUS = 0x500
FOCUS_GROUP_A_IDS = {"0x1F1", "0x160", "0x0F1", "0x12A", "0x451", "0x4E1", "0x514", "0x135", "0x137", "0x139"}
SHORT_GP_OP0 = {0x24, 0x25, 0x26, 0x2A, 0x2E, 0x2F, 0x84, 0x85, 0x86, 0xA4, 0xA5, 0xAD, 0xAF, 0xE3, 0xE5, 0xEA}
RAM_OP1 = {0x47, 0x4F, 0x57, 0x5F, 0x67, 0x6F, 0x77, 0x7F, 0x87, 0x8F}


def u16le(value: int) -> bytes:
    return bytes([value & 0xFF, (value >> 8) & 0xFF])


def s16(value: int) -> int:
    return value - 0x10000 if value >= 0x8000 else value


def load_focus_ram_addresses() -> set[int]:
    counts: dict[int, int] = defaultdict(int)
    with RAM_MAP.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            ram = int(row["ram_address"], 16)
            if 0x03FF0000 <= ram < 0x04000000:
                counts[ram] += 1
    return {ram for ram, count in counts.items() if count >= 2}


def load_group_a_builders() -> list[dict[str, object]]:
    builders: list[dict[str, object]] = []
    with GROUP_A.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            if row["frame_id"] not in FOCUS_GROUP_A_IDS:
                continue
            for col in ("aux_func_a", "aux_func_b"):
                if not row.get(col):
                    continue
                builder = int(row[col], 16)
                builders.append(
                    {
                        "frame_id": row["frame_id"],
                        "builder": builder,
                        "builder_kind": col,
                        "payload_ram_ptr": row["payload_ram_ptr"],
                    }
                )
    return builders


def op_family(op: bytes) -> str:
    if len(op) != 2:
        return "unknown"
    if op[1] == 0x57:
        return "load-family"
    if op[1] in (0x5F, 0x67, 0x6F, 0x77, 0x7F, 0x87, 0x8F):
        return "write-or-bitop-family"
    if op[1] in (0x47, 0x4F):
        return "other-ram-family"
    return "unknown"


def scan_for_addresses(data: bytes, addresses: set[int]) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    seen: set[tuple[int, int, str]] = set()
    pos = 0
    while pos + 8 <= len(data):
        if data[pos] == 0x40 and data[pos + 1] == 0x56:
            high = data[pos + 2] | (data[pos + 3] << 8)
            op = data[pos + 4 : pos + 6]
            raw = data[pos + 6] | (data[pos + 7] << 8)
            ram = (high << 16) + s16(raw)
            if ram in addresses and op[1] in RAM_OP1:
                key = (ram, pos, "absolute")
                if key not in seen:
                    seen.add(key)
                    rows.append(
                        {
                            "ram_address": f"0x{ram:08X}",
                            "access_offset": f"0x{pos:06X}",
                            "kind": "absolute",
                            "op": op.hex().upper(),
                            "op_family": op_family(op),
                        }
                    )
            pos += 2
            continue

        if data[pos] in SHORT_GP_OP0 and data[pos + 1] in RAM_OP1:
            op = data[pos : pos + 2]
            raw = data[pos + 2] | (data[pos + 3] << 8)
            ram = SHORT_GP_BASE + s16(raw)
            if ram in addresses:
                key = (ram, pos, "short-gp")
                if key not in seen:
                    seen.add(key)
                    rows.append(
                        {
                            "ram_address": f"0x{ram:08X}",
                            "access_offset": f"0x{pos:06X}",
                            "kind": "short-gp",
                            "op": op.hex().upper(),
                            "op_family": op_family(op),
                        }
                    )
        pos += 1
    return rows


def nearest_builder(access: int, builders: list[dict[str, object]]) -> tuple[dict[str, object] | None, int]:
    if not builders:
        return None, 0
    best = min(builders, key=lambda item: abs(access - int(item["builder"])))
    return best, abs(access - int(best["builder"]))


def main() -> int:
    data = FLASH.read_bytes()
    builders = load_group_a_builders()
    focus_ram = sorted(load_focus_ram_addresses())
    rows: list[dict[str, str]] = []

    for hit in scan_for_addresses(data, set(focus_ram)):
        access = int(hit["access_offset"], 16)
        builder, distance = nearest_builder(access, builders)
        rows.append(
            {
                "ram_address": hit["ram_address"],
                "access_offset": hit["access_offset"],
                "kind": hit["kind"],
                "op": hit["op"],
                "op_family": hit["op_family"],
                "nearest_group_a_frame": str(builder["frame_id"]) if builder else "",
                "nearest_group_a_builder": f"0x{int(builder['builder']):06X}" if builder else "",
                "nearest_group_a_builder_kind": str(builder["builder_kind"]) if builder else "",
                "distance_to_builder": f"0x{distance:X}",
                "near_focus_builder": "yes" if distance <= BUILDER_RADIUS else "no",
            }
        )

    OUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=[
                "ram_address",
                "access_offset",
                "kind",
                "op",
                "op_family",
                "nearest_group_a_frame",
                "nearest_group_a_builder",
                "nearest_group_a_builder_kind",
                "distance_to_builder",
                "near_focus_builder",
            ],
        )
        writer.writeheader()
        writer.writerows(rows)

    near = [row for row in rows if row["near_focus_builder"] == "yes"]
    kind_counts: defaultdict[str, int] = defaultdict(int)
    frame_counts: defaultdict[str, int] = defaultdict(int)
    for row in rows:
        kind_counts[row["kind"]] += 1
    for row in near:
        frame_counts[row["nearest_group_a_frame"]] += 1
    lines = [
        "# BCM Group B To Group A Direct Bridge Scan",
        "",
        "Generated by `tools/bcm_group_b_to_group_a_bridge.py`.",
        "This checks whether RAM addresses from the decoded Group B target",
        "structs are directly referenced near known IPC/body Group A builders.",
        "",
        f"- Focus RAM anchors scanned: {len(focus_ram)}",
        f"- Total structured access hits: {len(rows)}",
        f"- Hits within 0x{BUILDER_RADIUS:X} bytes of a focus Group A builder: {len(near)}",
        f"- Access kinds: {', '.join(f'{kind}={count}' for kind, count in sorted(kind_counts.items())) or 'none'}",
        "",
    ]

    if near:
        lines.extend(["## Near Hit Counts By Group A Frame", ""])
        for frame, count in sorted(frame_counts.items()):
            lines.append(f"- `{frame}`: {count}")

        lines.extend(["", "## Near Group A Builders", "", "| RAM | access | kind | op | family | Group A frame | builder | distance |", "|---:|---:|---|---|---|---:|---:|---:|"])
        for row in near:
            lines.append(
                f"| `{row['ram_address']}` | `{row['access_offset']}` | `{row['kind']}` | `{row['op']}` | `{row['op_family']}` | `{row['nearest_group_a_frame']}` | `{row['nearest_group_a_builder']}` | `{row['distance_to_builder']}` |"
            )
    else:
        lines.extend(
            [
                "No direct references landed near the current focus Group A builders.",
                "That suggests the ECU-derived Group B state is probably bridged through",
                "intermediate helper/state layers, not read directly inside the final",
                "Group A payload builders.",
            ]
        )

    OUT_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {len(rows)} bridge rows to {OUT_CSV} and {OUT_MD}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
