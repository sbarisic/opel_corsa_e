using System.Diagnostics;
using System.Globalization;

namespace cantool;

internal static class AppClock
{
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    public static void Start()
    {
        _ = Stopwatch.Elapsed;
    }

    public static double ElapsedSeconds => Stopwatch.Elapsed.TotalSeconds;
}

internal static class ConsoleFrames
{
    public static string FormatTx(int count, ScheduledTxFrame frame)
    {
        return $"[tx {count:00000} t={FormatElapsed()}] {frame.CandumpId}#{Convert.ToHexString(frame.Data)}";
    }

    public static string FormatTxLogComment(ScheduledTxFrame frame)
    {
        return $"# tx t={FormatElapsed()} {frame.CandumpId}#{Convert.ToHexString(frame.Data)} note={frame.Note}";
    }

    public static string FormatRx(int count, CanFrame frame)
    {
        return $"[rx {count:00000} t={FormatElapsed()}] {frame.CandumpId}#{frame.DataHex}";
    }

    private static string FormatElapsed()
    {
        return $"{AppClock.ElapsedSeconds.ToString("000.000", CultureInfo.InvariantCulture)}s";
    }
}
