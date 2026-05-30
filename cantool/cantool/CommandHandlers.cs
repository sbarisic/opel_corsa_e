using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace cantool;

internal static class CommandHandlers
{
    private static readonly Regex TxCommentRegex = new(@"^# tx t=(?<rel>[0-9.]+)s (?<id>[0-9A-Fa-f]+)#(?<data>[0-9A-Fa-f]*)\s+note=(?<note>.*)$", RegexOptions.Compiled);
    private static readonly Regex RxLineRegex = new(@"^\((?<ts>[^)]+)\)\s+\S+\s+(?<id>[0-9A-Fa-f]+)#(?<data>[0-9A-Fa-f]*)", RegexOptions.Compiled);
    private static readonly Regex WakeMatrixPhaseRegex = new(@"^# wake-matrix phase=(?<phase>\S+)\s+name=(?<name>\S+)\s+active_100=(?<active>\S+)(?:\s+active_nm_init=(?<nm>\S+))?\s+instruction=(?<instruction>.*)$", RegexOptions.Compiled);
    private static readonly HashSet<uint> IpcBootBurstIds =
    [
        0x10244060, 0x103BC060, 0x103D6060, 0x103DA060, 0x10424060,
        0x10448060, 0x1045C060, 0x10774060, 0x10812060, 0x1084A060,
        0x10A04060, 0x10ACE060, 0x10AE8060, 0x10AFC060, 0x10B0A060,
        0x10600060
    ];
    private static readonly HashSet<uint> IpcWakeEvidenceStandardIds = [0x54C, 0x62C, 0x64C];
    private static readonly HashSet<uint> CommonToolTxIds =
    [
        0x100, 0x621, 0x24C, 0x0AA, 0x13FFE040, 0x10002040,
        0x10754040, 0x10030040, 0x0C030040
    ];

    private const string IpcIsoTpFlowControlPayload = "30000AAAAAAAAAAA";

    private static readonly TimeSpan MaxSchedulerSleep = TimeSpan.FromMilliseconds(2);

    public static int InteractiveMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("cantool - IPC low-speed GMLAN profile runner");
            Console.WriteLine("Select a profile to run:");
            for (var i = 0; i < Profiles.Available.Length; i++)
            {
                var profile = Profiles.Available[i];
                Console.WriteLine($"  {i + 1}) {profile.Name,-20} {profile.Description}");
            }

            Console.WriteLine();
            Console.WriteLine("Wake-first tools:");
            Console.WriteLine("  w) wake-watch --stop-on-wake: no TX, stop after physical IPC wake evidence");
            Console.WriteLine("  m) wake-matrix --manual-advance: guided pin 3 / pin 4 physical wake test with log phase markers");
            Console.WriteLine($"  d) wake then diagnostics: wait for first IPC RX, then run {Profiles.IpcDiagnosticGmlanClassicProbe}");
            Console.WriteLine($"  i) wake then isolated diagnostics: wait for first IPC RX, then run {Profiles.IpcDiagnosticIsolatedClassicProbe}");
            Console.WriteLine($"  c) wake then confirmed identity: wait for first IPC RX, then run {Profiles.IpcDiagnosticConfirmedIdentityProbe}");
            Console.WriteLine($"  s) wake then simulator: wait for first IPC RX, then run {Profiles.IpcSimulator}");
            Console.WriteLine("  q) quit");
            Console.Write("Profile [1]: ");
            var profileText = (Console.ReadLine() ?? "").Trim();
            if (profileText.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (profileText.Equals("w", StringComparison.OrdinalIgnoreCase))
            {
                var wakeWatchResult = RunInteractiveWakeWatch();
                if (wakeWatchResult != 0)
                {
                    return wakeWatchResult;
                }

                continue;
            }

            if (profileText.Equals("m", StringComparison.OrdinalIgnoreCase))
            {
                var wakeMatrixResult = RunInteractiveWakeMatrix();
                if (wakeMatrixResult != 0)
                {
                    return wakeMatrixResult;
                }

                continue;
            }

            if (profileText.Equals("d", StringComparison.OrdinalIgnoreCase))
            {
                var wakeDiagnosticResult = RunInteractiveWakeThenProfile(Profiles.IpcDiagnosticGmlanClassicProbe);
                if (wakeDiagnosticResult != 0)
                {
                    return wakeDiagnosticResult;
                }

                continue;
            }

            if (profileText.Equals("i", StringComparison.OrdinalIgnoreCase))
            {
                var isolatedDiagnosticResult = RunInteractiveWakeThenProfile(Profiles.IpcDiagnosticIsolatedClassicProbe);
                if (isolatedDiagnosticResult != 0)
                {
                    return isolatedDiagnosticResult;
                }

                continue;
            }

            if (profileText.Equals("c", StringComparison.OrdinalIgnoreCase))
            {
                var confirmedIdentityResult = RunInteractiveWakeThenProfile(Profiles.IpcDiagnosticConfirmedIdentityProbe);
                if (confirmedIdentityResult != 0)
                {
                    return confirmedIdentityResult;
                }

                continue;
            }

            if (profileText.Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                var wakeSimulatorResult = RunInteractiveWakeThenProfile(Profiles.IpcSimulator);
                if (wakeSimulatorResult != 0)
                {
                    return wakeSimulatorResult;
                }

                continue;
            }

            var selected = ResolveInteractiveProfile(profileText);
            if (selected is null)
            {
                Console.WriteLine("Unknown profile selection.");
                continue;
            }

            var defaultSeconds = selected.DefaultSeconds;
            Console.Write($"Duration seconds [{defaultSeconds:0}] (0 = until Ctrl+C): ");
            var secondsText = (Console.ReadLine() ?? "").Trim();
            if (!TryParseDuration(secondsText, defaultSeconds, out var seconds))
            {
                Console.WriteLine("Invalid duration.");
                continue;
            }

            var duration = seconds <= 0 ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds);
            if (selected.ReadOnlyCapture)
            {
                var captureResult = CaptureReadOnly(
                    duration,
                    DefaultLiveOutput($"cantool_{selected.Name}_rx"),
                    listenOnly: selected.ReadOnlyListenOnly,
                    flushStale: selected.ReadOnlyFlushStale,
                    logFlush: false,
                    banner: duration is null
                        ? $"read-only sniffing {selected.Name} until Ctrl+C"
                        : $"read-only sniffing {selected.Name} for {seconds:0.###}s");
                if (captureResult != 0)
                {
                    return captureResult;
                }

                continue;
            }

