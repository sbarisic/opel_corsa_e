# Corsa E 1.4T / GM E78A / BCM / IPC CAN reverse-engineering notes

Generated from the current reverse-engineering session. This file is intended as a restart point for later bench work with a standalone IPC cluster and an ESP32-based CAN controller.

## Goal

Make a standalone ECU, or an ECU + BCM simulator, emit enough valid CAN/GMLAN traffic for the original Corsa E IPC/dashboard to wake up and show useful data such as RPM, vehicle speed, coolant temperature, oil/fuel warnings, cruise/limiter state, and related DIC messages.

Vehicle/module context:

- Vehicle target: 2019 Corsa E / 1.4T / GM platform, 2017+ family.
- ECU: AcDelco E78A / GM E78 family.
- ECU operating system from user notes: `12683718`.
- Security key from user notes: `0x5D88ACD905`.
- ECU serial expected by user: `86ACHMK48173ZYT0`.
- Important mismatch: the E78A internal flash dump searched during this session contains `86ACHMK482221Q6X`, not `86ACHMK48173ZYT0`.
- E78A internal flash VIN found: `W0V0XEP68K4090305`.
- BCM EEPROM VIN found: `W0V0XEP68K4028819`.

The VIN mismatch is probably not important for gauge-only IPC emulation, but it may matter for diagnostics, security environment, odometer/VIN checks, and learned-module behavior.

## High-level conclusion

The known CAN logs and the static firmware analysis strongly suggest the following topology:

```text
ECU / powertrain / chassis side
        |
        | high-speed GMLAN / CAN, likely 500 kbit/s
        |
      BCM
        |
        | IPC/body side, labelled Low Speed GMLAN in pinout data
        |
      IPC / instrument cluster
```

The current logs are very useful for the high-speed ECU/BCM side. They do not yet prove the exact direct BCM-to-IPC low-speed messages. For the next phase with a standalone IPC, start by replaying the known standard 11-bit IDs directly to the IPC. If the IPC does not react, capture the real BCM-to-IPC bus directly and determine whether the BCM repacks the signals.

## Electrical / pinout notes

### IPC connector from uploaded `pinout.txt`

Use this for the standalone cluster bench.

| IPC pin | Function |
|---:|---|
| 3 | Low Speed GMLAN |
| 4 | Low Speed GMLAN |
| 7 | Battery Positive Voltage |
| 8 | Run/Crank Ignition 1 Voltage |
| 9 | Driver Information Center Select Menu Switch Signal |
| 12 | Forward Collision Alert LED Control |
| 13 | Driver Information Center Switch Low Reference |
| 14 | Driver Information Center Switch Signal |
| 19 | Ground |
| 25 | Outside Ambient Temperature Sensor Low Reference |
| 27 | Outside Ambient Air Temperature Sensor Signal |
| 30 | Reflected LED Display Dimming Control |

Pins listed as unoccupied in the uploaded pinout: `1,2,5,6,10,11,15,16,17,18,20,21,22,23,24,26,28,29,31,32`.

Important electrical uncertainty: the IPC pinout labels pins 3 and 4 as Low Speed GMLAN. On some GM platforms, “Low Speed GMLAN” means single-wire GMLAN at about 33.333 kbit/s. On this Corsa/Opel IPC, the two pin entries may mean a two-wire low-speed CAN implementation. Verify with a scope or passive sniff before assuming the transceiver type. A normal ESP32 CAN/TWAI + two-wire CAN transceiver will not correctly drive a true single-wire GMLAN bus; a single-wire transceiver is needed if it is actually SWCAN.

Practical bitrate/transceiver test order:

1. Power IPC pin 7 with fused B+, pin 8 with fused ignition/run, and pin 19 ground.
2. Connect CAN only after confirming whether pins 3/4 are a two-wire bus or a single-wire/duplicated low-speed bus.
3. If using a normal two-wire CAN transceiver, try passive listen first. Candidate bitrates to test: 500k, 125k, 95.238k, 83.333k, and 33.333k.
4. If the bus behaves like single-wire GMLAN, use a proper single-wire GMLAN/SWCAN transceiver and try 33.333k.
5. Do not drive the bus hard until the physical layer is confirmed.

### BCM reference pinout from uploaded Cruze BCM PDF

