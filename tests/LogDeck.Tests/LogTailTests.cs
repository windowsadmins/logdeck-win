using System.Text;
using LogDeck.Core.Services;

namespace LogDeck.Tests;

public class LogTailTests
{
    [Fact]
    public void ReadsTheWholeFileThenOnlyWhatIsAppended()
    {
        using var temp = new TempDirectory();
        var path = temp.Write("a.log", "one\r\ntwo\n");
        var tail = new LogTail(path);

        Assert.Equal(["one", "two"], tail.ReadInitial().Lines);
        Assert.Empty(tail.ReadNew().Lines);

        File.AppendAllText(path, "three\nfour\n");
        var next = tail.ReadNew();
        Assert.False(next.Reset);
        Assert.Equal(["three", "four"], next.Lines);
    }

    [Fact]
    public void KeepsAPartialLineUntilItsNewlineArrives()
    {
        using var temp = new TempDirectory();
        var path = temp.Write("a.log", "one\npar");
        var tail = new LogTail(path);

        Assert.Equal(["one"], tail.ReadInitial().Lines);
        File.AppendAllText(path, "tial\n");
        Assert.Equal(["partial"], tail.ReadNew().Lines);
    }

    [Fact]
    public void StartsAgainWhenTheFileIsTruncated()
    {
        using var temp = new TempDirectory();
        var path = temp.Write("a.log", "a long first line\nsecond\n");
        var tail = new LogTail(path);
        tail.ReadInitial();

        File.WriteAllText(path, "new\n");
        var next = tail.ReadNew();
        Assert.True(next.Reset);
        Assert.Equal(["new"], next.Lines);
    }

    [Fact]
    public void ReadsOnlyTheEndOfALargeFile()
    {
        using var temp = new TempDirectory();
        var lines = Enumerable.Range(0, 1000).Select(i => $"line {i:D4}");
        var path = temp.Write("big.log", string.Join('\n', lines) + "\n");
        var tail = new LogTail(path, initialBytes: 100);

        var read = tail.ReadInitial();
        Assert.True(read.SkippedBytes > 0);
        Assert.Equal("line 0999", read.Lines[^1]);
        Assert.All(read.Lines, l => Assert.StartsWith("line ", l));
    }

    [Fact]
    public void ReadsUtf16WithAByteOrderMark()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "u16.log");
        File.WriteAllText(path, "héllo\nwörld\n", new UnicodeEncoding(false, true));

        Assert.Equal(["héllo", "wörld"], new LogTail(path).ReadInitial().Lines);
    }

    [Fact]
    public void ReadsUtf16WithoutAByteOrderMark()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "u16.log");
        File.WriteAllBytes(path, new UnicodeEncoding(false, false).GetBytes("abc\ndef\n"));

        Assert.Equal(["abc", "def"], new LogTail(path).ReadInitial().Lines);
    }

    [Fact]
    public void ReadsUtf8WithAByteOrderMark()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "u8.log");
        File.WriteAllText(path, "first\n", new UTF8Encoding(true));

        Assert.Equal(["first"], new LogTail(path).ReadInitial().Lines);
    }

    [Fact]
    public void ReadsAFileAnotherProcessHasOpenForWriting()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "open.log");
        using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        writer.Write("held\n"u8);
        writer.Flush();

        Assert.Equal(["held"], new LogTail(path).ReadInitial().Lines);
    }
}
