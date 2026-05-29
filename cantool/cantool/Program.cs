using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace cantool;

internal static class Program
{
	private const uint CanEffFlag = 0x80000000;
	private const uint CanEffMask = 0x1FFFFFFF;

	private const byte RequestTypeOut = 0x41;
	private const byte RequestTypeIn = 0xC1;
	private const byte BreqBitTiming = 1;
	private const byte BreqMode = 2;
	private const byte BreqBtConst = 4;
	private const byte BreqDeviceConfig = 5;

	private const uint ModeReset = 0;
	private const uint ModeStart = 1;
	private const uint ModeListenOnly = 1;
	private const uint ModeHwTimestamp = 16;

	private static readonly (int Vid, int Pid, string Name)[] KnownDevices =
	[
		(0x1D50, 0x606F, "gs_usb/candleLight"),
		(0x1209, 0x2323, "candleLight"),
		(0x1CD2, 0x606F, "CANext FD"),
		(0x16D0, 0x10B8, "CANDebugger FD"),
		(0x1209, 0xCA01, "CANnectivity")
	];

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return Capture(["--seconds", "0"]);
        }

        if (args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return 0;
        }

		try
		{
			return args[0].ToLowerInvariant() switch
			{
				"list" => ListDevices(),
				"capture" => Capture(args[1..]),
				"summarize" => Summarize(args[1..]),
				_ => Fail($"unknown command: {args[0]}")
			};
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"error: {ex.Message}");
			return 1;
		}
	}

	private static void PrintUsage()
	{
		Console.WriteLine("""
            cantool - candleLight/gs_usb low-speed CAN helper

            Commands:
              <no args>   capture continuously and print CAN frames until Ctrl+C
              list
              capture [--seconds N] [--out PATH] [--listen-only]
              summarize --log PATH

            Capture timing is the known-good IPC low-speed profile:
              bitrate 33.333 kbit/s, brp=255, prop=1, phase1=13, phase2=5, sjw=4
            """);
	}

	private static int ListDevices()
	{
		var found = 0;
		foreach (var (vid, pid, name) in KnownDevices)
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

    private static int Capture(string[] args)
    {
        var seconds = GetDouble(args, "--seconds", 5.0);
        var continuous = seconds <= 0;
        var listenOnly = HasFlag(args, "--listen-only");
        var outPath = GetString(args, "--out") ?? DefaultLiveOutput("cantool_gsusb_33333");

		Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

		using var device = GsUsbDevice.Open(listenOnly);
		var flushed = device.FlushStale(TimeSpan.FromMilliseconds(300));
		if (flushed > 0)
		{
			Console.WriteLine($"flushed {flushed} stale RX frame(s)");
		}

        using var done = new ManualResetEventSlim(false);
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            done.Set();
        };

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
                Console.WriteLine($"[rx {count:00000}] {line}");
            }
        }

		Console.WriteLine($"done: {count} frames");
		return 0;
	}

	private static int Summarize(string[] args)
	{
		var logPath = GetString(args, "--log") ?? throw new ArgumentException("--log is required");
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
			Console.WriteLine($"{idText,12} {format,-8} count={ordered.Count,5} period_ms={periodText,10} dlcs={dlcs} payload={firstPayload}");
		}

		return 0;
	}

	private static int Fail(string message)
	{
		Console.Error.WriteLine(message);
		PrintUsage();
		return 1;
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

	private static bool HasFlag(string[] args, string name) => args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

	private static string? GetString(string[] args, string name)
	{
		for (var i = 0; i < args.Length - 1; i++)
		{
			if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
			{
				return args[i + 1];
			}
		}

		return null;
	}

	private static double GetDouble(string[] args, string name, double defaultValue)
	{
		var value = GetString(args, name);
		return value is null ? defaultValue : double.Parse(value, CultureInfo.InvariantCulture);
	}

	private static double Median(List<double> values)
	{
		values.Sort();
		var mid = values.Count / 2;
		return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2.0;
	}

	private sealed class GsUsbDevice : IDisposable
	{
		private readonly UsbDevice _device;
		private readonly UsbEndpointReader _reader;
		private readonly bool _hwTimestamp;

		private GsUsbDevice(UsbDevice device, UsbEndpointReader reader, bool hwTimestamp)
		{
			_device = device;
			_reader = reader;
			_hwTimestamp = hwTimestamp;
		}

		public static GsUsbDevice Open(bool listenOnly)
		{
			UsbDevice? device = null;
			foreach (var (vid, pid, _) in KnownDevices)
			{
				device = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vid, pid));
				if (device is not null)
				{
					break;
				}
			}

			if (device is null)
			{
				throw new InvalidOperationException("no gs_usb/candleLight devices found");
			}

			if (device is IUsbDevice wholeDevice)
			{
				wholeDevice.SetConfiguration(1);
				wholeDevice.ClaimInterface(0);
			}

			var gs = new GsUsbDevice(device, device.OpenEndpointReader(ReadEndpointID.Ep01, 24), hwTimestamp: true);
			gs.Stop(ignoreErrors: true);
			var capability = gs.ReadCapability();
			var flags = ModeHwTimestamp | (listenOnly ? ModeListenOnly : 0u);
			flags &= capability.Feature;
			flags &= ModeHwTimestamp | ModeListenOnly;

			gs.SetTiming(propSeg: 1, phaseSeg1: 13, phaseSeg2: 5, sjw: 4, brp: 255);
			gs.Start(flags);
			gs._reader.Flush();
			return gs;
		}

		public int FlushStale(TimeSpan duration)
		{
			var end = DateTime.UtcNow + duration;
			var count = 0;
			while (DateTime.UtcNow < end)
			{
				if (ReadFrame(timeoutMs: 10) is not null)
				{
					count++;
				}
			}

			return count;
		}

		public CanFrame? ReadFrame(int timeoutMs)
		{
			var size = _hwTimestamp ? 24 : 20;
			var buffer = new byte[size];
			var result = _reader.Read(buffer, timeoutMs, out var transferred);
			if (result is ErrorCode.IoTimedOut or ErrorCode.None && transferred == 0)
			{
				return null;
			}

			if (result != ErrorCode.None || transferred < size)
			{
				return null;
			}

			return CanFrame.FromGsUsb(buffer, _hwTimestamp);
		}

		public void Dispose()
		{
			Stop(ignoreErrors: true);
			if (_device is IUsbDevice wholeDevice)
			{
				wholeDevice.ReleaseInterface(0);
			}

			_reader.Dispose();
			_device.Close();
		}

		private DeviceCapability ReadCapability()
		{
			var data = ControlIn(BreqBtConst, 40);
			return DeviceCapability.FromBytes(data);
		}

		private void SetTiming(uint propSeg, uint phaseSeg1, uint phaseSeg2, uint sjw, uint brp)
		{
			Span<byte> data = stackalloc byte[20];
			BinaryPrimitives.WriteUInt32LittleEndian(data[0..4], propSeg);
			BinaryPrimitives.WriteUInt32LittleEndian(data[4..8], phaseSeg1);
			BinaryPrimitives.WriteUInt32LittleEndian(data[8..12], phaseSeg2);
			BinaryPrimitives.WriteUInt32LittleEndian(data[12..16], sjw);
			BinaryPrimitives.WriteUInt32LittleEndian(data[16..20], brp);
			ControlOut(BreqBitTiming, data.ToArray());
		}

		private void Start(uint flags)
		{
			Span<byte> data = stackalloc byte[8];
			BinaryPrimitives.WriteUInt32LittleEndian(data[0..4], ModeStart);
			BinaryPrimitives.WriteUInt32LittleEndian(data[4..8], flags);
			ControlOut(BreqMode, data.ToArray());
		}

		private void Stop(bool ignoreErrors)
		{
			Span<byte> data = stackalloc byte[8];
			BinaryPrimitives.WriteUInt32LittleEndian(data[0..4], ModeReset);
			BinaryPrimitives.WriteUInt32LittleEndian(data[4..8], 0);
			try
			{
				ControlOut(BreqMode, data.ToArray());
			}
			catch when (ignoreErrors)
			{
			}
		}

		private byte[] ControlIn(byte request, short length)
		{
			var buffer = new byte[length];
			var packet = new UsbSetupPacket(RequestTypeIn, request, 0, 0, length);
			if (!_device.ControlTransfer(ref packet, buffer, length, out var transferred) || transferred != length)
			{
				throw new InvalidOperationException($"control IN request {request} failed: transferred={transferred}");
			}

			return buffer;
		}

		private void ControlOut(byte request, byte[] data)
		{
			var packet = new UsbSetupPacket(RequestTypeOut, request, 0, 0, (short)data.Length);
			if (!_device.ControlTransfer(ref packet, data, data.Length, out var transferred) || transferred != data.Length)
			{
				throw new InvalidOperationException($"control OUT request {request} failed: transferred={transferred}");
			}
		}
	}

	private readonly record struct DeviceCapability(uint Feature, uint Clock)
	{
		public static DeviceCapability FromBytes(byte[] data)
		{
			return new DeviceCapability(
				BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4)),
				BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4, 4)));
		}
	}

	private readonly record struct CanFrame(double Timestamp, uint CanId, byte[] Data, bool IsExtended)
	{
		public string DataHex => Convert.ToHexString(Data);
		public string CandumpId => IsExtended ? $"{CanId:X8}" : $"{CanId:X3}";

		public static CanFrame FromGsUsb(byte[] raw, bool hwTimestamp)
		{
			var canIdRaw = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(4, 4));
			var dlc = Math.Clamp(raw[8], (byte)0, (byte)8);
			var data = raw.Skip(12).Take(dlc).ToArray();
			var isExtended = (canIdRaw & CanEffFlag) != 0;
			var arbitrationId = canIdRaw & CanEffMask;
			var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
			return new CanFrame(ts, arbitrationId, data, isExtended);
		}

		public string ToCandumpLine() => $"({Timestamp:0.000000}) can0 {CandumpId}#{DataHex}";
	}

	private static class CandumpParser
	{
		private static readonly Regex LineRegex = new(@"^\((?<ts>[^)]+)\)\s+\S+\s+(?<id>[0-9A-Fa-f]+)#(?<data>[0-9A-Fa-f]*)", RegexOptions.Compiled);

		public static IEnumerable<CanFrame> Parse(string path)
		{
			foreach (var line in File.ReadLines(path))
			{
				var match = LineRegex.Match(line.Trim());
				if (!match.Success)
				{
					continue;
				}

				var idText = match.Groups["id"].Value;
				var dataText = match.Groups["data"].Value;
				yield return new CanFrame(
					double.Parse(match.Groups["ts"].Value, CultureInfo.InvariantCulture),
					uint.Parse(idText, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
					string.IsNullOrEmpty(dataText) ? [] : Convert.FromHexString(dataText),
					idText.Length > 3);
			}
		}
	}
}
