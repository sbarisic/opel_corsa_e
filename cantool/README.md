# cantool

C# candleLight/gs_usb helper for the Corsa E IPC low-speed CAN work.

## Commands

```powershell
dotnet run --project cantool\cantool\cantool.csproj
dotnet run --project cantool\cantool\cantool.csproj -- list
dotnet run --project cantool\cantool\cantool.csproj -- bench-health --seconds 5
dotnet run --project cantool\cantool\cantool.csproj -- wake-watch --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- wake-matrix --phase-seconds 12
dotnet run --project cantool\cantool\cantool.csproj -- wake-then-profile --profile ipc-simulator --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- capture --seconds 10 --out data\can_logs\live\capture.candump
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile read-only
dotnet run --project cantool\cantool\cantool.csproj -- send --id 13FFE040 --seconds 10 --period-ms 500
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-read-only-sniff --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-ack-sniff --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-power-toggle-watch --seconds 0
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-wake-pulse-only-probe --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-wake-first-probe --seconds 20
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-wake-recovery-probe --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator-p4-speed --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator-alt-sources --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-simulator-sweep --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-bcm-live-object-probe --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-restart-wake-only-probe --seconds 8
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-restart-nm-init-only-probe --seconds 8
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-restart-presence-only-probe --seconds 8
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-restart-keepalive-only-probe --seconds 8
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-restart-wake-then-nm-init-probe --seconds 12
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-probe --seconds 12
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-session-probe --seconds 16
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-did-scan --seconds 35
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-f1-range-scan --seconds 75
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-local-range-scan --seconds 75
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-local-range-slow-scan --seconds 145
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-read21-local-scan --seconds 80
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-gmlan-classic-probe --seconds 36
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-isolated-classic-probe --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-confirmed-identity-probe --seconds 18
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-classic-1a-scan --seconds 60
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-classic-1a-c0-ef-scan --seconds 36
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-diagnostic-aa00-repeat-probe --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-standard-engine-warning-probe --seconds 30
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-astra-h-ls-reference-probe --seconds 60
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-standard-byte-fuzz --seconds 300
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-gmlan29-byte-fuzz --seconds 180
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-gm-opel-hmi-probe --seconds 90
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-body-seed-probe --seconds 60
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-context-lite --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-keyon-context --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-key-transition --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-native-expanded-keyon --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-priority-tier1-probe --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-priority-tier2-probe --seconds 45
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-priority-tier3-probe --seconds 60
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-priority-all-probe --seconds 150
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-priority-tier1-byte-fuzz --seconds 180
dotnet run --project cantool\cantool\cantool.csproj -- send-profile --profile ipc-priority-tier2-byte-fuzz --seconds 240
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
dotnet run --project cantool\cantool\cantool.csproj -- summarize --latest --ignore-tx-echo
dotnet run --project cantool\cantool\cantool.csproj -- summarize --log data\can_logs\live\tx_rx.candump --ignore-tx-echo
dotnet run --project cantool\cantool\cantool.csproj -- analyze-wake --latest --pattern "cantool_ipc-power-toggle-watch_rx_*.candump"
dotnet run --project cantool\cantool\cantool.csproj -- analyze-wake --log data\can_logs\live\wake_watch.candump
dotnet run --project cantool\cantool\cantool.csproj -- analyze-diag --log data\can_logs\live\diagnostic.candump
dotnet run --project cantool\cantool\cantool.csproj -- analyze-diag --latest --pattern "cantool_wake_then_ipc-diagnostic-classic-1a-scan*.candump" --positive-only
dotnet run --project cantool\cantool\cantool.csproj -- analyze-restart --log data\can_logs\live\restart_probe.candump
dotnet run --project cantool\cantool\cantool.csproj -- profile-info --profile ipc-bcm-live-object-probe
```

