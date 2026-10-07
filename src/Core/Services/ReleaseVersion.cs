using System.Text.RegularExpressions;

namespace LogDeck.Core.Services;

/// <summary>
/// Formats a release version for display: the informational version without its +sha
/// suffix, and a date version zero-padded to YYYY.MM.DD.HHMM, the form release tags use.
/// </summary>
public static partial class ReleaseVersion
{
    public static string Display(string? version)
    {
        var bare = version?.Split('+')[0].Trim();
        if (string.IsNullOrEmpty(bare)) return "unknown";

        var match = DateVersion().Match(bare);
        if (!match.Success) return bare;

        return string.Join('.',
            match.Groups[1].Value,
            match.Groups[2].Value.PadLeft(2, '0'),
            match.Groups[3].Value.PadLeft(2, '0'),
            match.Groups[4].Value.PadLeft(4, '0'));
    }

    /// <summary>True for a YYYY.M.D.HHMM release version, with or without a +sha suffix.</summary>
    public static bool IsDateVersion(string? version) =>
        version is not null && DateVersion().IsMatch(version.Split('+')[0].Trim());

    [GeneratedRegex(@"^(\d{4})\.(\d{1,2})\.(\d{1,2})\.(\d{1,4})$")]
    private static partial Regex DateVersion();
}
