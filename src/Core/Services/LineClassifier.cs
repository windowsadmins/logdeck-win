using System.Text.RegularExpressions;

namespace LogDeck.Core.Services;

public enum LineLevel { Default, Error, Warning, Success, Debug, Header }

/// <summary>
/// Picks a severity for one log line. The tools write several formats, tried in this order:
/// CMTrace (the Intune Management Extension: type="3" error, type="2" warning), JSON lines
/// with a "level" field, a level token after a bracketed timestamp
/// ("[2026-10-07 14:05:33] ERROR ..."), a leading tag ("[WARN] ...", "Error: ..."), and last
/// an upper-case level word anywhere in the line.
/// </summary>
public static partial class LineClassifier
{
    public static LineLevel Classify(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return LineLevel.Default;
        var trimmed = line.TrimStart();

        if (CmTrace.TryParse(trimmed, out var entry))
        {
            return entry.Type switch
            {
                3 => LineLevel.Error,
                2 => LineLevel.Warning,
                _ => Classify(entry.Message) switch
                {
                    LineLevel.Header => LineLevel.Default,
                    var level => level,
                },
            };
        }

        if (trimmed.StartsWith('{') && JsonLevel().Match(trimmed) is { Success: true } json)
            return FromWord(json.Groups[1].Value) ?? LineLevel.Default;

        if (LeadingLevel().Match(trimmed) is { Success: true } leading)
        {
            var level = FromWord(leading.Groups[1].Value);
            if (level is not null) return level.Value;
        }

        if (trimmed.StartsWith("===", StringComparison.Ordinal) || trimmed.StartsWith("---", StringComparison.Ordinal)
            || trimmed.StartsWith("###", StringComparison.Ordinal) || trimmed.StartsWith("***", StringComparison.Ordinal))
            return LineLevel.Header;

        if (UpperError().IsMatch(line)) return LineLevel.Error;
        if (UpperWarning().IsMatch(line)) return LineLevel.Warning;
        if (FailureWord().IsMatch(line)) return LineLevel.Error;
        if (UpperDebug().IsMatch(line)) return LineLevel.Debug;
        if (UpperSuccess().IsMatch(line) || line.Contains("completed successfully", StringComparison.OrdinalIgnoreCase))
            return LineLevel.Success;
        return LineLevel.Default;
    }

    private static LineLevel? FromWord(string word) => word.ToUpperInvariant() switch
    {
        "ERROR" or "ERR" or "FATAL" or "FTL" or "CRITICAL" or "CRIT" or "FAIL" or "FAILED" or "FAILURE" => LineLevel.Error,
        "WARN" or "WARNING" or "WRN" => LineLevel.Warning,
        "DEBUG" or "DBG" or "TRACE" or "VERBOSE" or "VRB" => LineLevel.Debug,
        "SUCCESS" or "OK" or "PASS" or "PASSED" => LineLevel.Success,
        "INFO" or "INFORMATION" or "INF" or "NOTICE" => LineLevel.Default,
        _ => null,
    };

    // "[timestamp] LEVEL", "timestamp LEVEL", "[LEVEL]", "LEVEL:", "timestamp [LEVEL]", "Error: ..."
    [GeneratedRegex(@"^(?:\[[^\]]*\d[^\]]*\]\s*|\d{4}-\d{2}-\d{2}[T ][\d:.,]+(?:\s?(?:Z|[+-]\d{2}:?\d{2}))?\s+|\d{1,2}/\d{1,2}/\d{4}\s+[\d:.]+(?:\s*[AP]M)?\s+)?(?:-\s*)?\[?([A-Za-z]{2,11})\]?(?::|\s|$)")]
    private static partial Regex LeadingLevel();

    [GeneratedRegex(@"""(?:level|Level|severity|Severity|lvl|@l)""\s*:\s*""([A-Za-z]+)""")]
    private static partial Regex JsonLevel();

    [GeneratedRegex(@"\b(?:ERROR|FATAL|CRITICAL|FAILED|FAILURE)\b")]
    private static partial Regex UpperError();

    [GeneratedRegex(@"\b(?:WARN|WARNING)\b")]
    private static partial Regex UpperWarning();

    [GeneratedRegex(@"\b(?:DEBUG|TRACE|VERBOSE)\b")]
    private static partial Regex UpperDebug();

    [GeneratedRegex(@"\bSUCCESS\b")]
    private static partial Regex UpperSuccess();

    // "Exception", "failed to", "Error:" in ordinary prose.
    [GeneratedRegex(@"\b(?:[A-Za-z]+Exception|[Ff]ailed to|Error:)")]
    private static partial Regex FailureWord();
}
