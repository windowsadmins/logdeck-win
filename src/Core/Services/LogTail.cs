using System.Text;

namespace LogDeck.Core.Services;

/// <summary>Lines read from a log, and whether the file was replaced since the last read.</summary>
/// <param name="Lines">Complete lines, without line endings.</param>
/// <param name="Reset">The file shrank or was replaced: discard what was shown and use these lines.</param>
/// <param name="SkippedBytes">Bytes at the start of the file not read because the file is large.</param>
public sealed record TailRead(IReadOnlyList<string> Lines, bool Reset, long SkippedBytes);

/// <summary>
/// Reads a log file and then only what is appended to it. Opens with FileShare.ReadWrite and
/// Delete so a tool that is still writing is never blocked, keeps a partial last line until
/// its newline arrives, and starts again when the file is truncated or rotated.
/// UTF-8 and UTF-16 (with or without a byte-order mark) are both read.
/// </summary>
public sealed class LogTail
{
    /// <summary>A large log is shown from this many bytes before its end.</summary>
    public const long DefaultInitialBytes = 8L * 1024 * 1024;

    private readonly string _path;
    private readonly long _initialBytes;
    private long _offset;
    private Encoding _encoding = new UTF8Encoding(false);
    private Decoder _decoder = new UTF8Encoding(false).GetDecoder();
    private readonly StringBuilder _pending = new();
    private DateTime _created;

    public LogTail(string path, long initialBytes = DefaultInitialBytes)
    {
        _path = path;
        _initialBytes = initialBytes;
    }

    public string Path => _path;

    /// <summary>Reads the file from the start, or its last <c>initialBytes</c> when larger.</summary>
    /// <exception cref="UnauthorizedAccessException">This account may not read the file.</exception>
    /// <exception cref="IOException">The file cannot be opened.</exception>
    public TailRead ReadInitial()
    {
        using var stream = Open();
        var length = stream.Length;
        _created = SafeCreationTime();
        _pending.Clear();

        var (encoding, bomLength) = DetectEncoding(stream);
        _encoding = encoding;
        _decoder = encoding.GetDecoder();

        long start = bomLength;
        long skipped = 0;
        if (length - bomLength > _initialBytes)
        {
            start = length - _initialBytes;
            if (IsUtf16(encoding) && (start - bomLength) % 2 != 0) start++;
            skipped = start - bomLength;
        }

        var lines = ReadFrom(stream, start, length);
        // Starting mid-file lands inside a line; drop that fragment.
        if (skipped > 0 && lines.Count > 0) lines.RemoveAt(0);
        return new TailRead(lines, Reset: true, skipped);
    }

    /// <summary>
    /// Reads what was appended since the last read. When the file shrank or was recreated, reads
    /// it again from the start and reports <see cref="TailRead.Reset"/>.
    /// </summary>
    public TailRead ReadNew()
    {
        using var stream = Open();
        var length = stream.Length;
        var created = SafeCreationTime();
        if (length < _offset || created != _created)
        {
            stream.Dispose();
            return ReadInitial();
        }
        if (length == _offset) return new TailRead([], Reset: false, 0);
        return new TailRead(ReadFrom(stream, _offset, length), Reset: false, 0);
    }

    private FileStream Open() => new(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    private DateTime SafeCreationTime()
    {
        try { return File.GetCreationTimeUtc(_path); } catch { return default; }
    }

    private List<string> ReadFrom(FileStream stream, long start, long end)
    {
        stream.Seek(start, SeekOrigin.Begin);
        var buffer = new byte[64 * 1024];
        var chars = new char[_encoding.GetMaxCharCount(buffer.Length)];
        var lines = new List<string>();
        var remaining = end - start;

        while (remaining > 0)
        {
            var read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0) break;
            remaining -= read;
            var count = _decoder.GetChars(buffer, 0, read, chars, 0, flush: false);
            Split(chars.AsSpan(0, count), lines);
        }

        _offset = end - remaining;
        return lines;
    }

    private void Split(ReadOnlySpan<char> text, List<string> lines)
    {
        while (true)
        {
            var newline = text.IndexOf('\n');
            if (newline < 0)
            {
                _pending.Append(text);
                return;
            }
            _pending.Append(text[..newline]);
            var line = _pending.ToString();
            _pending.Clear();
            lines.Add(line.EndsWith('\r') ? line[..^1] : line);
            text = text[(newline + 1)..];
        }
    }

    /// <summary>
    /// The encoding from the byte-order mark, or UTF-16 LE when the first bytes are ASCII
    /// characters interleaved with zeros, else UTF-8.
    /// </summary>
    internal static (Encoding Encoding, int BomLength) DetectEncoding(Stream stream)
    {
        Span<byte> head = stackalloc byte[4];
        stream.Seek(0, SeekOrigin.Begin);
        var n = stream.Read(head);
        head = head[..n];

        if (n >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF) return (new UTF8Encoding(false), 3);
        if (n >= 2 && head[0] == 0xFF && head[1] == 0xFE) return (new UnicodeEncoding(false, false), 2);
        if (n >= 2 && head[0] == 0xFE && head[1] == 0xFF) return (new UnicodeEncoding(true, false), 2);
        if (n >= 4 && head[0] != 0 && head[1] == 0 && head[2] != 0 && head[3] == 0) return (new UnicodeEncoding(false, false), 0);
        return (new UTF8Encoding(false), 0);
    }

    private static bool IsUtf16(Encoding encoding) => encoding is UnicodeEncoding;
}
