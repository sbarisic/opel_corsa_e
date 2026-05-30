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
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator-data-fill --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-lights-probe --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-handbrake-probe --seconds 45
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
- `ipc-simulator-keyon-edge`: short pre-key/key-present prelude, then key-on hold.
- `ipc-simulator-native-transition`: off/key-in/key-on/cleanup transition replay.
- `ipc-simulator-data-fill`: key-on hold plus transient odometer/fuel/body data
  candidates.
- `ipc-native-lights-probe`: captured `10240040` / `102CC040` / `102CA040`
  lights and dimmer candidate windows.
- `ipc-native-handbrake-probe`: captured `1020C040` / `103B4040` handbrake and
  telltale candidate windows.
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
which makes IPC-origin evidence easier to spot.

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

