# cantool

C# candleLight/gs_usb helper for the Opel Corsa E IPC low-speed GMLAN bench.

## Recommended Workflow

```powershell
dotnet run --project cantool\cantool\cantool.csproj
dotnet run --project cantool\cantool\cantool.csproj -- list
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-read-only-sniff --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-ack-sniff --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-power-toggle-watch --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-wake-recovery-probe --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-key-first-turn-replay --seconds 60
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-dim-odo-probe --seconds 75
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-wrapped-gauge-probe --seconds 90
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-rpm-word-sweep --seconds 185
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator-data-fill --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-lights-probe --seconds 85
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-handbrake-probe --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-tc-probe --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-live-reload --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-range-fuzzer00 --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-range-fuzzer --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-range-fuzzer20 --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-range-fuzzer30 --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-range-fuzzerE0 --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-tpms-ok-dismiss-probe --seconds 70
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-dic-text-radio-probe --seconds 60
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-confirmed-identity-probe --seconds 18
dotnet run --project cantool\cantool\cantool.csproj -- summarize --latest --ignore-tx-echo
dotnet run --project cantool\cantool\cantool.csproj -- analyze-diag --latest --positive-only
dotnet run --project cantool\cantool\cantool.csproj -- profile-info --profile ipc-simulator
```

Running with no arguments opens the Visual Studio-friendly interactive runner.
The default menu selection is `ipc-read-only-sniff`, which opens the adapter in
listen-only mode, sends nothing, and captures until Ctrl+C. Console TX/RX
timestamps are relative seconds from app start; saved logs remain
candump-compatible with absolute timestamps.

## Current Profiles

Read-only/watch profiles:

- `read-only` and `ipc-read-only-sniff`: listen-only capture, no ACK and no TX.
- `ipc-ack-sniff`: normal-mode no-TX capture so the adapter ACKs IPC frames.
- `ipc-power-toggle-watch`: no-flush ACKing capture for pin 8 ignition/run-crank
  wake checks.

Wake/silent-IPC profiles:

- `ipc-wake-pulse-only-probe`: only slow `100#` SWCAN wake pulses.
- `ipc-wake-first-probe`: minimal wake/body context for a silent IPC.
- `ipc-wake-recovery-probe`: wake/body/diagnostic nudges for a currently silent
  bench IPC.

Native Corsa E profiles:

- `ipc-simulator`: recommended stable key-on hold replay from real SWCAN captures.
- `ipc-simulator-keyon-hold`: alias for the same stable key-on hold replay.
- `ipc-native-key-first-turn-replay`: captured-like source-`0x40`
  key-first-turn replay from the IPC-disconnected SWCAN logs. This is the
  preferred baseline before wrapped gauge tests.
- `ipc-native-dim-odo-probe`: focused dim/odometer isolation profile based on
  the key-first-turn replay finding. It keeps a minimal key-on core alive, then
  windows `10240040`, `102CA040`, and `10248040` alone and in combinations.
  Restart the IPC in the `60-72s` window to retry the dim + odometer-only state.
- `ipc-wrapped-gauge-probe`: key-first-turn replay plus experimental wrapped
  11-bit candidates for RPM, coolant/temp, oil/service, and warning/service
  status. Its wrapped `107D2040` speed rows are now lower priority because
  bench testing confirmed native `10210040` moves the speedometer.
- `ipc-rpm-word-sweep`: interactive full RPM-word sweep through likely
  native source-`0x40` frames. It inserts realistic RPM raw words (`0x0C80`,
  `0x12C0`, `0x1F40`, `0x2EE0`) into each 16-bit slot, shows sweep progress
  on the speedometer via `10210040` at roughly `0-100 km/h`, and accepts
  keyboard markers: `m`/Space marks a visible RPM event, `b` marks a
  bad/restart event, `n` skips, `p` pauses, and `q` quits.
- `ipc-simulator-keyon-edge`: short pre-key/key-present prelude, then key-on hold.
- `ipc-simulator-native-transition`: off/key-in/key-on/cleanup transition replay.
- `ipc-simulator-data-fill`: key-on hold plus transient odometer/fuel/body data
  candidates.
- `ipc-native-lights-probe`: source-`0x40` only lights/TC isolation profile.
  It suppresses the base `10240040` / `102CA040` copies, excludes source-`0x58`
  icon/DIC traffic, and holds one `10240040`, `102CC040`, or `102CA040` value
  at a time so TC and beam-symbol changes can be pinned to a single payload.
  Bench timing confirms `102CC040#0400C00000000000` turns the TC telltale on.
  Live testing also found `10240040#83FE12C000D70FFD` / `83FE1F4000D70FFD`
  toggle a TC message/lamp state.
- `ipc-native-handbrake-probe`: source-`0x40` parking-brake / handbrake
  telltale toggle. It holds `1020C040` released and alternates
  `103B4040#00` / `103B4040#04`; bench video confirms `103B4040#04` turns
  the indicator on and `103B4040#00` clears it.
- `ipc-native-tc-probe`: source-`0x40` TC telltale toggle. It alternates the
  confirmed ON payload `102CC040#0400C00000000000` with
  `102CC040#0000C00000000000` to verify whether the latter clears the telltale.
