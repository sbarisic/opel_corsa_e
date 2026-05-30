#!/usr/bin/env python3
"""Mine all CAN ID changes across saved Corsa E key-state captures."""

from __future__ import annotations

import csv
import re
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
LOG_DIR = REPO_ROOT / "data" / "can_logs"
OUT_MD = REPO_ROOT / "analysis" / "generated" / "can_state_id_deltas.md"
OUT_CSV = REPO_ROOT / "analysis" / "generated" / "can_state_id_deltas.csv"
LINE_RE = re.compile(r"^\((?P<ts>[0-9.]+)\)\s+\S+\s+(?P<id>[0-9A-Fa-f]+)#(?P<data>[0-9A-Fa-f]*)")


@dataclass(frozen=True)
class StateCapture:
    state: str
    file_name: str


STATE_CAPTURES = [
    StateCapture("keyoff", "corsa_e_opc_ecumaster_bcm_combined_keyoff.candump"),
    StateCapture("keyon", "corsa_e_opc_ecumaster_bcm_combined_keyon.candump"),
    StateCapture("keyon_startcar", "corsa_e_opc_ecumaster_bcm_combined_keyon_startcar.candump"),
    StateCapture("engine_running", "corsa_e_opc_ecumaster_read_engine_running.candump"),
]


