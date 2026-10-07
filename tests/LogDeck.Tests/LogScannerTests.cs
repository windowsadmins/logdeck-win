using LogDeck.Core.Models;
using LogDeck.Core.Services;

namespace LogDeck.Tests;

public class LogScannerTests
{
    [Fact]
    public void SessionsAreListedNewestFirst()
    {
        using var temp = new TempDirectory();
        temp.Write(@"2026-10-06\0900-boot\startset.log", "a", new DateTime(2026, 10, 6, 9, 0, 0));
        temp.Write(@"2026-10-07\1405-login\startset.log", "b", new DateTime(2026, 10, 7, 14, 5, 0));
        temp.Write(@"2026-10-07\0800-boot\startset.log", "c", new DateTime(2026, 10, 7, 8, 0, 0));
        temp.Write(@"2026-10-07\0800-boot\events.jsonl", "{}", new DateTime(2026, 10, 7, 8, 0, 0));

        var source = new LogSource("sessions", "Sessions", temp.Path, ["startset.log"], Depth: 2);
        var scan = LogScanner.Scan(source);

        Assert.Equal(SourceState.Ready, scan.State);
        Assert.Equal(3, scan.Count);
        Assert.Equal(
            ["2026-10-07 / 1405-login", "2026-10-07 / 0800-boot", "2026-10-06 / 0900-boot"],
            scan.Files.Select(f => f.Title));
    }

    [Fact]
    public void DepthLimitsTheSearch()
    {
        using var temp = new TempDirectory();
        temp.Write("top.log", "x");
        temp.Write(@"one\one.log", "x");
        temp.Write(@"one\two\two.log", "x");

        Assert.Single(LogScanner.Scan(new LogSource("s", "S", temp.Path, ["*.log"])).Files);
        Assert.Equal(2, LogScanner.Scan(new LogSource("s", "S", temp.Path, ["*.log"], Depth: 1)).Count);
        Assert.Equal(3, LogScanner.Scan(new LogSource("s", "S", temp.Path, ["*.log"], Depth: 2)).Count);
    }

    [Fact]
    public void OverlappingPatternsListAFileOnce()
    {
        using var temp = new TempDirectory();
        temp.Write("csharpdialog.log", "x");
        temp.Write("csharpdialog.log.1", "x");
        temp.Write("other.txt", "x");

        var scan = LogScanner.Scan(new LogSource("s", "S", temp.Path, ["csharpdialog.log", "csharpdialog.log.*", "*.log"]));
        Assert.Equal(2, scan.Count);
    }

    [Fact]
    public void MissingFolderIsReportedAsMissing()
    {
        var scan = LogScanner.Scan(new LogSource("s", "S", Path.Combine(Path.GetTempPath(), "no-such-" + Guid.NewGuid()), ["*.log"]));
        Assert.Equal(SourceState.Missing, scan.State);
        Assert.Empty(scan.Files);
    }

    [Fact]
    public void EmptyFolderIsReportedAsEmpty()
    {
        using var temp = new TempDirectory();
        Assert.Equal(SourceState.Empty, LogScanner.Scan(new LogSource("s", "S", temp.Path, ["*.log"])).State);
    }

    [Fact]
    public void FlatFileTitleIsItsName()
    {
        using var temp = new TempDirectory();
        temp.Write("reportmate-20261007.log", "x");
        var file = Assert.Single(LogScanner.Scan(new LogSource("s", "S", temp.Path, ["*.log"])).Files);
        Assert.Equal("reportmate-20261007.log", file.Title);
        Assert.Equal(string.Empty, file.Folder);
    }

    [Theory]
    [InlineData(10, "10 B")]
    [InlineData(2048, "2 KB")]
    [InlineData(5 * 1024 * 1024, "5 MB")]
    public void SizesAreReadable(long bytes, string expected) =>
        Assert.Equal(expected, LogFileEntry.FormatSize(bytes));
}