The Cruze BCM reference is not the Corsa E IPC, but it supports the GM gateway model:

- BCM X6 has high-speed GMLAN pins: X6 pin 18 = HS GMLAN +, X6 pin 19 = HS GMLAN -, and also X6 pin 24 = HS GMLAN +, X6 pin 25 = HS GMLAN -.
- BCM X7 pin 23 is Low Speed GMLAN Serial Data.

This matches the hypothesis that the BCM sits between a high-speed powertrain/chassis bus and a low-speed/body/IPC side.

### EBCM reference pinout from uploaded EBCM PDF

The EBCM reference shows multiple high-speed GMLAN connections:

- Pins 5/6: High Speed GMLAN +/− circuit 6105/6106.
- Pins 9/10/11/12: High Speed GMLAN −/−/+/+ circuit 2501/2500.

This is mainly useful as another confirmation that the brake/chassis modules are on the high-speed side.

## Files analyzed

### ECU / E78A files

`gm_delco_e78_bench_fullbackup_full_backup_20230625180754_int_flash.bin`

- Size: `0x300000` bytes / 3,145,728 bytes.
- Main useful ECU firmware/calibration image.
- Big-endian PowerPC-style code was observed.
- Contains OS ID `12683718` at offset `0x0CB010`.
- Contains VIN `W0V0XEP68K4090305` multiple times.
- Contains `86ACHMK482221Q6X` at offsets around `0x340` and `0x1C340`.
- Contains obvious CAN descriptor tables near `0x10D970` and `0x10DB90`.

`gm_delco_e78_bench_fullbackup_full_backup_20230625180754_psw.bin`

- Size: 256 bytes.
- Starts with ASCII `TPDU4;`.
- Looks like tool/security material rather than useful firmware or calibration.
- Not useful for the IPC CAN work.

`gm_delco_e78_bench_fullbackup_full_backup_20230625180754_fullbackup.mmf`

- Starts with `MMSF`.
- Looks like a proprietary container/archive for the backup.
- Since the extracted `int_flash.bin` exists, this is not the main reverse-engineering target.

### BCM files

`bcm_eeprom_pin2020.bin`

- Size: 2 KB.
- Contains VIN `W0V0XEP68K4028819` at offset `0x1E8`.
- Contains `2020` at offset `0xA7`.
- Configuration/NVM/security data, not executable firmware.

`bcm_p_flash.bin`

- Size: 1.5 MB.
- BCM program flash.
- Contains CAN-related descriptor/filter tables.
- Does not expose IPC payload logic as cleanly as the E78A ECU flash.

### Logs

The logs split into two useful sets:

1. ECU/standalone powertrain stream, especially `corsa_e_no_ecu_streams.candump`.
2. Native body/BCM/high-speed base traffic, especially `corsa_e_opc_ecumaster_read.candump` and `corsa_e_opc_ecumaster_read_engine_running.candump`.

The combined logs are effectively the union of the standalone ECU stream and the native body/BCM base stream.

Important: these logs appear to be high-speed-side logs. They should not be assumed to be the exact BCM-to-IPC low-speed traffic until confirmed at the IPC connector or BCM low-speed pin.

## Generated helper files from this session

The following CSVs were generated during analysis and are useful lookup tables:

- `e78a_candidate_can_tables.csv` — candidate E78A CAN descriptor tables.
- `e78a_shifted_id_dispatch_candidates.csv` — candidate shifted-ID dispatch references.
- `bcm_can_tables_decoded.csv` — decoded BCM candidate CAN TX/RX/filter tables.
- `bcm_rx_to_tx_static_correlations.csv` — static code-reference hints linking BCM RX and TX buffers.
- `corsa_e_can_log_file_summary.csv` — per-file log summary.
- `corsa_e_can_log_id_summary.csv` — per-ID timing, payload, DLC, and frequency summary.
- `corsa_e_can_id_presence_matrix.csv` — which IDs appear in which logs.
- `corsa_e_bcm_hs_replay_schedule_candidates.csv` — candidate high-speed replay schedule.
- `corsa_e_bcm_sim_initial_schedule.csv` — reduced first-pass emulator schedule.
- `corsa_e_can_log_source_classification.csv` — inferred source class per CAN ID.

