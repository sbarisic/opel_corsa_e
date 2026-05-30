using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace cantool;

internal static class CommandHandlers
{
    private static readonly Regex TxCommentRegex = new(@"^# tx t=(?<rel>[0-9.]+)s (?<id>[0-9A-Fa-f]+)#(?<data>[0-9A-Fa-f]*)\s+note=(?<note>.*)$", RegexOptions.Compiled);
    private static readonly Regex FuzzMarkRegex = new(@"^# mark t=(?<rel>[0-9.]+)s key=(?<key>\S+)\s+active=(?<id>[0-9A-Fa-f]+)#(?<data>[0-9A-Fa-f]*)\s+note=(?<note>.*)$", RegexOptions.Compiled);
    private static readonly Regex RxLineRegex = new(@"^\((?<ts>[^)]+)\)\s+\S+\s+(?<id>[0-9A-Fa-f]+)#(?<data>[0-9A-Fa-f]*)", RegexOptions.Compiled);
    private static readonly Regex WakeMatrixPhaseRegex = new(@"^# wake-matrix phase=(?<phase>\S+)\s+name=(?<name>\S+)\s+active_100=(?<active>\S+)(?:\s+active_nm_init=(?<nm>\S+))?\s+instruction=(?<instruction>.*)$", RegexOptions.Compiled);
    private static readonly HashSet<uint> IpcBootBurstIds =
    [
        0x10244060, 0x103BC060, 0x103D6060, 0x103DA060, 0x10424060,
        0x10448060, 0x1045C060, 0x10774060, 0x10812060, 0x1084A060,
        0x10A04060, 0x10ACE060, 0x10AE8060, 0x10AFC060, 0x10B0A060,
        0x10600060, 0x10308060
    ];
    private static readonly HashSet<uint> IpcEffectWatchIds =
    [
        0x10244060, 0x1045C060, 0x10ACE060, 0x10AE8060, 0x10AFC060,
        0x10308060, 0x624, 0x62C, 0x64C
    ];
    private static readonly HashSet<uint> IpcEffectTxIds =
    [
        0x624, 0x621, 0x1030A080, 0x1030C080, 0x1030A097, 0x1030C097,
        0x13FFE080, 0x13FFE058, 0x10030040, 0x10050040, 0x10052040,
        0x10064040, 0x1006E040, 0x1005E040, 0x100C4040, 0x10438040,
        0x10308040, 0x0AF
    ];
    private static readonly HashSet<uint> IpcWakeEvidenceStandardIds = [0x54C, 0x62C, 0x64C];
    private static readonly HashSet<uint> CommonToolTxIds =
    [
        0x100, 0x621, 0x24C, 0x0AA, 0x13FFE040, 0x10002040,
        0x10754040, 0x10030040, 0x0C030040
    ];

    private const string IpcIsoTpFlowControlPayload = "30000AAAAAAAAAAA";

    private static readonly TimeSpan MaxSchedulerSleep = TimeSpan.FromMilliseconds(2);
    private static readonly TimeSpan LiveReloadPollPeriod = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan RangeFuzzerActivePeriod = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan RangeFuzzerCandidateHold = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan RangeFuzzerProgressPeriod = TimeSpan.FromMilliseconds(100);
    private const uint RangeFuzzerProgressCanId = 0x10210040;
    private const int RangeFuzzerProgressMaxSpeedKmh = 100;
    private const int RangeFuzzerProgressMaxSpeedByte = 0x19;
    private static readonly TimeSpan RpmWordSweepActivePeriod = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan RpmWordSweepCandidateHold = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan RpmWordSweepProgressPeriod = TimeSpan.FromMilliseconds(100);
    private static readonly string[] RangeFuzzerPayloads =
    [
        "EEEEEEEEEEEEEEEE",
        "FFFFFFFFFFFFFFFF",
        "8888888888888888",
        "0000000000000000"
    ];

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
            var result = selected.Name.Equals(Profiles.IpcLiveReload, StringComparison.OrdinalIgnoreCase)
                ? SendLiveReloadProfile(
                    [],
                    duration,
                    rxLogPath: DefaultLiveOutput(logPrefix),
                    banner: duration is null
                        ? $"live-reload transmitting from send_profile.can until Ctrl+C"
                        : $"live-reload transmitting from send_profile.can for {seconds:0.###}s")
                : IsRangeFuzzerProfile(selected.Name)
                ? SendIpcRangeFuzzerProfile(
                    [],
                    duration,
                    selected.Name,
                    rxLogPath: DefaultLiveOutput(logPrefix),
                    banner: duration is null
                        ? $"interactive source-0x40 range fuzzer {FormatRangeFuzzerRange(GetRangeFuzzerRange(selected.Name))} until range complete or Ctrl+C"
                        : $"interactive source-0x40 range fuzzer {FormatRangeFuzzerRange(GetRangeFuzzerRange(selected.Name))} for {seconds:0.###}s")
                : selected.Name.Equals(Profiles.IpcRpmWordSweep, StringComparison.OrdinalIgnoreCase)
                ? SendIpcRpmWordSweepProfile(
                    [],
                    duration,
                    rxLogPath: DefaultLiveOutput(logPrefix),
                    banner: duration is null
                        ? "interactive full RPM word sweep until range complete or Ctrl+C"
                        : $"interactive full RPM word sweep for {seconds:0.###}s")
                : SendSchedule(
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
            Profiles.GetSchedule(Profiles.IpcSimulator),
            duration: null,
            countLimit: null,
            rxLogPath: DefaultLiveOutput("cantool_default_tx_rx"),
            banner: "transmitting ipc-simulator profile until Ctrl+C");
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
        var profile = CliOptions.GetString(args, "--profile") ?? Profiles.IpcSimulator;
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

        if (profile.Equals(Profiles.IpcLiveReload, StringComparison.OrdinalIgnoreCase))
        {
            return SendLiveReloadProfile(args, duration);
        }

        if (IsRangeFuzzerProfile(profile))
        {
            return SendIpcRangeFuzzerProfile(args, duration, profile);
        }

        if (profile.Equals(Profiles.IpcRpmWordSweep, StringComparison.OrdinalIgnoreCase))
        {
            return SendIpcRpmWordSweepProfile(args, duration);
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

    private static int SendLiveReloadProfile(
        string[] args,
        TimeSpan? duration,
        string? rxLogPath = null,
        string? banner = null)
    {
        var profilePath = Path.GetFullPath(CliOptions.GetString(args, "--file") ?? Path.Combine(FindRepoRoot(), "send_profile.can"));
        EnsureLiveReloadProfileFile(profilePath);

        var outPath = rxLogPath ?? CliOptions.GetString(args, "--rx-log") ?? DefaultLiveOutput("cantool_ipc-live-reload_tx_rx");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        using var device = GsUsbDevice.Open(listenOnly: false);
        using var done = new ConsoleCancelScope();

        Console.WriteLine(banner ?? (duration is null
            ? $"live-reload transmitting from {profilePath} until Ctrl+C; RX log {outPath}"
            : $"live-reload transmitting from {profilePath} for {duration.Value.TotalSeconds:0.###}s; RX log {outPath}"));
        Console.WriteLine("edit the file and save it; disabled/invalid rows are not transmitted");

        var sent = 0;
        var received = 0;
        var schedule = new List<ScheduledTxFrame>();
        var lastLoadedWriteUtc = DateTime.MinValue;

        using var writer = new StreamWriter(outPath, append: false);
        writer.WriteLine("# cantool live-reload TX/RX capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");
        writer.WriteLine($"# live-reload file={profilePath}");

        if (CliOptions.HasFlag(args, "--no-flush"))
        {
            writer.WriteLine("# stale RX flush skipped");
            Console.WriteLine("stale RX flush skipped");
        }
        else if (CliOptions.HasFlag(args, "--log-flush"))
        {
            DrainStartupRxToLog(device, writer, ref received, TimeSpan.FromMilliseconds(300));
        }
        else
        {
            var flushed = device.FlushStale(TimeSpan.FromMilliseconds(300));
            if (flushed > 0)
            {
                Console.WriteLine($"flushed {flushed} stale RX frame(s)");
            }
        }

        TryReloadLiveProfile(profilePath, ref lastLoadedWriteUtc, ref schedule, writer, force: true);

        var started = DateTimeOffset.UtcNow;
        var end = duration is null ? DateTimeOffset.MaxValue : started + duration.Value;
        var nextReloadCheck = DateTimeOffset.MinValue;

        while (!done.IsSet && DateTimeOffset.UtcNow < end)
        {
            var now = DateTimeOffset.UtcNow;
            if (now >= nextReloadCheck)
            {
                TryReloadLiveProfile(profilePath, ref lastLoadedWriteUtc, ref schedule, writer, force: false);
                nextReloadCheck = now + LiveReloadPollPeriod;
            }

            now = DateTimeOffset.UtcNow;
            foreach (var item in schedule)
            {
                if (now < item.NextDue)
                {
                    continue;
                }

                device.SendFrame(item);
                sent++;
                item.SentCount++;
                item.NextDue = item.NextDue + item.Period;
                writer.WriteLine(ConsoleFrames.FormatTxLogComment(item));
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatTx(sent, item));
            }

            DrainAvailableRx(device, writer, ref received, ref sent, autoIsoTpFlowControl: false);
            if (schedule.Count == 0)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }
            else
            {
                SleepUntilNextDue(schedule, end);
            }
        }

