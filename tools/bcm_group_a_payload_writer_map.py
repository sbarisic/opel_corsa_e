#!/usr/bin/env python3
"""Map structured RAM accesses to Group A IPC/body payload buffers."""

from __future__ import annotations

import csv
from collections import Counter, defaultdict
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
FLASH = REPO_ROOT / "data" / "raw" / "bcm_p_flash.bin"
GROUP_A = REPO_ROOT / "analysis" / "generated" / "bcm_group_a_object_metadata.csv"
OUT_CSV = REPO_ROOT / "analysis" / "generated" / "bcm_group_a_payload_access_map.csv"
OUT_MD = REPO_ROOT / "analysis" / "generated" / "bcm_group_a_payload_access_map.md"
SHORT_GP_BASE = 0x04000000
BUILDER_RADIUS = 0x500
FOCUS_GROUP_A_IDS = {
    "0x514",
    "0x4E1",
    "0x451",
    "0x1F1",
    "0x160",
    "0x139",
    "0x137",
    "0x135",
    "0x12A",
    "0x0F1",
}
SHORT_GP_OP0 = {0x24, 0x25, 0x26, 0x2A, 0x2E, 0x2F, 0x84, 0x85, 0x86, 0xA4, 0xA5, 0xAD, 0xAF, 0xE3, 0xE5, 0xEA}
RAM_OP1 = {0x47, 0x4F, 0x57, 0x5F, 0x67, 0x6F, 0x77, 0x7F, 0x87, 0x8F}


def s16(value: int) -> int:
    return value - 0x10000 if value >= 0x8000 else value


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


def load_focus_rows() -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    with GROUP_A.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            if row["frame_id"] not in FOCUS_GROUP_A_IDS or not row.get("payload_ram_ptr"):
                continue
            builders = []
            for col in ("aux_func_a", "aux_func_b"):
                if row.get(col):
                    builders.append((col, int(row[col], 16)))
            rows.append(
                {
                    "frame_id": row["frame_id"],
                    "index": row["index"],
                    "payload_ram_ptr": int(row["payload_ram_ptr"], 16),
                    "dlc": int(row["dlc"] or "8"),
                    "builders": builders,
                }
            )
    return rows


def payload_address_map(rows: list[dict[str, object]]) -> dict[int, list[dict[str, object]]]:
    by_address: dict[int, list[dict[str, object]]] = defaultdict(list)
    for row in rows:
        base = int(row["payload_ram_ptr"])
        dlc = int(row["dlc"])
        for byte_index in range(dlc):
            by_address[base + byte_index].append({**row, "byte_index": byte_index})
    return by_address


def nearest_builder(access: int, rows: list[dict[str, object]]) -> tuple[str, str, int | None, int | None]:
    best: tuple[str, str, int, int] | None = None
    for row in rows:
        for kind, builder in row["builders"]:  # type: ignore[index]
            distance = abs(access - int(builder))
            if best is None or distance < best[3]:
                best = (str(row["frame_id"]), str(kind), int(builder), distance)
    if best is None:
        return "", "", None, None
    return best


def add_hit(rows: list[dict[str, str]], access: int, kind: str, op: bytes, ram: int, targets: list[dict[str, object]], focus_rows: list[dict[str, object]]) -> None:
    near_frame, near_kind, near_builder, distance = nearest_builder(access, focus_rows)
    for target in targets:
        rows.append(
            {
                "frame_id": str(target["frame_id"]),
                "index": str(target["index"]),
                "payload_ram_ptr": f"0x{int(target['payload_ram_ptr']):08X}",
                "byte_index": str(target["byte_index"]),
                "ram_address": f"0x{ram:08X}",
                "access_offset": f"0x{access:06X}",
                "kind": kind,
                "op": op.hex().upper(),
                "op_family": op_family(op),
                "nearest_group_a_frame": near_frame,
                "nearest_group_a_builder_kind": near_kind,
                "nearest_group_a_builder": f"0x{near_builder:06X}" if near_builder is not None else "",
                "distance_to_builder": f"0x{distance:X}" if distance is not None else "",
                "near_focus_builder": "yes" if distance is not None and distance <= BUILDER_RADIUS else "no",
            }
        )


