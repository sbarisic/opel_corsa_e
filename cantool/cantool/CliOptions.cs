using System.Globalization;

namespace cantool;

internal static class CliOptions
{
    public static bool HasFlag(string[] args, string name) => args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    public static string? GetString(string[] args, string name)
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

    public static double GetDouble(string[] args, string name, double defaultValue)
    {
        var value = GetString(args, name);
        return value is null ? defaultValue : double.Parse(value, CultureInfo.InvariantCulture);
    }

    public static int GetInt(string[] args, string name, int defaultValue)
    {
        var value = GetString(args, name);
        return value is null ? defaultValue : int.Parse(value, CultureInfo.InvariantCulture);
    }

    public static uint ParseUInt(string value)
    {
        var text = value.Trim().Replace("_", "");
        return text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? uint.Parse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : uint.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    public static byte[] ParseHexData(string value)
    {
        var text = value.Trim().Replace(" ", "").Replace("_", "");
        if (text.Length == 0)
        {
            return [];
        }

        if (text.Length % 2 != 0)
        {
            throw new ArgumentException($"payload hex must have an even number of digits: {value}");
        }

        var data = Convert.FromHexString(text);
        if (data.Length > 8)
        {
            throw new ArgumentException("classic CAN payloads must be 8 bytes or fewer");
        }

        return data;
    }
}