        DrainFinalRxToLog(device, writer, ref received, ref sent, autoIsoTpFlowControl: false, TimeSpan.FromMilliseconds(300));
        Console.WriteLine($"done: sent={sent} rx_frames={received} rx_log={outPath}");
        return 0;
    }

    private static int SendIpcRpmWordSweepProfile(
        string[] args,
        TimeSpan? duration,
        string? rxLogPath = null,
        string? banner = null)
    {
        var fullCandidates = BuildRpmWordSweepCandidates().ToList();
        var candidates = fullCandidates;
        var outPath = rxLogPath ?? CliOptions.GetString(args, "--rx-log") ?? DefaultLiveOutput($"cantool_{Profiles.IpcRpmWordSweep}_tx_rx");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        using var device = GsUsbDevice.Open(listenOnly: false);
        using var done = new ConsoleCancelScope();

        var baseline = BuildRpmWordSweepBaseline();
        var sent = 0;
        var received = 0;
        var paused = false;
        var stopRequested = false;
        var candidateIndex = 0;
        var activeNextDue = DateTimeOffset.MinValue;
        var activeWindowEnd = DateTimeOffset.MinValue;
        ScheduledTxFrame? activeFrame = null;
        var progressFrame = BuildRpmWordSweepProgressFrame(candidates[0]);
        var progressNextDue = DateTimeOffset.MinValue;

        Console.WriteLine(banner ?? (duration is null
            ? $"interactive full RPM word sweep until complete or Ctrl+C; RX log {outPath}"
            : $"interactive full RPM word sweep for {duration.Value.TotalSeconds:0.###}s; RX log {outPath}"));
        Console.WriteLine($"full candidates: {candidates.Count}/{fullCandidates.Count}; full indexes 1-{fullCandidates.Count}");
        Console.WriteLine("keys: m/space=mark, b=bad marker, n=skip candidate, p=pause/resume, q=quit");
        if (Console.IsInputRedirected)
        {
            Console.WriteLine("console input is redirected; keyboard markers are disabled for this run");
        }

        using var writer = new StreamWriter(outPath, append: false);
        writer.WriteLine($"# cantool {Profiles.IpcRpmWordSweep} TX/RX capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");
        writer.WriteLine($"# rpm-word-sweep mode=interactive upper_half=false full_sweep=true full_candidates={fullCandidates.Count} active_candidates={candidates.Count} first_full_index=1 active_period_ms={RpmWordSweepActivePeriod.TotalMilliseconds:0.###} hold_ms={RpmWordSweepCandidateHold.TotalMilliseconds:0.###}");
        writer.WriteLine($"# rpm-word-sweep progress_indicator id={RangeFuzzerProgressCanId:X8} period_ms={RpmWordSweepProgressPeriod.TotalMilliseconds:0.###} display=0-{RangeFuzzerProgressMaxSpeedKmh}kmh note=uses confirmed 10210040 speedometer feed");
        writer.WriteLine("# mark keys: m/space=manual event, b=bad/restart/disruptive, n=skip, p=pause/resume, q=quit");

        if (CliOptions.HasFlag(args, "--no-flush"))
        {
            writer.WriteLine("# stale RX flush skipped");
            Console.WriteLine("stale RX flush skipped");
        }
        else if (CliOptions.HasFlag(args, "--log-flush"))
        {
            DrainStartupRxToLog(device, writer, ref received, TimeSpan.FromMilliseconds(300));
        }
        else
        {
            var flushed = device.FlushStale(TimeSpan.FromMilliseconds(300));
            if (flushed > 0)
            {
                Console.WriteLine($"flushed {flushed} stale RX frame(s)");
            }
        }

        var started = DateTimeOffset.UtcNow;
        var end = duration is null ? DateTimeOffset.MaxValue : started + duration.Value;
        foreach (var item in baseline)
        {
            item.NextDue = started + item.InitialDelay;
        }
        progressNextDue = started;

        StartCandidate(started);

        while (!done.IsSet && !stopRequested && DateTimeOffset.UtcNow < end)
        {
            var now = DateTimeOffset.UtcNow;
            while (TryReadConsoleKey(out var keyInfo))
            {
                HandleRpmSweepKey(keyInfo);
                if (stopRequested)
                {
                    break;
                }
            }

            if (stopRequested)
            {
                break;
            }

            now = DateTimeOffset.UtcNow;
            foreach (var item in baseline)
            {
                if (now < item.NextDue)
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

            if (!paused && activeFrame is not null && now >= activeWindowEnd)
            {
                EndCandidate("completed");
                candidateIndex++;
                if (candidateIndex >= candidates.Count)
                {
                    Console.WriteLine("RPM word sweep complete");
                    break;
                }

                StartCandidate(now);
            }

            if (!paused && activeFrame is not null && now >= activeNextDue)
            {
                device.SendFrame(activeFrame);
                sent++;
                activeFrame.SentCount++;
                activeNextDue += activeFrame.Period;
                writer.WriteLine(ConsoleFrames.FormatTxLogComment(activeFrame));
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatTx(sent, activeFrame));
            }

            if (!paused && now >= progressNextDue)
            {
                if (activeFrame?.CanId != RangeFuzzerProgressCanId)
                {
                    device.SendFrame(progressFrame);
                    sent++;
                    progressFrame.SentCount++;
                    writer.WriteLine(ConsoleFrames.FormatTxLogComment(progressFrame));
                    writer.Flush();
                    Console.WriteLine(ConsoleFrames.FormatTx(sent, progressFrame));
                }

                progressNextDue += RpmWordSweepProgressPeriod;
            }

            DrainAvailableRx(device, writer, ref received, ref sent, autoIsoTpFlowControl: false);
            Thread.Sleep(MaxSchedulerSleep);
        }

        DrainFinalRxToLog(device, writer, ref received, ref sent, autoIsoTpFlowControl: false, TimeSpan.FromMilliseconds(300));
        Console.WriteLine($"done: sent={sent} rx_frames={received} rx_log={outPath}");
        return 0;

        void StartCandidate(DateTimeOffset now)
        {
            var candidate = candidates[candidateIndex];
            activeFrame = candidate.ToFrame(RpmWordSweepActivePeriod);
            progressFrame = BuildRpmWordSweepProgressFrame(candidate);
            activeNextDue = now;
            activeWindowEnd = now + RpmWordSweepCandidateHold;
            var line = $"# rpm-window-start t={FormatFuzzerElapsed()}s full_index={candidate.FullIndex + 1}/{candidate.FullCount} upper_index={candidateIndex + 1}/{candidates.Count} active={activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)} note={SanitizeLogValue(activeFrame.Note)}";
            writer.WriteLine(line);
            writer.Flush();
            Console.WriteLine(line);
        }

        void EndCandidate(string reason)
        {
            if (activeFrame is null)
            {
                return;
            }

            var candidate = candidates[candidateIndex];
            var line = $"# rpm-window-end t={FormatFuzzerElapsed()}s full_index={candidate.FullIndex + 1}/{candidate.FullCount} upper_index={candidateIndex + 1}/{candidates.Count} active={activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)} reason={SanitizeLogValue(reason)}";
            writer.WriteLine(line);
            writer.Flush();
        }

        void HandleRpmSweepKey(ConsoleKeyInfo keyInfo)
        {
            if (activeFrame is null)
            {
                return;
            }

            var key = keyInfo.Key;
            if (key == ConsoleKey.Spacebar || key == ConsoleKey.M)
            {
                WriteFuzzerMark(writer, key == ConsoleKey.Spacebar ? "space" : "m", activeFrame, "manual visible RPM event marker");
                Console.WriteLine($"marked {activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)}");
                return;
            }

            if (key == ConsoleKey.B)
            {
                WriteFuzzerMark(writer, "b", activeFrame, "bad/restart/disruptive marker");
                Console.WriteLine($"bad marker {activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)}");
                return;
            }

            if (key == ConsoleKey.N)
            {
                WriteFuzzerMark(writer, "n", activeFrame, "manual skip current RPM candidate");
                EndCandidate("manual skip");
                candidateIndex++;
                if (candidateIndex >= candidates.Count)
                {
                    stopRequested = true;
                    Console.WriteLine("skip reached end of RPM sweep");
                    return;
                }

                StartCandidate(DateTimeOffset.UtcNow);
                return;
            }

            if (key == ConsoleKey.P)
            {
                paused = !paused;
                WriteFuzzerMark(writer, "p", activeFrame, paused ? "paused" : "resumed");
                if (!paused)
                {
                    var now = DateTimeOffset.UtcNow;
                    activeNextDue = now;
                    activeWindowEnd = now + RpmWordSweepCandidateHold;
                    progressNextDue = now;
                }

                Console.WriteLine(paused ? "RPM sweep paused; press p to resume" : "RPM sweep resumed");
                return;
            }

            if (key == ConsoleKey.Q)
            {
                WriteFuzzerMark(writer, "q", activeFrame, "manual quit");
                stopRequested = true;
            }
        }
    }

    private static int SendIpcRangeFuzzerProfile(
        string[] args,
        TimeSpan? duration,
        string profileName,
        string? rxLogPath = null,
        string? banner = null)
    {
        var range = GetRangeFuzzerRange(profileName);
        var rangeLabel = FormatRangeFuzzerRange(range);
        var outPath = rxLogPath ?? CliOptions.GetString(args, "--rx-log") ?? DefaultLiveOutput($"cantool_{profileName}_tx_rx");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        using var device = GsUsbDevice.Open(listenOnly: false);
        using var done = new ConsoleCancelScope();

        var candidates = BuildRangeFuzzerCandidates(range).ToList();
        var baseline = BuildRangeFuzzerBaseline();
        var sent = 0;
        var received = 0;
        var paused = false;
        var stopRequested = false;
        var candidateIndex = 0;
        var activeNextDue = DateTimeOffset.MinValue;
        var activeWindowEnd = DateTimeOffset.MinValue;
        ScheduledTxFrame? activeFrame = null;
        var progressFrame = BuildRangeFuzzerProgressFrame(candidateIndex, candidates.Count);
        var progressNextDue = DateTimeOffset.MinValue;

        Console.WriteLine(banner ?? (duration is null
            ? $"interactive source-0x40 range fuzzer {rangeLabel} until complete or Ctrl+C; RX log {outPath}"
            : $"interactive source-0x40 range fuzzer {rangeLabel} for {duration.Value.TotalSeconds:0.###}s; RX log {outPath}"));
        Console.WriteLine("keys: m/space=mark, b=bad marker, n=skip candidate, p=pause/resume, q=quit");
        if (Console.IsInputRedirected)
        {
            Console.WriteLine("console input is redirected; keyboard markers are disabled for this run");
        }

        using var writer = new StreamWriter(outPath, append: false);
        writer.WriteLine($"# cantool {profileName} TX/RX capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");
        writer.WriteLine($"# fuzzer profile={profileName} source=0x040 arb_start=0x{range.FirstArbitrationId:X3} arb_end=0x{range.LastArbitrationId:X3} active_period_ms={RangeFuzzerActivePeriod.TotalMilliseconds:0.###} hold_ms={RangeFuzzerCandidateHold.TotalMilliseconds:0.###}");
        writer.WriteLine("# fuzzer payloads=EEEEEEEEEEEEEEEE,FFFFFFFFFFFFFFFF,8888888888888888,0000000000000000");
        writer.WriteLine($"# fuzzer progress_indicator id={RangeFuzzerProgressCanId:X8} period_ms={RangeFuzzerProgressPeriod.TotalMilliseconds:0.###} display=0-{RangeFuzzerProgressMaxSpeedKmh}kmh note=uses confirmed 10210040 speedometer feed");
        writer.WriteLine("# mark keys: m/space=manual event, b=bad/restart/disruptive, n=skip, p=pause/resume, q=quit");

        if (CliOptions.HasFlag(args, "--no-flush"))
        {
            writer.WriteLine("# stale RX flush skipped");
            Console.WriteLine("stale RX flush skipped");
        }
        else if (CliOptions.HasFlag(args, "--log-flush"))
        {
            DrainStartupRxToLog(device, writer, ref received, TimeSpan.FromMilliseconds(300));
        }
        else
        {
            var flushed = device.FlushStale(TimeSpan.FromMilliseconds(300));
            if (flushed > 0)
            {
                Console.WriteLine($"flushed {flushed} stale RX frame(s)");
            }
        }

        var started = DateTimeOffset.UtcNow;
        var end = duration is null ? DateTimeOffset.MaxValue : started + duration.Value;
        foreach (var item in baseline)
        {
            item.NextDue = started + item.InitialDelay;
        }
        progressNextDue = started;

        StartCandidate(started);

        while (!done.IsSet && !stopRequested && DateTimeOffset.UtcNow < end)
        {
            var now = DateTimeOffset.UtcNow;
            while (TryReadConsoleKey(out var keyInfo))
            {
                HandleRangeFuzzerKey(keyInfo);
                if (stopRequested)
                {
                    break;
                }
            }

            if (stopRequested)
            {
                break;
            }

            now = DateTimeOffset.UtcNow;
            foreach (var item in baseline)
            {
                if (now < item.NextDue)
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

            if (!paused && activeFrame is not null && now >= activeWindowEnd)
            {
                EndCandidate("completed");
                candidateIndex++;
                if (candidateIndex >= candidates.Count)
                {
                    Console.WriteLine("fuzzer range complete");
                    break;
                }

                StartCandidate(now);
            }

            if (!paused && activeFrame is not null && now >= activeNextDue)
            {
                device.SendFrame(activeFrame);
                sent++;
                activeFrame.SentCount++;
                activeNextDue += activeFrame.Period;
                writer.WriteLine(ConsoleFrames.FormatTxLogComment(activeFrame));
                writer.Flush();
                Console.WriteLine(ConsoleFrames.FormatTx(sent, activeFrame));
            }

            if (!paused && now >= progressNextDue)
            {
                if (activeFrame?.CanId != RangeFuzzerProgressCanId)
                {
                    device.SendFrame(progressFrame);
                    sent++;
                    progressFrame.SentCount++;
                    writer.WriteLine(ConsoleFrames.FormatTxLogComment(progressFrame));
                    writer.Flush();
                    Console.WriteLine(ConsoleFrames.FormatTx(sent, progressFrame));
                }

                progressNextDue += RangeFuzzerProgressPeriod;
            }

            DrainAvailableRx(device, writer, ref received, ref sent, autoIsoTpFlowControl: false);
            Thread.Sleep(MaxSchedulerSleep);
        }

        DrainFinalRxToLog(device, writer, ref received, ref sent, autoIsoTpFlowControl: false, TimeSpan.FromMilliseconds(300));
        Console.WriteLine($"done: sent={sent} rx_frames={received} rx_log={outPath}");
        return 0;

        void StartCandidate(DateTimeOffset now)
        {
            activeFrame = candidates[candidateIndex].ToFrame(RangeFuzzerActivePeriod);
            progressFrame = BuildRangeFuzzerProgressFrame(candidateIndex, candidates.Count);
            activeNextDue = now;
            activeWindowEnd = now + RangeFuzzerCandidateHold;
            var line = $"# fuzz-window-start t={FormatFuzzerElapsed()}s index={candidateIndex + 1}/{candidates.Count} active={activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)} note={SanitizeLogValue(activeFrame.Note)}";
            writer.WriteLine(line);
            writer.Flush();
            Console.WriteLine(line);
        }

        void EndCandidate(string reason)
        {
            if (activeFrame is null)
            {
                return;
            }

            var line = $"# fuzz-window-end t={FormatFuzzerElapsed()}s index={candidateIndex + 1}/{candidates.Count} active={activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)} reason={SanitizeLogValue(reason)}";
            writer.WriteLine(line);
            writer.Flush();
        }

        void HandleRangeFuzzerKey(ConsoleKeyInfo keyInfo)
        {
            if (activeFrame is null)
            {
                return;
            }

            var key = keyInfo.Key;
            if (key == ConsoleKey.Spacebar || key == ConsoleKey.M)
            {
                WriteFuzzerMark(writer, key == ConsoleKey.Spacebar ? "space" : "m", activeFrame, "manual visible event marker");
                Console.WriteLine($"marked {activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)}");
                return;
            }

            if (key == ConsoleKey.B)
            {
                WriteFuzzerMark(writer, "b", activeFrame, "bad/restart/disruptive marker");
                Console.WriteLine($"bad marker {activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)}");
                return;
            }

            if (key == ConsoleKey.N)
            {
                WriteFuzzerMark(writer, "n", activeFrame, "manual skip current candidate");
                EndCandidate("manual skip");
                candidateIndex++;
                if (candidateIndex >= candidates.Count)
                {
                    stopRequested = true;
                    Console.WriteLine("skip reached end of fuzzer range");
                    return;
                }

                StartCandidate(DateTimeOffset.UtcNow);
                return;
            }

            if (key == ConsoleKey.P)
            {
                paused = !paused;
                WriteFuzzerMark(writer, "p", activeFrame, paused ? "paused" : "resumed");
                if (!paused)
                {
                    var now = DateTimeOffset.UtcNow;
                    activeNextDue = now;
                    activeWindowEnd = now + RangeFuzzerCandidateHold;
                    progressNextDue = now;
                }

                Console.WriteLine(paused ? "fuzzer paused; press p to resume" : "fuzzer resumed");
                return;
            }

            if (key == ConsoleKey.Q)
            {
                WriteFuzzerMark(writer, "q", activeFrame, "manual quit");
                stopRequested = true;
            }
        }
    }

    private static void TryReloadLiveProfile(
        string path,
        ref DateTime lastLoadedWriteUtc,
        ref List<ScheduledTxFrame> schedule,
        StreamWriter writer,
        bool force)
    {
        var writeUtc = File.GetLastWriteTimeUtc(path);
        if (!force && writeUtc == lastLoadedWriteUtc)
        {
            return;
        }

        try
        {
            var loaded = LoadLiveReloadSchedule(path, out var rows, out var active);
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < loaded.Count; i++)
            {
                loaded[i].NextDue = now + TimeSpan.FromMilliseconds(i * 2);
            }

            schedule = loaded;
            lastLoadedWriteUtc = writeUtc;
            var line = $"# live-reload loaded rows={rows} active={active} file={path}";
            writer.WriteLine(line);
            writer.Flush();
            Console.WriteLine(line);
        }
        catch (Exception ex)
        {
            schedule = [];
            lastLoadedWriteUtc = writeUtc;
            var line = $"# live-reload parse-error file={path} error={SanitizeLogValue(ex.Message)}";
            writer.WriteLine(line);
            writer.WriteLine("# live-reload active schedule cleared until the file parses cleanly");
            writer.Flush();
            Console.WriteLine(line);
            Console.WriteLine("active live schedule cleared; fix the file and save again");
        }
    }

    private static IEnumerable<RangeFuzzerCandidate> BuildRangeFuzzerCandidates(RangeFuzzerRange range)
    {
        const uint source = 0x040;
        for (uint arb = range.FirstArbitrationId; arb <= range.LastArbitrationId; arb++)
        {
            foreach (var payload in RangeFuzzerPayloads)
            {
                yield return new RangeFuzzerCandidate(
                    0x10000000 | (arb << 13) | source,
                    payload,
                    $"fuzz source=0x040 arb=0x{arb:X3} wrapped_std=0x{arb:X3} payload={payload}");
            }
        }
    }

    private static IEnumerable<RpmWordSweepCandidate> BuildRpmWordSweepCandidates()
    {
        var targets = new (uint CanId, string BasePayload, string Label)[]
        {
            (0x10220040, "1000000040080000", "10220040 body/status speed-limiter-adjacent"),
            (0x10240040, "83FE880100D70FFD", "10240040 TC/lamp/state"),
            (0x10248040, "00005DA6A300", "10248040 dynamic body/DIC status"),
            (0x10264040, "0000000000000000", "10264040 ABS/traction/eco status"),
            (0x102CA040, "0000000000000F00", "102CA040 body/DIC status"),
            (0x102E0040, "840000000000005A", "102E0040 temp/status"),
            (0x1062C040, "000BF7B714", "1062C040 ABS/traction/eco companion"),
            (0x10ACA040, "28F0000000000000", "10ACA040 extended status"),
            (0x10AEC040, "02081E08049FFF2C", "10AEC040 extended status")
        };
        var rawVariants = new (string Word, string Label)[]
        {
            ("0C80", "800 rpm raw 0x0C80"),
            ("12C0", "1200 rpm raw 0x12C0"),
            ("1F40", "2000 rpm raw 0x1F40"),
            ("2EE0", "3000 rpm raw 0x2EE0")
        };
        var pending = new List<(uint CanId, string PayloadHex, string Note)>();

        foreach (var target in targets)
        {
            foreach (var offset in RpmWordOffsets(target.BasePayload))
            {
                foreach (var variant in rawVariants)
                {
                    var payload = ReplacePayloadWord(target.BasePayload, offset, variant.Word);
                    pending.Add((
                        target.CanId,
                        payload,
                        $"{target.Label} rpm-word offset {offset}: {variant.Label}"));
                }
            }
        }

        for (var index = 0; index < pending.Count; index++)
        {
            var candidate = pending[index];
            yield return new RpmWordSweepCandidate(
                candidate.CanId,
                candidate.PayloadHex,
                $"{candidate.Note}; full_index={index + 1}/{pending.Count}",
                index,
                pending.Count);
        }
    }

    private static IEnumerable<int> RpmWordOffsets(string basePayload)
    {
        var byteCount = basePayload.Length / 2;
        for (var offset = 0; offset <= byteCount - 2; offset++)
        {
            yield return offset;
        }
    }

    private static string ReplacePayloadWord(string basePayload, int byteOffset, string word)
    {
        var charOffset = byteOffset * 2;
        return string.Concat(basePayload.AsSpan(0, charOffset), word, basePayload.AsSpan(charOffset + 4));
    }

    private static List<ScheduledTxFrame> BuildRpmWordSweepBaseline()
    {
        return
        [
            new ScheduledTxFrame(
                0x100,
                [],
                TimeSpan.Zero,
                false,
                "rpm word sweep baseline SWCAN wake pulse",
                MaxSends: 1),
            new ScheduledTxFrame(
                0x13FFE040,
                [],
                TimeSpan.FromMilliseconds(1200),
                true,
                "rpm word sweep baseline BCM/source 0x40 presence"),
            new ScheduledTxFrame(
                0x621,
                CliOptions.ParseHexData("0052000000000000"),
                TimeSpan.FromMilliseconds(1000),
                false,
                "rpm word sweep baseline key-on 621 keepalive"),
            new ScheduledTxFrame(
                0x102C0040,
                CliOptions.ParseHexData("803C96B503"),
                TimeSpan.FromMilliseconds(100),
                true,
                "rpm word sweep baseline ignition/power-mode key-on context"),
            new ScheduledTxFrame(
                0x10242040,
                CliOptions.ParseHexData("02"),
                TimeSpan.FromMilliseconds(1000),
                true,
                "rpm word sweep baseline key second-turn sub-state",
                TimeSpan.FromMilliseconds(500)),
            new ScheduledTxFrame(
                0x10754040,
                CliOptions.ParseHexData("040400"),
                TimeSpan.FromMilliseconds(1000),
                true,
                "rpm word sweep baseline key-present/key-on context",
                TimeSpan.FromMilliseconds(600)),
            new ScheduledTxFrame(
                0x102CC040,
                CliOptions.ParseHexData("0000400000000000"),
                TimeSpan.FromMilliseconds(250),
                true,
                "rpm word sweep baseline confirmed speed-valid prerequisite")
        ];
    }

    private static List<ScheduledTxFrame> BuildRangeFuzzerBaseline()
    {
        return
        [
            new ScheduledTxFrame(
                0x100,
                [],
                TimeSpan.Zero,
                false,
                "fuzzer baseline SWCAN wake pulse",
                MaxSends: 1),
            new ScheduledTxFrame(
                0x13FFE040,
                [],
                TimeSpan.FromMilliseconds(1200),
                true,
                "fuzzer baseline BCM/source 0x40 presence"),
            new ScheduledTxFrame(
                0x621,
                CliOptions.ParseHexData("0052000000000000"),
                TimeSpan.FromMilliseconds(1000),
                false,
                "fuzzer baseline key-on network-management keepalive"),
            new ScheduledTxFrame(
                0x102C0040,
                CliOptions.ParseHexData("803C96B503"),
                TimeSpan.FromMilliseconds(100),
                true,
                "fuzzer baseline ignition/power-mode key-on context"),
            new ScheduledTxFrame(
                0x10242040,
                CliOptions.ParseHexData("02"),
                TimeSpan.FromMilliseconds(1000),
                true,
                "fuzzer baseline key second-turn sub-state"),
            new ScheduledTxFrame(
                0x102CC040,
                CliOptions.ParseHexData("EEEEEEEEEEEEEEEE"),
                TimeSpan.FromMilliseconds(250),
                true,
                "fuzzer heartbeat speed-valid prerequisite confirmed")
        ];
    }

    private static ScheduledTxFrame BuildRangeFuzzerProgressFrame(int candidateIndex, int candidateCount)
    {
        var progress = candidateCount <= 1
            ? 100.0
            : candidateIndex * 100.0 / (candidateCount - 1);
        return BuildSpeedometerProgressFrame(
            progress,
            RangeFuzzerProgressPeriod,
            $"fuzzer progress indicator {{0:0.0}}% target {{1}} kmh via confirmed 10210040 byte0=0x{{2:X2}}");
    }

    private static ScheduledTxFrame BuildRpmWordSweepProgressFrame(RpmWordSweepCandidate candidate)
    {
        var progress = candidate.FullCount <= 1
            ? 100.0
            : candidate.FullIndex * 100.0 / (candidate.FullCount - 1);
        return BuildSpeedometerProgressFrame(
            progress,
            RpmWordSweepProgressPeriod,
            $"RPM sweep progress indicator {{0:0.0}}% full_index={candidate.FullIndex + 1}/{candidate.FullCount} target {{1}} kmh via confirmed 10210040 byte0=0x{{2:X2}}");
    }

    private static ScheduledTxFrame BuildSpeedometerProgressFrame(double progress, TimeSpan period, string noteFormat)
    {
        var targetKmh = (int)Math.Round(progress * RangeFuzzerProgressMaxSpeedKmh / 100.0);
        var speedByte = (byte)Math.Clamp(
            (int)Math.Round(progress * RangeFuzzerProgressMaxSpeedByte / 100.0),
            0,
            RangeFuzzerProgressMaxSpeedByte);
        var data = new byte[] { speedByte, 0, 0, 0, 0, 0, 0, 0 };
        var note = string.Format(CultureInfo.InvariantCulture, noteFormat, progress, targetKmh, speedByte);

        return new ScheduledTxFrame(
            RangeFuzzerProgressCanId,
            data,
            period,
            true,
            note);
    }

    private static bool TryReadConsoleKey(out ConsoleKeyInfo key)
    {
        key = default;
        if (Console.IsInputRedirected)
        {
            return false;
        }

        try
        {
            if (!Console.KeyAvailable)
            {
                return false;
            }

            key = Console.ReadKey(intercept: true);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void WriteFuzzerMark(StreamWriter writer, string key, ScheduledTxFrame activeFrame, string note)
    {
        var line = $"# mark t={FormatFuzzerElapsed()}s key={SanitizeLogValue(key)} active={activeFrame.CandumpId}#{Convert.ToHexString(activeFrame.Data)} note={SanitizeLogValue($"{note}; {activeFrame.Note}")}";
        writer.WriteLine(line);
        writer.Flush();
    }

    private static string FormatFuzzerElapsed()
    {
        return AppClock.ElapsedSeconds.ToString("000.000", CultureInfo.InvariantCulture);
    }

    private static bool IsRangeFuzzerProfile(string profile)
    {
        return profile.Equals(Profiles.IpcRangeFuzzer00, StringComparison.OrdinalIgnoreCase) ||
            profile.Equals(Profiles.IpcRangeFuzzer, StringComparison.OrdinalIgnoreCase) ||
            profile.Equals(Profiles.IpcRangeFuzzer20, StringComparison.OrdinalIgnoreCase) ||
            profile.Equals(Profiles.IpcRangeFuzzer30, StringComparison.OrdinalIgnoreCase) ||
            profile.Equals(Profiles.IpcRangeFuzzerE0, StringComparison.OrdinalIgnoreCase);
    }

    private static RangeFuzzerRange GetRangeFuzzerRange(string profile)
    {
        return profile.ToLowerInvariant() switch
        {
            Profiles.IpcRangeFuzzer00 => new RangeFuzzerRange(0x000, 0x0FF),
            Profiles.IpcRangeFuzzer => new RangeFuzzerRange(0x100, 0x1FF),
            Profiles.IpcRangeFuzzer20 => new RangeFuzzerRange(0x200, 0x2FF),
            Profiles.IpcRangeFuzzer30 => new RangeFuzzerRange(0x300, 0x3FF),
            "ipc-range-fuzzere0" => new RangeFuzzerRange(0xE00, 0xEFF),
            _ => throw new ArgumentException($"unknown range fuzzer profile: {profile}")
        };
    }

    private static string FormatRangeFuzzerRange(RangeFuzzerRange range)
    {
        return $"arb 0x{range.FirstArbitrationId:X3}-0x{range.LastArbitrationId:X3}";
    }

    private static List<ScheduledTxFrame> LoadLiveReloadSchedule(string path, out int rows, out int active)
    {
        rows = 0;
        active = 0;
        var schedule = new List<ScheduledTxFrame>();

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);

        var lineNumber = 0;
        while (reader.ReadLine() is { } rawLine)
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var cells = SplitLiveProfileCells(line);
            if (cells.Length == 0)
            {
                continue;
            }

            if (!double.TryParse(cells[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var periodMs))
            {
                if (lineNumber == 1 || cells[0].Contains("period", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                throw new ArgumentException($"line {lineNumber}: period_ms is not a number");
            }

            rows++;
            if (cells.Length < 3)
            {
                throw new ArgumentException($"line {lineNumber}: expected period_ms,message,enabled,note");
            }

            if (periodMs <= 0)
            {
                throw new ArgumentException($"line {lineNumber}: period_ms must be > 0");
            }

            var enabled = ParseLiveEnabled(cells[2], lineNumber);
            if (!enabled)
            {
                continue;
            }

            var note = cells.Length >= 4 && !string.IsNullOrWhiteSpace(cells[3])
                ? cells[3].Trim()
                : cells[1].Trim();
            var frame = ParseLiveCanMessage(
                cells[1],
                TimeSpan.FromMilliseconds(periodMs),
                $"live {note}",
                lineNumber);
            schedule.Add(frame);
            active++;
        }

        return schedule;
    }

    private static ScheduledTxFrame ParseLiveCanMessage(string text, TimeSpan period, string note, int lineNumber)
    {
        var parts = text.Trim().Split('#', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"line {lineNumber}: CAN message must be ID#DATA");
        }

        var idText = parts[0].Trim();
        var idDigits = idText.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? idText[2..] : idText;
        var canId = CliOptions.ParseUInt(idText);
        var data = CliOptions.ParseHexData(parts[1]);
        if (data.Length > 8)
        {
            throw new ArgumentException($"line {lineNumber}: CAN payload is {data.Length} bytes; max classic CAN DLC is 8");
        }

        var isExtended = idDigits.Length > 3 || canId > 0x7FF;
        return new ScheduledTxFrame(canId, data, period, isExtended, note);
    }

    private static bool ParseLiveEnabled(string text, int lineNumber)
    {
        return text.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "y" or "on" or "enabled" => true,
            "0" or "false" or "no" or "n" or "off" or "disabled" => false,
            _ => throw new ArgumentException($"line {lineNumber}: enabled must be true/false, 1/0, yes/no, or on/off")
        };
    }

    private static string[] SplitLiveProfileCells(string line)
    {
        if (line.Contains(','))
        {
            return SplitDelimitedCells(line, ',');
        }

        if (line.Contains(';'))
        {
            return SplitDelimitedCells(line, ';');
        }

        if (line.Contains('\t'))
        {
            return SplitDelimitedCells(line, '\t');
        }

        var match = Regex.Match(line.Trim(), @"^(\S+)\s+(\S+)\s+(\S+)(?:\s+(.*))?$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        return match.Success
            ?
            [
                UnquoteCell(match.Groups[1].Value),
                UnquoteCell(match.Groups[2].Value),
                UnquoteCell(match.Groups[3].Value),
                UnquoteCell(match.Groups[4].Value)
            ]
            : [UnquoteCell(line)];
    }

    private static string[] SplitDelimitedCells(string line, char delimiter)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == delimiter && !inQuotes && cells.Count < 3)
            {
                cells.Add(UnquoteCell(current.ToString()));
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        cells.Add(UnquoteCell(current.ToString()));
        return cells.ToArray();
    }

    private static string UnquoteCell(string value)
    {
        var text = value.Trim();
        return text.Length >= 2 && text[0] == '"' && text[^1] == '"'
            ? text[1..^1].Trim()
            : text;
    }

    private static string SanitizeLogValue(string value)
    {
        return value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }

    private static void EnsureLiveReloadProfileFile(string path)
    {
        if (File.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllLines(path, DefaultLiveReloadProfileLines(), Encoding.UTF8);
        Console.WriteLine($"created sample live profile: {path}");
    }

    private static string[] DefaultLiveReloadProfileLines()
    {
        return
        [
            "# send_profile.can",
            "# Columns: period_ms, CAN_MESSAGE, enabled, note",
            "# Edit enabled to true/on/1 for the one message you want to test, then save.",
            "# Keep every candidate disabled by default. Enable only one 10210040 speed row at a time.",
            "# The live-reload profile keeps RX logging while it reloads this file.",
            "period_ms,message,enabled,note",
            "",
            "# --- Native key-on baseline/context ---",
            "1000,100#,false,SWCAN wake pulse",
            "1200,13FFE040#,false,BCM source 0x40 presence",
            "1000,621#0052000000000000,false,key-on network-management keepalive",
            "100,102C0040#803C96B503,false,ignition power-mode key-on context",
            "1000,10242040#01,false,key first-turn sub-state",
            "1000,10242040#02,false,key second-turn sub-state",
            "1000,10754040#040400,false,key present key-on context",
            "100,10210040#0000800000008000,false,captured 10210040 speed/status baseline; disable while testing speed rows",
            "100,10220040#1000000040080000,false,native body base 0x110",
            "250,1022E040#1000000010000000,false,native body counter/state 0x117 value 1",
            "250,1022E040#3000000030000000,false,native body counter/state 0x117 value 3",
            "250,10230040#0000000000000000,false,native body counter/state 0x118 value 0",
            "250,10230040#2000000020000000,false,native body counter/state 0x118 value 2",
            "100,10240040#83FE880100D10FFD,false,lights-off neutral candidate",
            "100,10240040#83FE880100D70FFD,false,key-on lights/context captured value",
            "100,10240040#0000000000000000,false,unknown",
            "20,10240040#83FE12C000D70FFD,false,TC message/lamp ON finding; turns off TC with message",
            "20,10240040#83FE1F4000D70FFD,false,TC message/lamp OFF finding; turns TC back on with message",
            "100,10264040#0000000000000000,false,native body base 0x132",
            "100,102CA040#0000000000000F00,false,native body base 0x165",
            "",
            "# --- 10210040 confirmed speedometer byte0 ---",
            "# 102CC040#0000400000000000 needs to be sent for these to work.",
            "100,10210040#0000000000000000,false,speed_0_kmh_candidate",
            "100,10210040#0100000000000000,false,about 4 kmh confirmed",
            "100,10210040#0200000000000000,false,about 8 kmh confirmed",
            "100,10210040#0300000000000000,false,about 12 kmh confirmed",
            "100,10210040#0500000000000000,false,speed_20_kmh_candidate",
            "100,10210040#0800000000000000,false,speed_32_kmh_candidate",
            "100,10210040#0A00000000000000,false,speed_40_kmh_candidate",
            "100,10210040#0F00000000000000,false,speed_60_kmh_candidate",
            "100,10210040#1000000000000000,false,about 75 kmh confirmed",
            "100,10210040#1400000000000000,false,speed_80_kmh_candidate",
            "100,10210040#1900000000000000,false,speed_100_kmh_candidate",
            "100,10210040#2000000000000000,false,about 133 kmh confirmed",
            "100,10210040#3000000000000000,false,about 198 kmh confirmed",
            "",
            "# --- 10210040 byte1/status experiment, fixed byte0 0x10 ---",
            "500,10210040#1000000000000000,false,speed_byte1_00_baseline_confirmed",
            "500,10210040#1001000000000000,false,speed_byte1_01",
            "500,10210040#1002000000000000,false,speed_byte1_02",
            "500,10210040#1004000000000000,false,speed_byte1_04",
            "500,10210040#1008000000000000,false,speed_byte1_08",
            "500,10210040#1010000000000000,false,speed_byte1_10",
            "500,10210040#1080000000000000,false,speed_byte1_80",
            "",
            "# --- 10210040 mirrored BE16 speed format, 100 ms ---",
            "# Formula hypothesis: raw = round((kmh / 1.60934) * 100), payload = raw 80 00 raw 80 00.",
            "# Keep 102CC040#0000400000000000 enabled; disable the captured 10210040 baseline and byte0-only speed rows while testing these.",
            "100,10210040#0000800000008000,false,speed_mirrored_0_kmh_raw_0000",
            "100,10210040#0137800001378000,false,speed_mirrored_5_kmh_raw_0137",
            "100,10210040#026D8000026D8000,false,speed_mirrored_10_kmh_raw_026D",
            "100,10210040#04DB800004DB8000,false,speed_mirrored_20_kmh_raw_04DB",
            "100,10210040#0748800007488000,false,speed_mirrored_30_kmh_raw_0748",
            "100,10210040#09B5800009B58000,false,speed_mirrored_40_kmh_raw_09B5",
            "100,10210040#0C2380000C238000,false,speed_mirrored_50_kmh_raw_0C23",
            "100,10210040#0E9080000E908000,false,speed_mirrored_60_kmh_raw_0E90",
            "100,10210040#136B8000136B8000,false,speed_mirrored_80_kmh_raw_136B",
            "100,10210040#1846800018468000,false,speed_mirrored_100_kmh_raw_1846",
            "100,10210040#1D2080001D208000,false,speed_mirrored_120_kmh_raw_1D20",
            "100,10210040#21FB800021FB8000,false,speed_mirrored_140_kmh_raw_21FB",
            "100,10210040#26D6800026D68000,false,speed_mirrored_160_kmh_raw_26D6",
            "100,10210040#2BB180002BB18000,false,speed_mirrored_180_kmh_raw_2BB1",
            "100,10210040#308B8000308B8000,false,speed_mirrored_200_kmh_raw_308B",
            "",
            "# --- TC telltale candidates ---",
            "250,102CC040#0400C00000000000,false,TC telltale ON confirmed",
            "250,102CC040#0000400000000000,false,speed valid prerequisite / TC telltale clear candidate",
            "250,102CC040#0000C00000000000,false,old TC telltale clear candidate",
            "250,102CC040#0000000000000000,false,TC matrix 000000",
            "250,102CC040#0000800000000000,false,TC matrix 000080",
            "250,102CC040#0400000000000000,false,TC matrix 040000",
            "250,102CC040#0400800000000000,false,TC matrix 040080",
            "250,102CC040#0800000000000000,false,TC matrix 080000",
            "250,102CC040#0800800000000000,false,TC matrix 080080",
            "250,102CC040#0800C00000000000,false,TC matrix 0800C0",
            "500,10240040#FFFFFFFFFFFFFFFF,false,Turns off TC light potentially",
            "",
            "# --- Parking brake and lighting candidates ---",
            "500,103B4040#04,false,parking brake ON confirmed",
            "500,103B4040#00,false,parking brake OFF confirmed",
            "500,103B4040#01,false,parking brake bit 01",
            "500,103B4040#02,false,parking brake bit 02",
            "500,103B4040#08,false,parking brake bit 08",
            "500,103B4040#0400000000000000,false,parking brake ON bit long DLC",
            "500,1020C040#0040040401,false,parking brake support released candidate",
            "500,1020C040#00C0040401,false,parking brake support asserted candidate",
            "500,1020C040#0010000000,false,rear fog light on",
            "500,1020C040#0020000000,false,long beam light on",
            "500,1020C040#0030000000,false,long beam light on and rear fog light on",
            "500,1020C040#0040000000,false,low beams on",
            "500,1020C040#0050000000,false,low beams on and rear fog light on",
            "500,1020C040#0060000000,false,low beams on and long beam on",
            "500,1020C040#0070000000,false,low beams on and long beam on and rear fog light on",
            "500,1020C040#0002000000,false,fog light on",
            "500,1020C040#0072000000,false,fog light on and low beams on and long beam on and rear fog light on",
            "500,1020C040#0100000000,false,light/status byte0 01",
            "500,1020C040#0200000000,false,light/status byte0 02",
            "500,1020C040#0000000000000000,false,lights neutral long DLC",
            "500,1020C040#1800000000000000,false,left and right blinker on",
            "500,1020C040#0800000000,false,left blinker const on",
            "500,1020C040#1000000000,false,right blinker const on",
            "",
            "# --- Body/lock and DIC/status candidates ---",
            "500,10632040#0000,false,door/body state clear",
            "500,10632040#4000,false,door/body state set",
            "500,10414040#0000FB05,false,lock/body context captured 0000FB05",
            "500,10414040#00017B05,false,lock/body context captured 00017B05",
            "500,10414040#0000C10A,false,lock/body context captured 0000C10A",
            "500,10248040#0000000000000000,false,turn off battery light",
            "500,10248040#0400000000000000,false,turn on battery light",
            "500,10248040#00005BA6A500,false,dim/odometer DIC status candidate 5B",
            "500,10248040#00005CA6A400,false,dim/odometer DIC status candidate 5C",
            "500,10248040#00005DA6A300,false,dim/odometer DIC status candidate",
            "500,10248040#00005EA6A400,false,dim/odometer DIC status candidate 5E",
            "500,10248040#000060A6A400,false,dim/odometer DIC status candidate 60",
            "",
            "# --- Wrapped RPM/temp/service candidates ---",
            "100,10192040#0000000000500000,false,wrapped RPM neutral engine-state candidate",
            "100,10192040#0004C40000500000,false,wrapped RPM engine-state candidate",
            "100,10192040#8412DC0000500000,false,wrapped RPM alternate engine-state candidate",
            "100,101A6040#2BBC0007001000FF,false,wrapped RPM companion 0D3 candidate",
            "100,101A6040#0000000000000000,false,wrapped RPM companion neutral",
            "500,107D2040#0000000000000000,false,wrapped speed lower-priority neutral",
            "500,107D2040#0000800000008000,false,wrapped speed lower-priority seed",
            "500,107D2040#0010800000108000,false,wrapped speed lower-priority candidate",
            "500,107D2040#0040800000408000,false,wrapped speed lower-priority candidate high",
            "250,10982040#0000490000000000,false,wrapped coolant/temp low candidate",
            "250,10982040#0000730000000000,false,wrapped coolant/temp candidate",
            "250,10982040#00008C0000000000,false,wrapped coolant/temp warm candidate",
            "250,10982040#0000AA0000000000,false,wrapped coolant/temp hot candidate",
            "250,109A2040#0000000000000000,false,wrapped oil/service neutral",
            "250,109A2040#C900000000000000,false,wrapped oil/service C9",
            "250,109A2040#E900000000000000,false,wrapped oil/service E9",
            "250,103CA040#0000000000000000,false,wrapped warning/service neutral",
            "250,103CA040#44003910000000C9,false,wrapped warning/service candidate C9",
            "250,103CA040#44003930000000E9,false,wrapped warning/service candidate E9",
            "250,103CA040#4400395000000109,false,wrapped warning/service candidate 0109",
            "250,103CA040#44FFA0100000022F,false,wrapped warning/service alternate 22F",
            "250,103CA040#44FFA0300000024F,false,wrapped warning/service alternate 24F",
            "250,103CA040#44FFA0500000026F,false,wrapped warning/service alternate 26F",
            "250,103CA040#44FFA0700000028F,false,wrapped warning/service alternate 28F",
            "",
            "# --- HMI/DIC text and button candidates ---",
            "500,10438040#0000000000000000,false,HMI button release",
            "500,10438040#0100000000000000,false,HMI volume up candidate",
            "500,10438040#0200000000000000,false,HMI volume down candidate",
            "500,10438040#0500000000000000,false,HMI source candidate",
            "1000,1030A080#000448800003,false,DIC text radio display parameters",
            "1000,1030C080#4501005445535404,false,DIC text radio TEST payload",
            "",
            "# --- 10220040 speed-limiter / RPM word candidates ---",
            "20,10220040#10000C8040080000,false,10220040 RPM word1 800",
            "20,10220040#100012C040080000,false,10220040 RPM word1 1200",
            "20,10220040#00000FF020000000,false,--Speed limiter 265 kmh",
            "20,10220040#0000001020000000,false,--Speed limiter 1 kmh",
            "20,10220040#000000F020000000,false,--Speed limiter 15 kmh",
            "20,10220040#000000F120000000,false,--Speed limiter 16 kmh",
            "20,10220040#000001F020000000,true,--Speed limiter 33 kmh",
            "20,10220040#000001F120000000,true,--Speed limiter 33 kmh",
        ];
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
            var gmlan = group.Key.IsExtended ? FormatGmlanSummary(group.Key.CanId) : "";
            Console.WriteLine($"{idText,12} {format,-8} {gmlan,-96} count={ordered.Count,5} period_ms={periodText,10} dlcs={dlcs} payload={firstPayload}");
        }

        PrintIpcEffectSummary(logPath, records);
        PrintFuzzMarkSummary(logPath);
        return 0;
    }

    private static string FormatGmlanSummary(uint canId)
    {
        var decoded = Gmlan29Id.Decode(canId);
        var summary = decoded.ToAnnotatedSummaryString();
        return decoded.Sender == 0x040 && decoded.ArbitrationId <= 0x7FF
            ? $"{summary} wrapped_std=0x{decoded.ArbitrationId:X3} arb=0x{decoded.ArbitrationId:X3} source=0x040"
            : summary;
    }

    private static void PrintIpcEffectSummary(string logPath, List<CanFrame> records)
    {
        var watchedRx = records
            .Where(frame => IpcEffectWatchIds.Contains(frame.CanId))
            .GroupBy(frame => (frame.CanId, frame.IsExtended))
            .OrderBy(group => group.Key.IsExtended ? 1 : 0)
            .ThenBy(group => group.Key.CanId)
            .ToList();
        var events = ParseLogEvents(logPath).ToList();
        var watchedTx = events
            .Where(item => item.Kind == LogEventKind.Tx && IpcEffectTxIds.Contains(item.Frame.CanId))
            .OrderBy(item => item.LineNumber)
            .ToList();

        if (watchedRx.Count == 0 && watchedTx.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("IPC effect watch:");
        foreach (var group in watchedRx)
        {
            var ordered = group.OrderBy(frame => frame.Timestamp).ToList();
            var first = ordered.First();
            var uniquePayloads = ordered
                .Select(frame => frame.DataHex)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();
            var changed = uniquePayloads.Count > 1 ? "changed" : "stable";
            Console.WriteLine($"  rx {first.CandumpId,12} count={ordered.Count,5} unique_payloads={uniquePayloads.Count,2} {changed,-7} first={first.DataHex} last={ordered.Last().DataHex} samples={string.Join(",", uniquePayloads)}");
        }

        if (watchedTx.Count == 0)
        {
            return;
        }

        Console.WriteLine("Relevant TX windows:");
        foreach (var tx in watchedTx.Take(20))
        {
            var note = string.IsNullOrWhiteSpace(tx.Note) ? "" : $" note={tx.Note}";
            Console.WriteLine($"  tx line {tx.LineNumber,5}: t={tx.Frame.Timestamp,8:0.###}s {tx.Frame.CandumpId}#{tx.Frame.DataHex}{note}");
        }

        if (watchedTx.Any(item => item.Frame.CanId is 0x100C4040 or 0x1005E040))
        {
            Console.WriteLine("  note: 100C4040/1005E040 are the current ABS/traction and brake/cruise OK candidates; correlate these windows with the TC lamp clearing.");
        }
    }

    private static void PrintFuzzMarkSummary(string logPath)
    {
        var marks = ParseFuzzMarks(logPath).ToList();
        if (marks.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Fuzzer marks: {marks.Count}");
        foreach (var mark in marks)
        {
            var wrapped = mark.ActiveFrame.IsExtended
                ? FormatGmlanSummary(mark.ActiveFrame.CanId)
                : "";
            var suffix = string.IsNullOrWhiteSpace(wrapped) ? "" : $" {wrapped}";
            Console.WriteLine($"  line {mark.LineNumber,5}: key={mark.Key,-5} active={mark.ActiveFrame.CandumpId}#{mark.ActiveFrame.DataHex}{suffix} note={mark.Note}");
        }
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
                Console.WriteLine("  2. dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- wake-then-profile --profile ipc-simulator --seconds 45 --wait-rx-timeout-ms 5000");
                Console.WriteLine("  3. dotnet run --no-build --project cantool\\cantool\\cantool.csproj -- wake-then-profile --profile ipc-native-lights-probe --seconds 45 --wait-rx-timeout-ms 5000");
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
            var gmlan = first.IsExtended ? FormatGmlanSummary(first.CanId) : "";
            Console.WriteLine($"  {first.CandumpId,12} {gmlan,-96} count={group.Count(),5} first_payload={first.DataHex}");
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

        if (definition.Name.Equals(Profiles.IpcLiveReload, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("live-reload profile: TX schedule comes from send_profile.can at runtime");
            Console.WriteLine("file columns: period_ms,message,enabled,note");
            Console.WriteLine("use --file PATH to override the default send_profile.can");
            return 0;
        }

        if (definition.Name.Equals(Profiles.IpcRpmWordSweep, StringComparison.OrdinalIgnoreCase))
        {
            var fullCandidates = BuildRpmWordSweepCandidates().ToList();
            Console.WriteLine("interactive RPM word sweep: dynamic TX schedule generated at runtime");
            Console.WriteLine($"full sweep: active candidates {fullCandidates.Count}/{fullCandidates.Count}; full indexes 1-{fullCandidates.Count}");
            Console.WriteLine($"active period: {RpmWordSweepActivePeriod.TotalMilliseconds:0.###}ms; hold per candidate: {RpmWordSweepCandidateHold.TotalMilliseconds:0.###}ms");
            Console.WriteLine("baseline: 100#, 13FFE040#, 621#0052..., 102C0040#803C96B503, 10242040#02, 10754040#040400, 102CC040#000040...");
            Console.WriteLine($"progress indicator: {RangeFuzzerProgressCanId:X8} every {RpmWordSweepProgressPeriod.TotalMilliseconds:0.###}ms, 0-{RangeFuzzerProgressMaxSpeedKmh} km/h across full sweep");
            Console.WriteLine("keys: m/space=mark, b=bad marker, n=skip, p=pause/resume, q=quit");
            return 0;
        }

        if (IsRangeFuzzerProfile(definition.Name))
        {
            var range = GetRangeFuzzerRange(definition.Name);
            var candidateCount = BuildRangeFuzzerCandidates(range).Count();
            var payloads = string.Join(",", RangeFuzzerPayloads);
            Console.WriteLine("interactive range fuzzer: dynamic TX schedule generated at runtime");
            Console.WriteLine($"range: source=0x040 {FormatRangeFuzzerRange(range)}");
            Console.WriteLine($"active candidates: {candidateCount} ({RangeFuzzerPayloads.Length} payloads per arbitration ID)");
            Console.WriteLine($"active period: {RangeFuzzerActivePeriod.TotalMilliseconds:0.###}ms; hold per candidate: {RangeFuzzerCandidateHold.TotalMilliseconds:0.###}ms");
            Console.WriteLine($"payloads: {payloads}");
            Console.WriteLine("baseline: 100#, 13FFE040#, 621#0052..., 102C0040#803C96B503, 10242040#02, 102CC040#EEEE...");
            Console.WriteLine($"progress indicator: {RangeFuzzerProgressCanId:X8} every {RangeFuzzerProgressPeriod.TotalMilliseconds:0.###}ms, 0-{RangeFuzzerProgressMaxSpeedKmh} km/h across range");
            Console.WriteLine("keys: m/space=mark, b=bad marker, n=skip, p=pause/resume, q=quit");
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

    private static IEnumerable<FuzzMark> ParseFuzzMarks(string path)
    {
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            var match = FuzzMarkRegex.Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            var idText = match.Groups["id"].Value;
            var dataText = match.Groups["data"].Value;
            yield return new FuzzMark(
                lineNumber,
                match.Groups["key"].Value,
                new CanFrame(
                    double.Parse(match.Groups["rel"].Value, CultureInfo.InvariantCulture),
                    uint.Parse(idText, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    string.IsNullOrEmpty(dataText) ? [] : Convert.FromHexString(dataText),
                    idText.Length > 3),
                match.Groups["note"].Value);
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

internal sealed record FuzzMark(
    int LineNumber,
    string Key,
    CanFrame ActiveFrame,
    string Note);

internal sealed record RangeFuzzerRange(
    uint FirstArbitrationId,
    uint LastArbitrationId);

internal sealed record RangeFuzzerCandidate(
    uint CanId,
    string PayloadHex,
    string Note)
{
    public ScheduledTxFrame ToFrame(TimeSpan period)
    {
        return new ScheduledTxFrame(
            CanId,
            CliOptions.ParseHexData(PayloadHex),
            period,
            true,
            Note);
    }
}

internal sealed record RpmWordSweepCandidate(
    uint CanId,
    string PayloadHex,
    string Note,
    int FullIndex,
    int FullCount)
{
    public ScheduledTxFrame ToFrame(TimeSpan period)
    {
        return new ScheduledTxFrame(
            CanId,
            CliOptions.ParseHexData(PayloadHex),
            period,
            true,
            Note);
    }
}

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
