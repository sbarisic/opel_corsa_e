using System.Globalization;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace cantool;

internal static class CommandHandlers
{
    private const double InteractiveDefaultSeconds = 15.0;

    public static int InteractiveMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("cantool - IPC low-speed GMLAN profile sender");
            Console.WriteLine("Select a profile to transmit:");
            for (var i = 0; i < Profiles.Available.Length; i++)
            {
                var profile = Profiles.Available[i];
                Console.WriteLine($"  {i + 1}) {profile.Name,-20} {profile.Description}");
            }

            Console.WriteLine("  q) quit");
            Console.Write("Profile [1]: ");
            var profileText = (Console.ReadLine() ?? "").Trim();
            if (profileText.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var selected = ResolveInteractiveProfile(profileText);
            if (selected is null)
            {
                Console.WriteLine("Unknown profile selection.");
                continue;
            }

            Console.Write($"Duration seconds [{InteractiveDefaultSeconds:0}] (0 = until Ctrl+C): ");
            var secondsText = (Console.ReadLine() ?? "").Trim();
            if (!TryParseDuration(secondsText, out var seconds))
            {
                Console.WriteLine("Invalid duration.");
                continue;
            }

            var duration = seconds <= 0 ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds);
            var logPrefix = $"cantool_{selected.Name}_tx_rx";
            var result = SendSchedule(
                Profiles.GetSchedule(selected.Name),
                duration,
                countLimit: null,
                rxLogPath: DefaultLiveOutput(logPrefix),
                banner: duration is null
                    ? $"transmitting {selected.Name} until Ctrl+C"
                    : $"transmitting {selected.Name} for {seconds:0.###}s");
            if (result != 0)
            {
                return result;
            }
        }
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

    public static int Capture(string[] args)
    {
        var seconds = CliOptions.GetDouble(args, "--seconds", 5.0);
        var continuous = seconds <= 0;
        var listenOnly = CliOptions.HasFlag(args, "--listen-only");
        var outPath = CliOptions.GetString(args, "--out") ?? DefaultLiveOutput("cantool_gsusb_33333");

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        using var device = GsUsbDevice.Open(listenOnly);
        var flushed = device.FlushStale(TimeSpan.FromMilliseconds(300));
        if (flushed > 0)
        {
            Console.WriteLine($"flushed {flushed} stale RX frame(s)");
        }

        using var done = new ConsoleCancelScope();

        Console.WriteLine(
            continuous
                ? $"capturing until Ctrl+C to {outPath}"
                : $"capturing {seconds:F1}s to {outPath}");

        var count = 0;
        using (var writer = new StreamWriter(outPath, append: false))
        {
            writer.WriteLine("# cantool gs_usb direct capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");
            var stopAt = continuous ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddSeconds(seconds);
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

        Console.WriteLine($"done: {count} frames");
        return 0;
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
        var seconds = CliOptions.GetDouble(args, "--seconds", 15.0);
        return SendSchedule(
            Profiles.GetSchedule(profile),
            TimeSpan.FromSeconds(seconds),
            countLimit: null,
            CliOptions.GetString(args, "--rx-log"));
    }

    public static int Summarize(string[] args)
    {
        var logPath = CliOptions.GetString(args, "--log") ?? throw new ArgumentException("--log is required");
        var records = CandumpParser.Parse(logPath).ToList();
        var groups = records
            .GroupBy(r => (r.CanId, r.IsExtended))
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key.CanId)
            .ToList();

        Console.WriteLine($"{logPath}: {records.Count} frames / {groups.Count} unique IDs");
        foreach (var group in groups)
        {
            var ordered = group.OrderBy(r => r.Timestamp).ToList();
            var periods = ordered.Zip(ordered.Skip(1), (a, b) => (b.Timestamp - a.Timestamp) * 1000.0).ToList();
            var periodText = periods.Count == 0 ? "single" : Median(periods).ToString("0.###", CultureInfo.InvariantCulture);
            var dlcs = string.Join(";", ordered.GroupBy(r => r.Data.Length).OrderBy(g => g.Key).Select(g => $"{g.Key}:{g.Count()}"));
            var firstPayload = ordered.First().DataHex;
            var idText = ordered.First().CandumpId;
            var format = group.Key.IsExtended ? "extended" : "standard";
            var gmlan = group.Key.IsExtended ? Gmlan29Id.Decode(group.Key.CanId).ToSummaryString() : "";
            Console.WriteLine($"{idText,12} {format,-8} {gmlan,-38} count={ordered.Count,5} period_ms={periodText,10} dlcs={dlcs} payload={firstPayload}");
        }

        return 0;
    }

    private static int SendSchedule(List<ScheduledTxFrame> schedule, TimeSpan? duration, int? countLimit, string? rxLogPath, string? banner = null)
    {
        var outPath = rxLogPath ?? DefaultLiveOutput("cantool_tx_rx");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        using var device = GsUsbDevice.Open(listenOnly: false);
        var flushed = device.FlushStale(TimeSpan.FromMilliseconds(300));
        if (flushed > 0)
        {
            Console.WriteLine($"flushed {flushed} stale RX frame(s)");
        }

        using var done = new ConsoleCancelScope();

        if (banner is not null)
        {
            Console.WriteLine($"{banner}; RX log {outPath}");
        }
        else
        {
            Console.WriteLine($"transmitting for {duration?.TotalSeconds:0.###}s; RX log {outPath}");
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in schedule)
        {
            item.NextDue = now + item.InitialDelay;
        }

        var end = duration is null ? DateTimeOffset.MaxValue : now + duration.Value;
        var sent = 0;
        var received = 0;
        using var writer = new StreamWriter(outPath, append: false);
        writer.WriteLine("# cantool TX/RX capture bitrate=33333.333 fclk=170000000 brp=255 prop=1 phase1=13 phase2=5 sjw=4");

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
                item.NextDue = now + item.Period;
                Console.WriteLine(ConsoleFrames.FormatTx(sent, item));
            }

            var frame = device.ReadFrame(timeoutMs: 20);
            if (frame is null)
            {
                continue;
            }

            received++;
            var line = frame.Value.ToCandumpLine();
            writer.WriteLine(line);
            writer.Flush();
            Console.WriteLine(ConsoleFrames.FormatRx(received, frame.Value));
        }

        Console.WriteLine($"done: sent={sent} rx_frames={received} rx_log={outPath}");
        return 0;
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

    private static bool TryParseDuration(string input, out double seconds)
    {
        if (input.Length == 0)
        {
            seconds = InteractiveDefaultSeconds;
            return true;
        }

        return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds >= 0;
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
