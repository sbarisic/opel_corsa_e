# cantool

C# candleLight/gs_usb helper for the Corsa E IPC low-speed CAN work.

## Commands

```powershell
dotnet run --project cantool\cantool\cantool.csproj
dotnet run --project cantool\cantool\cantool.csproj -- list
dotnet run --project cantool\cantool\cantool.csproj -- capture --seconds 10 --out data\can_logs\live\capture.candump
dotnet run --project cantool\cantool\cantool.csproj -- send --id 13FFE040 --seconds 10 --period-ms 500
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator-p4-speed --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator-alt-sources --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator-sweep --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-probe --seconds 12
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-session-probe --seconds 16
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-did-scan --seconds 35
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-f1-range-scan --seconds 75
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-local-range-scan --seconds 75
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-local-range-slow-scan --seconds 145
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-read21-local-scan --seconds 80
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-standard-engine-warning-probe --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-standard-byte-fuzz --seconds 300
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-gmlan29-byte-fuzz --seconds 180
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile firmware-wake --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-all-targeted --seconds 60
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-speed-rpm --seconds 10
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-speed-rpm-alt-sources --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-power-mode --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-environment --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-known-payloads --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-chime --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-speed-sweep --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile opel-reference-probe --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile gmlan29-probe --seconds 15
dotnet run --project cantool\cantool\cantool.csproj -- summarize --log data\can_logs\live\capture.candump
```

Running with no arguments opens an interactive menu for Visual Studio/debugger
runs. Pick a profile, press Enter to use its default duration, or type `0` to
run until Ctrl+C. Each transmit run prints TX and RX activity and writes
received frames to a timestamped `.candump` file under `data\can_logs\live`.
Console TX/RX timestamps are relative seconds from app start; saved logs remain
candump-compatible with absolute timestamps.

`ipc-simulator` is the staged Corsa E IPC profile. It opens the CANable, waits
for the first post-flush RX frame from the IPC, logs that trigger frame, and
only then starts a 30 second fake BCM/body-gateway sequence. The sequence sends
startup wake frames, steady `13FFE040#` / `621#0040000000000000` keepalive,
`10002040#03` power mode, priority-3 speed/RPM
`0C050040#00012C03A9000000`, battery voltage `0C030040#0075A708`, one chime
command, and a final speed/RPM zero-out frame.

The companion simulator profiles use the same wait-for-first-RX staging and
should be run one at a time while watching the IPC:

- `ipc-simulator-p4-speed` swaps the speed/RPM probe to
  `10050040#00012C03A9000000`.
- `ipc-simulator-alt-sources` cycles `0C050010`, `0C050028`, `10050010`, and
  `10050028` speed/RPM source variants in separate windows.
- `ipc-simulator-sweep` keeps source `0x40` and sweeps `0C050040` payloads from
  zero through the workbook example and a higher speed/RPM value.
- `ipc-diagnostic-probe` sends `24C#023E00AAAAAAAAAA` six times after the
  staged wake; a real IPC diagnostic reply should appear as `64C#027E00...`.
- `ipc-diagnostic-session-probe` sends single-shot `10 01`, `10 03`, `3E 80`,
  `3E 00`, and VIN DID `22 F1 90` requests on `24C` to map which basic UDS
  services the IPC accepts.
- `ipc-diagnostic-did-scan` enters extended diagnostic session, probes common
  read-only UDS DIDs and DTC services, and automatically sends ISO-TP flow
  control `24C#300000AAAAAAAAAA` when the IPC starts a multi-frame `64C`
  response.
- `ipc-diagnostic-f1-range-scan` enters extended diagnostic session and walks
  read-only DIDs `F100` through `F1FF`, one request every 250 ms. This is useful
  because the IPC accepts service `0x22` but rejected the first small list as
  out of range.
- `ipc-diagnostic-local-range-scan` does the same for local DIDs `0000` through
  `00FF`.
- `ipc-diagnostic-local-range-slow-scan` repeats the local `0000` through
  `00FF` scan after an 8 second settle delay, with 500 ms between requests, so
  it starts after the recurring `64C#0160...` status-like response seen in
  earlier logs.
