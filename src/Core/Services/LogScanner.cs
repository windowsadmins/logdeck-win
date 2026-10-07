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
        {
            var hidden = HiddenByAccess(root);
            return new SourceScan(source, hidden ? SourceState.NeedsAdmin : SourceState.Missing, [], PartlyDenied: hidden);
        }

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

    /// <summary>
    /// Directory.Exists is false both for a folder that is not there and for one inside a
    /// folder this account may not list (the Intune Management Extension's Logs, under a
    /// parent readable by administrators only). Tells them apart by listing the nearest
    /// ancestor that can be seen.
    /// </summary>
    public static bool HiddenByAccess(string path)
    {
        var child = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Path.GetDirectoryName(child);
        while (parent is not null && !Directory.Exists(parent))
        {
            child = parent;
            parent = Path.GetDirectoryName(parent);
        }
        if (parent is null) return false;

        try
        {
            // Listing the parent succeeds when access is not the problem: then the folder
            // simply is not there.
            using var entries = Directory.EnumerateFileSystemEntries(parent, Path.GetFileName(child)).GetEnumerator();
            entries.MoveNext();
            return false;
        }
        catch (UnauthorizedAccessException) { return true; }
        catch (System.Security.SecurityException) { return true; }
        catch (IOException) { return false; }
    }

    private static string RelativeFolder(string root, string directory)
    {
        var relative = Path.GetRelativePath(root, directory);
        return relative == "." ? string.Empty : relative;
    }
}