## Static firmware findings

### E78A ECU CAN table

The E78A internal flash contains an obvious CAN descriptor format:

```text
00 00 <CAN_ID> <DLC> <direction_or_flag> 00 00
```

In the diagnostic descriptors, ECU diagnostic responses such as `0x7E8` and `0x5E8` use flag `1`, while diagnostic requests such as `0x7E0` and `0x7DF` use flag `0`. Therefore, flag `1` is likely ECU transmit and flag `0` is likely ECU receive.

Important E78A ECU transmit/dashboard-relevant IDs include:

```text
0x0C9, 0x0D3, 0x0F9,
0x1A1, 0x1A3,
0x1BC, 0x1BD,
0x1C3, 0x1C4, 0x1C5,
0x1F4, 0x1F5,
0x2C3, 0x2D1, 0x2D3,
0x3C1, 0x3D1, 0x3D3, 0x3D9, 0x3DC, 0x3E9, 0x3F9, 0x3FB, 0x3FC,
0x4C1, 0x4D1, 0x4EB, 0x4ED, 0x4EF, 0x4F1,
0x589, 0x772
```

The screenshot/symbol view supplied by the user also showed named E78 normal transmit objects such as:

```text
KaCANG_NormTransmitMsgObj[CeCANG_e_TxMsg_0C9_BusA]
KaCANG_NormTransmitMsgObj[CeCANG_e_TxMsg_3D1_BusA]
KaCANG_NormTransmitMsgObj[CeCANG_e_TxMsg_3E9_BusA]
KaCANG_NormTransmitMsgObj[CeCANG_e_TxMsg_4C1_BusA]
KaCANG_NormTransmitMsgObj[CeCANG_e_TxMsg_4D1_BusA]
```

### BCM CAN tables

The BCM program flash contains at least two large CAN descriptor groups:

| Group | Offset | Count | Current inference |
|---|---:|---:|---|
| Group A | `0x01DE74` | 131 | BCM transmit / periodic output / body-base table |
| Group B | `0x01E830` | 148 | BCM receive filter / input table |

Standard 11-bit IDs are encoded as:

```text
raw_value = standard_id << 18
```

Examples:

```text
0x0C9 << 18 = 0x03240000
0x3E9 << 18 = 0x0FA40000
0x4C1 << 18 = 0x13040000
0x4D1 << 18 = 0x13440000
```

The direction inference comes from diagnostics and log matching:

- Group A includes `0x641`, the likely BCM diagnostic response ID, and many IDs that appear as native body/base traffic in logs.
- Group B includes `0x241`, the likely BCM diagnostic request ID, and known ECU-side input IDs such as `0x0C9`, `0x3E9`, `0x4C1`, and `0x4D1`.

Important Group B / BCM receive candidates from the ECU side:

```text
0x0C9, 0x1F5, 0x348, 0x3D1, 0x3E9, 0x3F9, 0x4C1, 0x4D1, 0x4F1
```

Important Group A / BCM transmit or native body candidates:

```text
0x0F1, 0x100, 0x120, 0x12A, 0x130, 0x135, 0x137, 0x139, 0x140, 0x142, 0x160,
0x1E1, 0x1F1, 0x1F3, 0x1F9,
0x32A, 0x3C9, 0x3CB, 0x3E7, 0x3F1,
0x451, 0x4C5, 0x4D7, 0x4E1, 0x4E9,
0x514, 0x52A, 0x530, 0x541, 0x641, 0x7F9
```

There are also flagged/extended-looking raw IDs in the BCM static table. The strongest static RX→TX candidates were:

| HS/RX candidate | Static TX candidate | Comment |
|---:|---:|---|
| `0x4D1` | extended `0x10210000` | Static buffer-reference link; not seen in real HS logs. |
| `0x3D1` | extended `0x10264000` | Static link; not yet confirmed on IPC side. |
| `0x3D1` | extended `0x10220000` | Static link; not yet confirmed on IPC side. |
| `0x4F1` | extended `0x102CC000` | Static link; not yet confirmed on IPC side. |

Because the real logs did not show these extended IDs, do not start with them for direct IPC replay. Keep them as a secondary hypothesis in case a real IPC-side sniff shows extended frames.

## CAN log conclusions

