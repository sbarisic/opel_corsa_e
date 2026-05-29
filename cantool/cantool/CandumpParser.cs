using System.Globalization;
using System.Text.RegularExpressions;

namespace cantool;

internal static class CandumpParser
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