def parse_log(path: Path) -> dict[str, list[tuple[float, str]]]:
    rows: dict[str, list[tuple[float, str]]] = defaultdict(list)
    with path.open("r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            match = LINE_RE.match(line.strip())
            if not match:
                continue
            rows[match.group("id").upper()].append((float(match.group("ts")), match.group("data").upper()))
    return rows


def duration_seconds(rows: dict[str, list[tuple[float, str]]]) -> float:
    timestamps = [ts for samples in rows.values() for ts, _ in samples]
    if len(timestamps) < 2:
        return 0.0
    return max(timestamps) - min(timestamps)


def top_payload(samples: list[tuple[float, str]]) -> tuple[str, int]:
    if not samples:
        return "", 0
    return Counter(data for _, data in samples).most_common(1)[0]


def payload_preview(samples: list[tuple[float, str]], limit: int = 3) -> str:
    if not samples:
        return ""
    return ", ".join(f"{payload} x{count}" for payload, count in Counter(data for _, data in samples).most_common(limit))


def byte_delta(base: str, other: str) -> str:
    if not base or not other:
        return ""
    base_bytes = bytes.fromhex(base)
    other_bytes = bytes.fromhex(other)
    parts = []
    for index, (left, right) in enumerate(zip(base_bytes, other_bytes)):
        if left != right:
            parts.append(f"b{index}:{left:02X}->{right:02X}")
    if len(base_bytes) != len(other_bytes):
        parts.append(f"dlc:{len(base_bytes)}->{len(other_bytes)}")
    return ", ".join(parts) if parts else "same"


def score_row(counts: dict[str, int], rates: dict[str, float], top_by_state: dict[str, str]) -> tuple[int, str]:
    keyoff = counts.get("keyoff", 0)
    keyon = counts.get("keyon", 0)
    running = counts.get("engine_running", 0)
    startcar = counts.get("keyon_startcar", 0)

    if keyoff == 0 and (keyon or startcar or running):
        return 1000 + keyon + startcar + running, "appears_after_keyoff"

    if keyoff and (keyon == 0 and startcar == 0 and running == 0):
        return 800 + keyoff, "keyoff_only"

    if keyoff and rates.get("keyon", 0) > rates.get("keyoff", 0) * 2:
        return 700 + int(rates["keyon"]), "keyon_rate_increase"

    if keyoff and top_by_state.get("keyoff") and top_by_state.get("keyon") and top_by_state["keyoff"] != top_by_state["keyon"]:
        return 600 + keyon, "dominant_payload_changes_keyon"

    if keyoff and top_by_state.get("keyoff") and top_by_state.get("engine_running") and top_by_state["keyoff"] != top_by_state["engine_running"]:
        return 500 + running, "dominant_payload_changes_running"

    return counts.get("keyoff", 0) + counts.get("keyon", 0) + counts.get("engine_running", 0), "stable_or_low_signal"


def main() -> int:
    parsed_by_state: dict[str, dict[str, list[tuple[float, str]]]] = {}
    duration_by_state: dict[str, float] = {}
    for capture in STATE_CAPTURES:
        path = LOG_DIR / capture.file_name
        if not path.exists():
            raise FileNotFoundError(path)
        parsed = parse_log(path)
        parsed_by_state[capture.state] = parsed
        duration_by_state[capture.state] = max(duration_seconds(parsed), 0.001)

    all_ids = sorted({can_id for rows in parsed_by_state.values() for can_id in rows})
    rows = []
    for can_id in all_ids:
        counts = {state.state: len(parsed_by_state[state.state].get(can_id, [])) for state in STATE_CAPTURES}
        rates = {state.state: counts[state.state] / duration_by_state[state.state] for state in STATE_CAPTURES}
        top_payloads = {state.state: top_payload(parsed_by_state[state.state].get(can_id, []))[0] for state in STATE_CAPTURES}
        top_counts = {state.state: top_payload(parsed_by_state[state.state].get(can_id, []))[1] for state in STATE_CAPTURES}
        score, reason = score_row(counts, rates, top_payloads)
        row = {
            "id": can_id,
            "frame_type": "extended" if len(can_id) > 3 else "standard",
            "score": score,
            "reason": reason,
            **{f"{state}_count": counts[state] for state in counts},
            **{f"{state}_rate_hz": f"{rates[state]:.3f}" for state in rates},
            **{f"{state}_top": top_payloads[state] for state in top_payloads},
            **{f"{state}_top_count": top_counts[state] for state in top_counts},
            "keyon_delta": byte_delta(top_payloads.get("keyoff", ""), top_payloads.get("keyon", "")),
            "running_delta": byte_delta(top_payloads.get("keyoff", ""), top_payloads.get("engine_running", "")),
            "keyon_preview": payload_preview(parsed_by_state["keyon"].get(can_id, [])),
            "running_preview": payload_preview(parsed_by_state["engine_running"].get(can_id, [])),
        }
        rows.append(row)

    rows.sort(key=lambda item: (-int(item["score"]), item["id"]))

    OUT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with OUT_CSV.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()) if rows else ["id"])
        writer.writeheader()
        writer.writerows(rows)

    lines = [
        "# CAN State ID Deltas",
        "",
        "Generated by `tools/can_state_id_delta_miner.py` from saved Corsa E",
        "key-state captures. Rows are ranked by appearance, rate, and dominant",
        "payload changes from keyoff to keyon/engine-running.",
        "",
        "## Top Ranked IDs",
        "",
        "| ID | Type | Reason | Counts keyoff/keyon/start/running | Dominant keyoff -> keyon | Keyon delta | Dominant running |",
        "|---:|---|---|---:|---|---|---|",
    ]

    for row in rows[:40]:
        counts_text = f"{row['keyoff_count']}/{row['keyon_count']}/{row['keyon_startcar_count']}/{row['engine_running_count']}"
        dominant = f"`{row['keyoff_top']}` -> `{row['keyon_top']}`"
        running = f"`{row['engine_running_top']}`"
        lines.append(
            f"| `{row['id']}` | {row['frame_type']} | {row['reason']} | {counts_text} | {dominant} | {row['keyon_delta']} | {running} |"
        )

    lines.extend([
        "",
        "## Notes",
        "",
        "- `appears_after_keyoff` IDs are useful only if they are on the same physical",
        "  bus as the IPC bench wiring. Treat high-speed ECU-side IDs as gateway inputs,",
        "  not direct IPC-side proof.",
        "- `dominant_payload_changes_keyon` IDs are strong body-context replay",
        "  candidates, especially when they are already known low-speed/body IDs.",
        f"- Full CSV: `{OUT_CSV.relative_to(REPO_ROOT)}`",
    ])

    OUT_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote CAN state ID deltas to {OUT_MD}")
    print(f"wrote CAN state ID deltas CSV to {OUT_CSV}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
