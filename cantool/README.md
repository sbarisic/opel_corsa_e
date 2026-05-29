# cantool

C# candleLight/gs_usb helper for the Corsa E IPC low-speed CAN work.

## Commands

```powershell
dotnet run --project cantool\cantool\cantool.csproj
dotnet run --project cantool\cantool\cantool.csproj -- list
dotnet run --project cantool\cantool\cantool.csproj -- capture --seconds 10 --out data\can_logs\live\capture.candump
dotnet run --project cantool\cantool\cantool.csproj -- send --id 13FFE040 --seconds 10 --period-ms 500
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile firmware-wake --seconds 15
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

The default profile combines the BCM-derived `firmware-wake` frames, a small
older Opel low-speed GMLAN probe set (`0x278` wake, `0x0AA` IPC-on, and a slow
staggered `0x06C` needle sweep), and low-rate 29-bit GMLAN probes using guessed
BCM/source `0x40`.

The `gmlan29-probe` payloads are zero-filled discovery frames for watching IPC
reaction or status changes. They are not decoded gauge-control payloads yet.

Use `capture` when you want a passive receive-only run.

All commands use the same custom 33.333 kbit/s candleLight timing as
`tools/ipc_lowspeed_gsusb.py`:

```text
brp=255 prop=1 phase1=13 phase2=5 sjw=4
```

Output is candump-compatible so the existing Python comparison and BCM
projection tools can read it.