Running with no arguments opens an interactive runner menu for Visual Studio/debugger
runs. The default selection is `ipc-read-only-sniff`, which opens the adapter in
listen-only mode, sends nothing, and captures until Ctrl+C. Pick another
profile for transmit tests; press Enter to use a profile's default duration, or
type `0` to run until Ctrl+C. Console TX/RX timestamps are relative seconds
from app start; saved logs remain candump-compatible with absolute timestamps.
The no-argument menu also has wake-first shortcuts for the current bench state:
`w` runs `wake-watch --stop-on-wake` with no TX, `m` runs a manual-advance
guided pin 3 / pin 4 wake matrix with log phase markers, `d` waits for first
IPC RX and then runs `ipc-diagnostic-gmlan-classic-probe`, `i` waits for first
IPC RX and then runs the cleaner isolated classic diagnostic probe, `c` waits
for first IPC RX and then runs the fast confirmed identity probe, and `s` waits
for first IPC RX and then runs `ipc-simulator`.

`ipc-read-only-sniff` is the safest startup profile. It logs RX frames to
`data\can_logs\live\cantool_ipc-read-only-sniff_rx_*.candump`, writes no
`# tx` comments, and never enters normal TX mode.
`read-only` is kept as a compatibility alias for the same listen-only capture
behavior.

`ipc-ack-sniff` also sends no TX frames, but opens the CANable in normal mode
instead of listen-only mode. Use this when the standalone IPC may need another
node to ACK its transmissions before it continues talking. It is the best
watch mode while toggling pin 8 run/crank ignition power.

`ipc-power-toggle-watch` is the current best physical wake test. It is the same
normal-mode, no-TX, ACKing capture style as `ipc-ack-sniff`, but it deliberately
skips the startup stale-frame flush so the first few hundred milliseconds after
a pin 8 run/crank toggle cannot be hidden by the drain. Start it, then apply or
toggle pin 8 +12 V and watch for IPC-origin `...060`, `62C`, or `64C` frames.
If it immediately dumps old frames matching a previous TX profile, stop it and
run it again; the second clean run should show only real live traffic.

`ipc-wake-pulse-only-probe` is the narrowest active wake isolation test. It
opens the adapter in normal TX/RX mode and sends only `100#` once per second,
with no `621#0140...`, power-mode, diagnostics, body context, or fuzz frames.
Use it to answer one question cleanly: whether software SWCAN wake pulses alone
make the silent IPC start transmitting.

`ipc-wake-first-probe` is the clean first TX profile for a silent IPC. It does
not wait for first RX, sends no diagnostics, and sends only wake/body context:
slow `100#`, sparse `621#0140000000000000`, steady `13FFE040#` /
`621#0040000000000000`, `10002040#03` run power-mode context, and key/ignition
context candidates. Use it after verifying IPC pin 7 battery positive, pin 8
run/crank ignition positive, and pin 19 ground. If this makes the IPC talk,
move to `ipc-ack-sniff`, `ipc-read-only-sniff`, or a wait-for-RX profile.

`ipc-wake-recovery-probe` is for the current "IPC is silent" bench state. It
does not wait for first RX. It sends slow `100#` wake frames, sparse
`621#0140000000000000` network-init retries, steady `13FFE040#` /
`621#0040000000000000`, `10002040#03` power-mode context, key/ignition context
candidates, and isolated `24C` diagnostic pings. Use it before wait-for-RX
profiles when the cluster is not transmitting anything.

Current wake-state result from 2026-05-29: `ipc-ack-sniff`,
`ipc-wake-pulse-only-probe`, `ipc-wake-first-probe`, and
`ipc-wake-recovery-probe` all reduced to zero non-echo frames with
`summarize --ignore-tx-echo` in the current bench state. Treat this as a
physical/electrical wake problem before continuing payload fuzzing: verify pin
7 battery positive, pin 8 run/crank ignition positive, pin 19 ground, try both
IPC low-speed GMLAN pins 3 and 4, and run `ipc-power-toggle-watch --seconds 0`
while toggling pin 8.

`bench-health` is the quick pre-flight check for the current bench. It lists the
CANable, runs a normal-mode read-only capture with `--log-flush` behavior, sends
three harmless `13FFE040#` presence frames to verify local echo / TX, then runs
a clean follow-up capture. Its summaries use `--ignore-tx-echo` for the echo
probe so the output clearly separates adapter function from real IPC traffic.

