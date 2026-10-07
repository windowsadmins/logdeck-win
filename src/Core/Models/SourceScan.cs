namespace LogDeck.Core.Models;

public enum SourceState
{
    /// <summary>Log files were found.</summary>
    Ready,

    /// <summary>The folder exists and is readable but holds no logs yet.</summary>
    Empty,

    /// <summary>The folder does not exist on this machine.</summary>
    Missing,

    /// <summary>The folder exists but this account may not list it: run as administrator.</summary>
    NeedsAdmin,
}

/// <summary>What <see cref="Services.LogScanner"/> found for one source, newest file first.</summary>
/// <param name="PartlyDenied">Some subfolders could not be listed, so the list may be incomplete.</param>
public sealed record SourceScan(LogSource Source, SourceState State, IReadOnlyList<LogFileEntry> Files, bool PartlyDenied = false)
{
    public int Count => Files.Count;
}
