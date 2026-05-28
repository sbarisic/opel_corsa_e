# BCM CAN Gateway Disassembly Notes

This note tracks the BCM program-flash work that is directly useful for IPC and
dashboard simulation. It intentionally focuses on CAN gateway behavior rather
than immobilizer/security or a full firmware map.

## Repeatable Analysis

Run from the repository root:

```powershell
python tools\bcm_gateway_analysis.py
```

Default inputs:

- `data/raw/bcm_p_flash.bin`
- `data/can_logs/*`

Generated outputs:

- `analysis/generated/bcm_can_tables_decoded.csv`
- `analysis/generated/bcm_can_id_occurrences.csv`
- `analysis/generated/bcm_dispatch_table_candidates.csv`
- `analysis/generated/bcm_filter_mask_table_candidates.csv`
- `analysis/generated/bcm_gateway_xref_candidates.csv`
- `analysis/generated/bcm_group_a_tx_trace.csv`
- `analysis/generated/bcm_group_b_rx_trace.csv`
- `analysis/generated/bcm_can_helper_callsites.csv`
- `analysis/generated/bcm_low_speed_static_message_map.md`
- `analysis/generated/bcm_candidate_code_windows.md`

The first WSL/radare2 disassembly pass is captured in
`analysis/BCM_R2_DISASSEMBLY.md`.

The script asserts the current table anchors before writing outputs:

- Group A contains diagnostic response-style ID `0x641`.
- Group B contains `0x241`, `0x0C9`, `0x3E9`, `0x4C1`, and `0x4D1`.
- Parsed logs include the core ECU stream frames and native body frames from
  `INFO.md`.

## Current Findings

The BCM flash is `0x180000` bytes. The most useful confirmed CAN table format is
a little-endian 32-bit encoded value:

```text
raw_value = standard_id << 18
frame_id = (raw_value >> 18) & 0x7ff
flags_or_variant = raw_value & 0x3ffff
```

Known table groups:

| Group | Offset | Count | Working inference |
|---|---:|---:|---|
| Group A | `0x01DE74` | 131 | BCM transmit / periodic output / body-base table |
| Group B | `0x01E830` | 148 | BCM receive filter / input table |

The current script run decoded 279 primary CAN table entries, 514 dispatch
records, 24 focused filter/mask records, 68 helper callsites in focused r2
scan ranges, and parsed 83 CAN IDs from 8 log files.

Important correction: the earlier exact `CAN_ID << 18` hits outside Group A/B
are not executable code anchors. They are inside structured data tables:

| ID | Offset | Current context |
|---:|---:|---|
| `0x1F1` | `0x01770A` | Filter/mask table A |
| `0x4D1` | `0x022246` | Unaligned bytes crossing a dispatch-table object key and handler address |
| `0x160` | `0x02249E` | Unaligned bytes crossing a dispatch-table object key and handler address |
| `0x1F1` | `0x025D52` | Filter/mask table B |

`analysis/generated/bcm_candidate_code_windows.md` now reports no priority CAN
constants outside known structured tables.

The dispatch table starts at `0x021A40` and has 514 records:

```text
u32 object_key
u32 handler_address
u32 entry_class
```

For many Group A entries, `object_key` equals `raw_value >> 16`, and the handler
address points to tiny V850 wrapper functions around `0x130xxx..0x134xxx`.
Example:

```text
0x022244: object_key 0x906F -> handler 0x00131344 -> class 1
```

This record matches Group A object variants around `0x41B`, not Group B `0x4D1`.
The apparent `0x4D1` constant at `0x022246` is just an unaligned byte overlap.

## Static Low-Speed Message Model

`analysis/generated/bcm_low_speed_static_message_map.md` is the current
static-only low-speed map. It shows:

- Group A table-confirmed IPC/body TX candidates and log-derived rates.
- Group B table-confirmed ECU RX candidates and log-derived rates.
- Focused r2 callsites for helpers `0x34BD2` and `0x34AAE`.
- Representative wrapper, payload writer, and CAN helper disassembly snippets.

Current confidence level:

- `0x160`, `0x1F1`, `0x0F1`, `0x12A`, `0x135`, `0x137`, `0x139`, `0x451`,
  `0x4E1`, and `0x514` are confirmed by Group A table entries and observed
  native/body logs.
- Their direct scheduler/dispatch links remain unresolved statically; the
  standard-ID object keys do not map directly into the `0x021A40` dispatch
  table by `raw_value >> 16`.
- `0x0C9`, `0x3E9`, `0x4C1`, `0x4D1`, and `0x3D1` are confirmed by Group B RX
  entries and ECU-side logs, but their consumers still need RX interrupt/buffer
  tracing.

## Architecture Guess

The first r2 pass confirms this is V850-family code. The working part-level
guess remains Renesas/NEC V850E2/FK4, likely near `uPD70F3558M1`, because that
family matches the 1.5 MB program-flash size. Local evidence:

- The first vector/header area contains several big-endian-looking mapped
  address values in the `0x80000000..0x80180000` range.
- The CAN table constants are little-endian 32-bit values.
- r2 decodes `0x8260` as V850 vector/stub code, including `jr 0x26848`,
  `jr 0x2e66e`, and repeated `reti`.
- r2 decodes `0x26848`, `0x2e66e`, `0x2e88c`, `0x2e940`, and `0x34bd2` as
  coherent V850 code.

Use raw file offsets first in r2/Ghidra. Keep `0x80000000 + file_offset` as a
secondary mapped-address guess for pointer-like values.

## Manual Reversing Targets

Prioritize these ECU-side receive/filter paths:

- `0x0C9`: RPM / engine-running / cruise bits.
- `0x3E9`: vehicle speed and odometer-related data.
- `0x4C1`: coolant temperature and thermal warning data.
- `0x4D1`: oil pressure, oil level, fuel/status warnings.
- `0x3D1`: limiter/display-status behavior.

Prioritize these BCM/body transmit or IPC-facing candidates:

- Wake/keepalive: `0x160`, `0x1F1`, `0x0F1`, `0x451`.
- Body/display state: `0x12A`, `0x135`, `0x137`, `0x139`.
- VIN/config fragments: `0x4E1`, `0x514`.

Next manual steps:

1. In a disassembler, import `bcm_p_flash.bin` with tentative base
   `0x80000000`.
2. Label Group A and Group B table ranges and the non-table priority constants.
3. Label the dispatch table at `0x021A40` and handler wrappers such as
   `0x00131344`, `0x00130580`, and `0x001305B4`.
4. Find consumers of Group B entries for `0x0C9`, `0x3E9`, `0x4C1`, `0x4D1`,
   and `0x3D1`.
5. Follow those consumers to any buffers, scaling code, or scheduler functions
   feeding Group A entries near `0x160`, `0x1F1`, and the IPC/body wake set.
6. If direct IPC replay fails on the bench, use these anchors to compare real
   IPC-side captures against Group A transmit candidates.