`wake-watch` is the one-command version of the current physical wake test. It
opens the adapter in normal mode, sends no frames, discards any existing adapter
backlog for 300 ms, prints `ARMED`, captures until Ctrl+C by default, then
automatically runs `analyze-wake` on the new log. Wait for `ARMED`, then toggle
IPC pin 8 run/crank +12 V:

```powershell
dotnet run --project cantool\cantool\cantool.csproj -- wake-watch
```

Use `--preclear-ms N` to change the arming drain or `--no-preclear` when you
intentionally want to log any adapter backlog before the pin 8 toggle. Add
`--stop-on-wake` to stop automatically as soon as a known IPC wake signature is
seen, such as source-`0x060` extended traffic, `62C`, `64C`, or `54C`. The
Visual Studio/no-argument menu shortcut `w` enables this stop-on-wake behavior
by default.

`wake-matrix` is a guided version of the physical wake test. It logs clear
`# wake-matrix phase=...` comments while you try the likely combinations:
low-speed pin 3 passive, low-speed pin 4 passive, then pin 3 and pin 4 again
while the tool sends a gentle `100#` wake pulse once per second. Keep IPC pin 7
on battery +12 V and pin 19 on ground throughout; during each phase, follow the
console prompt and toggle/apply pin 8 run/crank +12 V. It stops automatically
when known IPC wake evidence appears unless you pass `--no-stop-on-wake`:

```powershell
dotnet run --project cantool\cantool\cantool.csproj -- wake-matrix --phase-seconds 12
```

Use `--passive-only` to test only pin 3/pin 4 physical wake without sending
`100#` pulses. Use `--manual-advance` when moving wires by hand; the tool pauses
before each phase until the wiring is ready. The Visual Studio/no-argument menu
shortcut `m` enables manual-advance mode by default.

`wake-then-profile` is the matching follow-up when you want the tool to start a
transmit profile only after the IPC wakes. It preclears stale RX, waits for the
first IPC frame, then runs the selected profile. This is useful after wiring is
fixed because it avoids sending simulator/fuzz frames into a sleeping cluster:

```powershell
dotnet run --project cantool\cantool\cantool.csproj -- wake-then-profile --profile ipc-simulator --seconds 30
```

Add `--wait-rx-timeout-ms N` for a timed bench check, or `--no-preclear` if you
intentionally want to preserve adapter backlog before the first-RX wait.

`analyze-diag --log PATH` pairs diagnostic TX comments on `24C` with following
`64C` / `54C` responses, decodes negative-response codes, reconstructs simple
multi-frame `64C` replies, and prints ASCII hints such as VIN / calibration
strings. It also prints a compact positive-response summary at the end, which
is the fastest way to review large DID scans. Use it after every IPC diagnostic
profile run. Add `--positive-only` to hide negative/no-response lines while
keeping the compact summary.

`analyze-restart --log PATH` correlates `# tx` comments with IPC
boot/status frames seen shortly afterward, such as `10244060`, `103BC060`,
`1045C060`, `10ACE060`, and `10AE8060`. Use it after the restart-isolation
profiles to identify whether `100#`, `621#0140...`, `13FFE040#`, or
`621#0040...` is the restart trigger. By default it prints only hits and a
summary; add `--verbose` to print no-hit TX lines too. A restart hit must have
at least three unique boot/status IDs by default; use `--min-unique N` to tune
that threshold.

`profile-info --profile NAME` prints the generated transmit schedule, default
duration, flags, estimated default send counts, and notes. Use it before a new
bench profile run to confirm exactly what will be transmitted.

Use `summarize --ignore-tx-echo` on TX/RX logs when you want to see only frames
that are not exact local echoes of logged `# tx` comments. This makes it easier
to distinguish IPC-origin evidence from CAN adapter self-echo.

`analyze-wake --log PATH` is tuned for `ipc-power-toggle-watch` and wake
recovery logs. It classifies a capture as clean silent, stale local-echo backlog,
exact TX echoes only, or IPC wake evidence. Wake evidence includes extended
source-`0x060` IPC frames, known IPC boot/status IDs, `62C`, and diagnostic
`64C` / `54C` responses. It also prints the next recommended command, such as
rerunning `wake-watch` for silent/stale logs or using `wake-then-profile` for
diagnostics/native context/simulator runs after real IPC wake evidence appears.

