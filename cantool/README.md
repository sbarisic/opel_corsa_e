# cantool

C# candleLight/gs_usb helper for the Corsa E IPC low-speed CAN work.

## Commands

```powershell
dotnet run --project cantool\cantool\cantool.csproj
dotnet run --project cantool\cantool\cantool.csproj -- list
dotnet run --project cantool\cantool\cantool.csproj -- capture --seconds 10 --out data\can_logs\live\capture.candump
dotnet run --project cantool\cantool\cantool.csproj -- send --id 13FFE040 --seconds 10 --period-ms 500
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
runs. Pick a profile, press Enter to use the default 15 second duration, or type
`0` to run until Ctrl+C. Each transmit run prints TX and RX activity and writes
received frames to a timestamped `.candump` file under `data\can_logs\live`.
Console TX/RX timestamps are relative seconds from app start; saved logs remain
candump-compatible with absolute timestamps.

The default profile is the recommended Corsa E IPC test flow. It sends the
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
