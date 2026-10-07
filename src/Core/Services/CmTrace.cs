using System.Text.RegularExpressions;

namespace LogDeck.Core.Services;

/// <summary>One CMTrace record: the format of the Intune Management Extension logs.</summary>
/// <param name="Type">1 information, 2 warning, 3 error.</param>
public sealed record CmTraceEntry(string Message, string Date, string Time, string Component, int Type);

/// <summary>
/// Reads the CMTrace line format,
/// <c>&lt;![LOG[message]LOG]!&gt;&lt;time="14:05:33.1234567" date="10-7-2026" component="AppWorkload" type="1" ...&gt;</c>,
/// and renders it as "2026-10-07 14:05:33.123  AppWorkload  message" for reading.
/// </summary>
public static partial class CmTrace
{
    public static bool TryParse(string line, out CmTraceEntry entry)
    {
        entry = null!;
        if (!line.StartsWith("<![LOG[", StringComparison.Ordinal)) return false;
        var match = Record().Match(line);
        if (!match.Success) return false;

        var attributes = match.Groups["attrs"].Value;
        entry = new CmTraceEntry(
            match.Groups["msg"].Value,
            Attribute(attributes, "date"),
            Attribute(attributes, "time"),
            Attribute(attributes, "component"),
            int.TryParse(Attribute(attributes, "type"), out var type) ? type : 1);
        return true;
    }

    /// <summary>The readable form of a CMTrace line, or the line unchanged when it is not one.</summary>
    public static string Format(string line)
    {
        if (!TryParse(line, out var entry)) return line;
        var stamp = $"{FormatDate(entry.Date)} {FormatTime(entry.Time)}".Trim();
        var component = entry.Component.Length > 0 ? $"  {entry.Component}" : string.Empty;
        return $"{stamp}{component}  {entry.Message}";
    }

    // CMTrace writes M-D-YYYY.
    private static string FormatDate(string date)
    {
        var parts = date.Split('-');
        return parts.Length == 3 && parts[2].Length == 4
            ? $"{parts[2]}-{parts[0].PadLeft(2, '0')}-{parts[1].PadLeft(2, '0')}"
            : date;
    }

    // 14:05:33.1234567 or 14:05:33.123+420: keep milliseconds, drop the bias.
    private static string FormatTime(string time)
    {
        var bias = time.IndexOfAny(['+', '-']);
        if (bias > 0) time = time[..bias];
        var dot = time.IndexOf('.');
        return dot > 0 && time.Length > dot + 4 ? time[..(dot + 4)] : time;
    }

    private static string Attribute(string attributes, string name)
    {
        var match = Regex.Match(attributes, $@"\b{name}=""([^""]*)""");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    [GeneratedRegex(@"^<!\[LOG\[(?<msg>.*?)\]LOG\]!><(?<attrs>[^>]*)>", RegexOptions.Singleline)]
    private static partial Regex Record();
}
