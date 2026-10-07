using System.Globalization;
using System.Text.RegularExpressions;

namespace LogDeck.Core.Models;

/// <summary>One log file found under a source, usually one run's session.</summary>
/// <param name="Path">Full path of the file.</param>
/// <param name="Folder">The folder below the source root that holds it, empty for the root itself.</param>
/// <param name="LastWrite">When the file was last written, local time.</param>
/// <param name="Size">Length in bytes.</param>
public sealed partial record LogFileEntry(string Path, string Folder, DateTime LastWrite, long Size)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>
    /// The session when the file sits in one, else the file name. Session folders read as a
    /// time: 2026-10-07\153941 is "2026-10-07 15:39:41", 2026-10-07\1405-login_2 is
    /// "2026-10-07 14:05  ·  login (2)", a day folder is its date.
    /// </summary>
    public string Title => Folder.Length == 0 ? FileName : SessionTitle(Folder);

    public static string SessionTitle(string folder)
    {
        var segments = folder.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 2 && DayPattern().IsMatch(segments[0]) && SessionPattern().Match(segments[1]) is { Success: true } session)
            return Describe(segments[0], session);
        if (segments.Length == 1 && StampPattern().Match(segments[0]) is { Success: true } stamp)
            return Describe(stamp.Groups["day"].Value, stamp);
        if (segments.Length == 1 && DayPattern().IsMatch(segments[0]))
            return segments[0];
        return string.Join(" / ", segments);
    }

    private static string Describe(string day, Match time)
    {
        var text = $"{day} {time.Groups["h"].Value}:{time.Groups["m"].Value}";
        if (time.Groups["s"].Success) text += $":{time.Groups["s"].Value}";
        if (time.Groups["run"].Success) text += $"  ·  {time.Groups["run"].Value}";
        if (time.Groups["n"].Success) text += $" ({time.Groups["n"].Value})";
        return text;
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    private static partial Regex DayPattern();

    // HHmm or HHmmss, then an optional -runtype, then an optional _2.._9.
    [GeneratedRegex(@"^(?<h>[01]\d|2[0-3])(?<m>[0-5]\d)(?<s>[0-5]\d)?(?:-(?<run>[A-Za-z0-9-]+?))?(?:_(?<n>\d))?$")]
    private static partial Regex SessionPattern();

    // yyyy-MM-dd-HHmmss in one folder name.
    [GeneratedRegex(@"^(?<day>\d{4}-\d{2}-\d{2})-(?<h>[01]\d|2[0-3])(?<m>[0-5]\d)(?<s>[0-5]\d)?(?:_(?<n>\d))?$")]
    private static partial Regex StampPattern();

    /// <summary>The file name when the title is the session folder, then when it was written and its size.</summary>
    public string Subtitle
    {
        get
        {
            var when = LastWrite.ToString("MMM d, yyyy  HH:mm", CultureInfo.CurrentCulture);
            return Folder.Length > 0 ? $"{FileName}  ·  {when}  ·  {DisplaySize}" : $"{when}  ·  {DisplaySize}";
        }
    }

    public string DisplaySize => FormatSize(Size);

    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB",
    };
}