- `ipc-diagnostic-read21-local-scan` scans older service `0x21` local
  identifiers `00` through `FF`. A useful positive response would start with
  `61`, while `7F 21 ...` means the service or identifier was rejected.
- `ipc-standard-engine-warning-probe` waits for IPC RX, then sends the
  requested standard 11-bit probe set: `0C9#0004C40000500000` every 20 ms and
  the three `1E5` warning/service variants staggered every 100 ms, with
  `13FFE040#` and `621#0040000000000000` kept alive at 1 Hz.
- `ipc-standard-byte-fuzz` is the matching deterministic 11-bit fuzzing
  profile. It waits for IPC RX, keeps the fake BCM/body presence alive, and
  sweeps one byte at a time on curated standard IDs: Corsa E candidates
  `0C9`, `1E5`, `3E9`, `4C1`, `4D1`, `3D1`, `160`, `1F1`, plus older
  Opel reference IDs `0AA`, `06C`, `092`, `104`, `119`, and `15E`. The
  default 300 second run prioritizes `0C9`, `1E5`, and `3E9`; running longer
  continues through the remaining IDs in order.
- `ipc-gmlan29-byte-fuzz` is the conservative deterministic fuzzing profile.
  It waits for IPC RX, keeps `13FFE040#`, `621#0040000000000000`, and
  `10002040#03` alive, then changes one byte at a time on curated 29-bit
  GM/GMLAN messages. The default 180 second run covers power mode plus both
  speed/RPM variants and zeroes speed/RPM near the end; running longer continues
  into battery, engine, fuel, brake/cruise, and chime-command byte sweeps.

Transmit scheduling compensates for USB/RX polling overhead by advancing each
periodic frame from its previous due time and draining pending RX frames between
TX batches. This keeps 50 ms and 100 ms simulator probes closer to their target
periods without changing saved log format.

`default-bench` is the non-waiting Corsa E IPC test flow. It sends the
updated wake prelude, then nonzero GMLAN Bible-derived payload probes for
vehicle speed/RPM, battery voltage, system power mode, and chime.

In `firmware-wake`, `100#` is sent once at startup, `621#0140000000000000` is
sent as a three-frame startup burst, and steady keepalive is handled by
`13FFE040#` plus `621#0040000000000000` at 1 Hz.

Each transmit run writes normal RX candump rows plus `# tx ...` comment lines
showing transmitted frames. Existing parsers ignore those comments, so saved
logs remain candump-compatible.

The GMLAN Bible-derived profiles add targeted payload probes:

- `gmlan29-all-targeted` runs the targeted 29-bit payload tests in one
  continuous 60 second sequence under one wake/keepalive session.
- `gmlan29-speed-rpm` sends `10050040#00012C03A9000000` at high rate, then
  zeros it near the end.
- `gmlan29-speed-rpm-alt-sources` tests priority/source variants for the same
  speed/RPM payload.
- `gmlan29-power-mode` tests short and full-DLC `10002040` values `02`, `03`,
  and `04`.
- `gmlan29-environment` tests battery voltage and outside-temperature examples.
- `gmlan29-chime` sends three isolated chime command variants.
- `gmlan29-known-payloads` mirrors the default GMLAN payload bundle for
  explicit command-line runs.
- `gmlan29-speed-sweep` is retained as a legacy arbid `0x028` sweep from
  earlier experiments.

`opel-reference-probe` is an older Opel/Corsa D 11-bit reference set, and
`gmlan29-probe` is a zero-payload discovery set. They are available for
comparison but are no longer part of default testing.

Use `capture` when you want a passive receive-only run.

`summarize` annotates 29-bit extended IDs with the decoded GMLAN priority,
arbitration ID name, and sender range, for example sender `0x060` as
`Driver Info/Displays` and sender `0x040` as `Body/Integration`.

All commands use the same custom 33.333 kbit/s candleLight timing as
`tools/ipc_lowspeed_gsusb.py`:

```text
brp=255 prop=1 phase1=13 phase2=5 sjw=4
```

Output is candump-compatible so the existing Python comparison and BCM
projection tools can read it.