### Clean ECU/standalone stream set

`corsa_e_no_ecu_streams.candump` contains the clean standalone ECU stream:

```text
0x0C9, 0x1BC, 0x1BD, 0x1C1,
0x1F5, 0x2C5,
0x3C9, 0x3D1, 0x3E9, 0x3F9,
0x4C1, 0x4D1
```

This set is the best starting point for making the original BCM believe the ECU exists.

### Native body/BCM base stream set

`corsa_e_opc_ecumaster_read.candump` and related logs contain the native body/BCM base stream:

```text
0x0F1, 0x120, 0x12A, 0x135, 0x137, 0x139, 0x140,
0x160, 0x1E1, 0x1F1, 0x1F3,
0x32A, 0x3C9, 0x3CB, 0x3E7, 0x3F1,
0x451, 0x4C5, 0x4D7, 0x4E1, 0x4E9,
0x514, 0x52A, 0x530
```

This set is the best starting point for waking/keeping alive the IPC if the IPC accepts the same standard 11-bit IDs on its bus.

### VIN/config fragments in logs

These native frames contain ASCII VIN/config fragments matching the BCM EEPROM VIN:

```text
0x514 = 30 56 30 58 45 50 36 38  -> ASCII "0V0XEP68"
0x4E1 = 4B 34 30 32 38 38 31 39  -> ASCII "K4028819"
```

Together they match the BCM VIN tail/middle from `W0V0XEP68K4028819`.

## Byte numbering convention

In this document:

```text
data[0] = first byte after # in candump
```

The earlier hand notes used “byte 1”, “byte 4”, etc. For those notes, “byte 1” appears to mean the first CAN data byte, i.e. `data[0]`. Confirm each field on the bench because some older notes may use zero-based indexing.

Confirmed from logs:

- `0x4D1 data[0]` toggles `0xC9` normal and `0xE9` oil-pressure-warning state.
- `0x4C1 data[2]` fits coolant temperature with formula `value - 40 °C`.
- `0x0C9 data[1:2]` fits RPM with likely formula `raw * 0.25 RPM`, where `raw = data[1]<<8 | data[2]`.

## Key messages and signal notes

### `0x0C9` — RPM / engine running / cruise status

Role: ECU stream. Very high priority.

Observed rate: about 200 Hz in logs.

Common payloads:

```text
Engine off / no RPM candidate: 00 00 00 00 00 50 00 00
Running examples:             84 12 DC 00 00 50 00 00
                              84 13 00 00 00 50 00 00
                              84 12 E8 00 00 50 00 00
```

RPM candidate:

```text
raw = (data[1] << 8) | data[2]
rpm = raw * 0.25
```

Examples:

```text
0x12DC = 4828 -> 1207 rpm
0x1300 = 4864 -> 1216 rpm
```

User-observed fields:

- First byte / `data[0]`: `0x80` bit indicates engine on/running.
- Cruise-control lamp/status bits were observed in the original notes. The note says “byte 4: `0x40` green cruise control, `0x20` white cruise control”; verify exact zero-based byte before implementing.
- If RPM is zero while the engine state is inconsistent, the IPC may chime.

Direct IPC test suggestion:

```text
0x0C9 @ 5 ms / 200 Hz
Idle test: 84 12 DC 00 00 50 00 00
Off test:  00 00 00 00 00 50 00 00
```

### `0x3E9` — vehicle speed / odometer-related

Role: ECU stream. Very high priority, but handle carefully.

Observed rate: about 20 Hz.

Common payloads:

```text
00 00 80 00 00 00 80 00
00 10 80 00 00 10 80 00
00 20 80 00 00 20 80 00
00 40 80 00 00 40 80 00
```

Candidate speed formula from earlier notes:

```text
speed = ((A * 256) + B) / 100
```

Possible field pairs:

- `data[0:1]`
- mirrored/alternate `data[4:5]`

Do not assume final units yet. One external note said 1/100 mph; Corsa E EU calibration may differ. Also, this frame affects odometer behavior, so keep values at zero or very low during bench experiments unless intentionally testing odometer increment.

Direct IPC test suggestion:

```text
0x3E9 @ 50 ms / 20 Hz
Zero speed baseline: 00 00 80 00 00 00 80 00
Small sweep candidates:
  00 10 80 00 00 10 80 00
  00 20 80 00 00 20 80 00
  00 40 80 00 00 40 80 00
```