Analyzer commands also accept `--latest` instead of `--log PATH`. By default it
selects the newest `data\can_logs\live\*.candump`; add `--pattern GLOB` to pick
the newest matching profile log, for example
`--pattern "cantool_ipc-power-toggle-watch_rx_*.candump"`.

For wait-for-first-RX transmit profiles, add `--wait-rx-timeout-ms N` when you
want the process to exit if the IPC is silent. On timeout the tool writes
`# first RX wait timeout; no TX sent` to the log and sends nothing.

Use `--no-flush` with `capture` or `send-profile` when you want to preserve the
first post-open RX window. This is useful for IPC startup work because the
normal 300 ms stale-frame drain can otherwise discard immediate boot chatter.
Use `--log-flush` instead when you still want that 300 ms drain, but want the
drained frames written into the same candump-compatible log before the main run.

`ipc-simulator` is the staged Corsa E IPC profile. It opens the CANable, waits
for the first post-flush RX frame from the IPC, logs that trigger frame, and
only then starts a 30 second fake BCM/body-gateway sequence. The sequence uses a
quiet wake/context prelude, steady `13FFE040#` / `621#0040000000000000` keepalive,
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
- `ipc-bcm-live-object-probe` uses the quieter body baseline, then sends
  fake-BCM/source-`0x40` variants of the exact IPC startup/status frame
  families observed on the direct low-speed wire: `102440xx`, `104240xx`,
  `1045C0xx`, `10AC/10AE/10AF/10B0xx`, `106000xx`, `107740xx`, and the
  one-byte boot/status frames. It tests whether the IPC treats any source-`0x60`
  announcements as command/status families when sent from a BCM-like source,
  without the hard `621#0140...` wake burst.
- `ipc-restart-wake-only-probe`, `ipc-restart-nm-init-only-probe`,
  `ipc-restart-presence-only-probe`, and `ipc-restart-keepalive-only-probe`
  isolate the startup frames that previously appeared just before the IPC
  boot/status burst. Run these one at a time after the IPC is already
  transmitting. A restart after only `100#` points at SWCAN wake handling; a
  restart after only `621#0140000000000000` points at the network-init frame;
  no restart from the presence/keepalive-only runs supports keeping diagnostic
  profiles on the quiet baseline.
- `ipc-restart-wake-then-nm-init-probe` is for the common silent-bus bench
  state. It sends `100#` once, waits five seconds, then sends only the
  `621#0140000000000000` three-frame network-init burst. Analyze the resulting
  log with `analyze-restart` to see whether the boot/status sequence follows
  the initial wake, the delayed `621#0140...`, or neither.
- `ipc-diagnostic-probe` sends `24C#023E00AAAAAAAAAA` six times after the
  staged wake; a real IPC diagnostic reply should appear as `64C#027E00...`.
- `ipc-diagnostic-session-probe` sends single-shot `10 01`, `10 03`, `3E 80`,
  `3E 00`, and VIN DID `22 F1 90` requests on `24C` to map which basic UDS
  services the IPC accepts.
- `ipc-diagnostic-did-scan` enters extended diagnostic session, probes common
  read-only UDS DIDs and DTC services, and automatically sends ISO-TP flow
  control `24C#30000AAAAAAAAAAA` when the IPC starts a multi-frame `64C`
  response. The flow-control frame requests blocksize 0 and STmin `0x0A`
  because a zero-STmin run on the 33.3 kbit/s IPC bus missed consecutive frame
  sequence `21` during a VIN response.
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
- Diagnostic profiles use a quiet post-RX baseline: they no longer send the
  hard `100#` wake or `621#0140...` initiate burst after the first IPC frame.
  The latest AA00 repeat log showed the IPC boot/status burst immediately
  after that startup baseline, before any `AA 00` request was sent. Diagnostic
  profiles now keep only `13FFE040#` and `621#0040000000000000` alive at 1 Hz
  while probing `24C`.
