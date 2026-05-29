# cantool

C# candleLight/gs_usb helper for the Corsa E IPC low-speed CAN work.

## Commands

```powershell
dotnet run --project cantool\cantool\cantool.csproj
dotnet run --project cantool\cantool\cantool.csproj -- list
dotnet run --project cantool\cantool\cantool.csproj -- capture --seconds 10 --out data\can_logs\live\capture.candump
dotnet run --project cantool\cantool\cantool.csproj -- summarize --log data\can_logs\live\capture.candump
```

Running with no arguments starts a continuous capture, prints every received
frame, and writes a timestamped `.candump` file under `data\can_logs\live`.
Stop it with Ctrl+C.

The capture command uses the same custom 33.333 kbit/s candleLight timing as
`tools/ipc_lowspeed_gsusb.py`:

```text
brp=255 prop=1 phase1=13 phase2=5 sjw=4
```

Output is candump-compatible so the existing Python comparison and BCM
projection tools can read it.