def scan(data: bytes, address_map: dict[int, list[dict[str, object]]], focus_rows: list[dict[str, object]]) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    seen: set[tuple[int, int, str]] = set()
    pos = 0
    while pos + 8 <= len(data):
        if data[pos] == 0x40 and data[pos + 1] == 0x56:
            high = data[pos + 2] | (data[pos + 3] << 8)
            op = data[pos + 4 : pos + 6]
            raw = data[pos + 6] | (data[pos + 7] << 8)
            ram = (high << 16) + s16(raw)
            if ram in address_map and op[1] in RAM_OP1:
                key = (ram, pos, "absolute")
                if key not in seen:
                    seen.add(key)
                    add_hit(rows, pos, "absolute", op, ram, address_map[ram], focus_rows)
            pos += 2
            continue

        if data[pos] in SHORT_GP_OP0 and data[pos + 1] in RAM_OP1:
            op = data[pos : pos + 2]
            raw = data[pos + 2] | (data[pos + 3] << 8)
            ram = SHORT_GP_BASE + s16(raw)
            if ram in address_map:
                key = (ram, pos, "short-gp")
                if key not in seen:
                    seen.add(key)
                    add_hit(rows, pos, "short-gp", op, ram, address_map[ram], focus_rows)
        pos += 1
    return rows


def write_outputs(rows: list[dict[str, str]], focus_rows: list[dict[str, object]]) -> None:
    OUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    fields = [
        "frame_id",
        "index",
        "payload_ram_ptr",
        "byte_index",
        "ram_address",
        "access_offset",
        "kind",
        "op",
        "op_family",
        "nearest_group_a_frame",
        "nearest_group_a_builder_kind",
        "nearest_group_a_builder",
        "distance_to_builder",
        "near_focus_builder",
    ]
    with OUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)

    near = [row for row in rows if row["near_focus_builder"] == "yes"]
    frame_counts = Counter(row["frame_id"] for row in rows)
    near_counts = Counter(row["frame_id"] for row in near)
    op_counts = Counter(row["op_family"] for row in rows)
    lines = [
        "# BCM Group A Payload Access Map",
        "",
        "Generated by `tools/bcm_group_a_payload_writer_map.py`.",
        "This scans firmware for recognized RAM-access instruction shapes that",
        "touch the selected Group A IPC/body payload buffers directly.",
        "",
        f"- Focus Group A rows: {len(focus_rows)}",
        f"- Payload byte addresses scanned: {sum(int(row['dlc']) for row in focus_rows)}",
        f"- Total structured payload-buffer hits: {len(rows)}",
        f"- Hits within 0x{BUILDER_RADIUS:X} bytes of a focus Group A builder: {len(near)}",
        f"- Access families: {', '.join(f'{name}={count}' for name, count in sorted(op_counts.items())) or 'none'}",
        "",
        "## Hits By Frame",
        "",
        "| Frame | All hits | Near builder hits |",
        "|---:|---:|---:|",
    ]
    for frame in sorted(FOCUS_GROUP_A_IDS):
        lines.append(f"| `{frame}` | {frame_counts[frame]} | {near_counts[frame]} |")

    if near:
        lines.extend(["", "## Near Builder Hits", "", "| Frame | byte | RAM | access | op | family | nearest builder | distance |", "|---:|---:|---:|---:|---|---|---:|---:|"])
        for row in near[:120]:
            lines.append(
                f"| `{row['frame_id']}` | {row['byte_index']} | `{row['ram_address']}` | `{row['access_offset']}` | `{row['op']}` | `{row['op_family']}` | `{row['nearest_group_a_builder']}` | `{row['distance_to_builder']}` |"
            )
    else:
        lines.extend(
            [
                "",
                "No structured direct accesses landed near the selected final builders.",
                "That suggests the builders mostly write via pointer-oriented helper",
                "functions rather than direct absolute/short-GP stores to payload bytes.",
            ]
        )

    OUT_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    focus_rows = load_focus_rows()
    rows = scan(FLASH.read_bytes(), payload_address_map(focus_rows), focus_rows)
    write_outputs(rows, focus_rows)
    print(f"wrote {len(rows)} payload access rows to {OUT_CSV} and {OUT_MD}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