- `ipc-diagnostic-gmlan-classic-probe` follows up the confirmed
  `24C#021003...` -> `64C#0150...` path with older read-only GMLAN services:
  tester-present `3E`, initiate/wake-links `10 04`, static identifier reads
  `1A 90/91/92/97/9A`, and dynamic packet reads `AA 00/01/02`. It uses
  automatic ISO-TP flow control for any multi-frame `64C` response and avoids
  security, programming, communication-disable, and write/control services.
- `ipc-diagnostic-isolated-classic-probe` repeats that classic read-only
  diagnostic path without periodic tester-present overlap. It spaces each
  `24C` request into its own response window and covers `10 03`, tester-present
  variants, `1A 90/91/92/97/9A`, and `AA 00` through `AA 0F`. When reviewing
  the log, pair each request with any `64C` or `54C` response within roughly
  250 ms; `AA 00` is especially interesting because an earlier run produced
  `54C#0000000000000000`. The clean 2026-05-29 21:49 run confirmed:
  `1A 90` returns VIN `W0L0XEP68G4104545`, `1A 9A` returns `0A 5F`,
  `013E` tester-present is accepted, `023E00` / `023E80` are rejected with
  NRC `0x12`, and `AA 00` consistently produces the all-zero `54C` response.
- `ipc-diagnostic-confirmed-identity-probe` is the short repeatable version of
  the confirmed 21:49 path. It waits for first IPC RX, then sends only `10 03`,
  accepted `013E`, VIN `1A 90`, confirmed `1A 9A`, and `AA 00`. Use this when
  the IPC wake window is short and you want a quick proof that diagnostics are
  still alive before running longer scans.
- `ipc-diagnostic-classic-1a-scan` is the next focused follow-up after the
  isolated classic probe. It uses the accepted `10 03` session, keeps the
  session alive with the working `013EAAAAAAAAAAAA` tester-present shape, and
  scans classic `1A` identifiers `80-BF` and `F0-FF`. Positive responses start
  with `5A`; the confirmed VIN path was `1A 90`.
- `ipc-diagnostic-classic-1a-c0-ef-scan` fills the remaining classic `1A`
  identifier gap from `C0` through `EF`, using the same quiet baseline,
  accepted tester-present shape, and ISO-TP flow-control timing.
- `ipc-diagnostic-aa00-repeat-probe` repeats only `AA 00` in isolated windows
  to check whether the `54C#0000000000000000` response is stable, stateful, or
  changes after multiple requests. It stays read-only and does not send
  security, programming, communication-disable, write, or control services.
- `ipc-standard-engine-warning-probe` waits for IPC RX, then sends the
  requested standard 11-bit probe set: `0C9#0004C40000500000` every 20 ms and
  the three `1E5` warning/service variants staggered every 100 ms, with
  `13FFE040#` and `621#0040000000000000` kept alive at 1 Hz.
- `ipc-astra-h-ls-reference-probe` waits for IPC RX, then sends the Astra H
  LS-CAN/SWCAN reference set from Car-CAN-Message-DB under the normal staged
  baseline. It keeps `108#2320980004E50000` speed/RPM, `145` engine-state /
  coolant variants, `160` / `170` power-style frames, brightness, fuel,
  outside-temperature, voltage, and a short `260` turn/hazard cycle alive.
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
- `ipc-gm-opel-hmi-probe` waits for IPC RX, then runs a 90 second GM/Opel
  HMI/body/DIC probe before going back to gauge-heavy tests. It keeps the
  priority baseline alive, sends the `24C#021003AAAAAAAAAA` IPC diagnostic
  sanity check, then tries dimming (`10022040`, `0EB`), Astra J steering-wheel
  button press/release frames (`10438040`), key/door/lock context candidates
  (`10754040`, `10630040`, `10414040`), Corsa D body leads (`0AA`, `131`,
  `15E`), spaced chime variants (`1001E058`, `1001E040`, cautious
  `1001E060`), and short ARB/DIC text-family probes (`10300040` through
  `1030C040` with a tentative `TEST` payload).