            var logPrefix = $"cantool_{selected.Name}_tx_rx";
            var result = SendSchedule(
                Profiles.GetSchedule(selected.Name),
                duration,
                countLimit: null,
                rxLogPath: DefaultLiveOutput(logPrefix),
                banner: duration is null
                    ? $"transmitting {selected.Name} until Ctrl+C"
                    : $"transmitting {selected.Name} for {seconds:0.###}s",
                waitForFirstRx: selected.WaitForFirstRx,
                autoIsoTpFlowControl: selected.AutoIsoTpFlowControl);
            if (result != 0)
            {
                return result;
            }
        }
    }

    private static int RunInteractiveWakeWatch()
    {
        Console.Write("Wake-watch duration seconds [0] (0 = until Ctrl+C): ");
        var secondsText = (Console.ReadLine() ?? "").Trim();
        if (!TryParseDuration(secondsText, defaultSeconds: 0, out var seconds))
        {
            Console.WriteLine("Invalid duration.");
            return 0;
        }

        return seconds > 0
            ? WakeWatch(["--seconds", seconds.ToString(CultureInfo.InvariantCulture), "--stop-on-wake"])
            : WakeWatch(["--stop-on-wake"]);
    }

    private static int RunInteractiveWakeMatrix()
    {
        Console.Write("Wake-matrix phase seconds [12]: ");
        var secondsText = (Console.ReadLine() ?? "").Trim();
        if (!TryParseDuration(secondsText, defaultSeconds: 12, out var seconds) || seconds <= 0)
        {
            Console.WriteLine("Invalid duration.");
            return 0;
        }

        return WakeMatrix([
            "--phase-seconds",
            seconds.ToString(CultureInfo.InvariantCulture),
            "--manual-advance"
        ]);
    }

    private static int RunInteractiveWakeThenProfile(string profileName)
    {
        var defaultSeconds = Profiles.GetDefaultSeconds(profileName);
        Console.Write($"Duration seconds [{defaultSeconds:0}] for {profileName}: ");
        var secondsText = (Console.ReadLine() ?? "").Trim();
        if (!TryParseDuration(secondsText, defaultSeconds, out var seconds))
        {
            Console.WriteLine("Invalid duration.");
            return 0;
        }

        if (seconds <= 0)
        {
            seconds = defaultSeconds;
        }

        return WakeThenProfile([
            "--profile",
            profileName,
            "--seconds",
            seconds.ToString(CultureInfo.InvariantCulture)
        ]);
    }

    public static int DefaultTxRx()
    {
        return SendSchedule(
            Profiles.GetSchedule(Profiles.DefaultBench),
            duration: null,
            countLimit: null,
            rxLogPath: DefaultLiveOutput("cantool_default_tx_rx"),
            banner: "transmitting default-bench profile until Ctrl+C");
    }

    public static int ListDevices()
    {
        var found = 0;
        foreach (var (vid, pid, name) in GsUsbDevice.KnownDevices)
        {
            var device = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vid, pid));
            if (device is null)
            {
                continue;
            }

            found++;
            Console.WriteLine($"{name}: VID=0x{vid:X4} PID=0x{pid:X4} path={device.DevicePath}");
            device.Close();
        }

        Console.WriteLine(found == 0 ? "no gs_usb/candleLight devices found" : $"found {found} device(s)");
        return found == 0 ? 1 : 0;
    }

    public static int BenchHealth(string[] args)
    {
        var seconds = CliOptions.GetDouble(args, "--seconds", 5.0);
        var captureDuration = TimeSpan.FromSeconds(Math.Max(0.1, seconds));

        Console.WriteLine("bench health: device scan");
        var listResult = ListDevices();
        if (listResult != 0)
        {
            return listResult;
        }

        var passiveLog = DefaultLiveOutput("cantool_bench_health_passive");
        Console.WriteLine();
        Console.WriteLine("bench health: passive normal-mode read-only capture with startup-drain logging");
        var passiveResult = CaptureReadOnly(
            captureDuration,
            passiveLog,
            listenOnly: false,
            flushStale: true,
            logFlush: true,
            banner: $"bench-health passive capture for {captureDuration.TotalSeconds:0.###}s");
        if (passiveResult != 0)
        {
            return passiveResult;
        }

        var echoLog = DefaultLiveOutput("cantool_bench_health_echo");
        Console.WriteLine();
        Console.WriteLine("bench health: harmless local echo probe");
        var echoResult = SendSchedule(
            [
                new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(100), true, "bench-health harmless fake BCM presence echo probe", MaxSends: 3)
            ],
            duration: TimeSpan.FromMilliseconds(300),
            countLimit: null,
            rxLogPath: echoLog,
            banner: "bench-health echo probe",
            flushStale: true,
            logFlush: true);
        if (echoResult != 0)
        {
            return echoResult;
        }

        var cleanLog = DefaultLiveOutput("cantool_bench_health_cleancheck");
        Console.WriteLine();
        Console.WriteLine("bench health: follow-up capture to detect leaked echo backlog or live IPC traffic");
        var cleanResult = CaptureReadOnly(
            captureDuration,
            cleanLog,
            listenOnly: false,
            flushStale: true,
            logFlush: true,
            banner: $"bench-health clean-check capture for {captureDuration.TotalSeconds:0.###}s");
        if (cleanResult != 0)
        {
            return cleanResult;
        }

        Console.WriteLine();
        Console.WriteLine("bench health summaries:");
        Summarize(["--log", passiveLog]);
        Summarize(["--log", echoLog, "--ignore-tx-echo"]);
        Summarize(["--log", cleanLog]);
        Console.WriteLine($"bench health logs: passive={passiveLog}");
        Console.WriteLine($"bench health logs: echo={echoLog}");
        Console.WriteLine($"bench health logs: clean={cleanLog}");
        return 0;
    }

    public static int Capture(string[] args)
    {
        var seconds = CliOptions.GetDouble(args, "--seconds", 5.0);
        var duration = seconds <= 0 ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds);
        var listenOnly = CliOptions.HasFlag(args, "--listen-only");
        var flushStale = !CliOptions.HasFlag(args, "--no-flush");
        var logFlush = flushStale && CliOptions.HasFlag(args, "--log-flush");
        var outPath = CliOptions.GetString(args, "--out") ?? DefaultLiveOutput("cantool_gsusb_33333");

        return CaptureReadOnly(duration, outPath, listenOnly, flushStale, logFlush, null);
    }

    public static int WakeWatch(string[] args)
    {
        var seconds = CliOptions.GetDouble(args, "--seconds", 0.0);
        var duration = seconds <= 0 ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds);
        var outPath = CliOptions.GetString(args, "--out") ?? DefaultLiveOutput("cantool_wake_watch");
        var preclearMs = CliOptions.HasFlag(args, "--no-preclear")
            ? 0.0
            : CliOptions.GetDouble(args, "--preclear-ms", 300.0);
        var stopOnWake = CliOptions.HasFlag(args, "--stop-on-wake");
        if (preclearMs < 0)
        {
            throw new ArgumentException("--preclear-ms must be >= 0");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        Console.WriteLine("wake-watch: normal-mode no-TX capture; adapter will ACK IPC frames");
        Console.WriteLine(preclearMs > 0
            ? $"wake-watch: preclearing existing RX backlog for {preclearMs:0.###} ms before arming"
            : "wake-watch: preclear disabled; existing adapter backlog will be logged");
        if (stopOnWake)
        {
            Console.WriteLine("wake-watch: stop-on-wake enabled; capture stops after first known IPC wake frame");
        }

        Console.WriteLine("wake-watch: wait for ARMED, then toggle/apply IPC pin 8 run/crank +12 V");

        var count = 0;
        using (var device = GsUsbDevice.Open(listenOnly: false))
        using (var done = new ConsoleCancelScope())
        using (var writer = new StreamWriter(outPath, append: false))
        {
            writer.WriteLine("# cantool wake-watch capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");
            writer.WriteLine("# mode=normal read-only no-tx");
            if (stopOnWake)
            {
                writer.WriteLine("# wake-watch stop-on-wake enabled");
            }

            if (preclearMs > 0)
            {
                var cleared = device.FlushStale(TimeSpan.FromMilliseconds(preclearMs));
                writer.WriteLine($"# wake-watch preclear discarded_count={cleared} duration_ms={preclearMs:0.###}");
                Console.WriteLine(cleared == 0
                    ? "wake-watch preclear discarded 0 frame(s)"
                    : $"wake-watch preclear discarded {cleared} stale frame(s)");
            }
            else
            {
                writer.WriteLine("# wake-watch preclear skipped");
            }

            writer.WriteLine("# wake-watch ARMED; toggle/apply IPC pin 8 run/crank +12 V now");
            writer.Flush();
            Console.WriteLine("wake-watch ARMED: toggle/apply IPC pin 8 run/crank +12 V now");
            Console.WriteLine(duration is null
                ? $"wake-watch capture until Ctrl+C; RX log {outPath}"
                : $"wake-watch capture for {seconds:0.###}s; RX log {outPath}");

            var stopAt = duration is null ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow + duration.Value;
            while (!done.IsSet && DateTimeOffset.UtcNow < stopAt)
            {
                var frameMaybe = device.ReadFrame(timeoutMs: 100);
                if (frameMaybe is not { } frame)
                {
                    continue;
                }

                count++;
                writer.WriteLine(frame.ToCandumpLine());
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatRx(count, frame));
                if (stopOnWake && IsIpcWakeEvidenceFrame(frame))
                {
                    writer.WriteLine($"# wake-watch stop-on-wake triggered by {frame.CandumpId}#{frame.DataHex}");
                    writer.Flush();
                    Console.WriteLine($"wake-watch STOP-ON-WAKE: IPC wake evidence {frame.CandumpId}#{frame.DataHex}");
                    break;
                }
            }
        }

        Console.WriteLine($"done: {count} frames rx_log={outPath}");
        Console.WriteLine();
        Console.WriteLine("wake-watch analysis:");
        return AnalyzeWake(["--log", outPath]);
    }

    public static int WakeMatrix(string[] args)
    {
        var phaseSeconds = CliOptions.GetDouble(args, "--phase-seconds", 12.0);
        var outPath = CliOptions.GetString(args, "--out") ?? DefaultLiveOutput("cantool_wake_matrix");
        var preclearMs = CliOptions.HasFlag(args, "--no-preclear")
            ? 0.0
            : CliOptions.GetDouble(args, "--preclear-ms", 300.0);
        var stopOnWake = !CliOptions.HasFlag(args, "--no-stop-on-wake");
        var passiveOnly = CliOptions.HasFlag(args, "--passive-only");
        var includeNmInit = CliOptions.HasFlag(args, "--include-nm-init");
        var manualAdvance = CliOptions.HasFlag(args, "--manual-advance");
        if (phaseSeconds <= 0)
        {
            throw new ArgumentException("--phase-seconds must be > 0");
        }

        if (preclearMs < 0)
        {
            throw new ArgumentException("--preclear-ms must be >= 0");
        }

        var phases = new List<WakeMatrixPhase>
        {
            new("pin3-passive", false, false, "connect SWCAN/CAN-H lead to IPC low-speed pin 3; toggle/apply pin 8 +12 V during this phase"),
            new("pin4-passive", false, false, "move SWCAN/CAN-H lead to IPC low-speed pin 4; toggle/apply pin 8 +12 V during this phase"),
        };
        if (!passiveOnly)
        {
            phases.Add(new("pin3-active-100", true, false, "connect SWCAN/CAN-H lead to IPC low-speed pin 3; toggle pin 8; tool sends 100# once per second"));
            phases.Add(new("pin4-active-100", true, false, "move SWCAN/CAN-H lead to IPC low-speed pin 4; toggle pin 8; tool sends 100# once per second"));
            if (includeNmInit)
            {
                phases.Add(new("pin3-active-nm-init", true, true, "connect SWCAN/CAN-H lead to IPC low-speed pin 3; toggle pin 8; tool sends 100# plus sparse 621#0140 network-init"));
                phases.Add(new("pin4-active-nm-init", true, true, "move SWCAN/CAN-H lead to IPC low-speed pin 4; toggle pin 8; tool sends 100# plus sparse 621#0140 network-init"));
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        Console.WriteLine("wake-matrix: normal-mode capture; adapter will ACK IPC frames");
        Console.WriteLine($"wake-matrix: {phases.Count} phase(s), {phaseSeconds:0.###}s each, stop_on_wake={stopOnWake}, include_nm_init={includeNmInit}, manual_advance={manualAdvance}");
        Console.WriteLine("wake-matrix: keep pin 7 +12 V and pin 19 ground; change only low-speed pin and pin 8 as prompted");

        var sent = 0;
        var received = 0;
        using (var device = GsUsbDevice.Open(listenOnly: false))
        using (var done = new ConsoleCancelScope())
        using (var writer = new StreamWriter(outPath, append: false))
        {
            writer.WriteLine("# cantool wake-matrix capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");
            writer.WriteLine("# mode=normal guided physical wake matrix");
            writer.WriteLine($"# phase_seconds={phaseSeconds:0.###} stop_on_wake={stopOnWake} passive_only={passiveOnly} include_nm_init={includeNmInit} manual_advance={manualAdvance}");
            if (preclearMs > 0)
            {
                var cleared = device.FlushStale(TimeSpan.FromMilliseconds(preclearMs));
                writer.WriteLine($"# wake-matrix preclear discarded_count={cleared} duration_ms={preclearMs:0.###}");
                Console.WriteLine(cleared == 0
                    ? "wake-matrix preclear discarded 0 frame(s)"
                    : $"wake-matrix preclear discarded {cleared} stale frame(s)");
            }
            else
            {
                writer.WriteLine("# wake-matrix preclear skipped");
            }

            writer.Flush();

            var wakePulse = new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "wake-matrix active phase SWCAN wake 100#", MaxSends: 1);
            var nmInit = new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.Zero, false, "wake-matrix active phase 621#0140 network-init", MaxSends: 1);
            var woke = false;
            for (var index = 0; index < phases.Count && !done.IsSet && !woke; index++)
            {
                var phase = phases[index];
                var phaseNumber = index + 1;
                var phaseLine = $"# wake-matrix phase={phaseNumber}/{phases.Count} name={phase.Name} active_100={phase.SendWake100} active_nm_init={phase.SendNmInit} instruction={phase.Instruction}";
                writer.WriteLine(phaseLine);
                writer.Flush();
                Console.WriteLine();
                Console.WriteLine($"wake-matrix phase {phaseNumber}/{phases.Count}: {phase.Name}");
                Console.WriteLine(phase.Instruction);
                if (manualAdvance)
                {
                    Console.Write("Set wiring for this phase, then press Enter to capture...");
                    Console.ReadLine();
                    writer.WriteLine($"# wake-matrix phase-start-confirmed name={phase.Name}");
                    writer.Flush();
                }

                var phaseEnd = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(phaseSeconds);
                var nextWake = DateTimeOffset.UtcNow;
                var nextNmInit = DateTimeOffset.UtcNow;
                while (!done.IsSet && DateTimeOffset.UtcNow < phaseEnd)
                {
                    if (phase.SendWake100 && DateTimeOffset.UtcNow >= nextWake)
                    {
                        device.SendFrame(wakePulse);
                        sent++;
                        writer.WriteLine(ConsoleFrames.FormatTxLogComment(wakePulse));
                        writer.Flush();
                        Console.WriteLine(ConsoleFrames.FormatTx(sent, wakePulse));
                        nextWake += TimeSpan.FromSeconds(1);
                    }

                    if (phase.SendNmInit && DateTimeOffset.UtcNow >= nextNmInit)
                    {
                        device.SendFrame(nmInit);
                        sent++;
                        writer.WriteLine(ConsoleFrames.FormatTxLogComment(nmInit));
                        writer.Flush();
                        Console.WriteLine(ConsoleFrames.FormatTx(sent, nmInit));
                        nextNmInit += TimeSpan.FromSeconds(3);
                    }

                    var frameMaybe = device.ReadFrame(timeoutMs: 25);
                    if (frameMaybe is not { } frame)
                    {
                        continue;
                    }

                    received++;
                    writer.WriteLine(frame.ToCandumpLine());
                    writer.Flush();
                    Console.WriteLine(ConsoleFrames.FormatRx(received, frame));
                    if (stopOnWake && IsIpcWakeEvidenceFrame(frame))
                    {
                        writer.WriteLine($"# wake-matrix stop-on-wake phase={phase.Name} triggered_by={frame.CandumpId}#{frame.DataHex}");
                        writer.Flush();
                        Console.WriteLine($"wake-matrix STOP-ON-WAKE: phase {phase.Name} saw IPC wake evidence {frame.CandumpId}#{frame.DataHex}");
                        woke = true;
                        break;
                    }
                }
            }
        }

        Console.WriteLine($"done: sent={sent} rx_frames={received} rx_log={outPath}");
        Console.WriteLine();
        Console.WriteLine("wake-matrix analysis:");
        return AnalyzeWake(["--log", outPath]);
    }

    public static int WakeThenProfile(string[] args)
    {
        var profile = CliOptions.GetString(args, "--profile") ?? throw new ArgumentException("--profile is required");
        var definition = Profiles.Find(profile) ?? throw new ArgumentException($"unknown profile: {profile}");
        if (definition.ReadOnlyCapture)
        {
            throw new ArgumentException("wake-then-profile requires a transmit profile, not a read-only capture profile");
        }

        var seconds = CliOptions.GetDouble(args, "--seconds", Profiles.GetDefaultSeconds(profile));
        var duration = TimeSpan.FromSeconds(seconds);
        var outPath = CliOptions.GetString(args, "--rx-log") ?? DefaultLiveOutput($"cantool_wake_then_{profile}_tx_rx");
        var preclear = !CliOptions.HasFlag(args, "--no-preclear") && !CliOptions.HasFlag(args, "--no-flush");

        Console.WriteLine($"wake-then-profile: preclear={(preclear ? "on" : "off")} profile={profile} duration={seconds:0.###}s");
        Console.WriteLine("wake-then-profile: wait for the first IPC RX, then start the selected TX profile");
        return SendSchedule(
            Profiles.GetSchedule(profile),
            duration,
            countLimit: null,
            rxLogPath: outPath,
            banner: $"wake-then-profile {profile}",
            waitForFirstRx: true,
            waitForFirstRxTimeout: ReadOptionalTimeout(args, "--wait-rx-timeout-ms"),
            flushStale: preclear,
            logFlush: false,
            autoIsoTpFlowControl: Profiles.UsesAutoIsoTpFlowControl(profile));
    }

    public static int Send(string[] args)
    {
        var idText = CliOptions.GetString(args, "--id") ?? throw new ArgumentException("--id is required");
        var canId = CliOptions.ParseUInt(idText);
        var data = CliOptions.ParseHexData(CliOptions.GetString(args, "--data") ?? "");
        var period = TimeSpan.FromMilliseconds(CliOptions.GetDouble(args, "--period-ms", 1000.0));
        var count = CliOptions.GetInt(args, "--count", 1);
        var seconds = CliOptions.GetDouble(args, "--seconds", 0);
        var isExtended = CliOptions.HasFlag(args, "--extended") || (!CliOptions.HasFlag(args, "--standard") && canId > 0x7FF);
        var frame = new ScheduledTxFrame(canId, data, period, isExtended, $"manual {idText}");

        var schedule = new List<ScheduledTxFrame> { frame };
        var duration = seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.FromMilliseconds(Math.Max(1, count) * period.TotalMilliseconds);
        return SendSchedule(schedule, duration, countLimit: seconds > 0 ? null : count, CliOptions.GetString(args, "--rx-log"));
    }

    public static int SendProfile(string[] args)
    {
        var profile = CliOptions.GetString(args, "--profile") ?? Profiles.FirmwareWake;
        var seconds = CliOptions.GetDouble(args, "--seconds", Profiles.GetDefaultSeconds(profile));
        var duration = seconds <= 0 ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds);
        if (Profiles.IsReadOnlyCapture(profile))
        {
            return CaptureReadOnly(
                duration,
                CliOptions.GetString(args, "--rx-log") ?? DefaultLiveOutput($"cantool_{profile}_rx"),
                listenOnly: Profiles.IsReadOnlyListenOnly(profile),
                flushStale: Profiles.ShouldReadOnlyFlushStale(profile) && !CliOptions.HasFlag(args, "--no-flush"),
                logFlush: !CliOptions.HasFlag(args, "--no-flush") && CliOptions.HasFlag(args, "--log-flush"),
                banner: duration is null
                    ? $"read-only sniffing {profile} until Ctrl+C"
                    : $"read-only sniffing {profile} for {seconds:0.###}s");
        }

        return SendSchedule(
            Profiles.GetSchedule(profile),
            duration,
            countLimit: null,
            CliOptions.GetString(args, "--rx-log"),
            waitForFirstRx: Profiles.WaitsForFirstRx(profile),
            waitForFirstRxTimeout: ReadOptionalTimeout(args, "--wait-rx-timeout-ms"),
            flushStale: !CliOptions.HasFlag(args, "--no-flush"),
            logFlush: !CliOptions.HasFlag(args, "--no-flush") && CliOptions.HasFlag(args, "--log-flush"),
            autoIsoTpFlowControl: Profiles.UsesAutoIsoTpFlowControl(profile));
    }

    private static int CaptureReadOnly(TimeSpan? duration, string outPath, bool listenOnly, bool flushStale, bool logFlush, string? banner)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        using var device = GsUsbDevice.Open(listenOnly);
        using var done = new ConsoleCancelScope();

        Console.WriteLine(banner ?? (duration is null
            ? $"capturing until Ctrl+C to {outPath}"
            : $"capturing {duration.Value.TotalSeconds:F1}s to {outPath}"));

        var count = 0;
        using (var writer = new StreamWriter(outPath, append: false))
        {
            writer.WriteLine("# cantool gs_usb direct capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");
            writer.WriteLine(listenOnly ? "# mode=listen-only read-only no-tx" : "# mode=normal read-only no-tx");
            if (!flushStale)
            {
                writer.WriteLine("# stale RX flush skipped");
            }

            if (flushStale && logFlush)
            {
                DrainStartupRxToLog(device, writer, ref count, TimeSpan.FromMilliseconds(300));
            }
            else if (flushStale)
            {
                var flushed = device.FlushStale(TimeSpan.FromMilliseconds(300));
                if (flushed > 0)
                {
                    Console.WriteLine($"flushed {flushed} stale RX frame(s)");
                }
            }
            else
            {
                Console.WriteLine("stale RX flush skipped");
            }

            var stopAt = duration is null ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow + duration.Value;
            while (!done.IsSet && DateTimeOffset.UtcNow < stopAt)
            {
                var frameMaybe = device.ReadFrame(timeoutMs: 100);
                if (frameMaybe is not { } frame)
                {
                    continue;
                }

                count++;
                var line = frame.ToCandumpLine();
                writer.WriteLine(line);
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatRx(count, frame));
            }
        }

        Console.WriteLine($"done: {count} frames rx_log={outPath}");
        return 0;
    }

    public static int Summarize(string[] args)
    {
        var logPath = ResolveLogPath(args);
        var ignoreTxEcho = CliOptions.HasFlag(args, "--ignore-tx-echo");
        var ignoredEchoes = 0;
        var records = ignoreTxEcho
            ? ParseRxExcludingTxEchoes(logPath, out ignoredEchoes)
            : CandumpParser.Parse(logPath).ToList();
        var groups = records
            .GroupBy(r => (r.CanId, r.IsExtended))
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.CanId)
            .ToList();

        Console.WriteLine($"{logPath}: {records.Count} frames / {groups.Count} unique IDs");
        if (ignoreTxEcho)
        {
            Console.WriteLine($"ignored_tx_echoes={ignoredEchoes}");
        }

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(r => r.Timestamp).ToList();
            var periods = ordered.Zip(ordered.Skip(1), (a, b) => (b.Timestamp - a.Timestamp) * 1000.0).ToList();
            var periodText = periods.Count == 0 ? "single" : Median(periods).ToString("0.###", CultureInfo.InvariantCulture);
            var dlcs = string.Join(";", ordered.GroupBy(r => r.Data.Length).OrderBy(g => g.Key).Select(g => $"{g.Key}:{g.Count()}"));
            var firstPayload = ordered.First().DataHex;
            var idText = ordered.First().CandumpId;
            var format = group.Key.IsExtended ? "extended" : "standard";
            var gmlan = group.Key.IsExtended ? Gmlan29Id.Decode(group.Key.CanId).ToAnnotatedSummaryString() : "";
            Console.WriteLine($"{idText,12} {format,-8} {gmlan,-76} count={ordered.Count,5} period_ms={periodText,10} dlcs={dlcs} payload={firstPayload}");
        }

        return 0;
    }

    private static List<CanFrame> ParseRxExcludingTxEchoes(string logPath, out int ignoredEchoes)
    {
        var events = ParseLogEvents(logPath).ToList();
        var pendingTx = new List<LogEvent>();
        var rx = new List<CanFrame>();
        ignoredEchoes = 0;

        foreach (var item in events)
        {
            if (item.Kind == LogEventKind.Tx)
            {
                pendingTx.Add(item);
                continue;
            }

            var echoIndex = pendingTx.FindIndex(tx => SameFrame(tx.Frame, item.Frame));
            if (echoIndex >= 0)
            {
                pendingTx.RemoveAt(echoIndex);
                ignoredEchoes++;
                continue;
            }

            rx.Add(item.Frame);
        }

        return rx;
    }

    public static int AnalyzeDiagnostics(string[] args)
    {
        var logPath = ResolveLogPath(args);
        var positiveOnly = CliOptions.HasFlag(args, "--positive-only") || CliOptions.HasFlag(args, "--positives-only");
        var events = ParseLogEvents(logPath).ToList();
        var requests = events
            .Where(IsDiagnosticTxRequest)
            .ToList();

        Console.WriteLine($"{logPath}: {requests.Count} diagnostic request(s)");
        if (requests.Count == 0)
        {
            return 0;
        }

        var positiveResponses = new List<(int Line, byte Sid, byte? Identifier, byte[] Data, string Summary)>();
        foreach (var request in requests)
        {
            var nextRequestLine = events
                .Where(item => IsDiagnosticTxRequest(item) && item.LineNumber > request.LineNumber)
                .Select(item => item.LineNumber)
                .DefaultIfEmpty(int.MaxValue)
                .First();
            var window = events
                .Where(item => item.LineNumber > request.LineNumber && item.LineNumber < nextRequestLine)
                .ToList();

            var response = window.FirstOrDefault(item =>
                item.Kind == LogEventKind.Rx &&
                !item.Frame.IsExtended &&
                (item.Frame.CanId == 0x64C || item.Frame.CanId == 0x54C));
            var requestPayload = DecodeIsoTpPayload(request.Frame.Data, []);
            var requestText = DescribeDiagnosticPayload(requestPayload, request.Frame.Data, isRequest: true);

            if (response is null)
            {
                if (!positiveOnly)
                {
                    Console.WriteLine($"line {request.LineNumber,5}: 24C#{request.Frame.DataHex,-16} {requestText} -> no 64C/54C before next request");
                }

                continue;
            }

            var responsePayload = DecodeResponsePayload(response, window);
            var responseText = DescribeDiagnosticPayload(responsePayload, response.Frame.Data, isRequest: false) + DescribeIsoTpCompleteness(response, window);
            TrackPositiveDiagnosticResponse(positiveResponses, request.LineNumber, responsePayload, responseText);
            var latency = response.Frame.Timestamp > 0 && window.Any(item => item.Kind == LogEventKind.Rx && item.Frame.CanId == request.Frame.CanId && item.Frame.DataHex == request.Frame.DataHex)
                ? $" latency~{EstimateLatencyMs(request, response, window):0}ms"
                : "";
            if (!positiveOnly || IsPositiveDiagnosticPayload(responsePayload))
            {
                Console.WriteLine($"line {request.LineNumber,5}: 24C#{request.Frame.DataHex,-16} {requestText} -> {response.Frame.CandumpId}#{response.Frame.DataHex,-16} {responseText}{latency}");
            }
        }

        PrintPositiveDiagnosticSummary(positiveResponses);
        return 0;
    }

    public static int AnalyzeRestart(string[] args)
    {
        var logPath = ResolveLogPath(args);
        var windowMs = CliOptions.GetDouble(args, "--window-ms", 500.0);
        var minUnique = CliOptions.GetInt(args, "--min-unique", 3);
        var verbose = CliOptions.HasFlag(args, "--verbose");
        var events = ParseLogEvents(logPath).ToList();
        var txEvents = events.Where(item => item.Kind == LogEventKind.Tx).ToList();
        var noHitCount = 0;
        var hitCount = 0;
        var weakCount = 0;

        Console.WriteLine($"{logPath}: {txEvents.Count} TX event(s), restart window {windowMs:0.###} ms, min_unique={minUnique}");
        if (txEvents.Count == 0)
        {
            return 0;
        }

        foreach (var tx in txEvents)
        {
            var echo = events.FirstOrDefault(item =>
                item.Kind == LogEventKind.Rx &&
                item.LineNumber > tx.LineNumber &&
                SameFrame(item.Frame, tx.Frame));
            var searchStartLine = echo?.LineNumber ?? tx.LineNumber;
            var txTimestamp = echo?.Frame.Timestamp;

            var candidates = events
                .Where(item =>
                    item.Kind == LogEventKind.Rx &&
                    item.LineNumber > searchStartLine &&
                    item.Frame.IsExtended &&
                    IpcBootBurstIds.Contains(item.Frame.CanId))
                .ToList();

            if (txTimestamp is not null)
            {
                candidates = candidates
                    .Where(item => item.Frame.Timestamp >= txTimestamp.Value &&
                        (item.Frame.Timestamp - txTimestamp.Value) * 1000.0 <= windowMs)
                    .ToList();
            }
            else
            {
                candidates = candidates.Take(20).ToList();
            }

            if (candidates.Count == 0)
            {
                noHitCount++;
                if (verbose)
                {
                    Console.WriteLine($"line {tx.LineNumber,5}: {tx.Frame.CandumpId}#{tx.Frame.DataHex,-16} no IPC boot/status burst in window  note={tx.Note}");
                }

                continue;
            }

            var first = candidates[0];
            var latency = txTimestamp is null
                ? "latency unknown"
                : $"latency~{(first.Frame.Timestamp - txTimestamp.Value) * 1000.0:0}ms";
            var unique = candidates.Select(item => item.Frame.CanId).Distinct().Count();
            var sample = string.Join(", ", candidates.Take(5).Select(item => $"{item.Frame.CandumpId}#{item.Frame.DataHex}"));
            if (unique < minUnique)
            {
                weakCount++;
                if (verbose)
                {
                    Console.WriteLine($"line {tx.LineNumber,5}: {tx.Frame.CandumpId}#{tx.Frame.DataHex,-16} weak boot/status match candidates={candidates.Count} unique={unique} {latency} first_line={first.LineNumber} sample={sample} note={tx.Note}");
                }

                continue;
            }

            Console.WriteLine($"line {tx.LineNumber,5}: {tx.Frame.CandumpId}#{tx.Frame.DataHex,-16} -> boot/status candidates={candidates.Count} unique={unique} {latency} first_line={first.LineNumber} sample={sample} note={tx.Note}");
            hitCount++;
        }

        Console.WriteLine($"summary: {hitCount} TX event(s) followed by boot/status bursts; {weakCount} weak match(es); {noHitCount} without a boot/status hit");
        return 0;
    }

    public static int AnalyzeWake(string[] args)
    {
        var logPath = ResolveLogPath(args);
        var events = ParseLogEvents(logPath).ToList();
        var wakeMatrixPhases = ParseWakeMatrixPhases(logPath).ToList();
        var txEvents = events.Where(item => item.Kind == LogEventKind.Tx).ToList();
        var rxEvents = events.Where(item => item.Kind == LogEventKind.Rx).ToList();
        var ignoredEchoes = 0;
        var nonEchoRx = txEvents.Count == 0
            ? rxEvents.Select(item => item.Frame).ToList()
            : ParseRxExcludingTxEchoes(logPath, out ignoredEchoes);

        Console.WriteLine($"{logPath}: tx={txEvents.Count} rx={rxEvents.Count} non_echo_rx={nonEchoRx.Count} ignored_tx_echoes={ignoredEchoes}");
        if (rxEvents.Count == 0)
        {
            Console.WriteLine("verdict: clean silent capture; no IPC wake evidence");
            PrintWakeMatrixPhaseSummary(wakeMatrixPhases);
            PrintWakeNextActions(WakeVerdict.CleanSilent);
            return 0;
        }

        var orderedRx = rxEvents.OrderBy(item => item.Frame.Timestamp).ToList();
        var rxSpanMs = (orderedRx.Last().Frame.Timestamp - orderedRx.First().Frame.Timestamp) * 1000.0;
        var startupBurstMs = (orderedRx.Take(Math.Min(orderedRx.Count, 64)).Last().Frame.Timestamp - orderedRx.First().Frame.Timestamp) * 1000.0;
        var likelyStaleBacklog = txEvents.Count == 0 &&
            rxEvents.Count >= 5 &&
            startupBurstMs <= 100.0 &&
            nonEchoRx.All(frame => CommonToolTxIds.Contains(frame.CanId));

        var ipcSource60 = nonEchoRx
            .Where(frame => frame.IsExtended && (frame.CanId & 0x1FFF) == 0x060)
            .ToList();
        var ipcBoot = nonEchoRx.Where(frame => frame.IsExtended && IpcBootBurstIds.Contains(frame.CanId)).ToList();
        var standardEvidence = nonEchoRx.Where(frame => !frame.IsExtended && IpcWakeEvidenceStandardIds.Contains(frame.CanId)).ToList();
        var unknownNonEcho = nonEchoRx
            .Where(frame => !ipcSource60.Contains(frame) && !standardEvidence.Contains(frame))
            .ToList();

        Console.WriteLine($"rx_span_ms={rxSpanMs:0.###} startup64_span_ms={startupBurstMs:0.###}");
        Console.WriteLine($"ipc_source_060={ipcSource60.Count} ipc_boot_status={ipcBoot.Count} standard_62c_64c_54c={standardEvidence.Count}");
        PrintWakeMatrixPhaseSummary(wakeMatrixPhases);
        PrintFirstWakeEvidencePhase(rxEvents, wakeMatrixPhases);

        if (likelyStaleBacklog)
        {
            Console.WriteLine("verdict: likely stale local-echo backlog from a previous TX profile; rerun the watch profile for a clean baseline");
            PrintWakeNextActions(WakeVerdict.StaleBacklog);
        }
        else if (ipcSource60.Count > 0 || standardEvidence.Count > 0)
        {
            Console.WriteLine("verdict: IPC wake evidence present");
            PrintWakeNextActions(WakeVerdict.IpcAwake);
        }
        else if (txEvents.Count > 0 && nonEchoRx.Count == 0)
        {
            Console.WriteLine("verdict: only exact local echoes of this log's TX frames; no IPC wake evidence");
            PrintWakeNextActions(WakeVerdict.TxEchoOnly);
        }
        else if (nonEchoRx.Count == 0)
        {
            Console.WriteLine("verdict: no non-echo RX frames; no IPC wake evidence");
            PrintWakeNextActions(WakeVerdict.CleanSilent);
        }
        else
        {
            Console.WriteLine("verdict: RX present but no known IPC wake signature; inspect unknown IDs below");
            PrintWakeNextActions(WakeVerdict.UnknownRx);
        }

        PrintWakeSamples("IPC source 0x060", ipcSource60);
        PrintWakeSamples("standard wake evidence", standardEvidence);
        PrintWakeSamples("other non-echo RX", unknownNonEcho);
        return 0;
    }

    private static void PrintWakeNextActions(WakeVerdict verdict)
    {
        Console.WriteLine("next:");
        switch (verdict)
        {
            case WakeVerdict.IpcAwake:
                Console.WriteLine("  1. dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- wake-then-profile --profile ipc-diagnostic-gmlan-classic-probe --seconds 36 --wait-rx-timeout-ms 5000");
                Console.WriteLine("  2. dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- wake-then-profile --profile ipc-native-keyon-context --seconds 45 --wait-rx-timeout-ms 5000");
                Console.WriteLine("  3. dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- wake-then-profile --profile ipc-simulator --seconds 30 --wait-rx-timeout-ms 5000");
                break;
            case WakeVerdict.StaleBacklog:
                Console.WriteLine("  dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- wake-watch");
                Console.WriteLine("  If it reports ARMED cleanly, toggle/apply IPC pin 8 run/crank +12 V after ARMED.");
                break;
            case WakeVerdict.TxEchoOnly:
                Console.WriteLine("  dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- wake-watch");
                Console.WriteLine("  Software TX is working, but no IPC response was seen; re-check pin 7 +12 V, pin 8 +12 V, pin 19 ground, and GMLAN pin 3/4 side.");
                break;
            case WakeVerdict.UnknownRx:
                Console.WriteLine("  dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- summarize --latest --ignore-tx-echo");
                Console.WriteLine("  Save the log and compare unknown IDs against known body/BCM captures before sending fuzz.");
                break;
            case WakeVerdict.CleanSilent:
            default:
                Console.WriteLine("  dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- wake-watch");
                Console.WriteLine("  Wait for ARMED, then toggle/apply IPC pin 8 run/crank +12 V; also try both low-speed GMLAN pins 3 and 4.");
                break;
        }
    }

    private static void PrintWakeMatrixPhaseSummary(List<WakeMatrixPhaseMarker> phases)
    {
        if (phases.Count == 0)
        {
            return;
        }

        Console.WriteLine($"wake_matrix_phases={phases.Count} ({string.Join(", ", phases.Select(item => item.Name))})");
    }

    private static void PrintFirstWakeEvidencePhase(List<LogEvent> rxEvents, List<WakeMatrixPhaseMarker> phases)
    {
        var firstWake = rxEvents
            .Where(item => IsIpcWakeEvidenceFrame(item.Frame))
            .OrderBy(item => item.LineNumber)
            .FirstOrDefault();
        if (firstWake is null)
        {
            return;
        }

        Console.WriteLine($"first_wake_evidence: line={firstWake.LineNumber} {firstWake.Frame.CandumpId}#{firstWake.Frame.DataHex}");
        var phase = phases
            .Where(item => item.LineNumber < firstWake.LineNumber)
            .OrderBy(item => item.LineNumber)
            .LastOrDefault();
        if (phase is null)
        {
            return;
        }

        Console.WriteLine($"first_wake_phase: {phase.Phase} {phase.Name} active_100={phase.Active100} active_nm_init={phase.ActiveNmInit} instruction={phase.Instruction}");
    }

    private static bool IsIpcWakeEvidenceFrame(CanFrame frame)
    {
        if (frame.IsExtended)
        {
            return (frame.CanId & 0x1FFF) == 0x060 || IpcBootBurstIds.Contains(frame.CanId);
        }

        return IpcWakeEvidenceStandardIds.Contains(frame.CanId);
    }

    private static IEnumerable<WakeMatrixPhaseMarker> ParseWakeMatrixPhases(string path)
    {
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            var match = WakeMatrixPhaseRegex.Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            yield return new WakeMatrixPhaseMarker(
                lineNumber,
                match.Groups["phase"].Value,
                match.Groups["name"].Value,
                match.Groups["active"].Value.Equals("True", StringComparison.OrdinalIgnoreCase),
                match.Groups["nm"].Success && match.Groups["nm"].Value.Equals("True", StringComparison.OrdinalIgnoreCase),
                match.Groups["instruction"].Value);
        }
    }

    private static void PrintWakeSamples(string title, List<CanFrame> frames)
    {
        if (frames.Count == 0)
        {
            return;
        }

        Console.WriteLine($"{title}:");
        foreach (var group in frames
            .GroupBy(frame => (frame.CanId, frame.IsExtended))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.CanId)
            .Take(12))
        {
            var first = group.OrderBy(frame => frame.Timestamp).First();
            var gmlan = first.IsExtended ? Gmlan29Id.Decode(first.CanId).ToAnnotatedSummaryString() : "";
            Console.WriteLine($"  {first.CandumpId,12} {gmlan,-76} count={group.Count(),5} first_payload={first.DataHex}");
        }
    }

    public static int ProfileInfo(string[] args)
    {
        var profileName = CliOptions.GetString(args, "--profile");
        if (string.IsNullOrWhiteSpace(profileName))
        {
            Console.WriteLine("Available profiles:");
            foreach (var profile in Profiles.Available)
            {
                var mode = profile.ReadOnlyCapture
                    ? $"readonly listen_only={profile.ReadOnlyListenOnly,-5} flush={profile.ReadOnlyFlushStale,-5}"
                    : "tx-normal";
                Console.WriteLine($"{profile.Name,-36} default={profile.DefaultSeconds,6:0.#}s wait_rx={profile.WaitForFirstRx,-5} mode={mode,-40} isotp={profile.AutoIsoTpFlowControl,-5} {profile.Description}");
            }

            return 0;
        }

        var definition = Profiles.Find(profileName) ?? throw new ArgumentException($"unknown profile: {profileName}");
        var duration = definition.DefaultSeconds <= 0 ? (TimeSpan?)null : TimeSpan.FromSeconds(definition.DefaultSeconds);

        Console.WriteLine($"{definition.Name}: {definition.Description}");
        var profileMode = definition.ReadOnlyCapture
            ? $"read_only=true listen_only={definition.ReadOnlyListenOnly} read_only_flush={definition.ReadOnlyFlushStale}"
            : "tx_normal=true listen_only=false read_only_flush=n/a";
        Console.WriteLine($"default_seconds={definition.DefaultSeconds:0.###} wait_for_first_rx={definition.WaitForFirstRx} {profileMode} auto_isotp={definition.AutoIsoTpFlowControl}");
        if (definition.ReadOnlyCapture)
        {
            Console.WriteLine("read-only capture profile: no TX schedule");
            return 0;
        }

        var schedule = Profiles.GetSchedule(definition.Name);
        Console.WriteLine($"TX schedule: {schedule.Count} item(s)");
        foreach (var item in schedule.OrderBy(item => item.InitialDelay).ThenBy(item => item.CanId).ThenBy(item => Convert.ToHexString(item.Data)))
        {
            var sends = EstimateScheduledSends(item, duration);
            var sendsText = sends is null ? "unbounded" : sends.Value.ToString(CultureInfo.InvariantCulture);
            var periodText = item.Period <= TimeSpan.Zero ? "one-shot" : $"{item.Period.TotalMilliseconds:0.###}ms";
            Console.WriteLine($"{item.CandumpId}#{Convert.ToHexString(item.Data),-16} ext={item.IsExtended,-5} start={item.InitialDelay.TotalSeconds,7:0.###}s period={periodText,10} max={item.MaxSends?.ToString(CultureInfo.InvariantCulture) ?? "-",-4} sends_default={sendsText,-9} note={item.Note}");
        }

        return 0;
    }

    private static int? EstimateScheduledSends(ScheduledTxFrame frame, TimeSpan? duration)
    {
        if (duration is null)
        {
            return frame.MaxSends;
        }

        if (frame.InitialDelay >= duration.Value)
        {
            return 0;
        }

        var remaining = duration.Value - frame.InitialDelay;
        var byDuration = frame.Period <= TimeSpan.Zero
            ? 1
            : Math.Max(1, (int)Math.Ceiling(remaining.TotalMilliseconds / frame.Period.TotalMilliseconds));

        return frame.MaxSends is null ? byDuration : Math.Min(byDuration, frame.MaxSends.Value);
    }

    private static bool IsDiagnosticTxRequest(LogEvent item)
    {
        return item.Kind == LogEventKind.Tx &&
            item.Frame.CanId == 0x24C &&
            !item.Frame.IsExtended &&
            item.Frame.Data.Length > 0 &&
            (item.Frame.Data[0] & 0xF0) != 0x30;
    }

    private static bool SameFrame(CanFrame left, CanFrame right)
    {
        return left.CanId == right.CanId &&
            left.IsExtended == right.IsExtended &&
            left.Data.SequenceEqual(right.Data);
    }

    private static IEnumerable<LogEvent> ParseLogEvents(string path)
    {
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            var trimmed = line.Trim();

            var txMatch = TxCommentRegex.Match(trimmed);
            if (txMatch.Success)
            {
                var idText = txMatch.Groups["id"].Value;
                var dataText = txMatch.Groups["data"].Value;
                yield return new LogEvent(
                    lineNumber,
                    LogEventKind.Tx,
                    new CanFrame(
                        double.Parse(txMatch.Groups["rel"].Value, CultureInfo.InvariantCulture),
                        uint.Parse(idText, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        string.IsNullOrEmpty(dataText) ? [] : Convert.FromHexString(dataText),
                        idText.Length > 3),
                    txMatch.Groups["note"].Value);
                continue;
            }

            var rxMatch = RxLineRegex.Match(trimmed);
            if (!rxMatch.Success)
            {
                continue;
            }

            var rxIdText = rxMatch.Groups["id"].Value;
            var rxDataText = rxMatch.Groups["data"].Value;
            yield return new LogEvent(
                lineNumber,
                LogEventKind.Rx,
                new CanFrame(
                    double.Parse(rxMatch.Groups["ts"].Value, CultureInfo.InvariantCulture),
                    uint.Parse(rxIdText, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    string.IsNullOrEmpty(rxDataText) ? [] : Convert.FromHexString(rxDataText),
                    rxIdText.Length > 3),
                "");
        }
    }

    private static byte[] DecodeResponsePayload(LogEvent response, List<LogEvent> window)
    {
        var data = response.Frame.Data;
        if (data.Length == 0 || (data[0] & 0xF0) != 0x10)
        {
            return DecodeIsoTpPayload(data, []);
        }

        var totalLength = ((data[0] & 0x0F) << 8) | data[1];
        var bytes = new List<byte>();
        bytes.AddRange(data.Skip(2));

        foreach (var consecutive in window.Where(item =>
            item.Kind == LogEventKind.Rx &&
            item.LineNumber > response.LineNumber &&
            item.Frame.CanId == response.Frame.CanId &&
            !item.Frame.IsExtended &&
            item.Frame.Data.Length > 0 &&
            (item.Frame.Data[0] & 0xF0) == 0x20))
        {
            bytes.AddRange(consecutive.Frame.Data.Skip(1));
            if (bytes.Count >= totalLength)
            {
                break;
            }
        }

        return bytes.Take(totalLength).ToArray();
    }

    private static string DescribeIsoTpCompleteness(LogEvent response, List<LogEvent> window)
    {
        var data = response.Frame.Data;
        if (data.Length < 2 || (data[0] & 0xF0) != 0x10)
        {
            return "";
        }

        var totalLength = ((data[0] & 0x0F) << 8) | data[1];
        var byteCount = Math.Max(0, data.Length - 2);
        var expectedSequence = 1;
        var missingSequences = new List<int>();

        foreach (var consecutive in window.Where(item =>
            item.Kind == LogEventKind.Rx &&
            item.LineNumber > response.LineNumber &&
            item.Frame.CanId == response.Frame.CanId &&
            !item.Frame.IsExtended &&
            item.Frame.Data.Length > 0 &&
            (item.Frame.Data[0] & 0xF0) == 0x20))
        {
            var actualSequence = consecutive.Frame.Data[0] & 0x0F;
            while (expectedSequence != actualSequence)
            {
                missingSequences.Add(expectedSequence);
                expectedSequence = (expectedSequence + 1) & 0x0F;
            }

            byteCount += consecutive.Frame.Data.Length - 1;
            expectedSequence = (expectedSequence + 1) & 0x0F;
            if (byteCount >= totalLength)
            {
                break;
            }
        }

        if (byteCount >= totalLength && missingSequences.Count == 0)
        {
            return "";
        }

        var missing = missingSequences.Count == 0
            ? ""
            : $", missing_cf_seq={string.Join("/", missingSequences.Select(item => item.ToString("X", CultureInfo.InvariantCulture)))}";
        return $" [ISO-TP incomplete got={Math.Min(byteCount, totalLength)}/{totalLength}{missing}]";
    }

    private static byte[] DecodeIsoTpPayload(byte[] data, byte[] fallback)
    {
        if (data.Length == 0)
        {
            return fallback;
        }

        if ((data[0] & 0xF0) == 0x00)
        {
            var length = data[0] & 0x0F;
            return data.Skip(1).Take(Math.Min(length, data.Length - 1)).ToArray();
        }

        return fallback.Length == 0 ? data : fallback;
    }

    private static string DescribeDiagnosticPayload(byte[] payload, byte[] rawData, bool isRequest)
    {
        if (payload.Length == 0)
        {
            return rawData.All(item => item == 0)
                ? "raw all-zero response/status"
                : $"raw={Convert.ToHexString(rawData)}";
        }

        var sid = payload[0];
        if (!isRequest && sid == 0x7F && payload.Length >= 3)
        {
            return $"negative response to SID 0x{payload[1]:X2}, NRC 0x{payload[2]:X2} ({NegativeResponseName(payload[2])})";
        }

        if (isRequest)
        {
            return sid switch
            {
                0x10 when payload.Length >= 2 => $"request session/init sub=0x{payload[1]:X2}",
                0x1A when payload.Length >= 2 => $"request classic DID 0x{payload[1]:X2}",
                0x21 when payload.Length >= 2 => $"request local ID 0x{payload[1]:X2}",
                0x22 when payload.Length >= 3 => $"request DID 0x{payload[1]:X2}{payload[2]:X2}",
                0x3E => payload.Length >= 2 ? $"request tester-present sub=0x{payload[1]:X2}" : "request tester-present",
                0xAA when payload.Length >= 2 => $"request packet ID 0x{payload[1]:X2}",
                _ => $"request SID 0x{sid:X2} payload={Convert.ToHexString(payload)}",
            };
        }

        return sid switch
        {
            0x50 when payload.Length >= 2 => $"positive session/init sub=0x{payload[1]:X2}",
            0x5A when payload.Length >= 2 => DescribeClassicReadResponse(payload),
            0x61 when payload.Length >= 2 => $"positive local ID 0x{payload[1]:X2} data={Convert.ToHexString(payload.Skip(2).ToArray())} {AsciiHint(payload.Skip(2))}",
            0x62 when payload.Length >= 3 => $"positive DID 0x{payload[1]:X2}{payload[2]:X2} data={Convert.ToHexString(payload.Skip(3).ToArray())} {AsciiHint(payload.Skip(3))}",
            0x7E => payload.Length >= 2 ? $"positive tester-present sub=0x{payload[1]:X2}" : "positive tester-present",
            0xEA when payload.Length >= 2 => $"positive packet ID 0x{payload[1]:X2} data={Convert.ToHexString(payload.Skip(2).ToArray())} {AsciiHint(payload.Skip(2))}",
            _ => $"payload={Convert.ToHexString(payload)} {AsciiHint(payload)}",
        };
    }

    private static void TrackPositiveDiagnosticResponse(
        List<(int Line, byte Sid, byte? Identifier, byte[] Data, string Summary)> positives,
        int requestLine,
        byte[] payload,
        string responseText)
    {
        if (!IsPositiveDiagnosticPayload(payload))
        {
            return;
        }

        var sid = payload[0];
        byte? identifier = sid switch
        {
            0x5A or 0x61 or 0xEA when payload.Length >= 2 => payload[1],
            _ => null
        };
        var dataOffset = sid switch
        {
            0x5A or 0x61 or 0xEA => 2,
            0x62 => 3,
            _ => 1
        };
        var data = payload.Skip(Math.Min(dataOffset, payload.Length)).ToArray();
        positives.Add((requestLine, sid, identifier, data, responseText));
    }

    private static bool IsPositiveDiagnosticPayload(byte[] payload)
    {
        return payload.Length > 0 && payload[0] != 0x7F;
    }

    private static void PrintPositiveDiagnosticSummary(List<(int Line, byte Sid, byte? Identifier, byte[] Data, string Summary)> positives)
    {
        if (positives.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"positive diagnostic summary ({positives.Count}):");
        foreach (var item in positives)
        {
            var idText = item.Identifier is null ? "" : $" id=0x{item.Identifier.Value:X2}";
            var dataText = item.Data.Length == 0 ? "" : $" data={Convert.ToHexString(item.Data)}";
            var ascii = AsciiHint(item.Data);
            Console.WriteLine($"  line {item.Line,5}: sid=0x{item.Sid:X2}{idText}{dataText} {ascii}".TrimEnd());
        }
    }

    private static string DescribeClassicReadResponse(byte[] payload)
    {
        var identifier = payload[1];
        var value = payload.Skip(2).ToArray();
        var hex = Convert.ToHexString(value);
        var ascii = AsciiHint(value);
        return identifier == 0x90
            ? $"positive classic DID 0x90 VIN={Encoding.ASCII.GetString(value.Where(IsPrintableAscii).ToArray())}"
            : $"positive classic DID 0x{identifier:X2} data={hex} {ascii}";
    }

    private static string AsciiHint(IEnumerable<byte> bytes)
    {
        var array = bytes.ToArray();
        if (array.Length == 0 || array.Count(IsPrintableAscii) < Math.Max(3, array.Length / 2))
        {
            return "";
        }

        var text = Encoding.ASCII.GetString(array.Select(item => IsPrintableAscii(item) ? item : (byte)'.').ToArray()).Trim();
        return text.Length == 0 ? "" : $"ascii=\"{text}\"";
    }

    private static bool IsPrintableAscii(byte value) => value >= 0x20 && value <= 0x7E;

    private static string NegativeResponseName(byte code)
    {
        return code switch
        {
            0x11 => "serviceNotSupported",
            0x12 => "subFunctionNotSupported",
            0x13 => "incorrectMessageLengthOrInvalidFormat",
            0x22 => "conditionsNotCorrect",
            0x31 => "requestOutOfRange",
            0x33 => "securityAccessDenied",
            0x78 => "responsePending",
            _ => "unknown",
        };
    }

    private static double EstimateLatencyMs(LogEvent request, LogEvent response, List<LogEvent> window)
    {
        var echo = window.FirstOrDefault(item =>
            item.Kind == LogEventKind.Rx &&
            item.Frame.CanId == request.Frame.CanId &&
            item.Frame.DataHex == request.Frame.DataHex);
        return echo is null ? 0 : (response.Frame.Timestamp - echo.Frame.Timestamp) * 1000.0;
    }

    private static int SendSchedule(
        List<ScheduledTxFrame> schedule,
        TimeSpan? duration,
        int? countLimit,
        string? rxLogPath,
        string? banner = null,
        bool waitForFirstRx = false,
        TimeSpan? waitForFirstRxTimeout = null,
        bool flushStale = true,
        bool logFlush = false,
        bool autoIsoTpFlowControl = false)
    {
        var outPath = rxLogPath ?? DefaultLiveOutput("cantool_tx_rx");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        using var device = GsUsbDevice.Open(listenOnly: false);
        using var done = new ConsoleCancelScope();

        if (banner is not null)
        {
            Console.WriteLine($"{banner}; RX log {outPath}");
        }
        else
        {
            Console.WriteLine(duration is null
                ? $"transmitting until Ctrl+C; RX log {outPath}"
                : $"transmitting for {duration.Value.TotalSeconds:0.###}s; RX log {outPath}");
        }

        var sent = 0;
        var received = 0;
        using var writer = new StreamWriter(outPath, append: false);
        writer.WriteLine("# cantool TX/RX capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");
        if (!flushStale)
        {
            writer.WriteLine("# stale RX flush skipped");
        }

        if (flushStale && logFlush)
        {
            DrainStartupRxToLog(device, writer, ref received, TimeSpan.FromMilliseconds(300));
        }
        else if (flushStale)
        {
            var flushed = device.FlushStale(TimeSpan.FromMilliseconds(300));
            if (flushed > 0)
            {
                Console.WriteLine($"flushed {flushed} stale RX frame(s)");
            }
        }
        else
        {
            Console.WriteLine("stale RX flush skipped");
        }

        if (waitForFirstRx)
        {
            var waitText = waitForFirstRxTimeout is null
                ? "waiting for first RX before starting TX..."
                : $"waiting up to {waitForFirstRxTimeout.Value.TotalSeconds:0.###}s for first RX before starting TX...";
            Console.WriteLine(waitText);
            writer.WriteLine(waitForFirstRxTimeout is null
                ? "# waiting for first RX before starting TX"
                : $"# waiting for first RX before starting TX timeout_ms={waitForFirstRxTimeout.Value.TotalMilliseconds:0.###}");
            writer.Flush();

            var waitStarted = DateTimeOffset.UtcNow;
            var triggered = false;
            while (!done.IsSet)
            {
                if (waitForFirstRxTimeout is not null && DateTimeOffset.UtcNow - waitStarted >= waitForFirstRxTimeout.Value)
                {
                    var timeoutLine = "# first RX wait timeout; no TX sent";
                    writer.WriteLine(timeoutLine);
                    writer.Flush();
                    Console.WriteLine(timeoutLine);
                    Console.WriteLine($"done: sent=0 rx_frames={received} rx_log={outPath}");
                    return 0;
                }

                var trigger = device.ReadFrame(timeoutMs: 100);
                if (trigger is null)
                {
                    continue;
                }

                received++;
                writer.WriteLine(trigger.Value.ToCandumpLine());
                writer.WriteLine("# first RX observed; starting TX schedule");
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatRx(received, trigger.Value));
                Console.WriteLine("first RX observed; starting TX schedule");
                triggered = true;
                break;
            }

            if (!triggered)
            {
                writer.WriteLine("# first RX wait cancelled; no TX sent");
                writer.Flush();
                Console.WriteLine($"done: sent=0 rx_frames={received} rx_log={outPath}");
                return 0;
            }
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in schedule)
        {
            item.NextDue = now + item.InitialDelay;
        }

        var end = duration is null ? DateTimeOffset.MaxValue : now + duration.Value;

        while (!done.IsSet && DateTimeOffset.UtcNow < end && (countLimit is null || sent < countLimit.Value))
        {
            now = DateTimeOffset.UtcNow;
            foreach (var item in schedule)
            {
                if (now < item.NextDue || (countLimit is not null && sent >= countLimit.Value))
                {
                    continue;
                }

                device.SendFrame(item);
                sent++;
                item.SentCount++;
                item.NextDue = item.Period <= TimeSpan.Zero || (item.MaxSends is not null && item.SentCount >= item.MaxSends.Value)
                    ? DateTimeOffset.MaxValue
                    : item.NextDue + item.Period;
                writer.WriteLine(ConsoleFrames.FormatTxLogComment(item));
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatTx(sent, item));
            }

            DrainAvailableRx(device, writer, ref received, ref sent, autoIsoTpFlowControl);
            SleepUntilNextDue(schedule, end);
        }

        DrainFinalRxToLog(device, writer, ref received, ref sent, autoIsoTpFlowControl, TimeSpan.FromMilliseconds(300));
        Console.WriteLine($"done: sent={sent} rx_frames={received} rx_log={outPath}");
        return 0;
    }

    private static void DrainAvailableRx(
        GsUsbDevice device,
        StreamWriter writer,
        ref int received,
        ref int sent,
        bool autoIsoTpFlowControl)
    {
        while (true)
        {
            var frame = device.ReadFrame(timeoutMs: 1);
            if (frame is null)
            {
                return;
            }

            received++;
            writer.WriteLine(frame.Value.ToCandumpLine());
            writer.Flush();
            Console.WriteLine(ConsoleFrames.FormatRx(received, frame.Value));

            if (autoIsoTpFlowControl && IsIpcIsoTpFirstFrame(frame.Value))
            {
                var flowControl = CreateIpcIsoTpFlowControl("auto ISO-TP flow control for IPC 64C first frame");
                device.SendFrame(flowControl);
                sent++;
                writer.WriteLine(ConsoleFrames.FormatTxLogComment(flowControl));
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatTx(sent, flowControl));
            }
        }
    }

    private static void DrainStartupRxToLog(
        GsUsbDevice device,
        StreamWriter writer,
        ref int received,
        TimeSpan duration)
    {
        writer.WriteLine($"# startup RX drain begin duration_ms={duration.TotalMilliseconds:0.###}");
        writer.Flush();

        var before = received;
        var end = DateTimeOffset.UtcNow + duration;
        while (DateTimeOffset.UtcNow < end)
        {
            var frame = device.ReadFrame(timeoutMs: 10);
            if (frame is null)
            {
                continue;
            }

            received++;
            writer.WriteLine(frame.Value.ToCandumpLine());
            writer.Flush();
            Console.WriteLine(ConsoleFrames.FormatRx(received, frame.Value));
        }

        var drained = received - before;
        writer.WriteLine($"# startup RX drain end count={drained}");
        writer.Flush();
        Console.WriteLine(drained == 0
            ? "startup RX drain captured 0 frame(s)"
            : $"startup RX drain captured {drained} frame(s)");
    }

    private static void DrainFinalRxToLog(
        GsUsbDevice device,
        StreamWriter writer,
        ref int received,
        ref int sent,
        bool autoIsoTpFlowControl,
        TimeSpan duration)
    {
        writer.WriteLine($"# final RX drain begin duration_ms={duration.TotalMilliseconds:0.###}");
        writer.Flush();

        var before = received;
        var end = DateTimeOffset.UtcNow + duration;
        while (DateTimeOffset.UtcNow < end)
        {
            var frame = device.ReadFrame(timeoutMs: 10);
            if (frame is null)
            {
                continue;
            }

            received++;
            writer.WriteLine(frame.Value.ToCandumpLine());
            writer.Flush();
            Console.WriteLine(ConsoleFrames.FormatRx(received, frame.Value));

            if (autoIsoTpFlowControl && IsIpcIsoTpFirstFrame(frame.Value))
            {
                var flowControl = CreateIpcIsoTpFlowControl("auto ISO-TP flow control for IPC 64C first frame during final drain");
                device.SendFrame(flowControl);
                sent++;
                writer.WriteLine(ConsoleFrames.FormatTxLogComment(flowControl));
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatTx(sent, flowControl));
            }
        }

        var drained = received - before;
        writer.WriteLine($"# final RX drain end count={drained}");
        writer.Flush();
        Console.WriteLine(drained == 0
            ? "final RX drain captured 0 frame(s)"
            : $"final RX drain captured {drained} frame(s)");
    }

    private static bool IsIpcIsoTpFirstFrame(CanFrame frame)
    {
        return !frame.IsExtended &&
            frame.CanId == 0x64C &&
            frame.Data.Length >= 1 &&
            (frame.Data[0] & 0xF0) == 0x10;
    }

    private static ScheduledTxFrame CreateIpcIsoTpFlowControl(string note)
    {
        return new ScheduledTxFrame(
            0x24C,
            CliOptions.ParseHexData(IpcIsoTpFlowControlPayload),
            TimeSpan.Zero,
            false,
            $"{note}; blocksize=0 stmin=0x0A",
            MaxSends: 1);
    }

    private static void SleepUntilNextDue(List<ScheduledTxFrame> schedule, DateTimeOffset end)
    {
        var nextDue = schedule
            .Select(item => item.NextDue)
            .Where(due => due != DateTimeOffset.MaxValue)
            .DefaultIfEmpty(end)
            .Min();

        var wait = (nextDue < end ? nextDue : end) - DateTimeOffset.UtcNow;
        if (wait <= TimeSpan.Zero)
        {
            Thread.Yield();
            return;
        }

        Thread.Sleep(wait < MaxSchedulerSleep ? wait : MaxSchedulerSleep);
    }

    private static ProfileDefinition? ResolveInteractiveProfile(string input)
    {
        if (input.Length == 0)
        {
            return Profiles.Available[0];
        }

        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) &&
            index >= 1 &&
            index <= Profiles.Available.Length)
        {
            return Profiles.Available[index - 1];
        }

        return Profiles.Available.FirstOrDefault(profile => profile.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseDuration(string input, double defaultSeconds, out double seconds)
    {
        if (input.Length == 0)
        {
            seconds = defaultSeconds;
            return true;
        }

        return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds >= 0;
    }

    private static TimeSpan? ReadOptionalTimeout(string[] args, string name)
    {
        var value = CliOptions.GetString(args, name);
        if (value is null)
        {
            return null;
        }

        var milliseconds = double.Parse(value, CultureInfo.InvariantCulture);
        if (milliseconds < 0)
        {
            throw new ArgumentException($"{name} must be >= 0");
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static string ResolveLogPath(string[] args)
    {
        var explicitPath = CliOptions.GetString(args, "--log");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath;
        }

        if (!CliOptions.HasFlag(args, "--latest"))
        {
            throw new ArgumentException("--log is required, or use --latest");
        }

        var pattern = CliOptions.GetString(args, "--pattern") ?? "*.candump";
        var liveDir = Path.Combine(FindRepoRoot(), "data", "can_logs", "live");
        var latest = Directory.Exists(liveDir)
            ? Directory.EnumerateFiles(liveDir, pattern, SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Length)
                .FirstOrDefault()
            : null;

        if (latest is null)
        {
            throw new ArgumentException($"no candump logs found in {liveDir} matching {pattern}");
        }

        return latest.FullName;
    }

    private static string DefaultLiveOutput(string prefix)
    {
        var repo = FindRepoRoot();
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(repo, "data", "can_logs", "live", $"{prefix}_{stamp}.candump");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static double Median(List<double> values)
    {
        values.Sort();
        var mid = values.Count / 2;
        return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2.0;
    }

    private sealed class ConsoleCancelScope : IDisposable
    {
        private readonly ManualResetEventSlim _done = new(false);
        private readonly ConsoleCancelEventHandler _handler;

        public ConsoleCancelScope()
        {
            _handler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                _done.Set();
            };
            Console.CancelKeyPress += _handler;
        }

        public bool IsSet => _done.IsSet;

        public void Dispose()
        {
            Console.CancelKeyPress -= _handler;
            _done.Dispose();
        }
    }
}

internal enum LogEventKind
{
    Tx,
    Rx,
}

internal enum WakeVerdict
{
    CleanSilent,
    StaleBacklog,
    IpcAwake,
    TxEchoOnly,
    UnknownRx,
}

internal sealed record LogEvent(
    int LineNumber,
    LogEventKind Kind,
    CanFrame Frame,
    string Note);

internal sealed record WakeMatrixPhase(
    string Name,
    bool SendWake100,
    bool SendNmInit,
    string Instruction);

internal sealed record WakeMatrixPhaseMarker(
    int LineNumber,
    string Phase,
    string Name,
    bool Active100,
    bool ActiveNmInit,
    string Instruction);