### `0x4C1` — coolant temperature / IAT / thermal warnings

Role: ECU stream. Very high priority.

Observed rate: about 20 Hz.

Common payloads:

```text
00 00 6E 00 00 00 00 00
00 00 6F 00 00 00 00 00
00 00 70 00 00 00 00 00
00 00 73 00 00 00 00 00
00 00 74 00 00 00 00 00
00 00 75 00 00 00 00 00
```

Coolant candidate confirmed by payload trend:

```text
coolant_C = data[2] - 40
```

Examples:

```text
0x6E - 40 = 70 °C
0x74 - 40 = 76 °C
```

User-observed warning field:

- Original note: `$4C1 byte 6: 0x20 = Engine oil hot, idle engine`.
- Confirm exact byte index before implementing; likely one-based note means `data[5]`, but test both carefully if needed.

Direct IPC test suggestion:

```text
0x4C1 @ 50 ms / 20 Hz
70 °C: 00 00 6E 00 00 00 00 00
90 °C: 00 00 82 00 00 00 00 00
```

### `0x4D1` — oil pressure / oil level / fuel level / fuel cap / service warnings

Role: ECU stream. Very high priority.

Observed rate: about 20 Hz.

Important correction: static E78 table initially suggested DLC 7, but real logs show DLC 8. Use DLC 8 first.

Observed payloads:

```text
C9 00 00 00 00 00 00 00  -> normal / no oil-pressure warning candidate
E9 00 00 00 00 00 00 00  -> oil pressure low / stop engine candidate
```

Confirmed relation:

```text
0xC9 ^ 0xE9 = 0x20
```

So the oil-pressure warning is very likely `data[0]` bit `0x20`, with `0xC9` as a normal baseline value.

User-observed `0x4D1` warning notes:

| Field from original notes | Observed/claimed meaning |
|---|---|
| byte 1 / likely `data[0]`, bit `0x20` | Oil pressure low, stop engine |
| byte 1 / likely `data[0]`, bit `0x10` | Engine oil low, add oil |
| byte 1 / likely `data[0]`, bit `0x08` | Change oil soon |
| byte 1 / likely `data[0]`, bit `0x04` | Engine overheating, idle engine |
| byte 1 / likely `data[0]`, bit `0x02` | Engine overheated, stop engine |
| byte 1 / likely `data[0]`, bit `0x01` | AC off due to high engine temp |
| byte 4 / likely `data[3]`, bit `0x80` | Engine power reduced |
| byte 4 / likely `data[3]`, bit `0x20` | Tighten fuel cap |
| byte 6 / likely `data[5]` | Fuel level, `0x00..0xFF` |
| byte 7 / likely `data[6]`, bit `0x40` | Water in fuel, contact service |
| byte 7 / likely `data[6]`, bit `0x20` | Cleaning exhaust filter, continue driving |
| byte 7 / likely `data[6]`, bit `0x08` | Change fuel filter |
| byte 7 / likely `data[6]`, bit `0x04` | Cleaning exhaust filter, must continue driving |
| byte 7 / likely `data[6]`, bit `0x02` | Diesel engine shutdown soon |

Caution: because normal baseline `0xC9` already has some lower bits set, not every warning can be implemented by simply ORing a bit into `0xC9`. The oil-pressure `0x20` case is confirmed by `C9 -> E9`. The others need controlled bench tests.

Direct IPC test suggestion:

```text
0x4D1 @ 50 ms / 20 Hz
Normal:            C9 00 00 00 00 00 00 00
Oil pressure low:  E9 00 00 00 00 00 00 00
```

### `0x3D1` — speed limiter / dashboard settings screen

Role: ECU stream. High priority.

Observed rate: about 20 Hz.

Common payloads:

```text
10 00 00 00 00 00 00 00
11 00 00 00 00 00 00 00
```

User observation: affects speed-limiter screen and different dashboard settings.

Direct IPC test suggestion:

```text
0x3D1 @ 50 ms / 20 Hz
Baseline: 10 00 00 00 00 00 00 00
Alt:      11 00 00 00 00 00 00 00
```

### `0x120` — trip/display candidate