- `ipc-native-body-seed-probe` replays real Corsa E standard-ID body seeds
  mined from the saved BCM captures. It does not wait for first RX, so it can be
  started just before applying IPC ignition/run power. It keeps the normal wake
  prelude alive, then sends observed `0F1`, `451`, `12A`, `135`, `137`, `139`,
  `160`, `1F1`, `4E1`, and `514` payload variants at roughly their native
  cadences. This is a body-context replay profile, not a fuzz profile.
- `ipc-native-context-lite` is the gentler wake/context variant. It also starts
  immediately with no wait-for-RX, avoids the `621#0140...` initiate burst, and
  sends the same native body context at lower rates while holding `13FFE040#`,
  `621#0040...`, and `10002040#03`. Use it while physically toggling IPC pin 8
  run/crank power if the full native replay is too busy.
- `ipc-native-keyon-context` is narrower still: it replays the dominant key-on
  payloads from `analysis\generated\native_body_state_deltas.md`, especially
  `160#803C96B503` and `1F1#AE0F173E18000072`, which are the strongest
  keyoff-to-keyon body-state deltas. Use this before broader fuzzing when the
  cluster is awake enough to receive but missing key-on context.
- `ipc-native-key-transition` sends the dominant key-off body context first,
  then transitions to the dominant key-on context with a fresh `100#` wake edge.
  Use it when static key-on context does not help and you want to mimic the
  saved vehicle's keyoff-to-keyon bus transition.
- `ipc-native-expanded-keyon` adds the top all-ID key-on delta candidates from
  `analysis\generated\can_state_id_deltas.md`, including `0C1`, `0C5`, `0D1`,
  `1C8`, `1E1`, `1F3`, `210`, `214`, `2F9`, `3CB`, `17D`, and `1E5`. It is
  intentionally opt-in because some of these may be gateway-side context rather
  than direct IPC inputs.
- `ipc-priority-tier1-probe`, `ipc-priority-tier2-probe`, and
  `ipc-priority-tier3-probe` replay the current priority-table seed payloads
  under one staged quiet baseline: `100#`, `621#004...`, `13FFE040#`,
  `10002040#03`, and one `24C#021003AAAAAAAAAA` diagnostic sanity check. Run
  tier 1, then tier 2, then tier 3 while watching the IPC.
- `ipc-priority-all-probe` runs those three seed tiers in separated windows
  under one longer baseline session.
- `ipc-priority-tier1-byte-fuzz` and `ipc-priority-tier2-byte-fuzz` are focused
  follow-ups. Use them after the matching seed profile shows no visible result;
  they fuzz only the high-priority bytes from the priority table rather than
  repeating the broad 11-bit sweep.

Transmit scheduling compensates for USB/RX polling overhead by advancing each
periodic frame from its previous due time and draining pending RX frames between
TX batches. This keeps 50 ms and 100 ms simulator probes closer to their target
periods without changing saved log format.

`default-bench` is the non-waiting Corsa E IPC test flow. It sends the quiet
normal wake prelude, then nonzero GMLAN Bible-derived payload probes for
vehicle speed/RPM, battery voltage, system power mode, and chime.

In `firmware-wake`, `100#` is sent once at startup, `621#0140000000000000` is
sent as a three-frame startup burst, and steady keepalive is handled by
`13FFE040#` plus `621#0040000000000000` at 1 Hz.

Each transmit run writes normal RX candump rows plus `# tx ...` comment lines
showing transmitted frames. Existing parsers ignore those comments, so saved
logs remain candump-compatible. TX runs also do a short final RX drain and log
it as `# final RX drain ...`, so late local echoes or delayed IPC responses stay
with the run that caused them instead of leaking into the next command.

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

Use `send-profile --profile read-only` when you want a passive receive-only
profile. It opens the gs_usb adapter in listen-only mode, waits until Ctrl+C by
default, writes every RX frame to the normal candump log, and never transmits
CAN frames or automatic ISO-TP flow-control replies.

Use `capture --listen-only` when you want the older direct capture command
instead of a named profile.

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