- `ipc-live-reload`: live editable transmitter. It reads `send_profile.can`
  while it is running, reloads the file when saved, and transmits only enabled
  rows. Columns are `period_ms,message,enabled,note`; use `--file PATH` to test
  another file. The default file is resolved from the repository root, so it is
  stable when launched from Visual Studio. The starter file keeps all candidate
  rows disabled so you can turn on speed, TC, parking brake, dim/odometer,
  key-on, HMI/DIC, or wrapped gauge messages one at a time. `10210040` is the
  current confirmed speedometer input; byte 0 moves the needle. Enable only one
  `10210040` row at a time. Speed rows also require
  `102CC040#0000400000000000` to be sent as the confirmed speed-valid
  prerequisite. Disable the captured `10210040#0000800000008000` baseline while
  calibrating speed rows. Live testing found `10220040` can show speed-limiter
  messages, including `000001F0...` / `000001F1...` around 33 km/h.
- `ipc-range-fuzzer00`, `ipc-range-fuzzer`, `ipc-range-fuzzer20`,
  `ipc-range-fuzzer30`, and `ipc-range-fuzzerE0`: interactive source-`0x40`
  wrapped-ID fuzzers for arbitration ranges `0x000-0x0FF`, `0x100-0x1FF`,
  `0x200-0x2FF`, `0x300-0x3FF`, and `0xE00-0xEFF`. For the simple RPM hunt,
  start with `ipc-range-fuzzer00`, then run `ipc-range-fuzzerE0`. They keep the key-on baseline plus
  `102CC040#EEEEEEEEEEEEEEEE` speed-valid heartbeat alive, then test one
  `EEEE...`, `FFFF...`, `8888...`, or `0000...` payload at a time. They also
  send `10210040` as a 0-100 km/h progress indicator, so the speedometer shows
  roughly how far through the active range the fuzzer has scanned. Press `m` or
  Space when something visible happens, `b` for a bad/restart event, `n` to
  skip, `p` to pause/resume, and `q` to stop. Markers are saved as `# mark ...`
  comments so `summarize` can print the exact active ID/payload.
- `ipc-tpms-ok-dismiss-probe`: safe body/HMI button candidates to try clearing
  the TPMS message before DIC text tests.
- `ipc-dic-text-radio-probe`: radio/OnStar-style DIC text candidates without
  transmitting source `0x60`.
- `ipc-624-state-probe`: isolated `621` / `624` state-heartbeat test. If it
  restarts the IPC, treat `624` as unsafe for normal context.
- `ipc-active-gauge-context-probe`: native key-on context plus battery,
  speed/RPM, engine, ABS, and brake/cruise candidates.

Diagnostic profiles:

- `ipc-diagnostic-probe`
- `ipc-diagnostic-session-probe`
- `ipc-diagnostic-did-scan`
- `ipc-diagnostic-f1-range-scan`
- `ipc-diagnostic-local-range-scan`
- `ipc-diagnostic-local-range-slow-scan`
- `ipc-diagnostic-read21-local-scan`
- `ipc-diagnostic-gmlan-classic-probe`
- `ipc-diagnostic-isolated-classic-probe`
- `ipc-diagnostic-confirmed-identity-probe`
- `ipc-diagnostic-classic-1a-scan`
- `ipc-diagnostic-classic-1a-c0-ef-scan`
- `ipc-diagnostic-aa00-repeat-probe`

The confirmed IPC diagnostic request/response path is `24C` -> `64C`.
Diagnostic profiles use the quiet post-RX baseline, keep `13FFE040#` and
`621#0040000000000000` alive, and avoid security, programming,
communication-disable, write, and device-control services.

## Analysis Helpers

`summarize --ignore-tx-echo` hides exact local echoes of logged `# tx` comments,
which makes IPC-origin evidence easier to spot. Extended source-`0x40` frames
with arbitration IDs `<= 0x7FF` are annotated as `wrapped_std=0xNNN` so they
can be compared directly with older standard-ID notes. Logs from
`ipc-range-fuzzer` also print `# mark` rows, including the key you pressed and
the active fuzzer candidate at that moment.

`analyze-diag` pairs diagnostic TX comments on `24C` with following `64C` /
`54C` responses, decodes negative-response codes, reconstructs simple
multi-frame replies, and prints ASCII hints such as VIN or calibration strings.

`analyze-wake` classifies a wake capture as clean silent, stale local-echo
backlog, exact TX echoes only, unknown RX, or IPC wake evidence. Wake evidence
includes source-`0x060` traffic, `62C`, `64C`, and `54C`.

`profile-info --profile NAME` prints the generated transmit schedule, default
duration, flags, estimated send counts, and notes.

## Notes

The current public profile list has been pruned to the profiles that match the
newer Corsa E SWCAN logs. Older GMLAN Bible, broad fuzz, generic priority,
Astra/reference, and restart-isolation profiles were removed from the public
CLI/TUI profile registry and from this document. Historical behavior can be
recovered from git if needed.

All commands use the same custom 33.333 kbit/s candleLight timing as
`tools/ipc_lowspeed_gsusb.py`:

```text
brp=255 prop=1 phase1=13 phase2=5 sjw=4
```