Role: native body/BCM stream or display-related stream.

Observed rate: about 0.2 Hz in native body logs.

Observed payload:

```text
00 7E 79 10 01
```

Earlier unconfirmed note:

```text
trip = ((A * 256) + B) / 64
```

Need confirmation with direct IPC capture.

### `0x160` — ignition / power mode candidate

Role: native body/BCM stream. Very useful for waking IPC.

Observed rate: about 10 Hz.

Key-on payload:

```text
80 3C 96 B5 03
```

Key-off payload observed in combined key-off context:

```text
00 00 00 00 00
```

Direct IPC test suggestion:

```text
0x160 @ 100 ms / 10 Hz
Key-on: 80 3C 96 B5 03
```

### `0x1F1` — ignition/environment/power-mode alternate candidate

Role: native body/BCM stream. High priority for IPC keepalive.

Observed rate: about 10 Hz.

Observed payload:

```text
AE 0F 17 3E 18 00 00 72
```

Direct IPC test suggestion:

```text
0x1F1 @ 100 ms / 10 Hz
AE 0F 17 3E 18 00 00 72
```

### Other important keepalive/body frames

These are likely useful to make the IPC feel like the car/body network is alive:

| ID | DLC | Approx rate | Payload to start with | Notes |
|---:|---:|---:|---|---|
| `0x0F1` | 6 | 100 Hz | rotate `00/1C/28/34 00 00 40 00 00` | Native high-rate body/chassis/sync candidate. |
| `0x12A` | 8 | 10 Hz | `00 00 60 59 00 00 00 80` | Door/belt/body status candidate. |
| `0x135` | 8 | 10 Hz | `04 08 0D 00 02 18 00 00` | Body/display keepalive candidate. |
| `0x137` | 8 | 10 Hz | `00 00 00 00 30 00 00 00` | Body/display keepalive candidate. |
| `0x139` | 8 | 10 Hz | `00 00 00 00 00 00 00 00` | Body/display keepalive candidate. |
| `0x140` | 3 | 1 Hz | `00 02 02` | Turn/lamp status candidate. |
| `0x1E1` | 7 | 33 Hz | rotate observed values | Chassis/body rolling/status. |
| `0x1F3` | 3 | 33 Hz | rotate `00 3C 00`, `40 7C 00`, `80 BC 00`, `C0 FC 00` | Rolling/status candidate. |
| `0x32A` | 8 | 10 Hz | `00 00 00 00 00 00 00 00` | Native BCM table TX candidate. |
| `0x3CB` | 6 | 10 Hz | observed values vary | Highly variable; maybe counter/odometer/time/status. |
| `0x3E7` | 2 | 10 Hz | `00 68` or `00 65` | Native 2-byte constant-ish frame. |
| `0x3F1` | 8 | 4 Hz | `00 FF FA 0A 06 FF 0A 68` | Native slow body/chassis candidate. |
| `0x451` | 6 | 33 Hz | `00 00 00 00 00 00` | All-zero keepalive candidate. |
| `0x4C5` | 3 | 2 Hz | `00 00 00` | Slow keepalive. |
| `0x4D7` | 5 | 2 Hz | `01 3A 87 DA A1` or similar | Slow variable/checksum/config frame. |
| `0x4E1` | 8 | 1 Hz | `4B 34 30 32 38 38 31 39` | ASCII VIN tail `K4028819`. |
| `0x4E9` | 6 | 1 Hz | observed values vary | Config/status. |
| `0x514` | 8 | 1 Hz | `30 56 30 58 45 50 36 38` | ASCII VIN middle `0V0XEP68`. |
| `0x52A` | 6 | 1 Hz | `24 24 37 36 37 36` | Config/VIN-like ASCII. |
| `0x530` | 4 | 1 Hz | `00 00 00 00` | Slow keepalive. |

## Initial direct-IPC replay plan

Since the next step is a standalone IPC and ESP32 controller, use this order.

### Phase 1 — prove bus physical layer

1. Power IPC B+, ignition/run, and ground.
2. Try passive listen on pins 3/4 with the intended transceiver.
3. Try candidate bitrates without transmitting first.
4. If the IPC never transmits, that is normal; many clusters may stay mostly silent without a BCM.
5. After confirming no electrical fault, begin low-duty transmission of wake frames.

