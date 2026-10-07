using LogDeck.Core.Services;

namespace LogDeck.Tests;

public class LineClassifierTests
{
    [Theory]
    // The Managed tools' own format: "[yyyy-MM-dd HH:mm:ss] LEVEL message", level padded to 5.
    [InlineData("[2026-10-07 14:05:33] ERROR Could not reach the repo", LineLevel.Error)]
    [InlineData("[2026-10-07 14:05:33] WARN  Retrying download", LineLevel.Warning)]
    [InlineData("[2026-10-07 14:05:33] DEBUG cache hit", LineLevel.Debug)]
    [InlineData("[2026-10-07 14:05:33] INFO  Starting run", LineLevel.Default)]
    // ReportMate and the Cimian watcher: Serilog with a three-letter level.
    [InlineData("2026-10-06 00:24:27.416 -07:00 [WRN] Slow module", LineLevel.Warning)]
    [InlineData("2026-10-06 00:24:27.416 -07:00 [ERR] Upload failed", LineLevel.Error)]
    [InlineData("2026-10-06 00:24:27.416 -07:00 [INF] Collected 12 modules", LineLevel.Default)]
    [InlineData("2026-10-06 00:24:27.416 -07:00 [DBG] detail", LineLevel.Debug)]
    [InlineData("2026-10-07 09:00:00.123 [FTL] crash", LineLevel.Error)]
    // events.jsonl
    [InlineData("{\"timestamp\":\"2026-10-07T14:05:33.000-07:00\",\"level\":\"error\",\"message\":\"x\"}", LineLevel.Error)]
    [InlineData("{\"timestamp\":\"2026-10-07T14:05:33.000-07:00\",\"level\":\"WARN\",\"message\":\"x\"}", LineLevel.Warning)]
    [InlineData("{\"timestamp\":\"2026-10-07T14:05:33.000-07:00\",\"level\":\"INFO\",\"message\":\"failed earlier\"}", LineLevel.Default)]
    // Leading tags and prose.
    [InlineData("[WARN] disk nearly full", LineLevel.Warning)]
    [InlineData("Error: access denied", LineLevel.Error)]
    [InlineData("System.IO.IOException: The file is in use", LineLevel.Error)]
    [InlineData("Installer failed to start", LineLevel.Error)]
    [InlineData("=== Session start ===", LineLevel.Header)]
    [InlineData("Install completed successfully", LineLevel.Success)]
    [InlineData("Downloading 3 items", LineLevel.Default)]
    [InlineData("", LineLevel.Default)]
    public void Classifies(string line, LineLevel expected) =>
        Assert.Equal(expected, LineClassifier.Classify(line));

    [Theory]
    [InlineData("3", LineLevel.Error)]
    [InlineData("2", LineLevel.Warning)]
    [InlineData("1", LineLevel.Default)]
    public void ClassifiesCmTraceByType(string type, LineLevel expected)
    {
        var line = $"<![LOG[Checking policy]LOG]!><time=\"14:05:33.1234567\" date=\"10-7-2026\" component=\"AppWorkload\" context=\"\" type=\"{type}\" thread=\"5\" file=\"\">";
        Assert.Equal(expected, LineClassifier.Classify(line));
    }

    [Fact]
    public void FormatsCmTraceForReading()
    {
        var line = "<![LOG[[Win32App] Downloading content]LOG]!><time=\"14:05:33.1234567\" date=\"10-7-2026\" component=\"AppWorkload\" context=\"\" type=\"1\" thread=\"5\" file=\"\">";
        Assert.Equal("2026-10-07 14:05:33.123  AppWorkload  [Win32App] Downloading content", CmTrace.Format(line));
    }

    [Fact]
    public void LeavesOtherLinesUnformatted() =>
        Assert.Equal("[2026-10-07 14:05:33] INFO  x", CmTrace.Format("[2026-10-07 14:05:33] INFO  x"));

    [Theory]
    [InlineData("2026.10.7.905+abc123", "2026.10.07.0905")]
    [InlineData("2026.10.07.1405", "2026.10.07.1405")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData(null, "unknown")]
    public void DisplaysReleaseVersions(string? version, string expected) =>
        Assert.Equal(expected, ReleaseVersion.Display(version));
}
