using LogDeck.Core.Models;

namespace LogDeck.Core.Services;

/// <summary>
/// Lists the log files of a source, newest first. Never throws: a folder this account may not
/// read is reported as <see cref="SourceState.NeedsAdmin"/> so the viewer can say so.
/// </summary>
public static class LogScanner
{
    private static readonly EnumerationOptions Options = new()
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        RecurseSubdirectories = false,
        IgnoreInaccessible = false,
        AttributesToSkip = FileAttributes.System,
    };

    public static SourceScan Scan(LogSource source) => Scan(source, source.ResolvedRoot);

    /// <summary>Scans <paramref name="source"/> with its root replaced by <paramref name="root"/>.</summary>
    public static SourceScan Scan(LogSource source, string root)
    {
        if (!Directory.Exists(root))
            return new SourceScan(source, SourceState.Missing, []);

        var found = new Dictionary<string, LogFileEntry>(StringComparer.OrdinalIgnoreCase);
        var denied = 0;
        var rootDenied = false;

        void Walk(string directory, int level)
        {
            try
            {
                foreach (var pattern in source.Patterns)
                {
                    foreach (var file in Directory.EnumerateFiles(directory, pattern, Options))
                    {
                        if (found.ContainsKey(file)) continue;
                        try
                        {
                            var info = new FileInfo(file);
                            found[file] = new LogFileEntry(file, RelativeFolder(root, directory), info.LastWriteTime, info.Length);
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                }

                if (level >= source.Depth) return;
                foreach (var child in Directory.EnumerateDirectories(directory, "*", Options))
                    Walk(child, level + 1);
            }
            catch (UnauthorizedAccessException)
            {
                if (level == 0) rootDenied = true;
                denied++;
            }
            catch (System.Security.SecurityException)
            {
                if (level == 0) rootDenied = true;
                denied++;
            }
            catch (IOException) { }
        }

        Walk(root, 0);

        var files = found.Values
            .OrderByDescending(f => f.LastWrite)
            .ThenByDescending(f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rootDenied && files.Count == 0)
            return new SourceScan(source, SourceState.NeedsAdmin, files, PartlyDenied: true);
        if (files.Count == 0)
            return new SourceScan(source, denied > 0 ? SourceState.NeedsAdmin : SourceState.Empty, files, denied > 0);
        return new SourceScan(source, SourceState.Ready, files, denied > 0);
    }

    private static string RelativeFolder(string root, string directory)
    {
        var relative = Path.GetRelativePath(root, directory);
        return relative == "." ? string.Empty : relative;
    }
}