### Phase 2 — wake / base body frames

Start with only BCM/body wake and keepalive frames. Send these for 5–10 seconds and observe whether the IPC wakes, backlight changes, lamps behave differently, or DIC changes.

Recommended minimal wake set:

```text
0x160  DLC 5  80 3C 96 B5 03                @ 100 ms
0x1F1  DLC 8  AE 0F 17 3E 18 00 00 72       @ 100 ms
0x12A  DLC 8  00 00 60 59 00 00 00 80       @ 100 ms
0x135  DLC 8  04 08 0D 00 02 18 00 00       @ 100 ms
0x137  DLC 8  00 00 00 00 30 00 00 00       @ 100 ms
0x139  DLC 8  00 00 00 00 00 00 00 00       @ 100 ms
0x140  DLC 3  00 02 02                      @ 1000 ms
0x451  DLC 6  00 00 00 00 00 00             @ 30 ms
```

Add `0x0F1` if the cluster still behaves dead or unstable:

```text
0x0F1 @ 10 ms / 100 Hz, rotate:
00 00 00 40 00 00
1C 00 00 40 00 00
28 00 00 40 00 00
34 00 00 40 00 00
```

### Phase 3 — add engine/gauge frames

Once the IPC is awake or stable, add the core ECU stream.

```text
0x0C9  DLC 8  84 12 DC 00 00 50 00 00       @ 5 ms    # RPM / engine running
0x1BC  DLC 8  48 01 00 00 7C 00 00 00       @ 5 ms    # ECU keepalive/status
0x1BD  DLC 8  00 00 00 00 00 00 00 00       @ 50 ms
0x1C1  DLC 8  00 00 00 00 00 00 00 00       @ 50 ms
0x1F5  DLC 8  00 00 00 0F 00 00 07 00       @ 50 ms
0x2C5  DLC 8  00 00 00 00 00 00 00 00       @ 50 ms
0x3D1  DLC 8  10 00 00 00 00 00 00 00       @ 50 ms
0x3E9  DLC 8  00 00 80 00 00 00 80 00       @ 50 ms    # speed zero
0x3F9  DLC 8  80 00 00 00 00 00 00 07       @ 50 ms
0x4C1  DLC 8  00 00 74 00 00 00 00 00       @ 50 ms    # coolant ~76 C
0x4D1  DLC 8  C9 00 00 00 00 00 00 00       @ 50 ms    # oil/fuel status normal
```

If this makes gauges move, reduce the set one frame at a time to find the minimum IPC-required set.

### Phase 4 — controlled one-signal experiments

Only change one signal at a time and record the dashboard reaction.

Suggested experiments:

| Test | Frame change | Expected observation |
|---|---|---|
| RPM sweep | `0x0C9 data[1:2]`, raw `rpm / 0.25` | Tachometer movement. |
| Engine off/on | `0x0C9 data[0]` bit `0x80` and RPM zero/nonzero | Engine-state lamps/chime behavior. |
| Coolant sweep | `0x4C1 data[2] = temp_C + 40` | Coolant gauge or temp warnings. |
| Oil pressure | `0x4D1 data[0] C9 -> E9` | “Oil pressure low / stop engine”. |
| Speed zero/small | `0x3E9` small speed payloads | Speedometer movement; odometer caution. |
| Limiter toggle | `0x3D1 10... -> 11...` | Speed limiter / DIC settings behavior. |
| Body wake | toggle `0x160` key-off/key-on payload | IPC sleep/wake behavior. |

Recommended log sheet format:

```text
Date/time:
IPC wiring:
Transceiver:
Bitrate:
Frame ID:
Payload before:
Payload after:
Period:
IPC reaction:
Does IPC transmit anything back?:
Notes / photo / video:
```

## If direct IPC replay does not work

If the cluster does not react to the standard 11-bit HS IDs, the BCM is likely repacking the powertrain signals before sending them to the IPC. In that case, the next required capture is a real IPC-side capture.

Capture plan:

1. Reconnect real BCM and IPC.
2. Sniff directly at IPC pins 3/4, or at BCM low-speed side, with the correct transceiver.
3. Capture these states:
   - Key off / sleeping.
   - Key on, engine off.
   - Idle / RPM sweep.
   - Speed sweep or rolling road / simulated speed if safe.
   - Coolant warning trigger.
   - Oil-pressure warning trigger.
   - Fuel-level change if possible.
4. At the same time, log high-speed side if possible.
5. Correlate high-speed input changes against IPC-side output changes.

Search specifically for:

- Does `0x0C9` appear on the IPC bus? If yes, same-ID forwarding is likely.
- Does `0x3E9` appear on the IPC bus? If yes, speed is likely same-ID forwarded.
- Does `0x4C1` appear on the IPC bus? If yes, temp may be same-ID forwarded.
- Does `0x4D1` appear on the IPC bus? If yes, warning/status may be same-ID forwarded.
- If not, find which IPC-side IDs change in sync with RPM, speed, temp, and oil-warning changes.

## Current best hypothesis of BCM gateway behavior

The BCM is not a transparent repeater. It likely does a mix of:

```text
1. Receive high-speed ECU/powertrain frames.
2. Decode important signals: RPM, speed, coolant, oil/fuel warnings, cruise/limiter status.
3. Maintain local body/power-mode state.
4. Transmit body/IPC display frames periodically.
5. Filter out most high-speed frames from the low-speed IPC side.
```

However, some GM/Opel messages may be forwarded with the same 11-bit arbitration IDs. That is why the first direct-IPC test should try the same known standard IDs before spending time on full BCM disassembly.

## Priority checklist for next session

1. Confirm IPC physical layer on pins 3/4.
2. Confirm bitrate.
3. Power IPC with B+ and Run/Crank ignition.
4. Try minimal wake set: `0x160`, `0x1F1`, `0x12A`, `0x135`, `0x137`, `0x139`, `0x140`, `0x451`.
5. Add `0x0F1` rotating counter/sync payloads.
6. Add ECU gauge set: `0x0C9`, `0x3E9`, `0x4C1`, `0x4D1`, `0x3D1`, `0x1F5`, `0x3F9`.
7. If tach moves, isolate exact `0x0C9` RPM bytes.
8. If coolant gauge/message reacts, isolate `0x4C1` bytes.
9. If oil warning reacts, isolate `0x4D1` bits.
10. Keep speed tests low/brief because `0x3E9` may affect odometer.
11. If nothing reacts, capture real BCM-to-IPC low-speed traffic and revisit static BCM TX table.

## Short list: most important frames to try first

For standalone IPC direct injection, this is the smallest practical first set:

```text
# Wake / body
0x160  80 3C 96 B5 03
0x1F1  AE 0F 17 3E 18 00 00 72
0x12A  00 00 60 59 00 00 00 80
0x0F1  rotate 00/1C/28/34 00 00 40 00 00

# Gauges / warnings
0x0C9  84 12 DC 00 00 50 00 00
0x3E9  00 00 80 00 00 00 80 00
0x4C1  00 00 74 00 00 00 00 00
0x4D1  C9 00 00 00 00 00 00 00
0x3D1  10 00 00 00 00 00 00 00
```

Then add these if needed:

```text
0x1BC  48 01 00 00 7C 00 00 00
0x1BD  00 00 00 00 00 00 00 00
0x1C1  00 00 00 00 00 00 00 00
0x1F5  00 00 00 0F 00 00 07 00
0x2C5  00 00 00 00 00 00 00 00
0x3F9  80 00 00 00 00 00 00 07
0x451  00 00 00 00 00 00
0x4E1  4B 34 30 32 38 38 31 39
0x514  30 56 30 58 45 50 36 38
```

## Notes to avoid false conclusions

- The current logs are likely high-speed side, not confirmed direct IPC side.
- The IPC pinout says Low Speed GMLAN, so the ESP32 CAN controller must match the correct physical layer and bitrate.
- `0x4D1` should be sent as DLC 8 first, because real logs show DLC 8.
- Some signals may require rolling counters/checksums. If a frame works briefly and then times out, look for counters.
- `0x0F1`, `0x1E1`, `0x1F3`, and `0x3CB` have rotating or variable payloads and may be keepalive/counter-style frames.
- `0x3E9` may affect odometer; test speed values carefully.
- Do not spend time on security/environment emulation until the display/gauge cyclic messages are proven.
