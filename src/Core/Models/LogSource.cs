namespace LogDeck.Core.Models;

/// <summary>
/// A place a tool writes logs: a folder, the file-name masks that count as logs in it, and
/// how many levels of subfolders to look through. Session-per-folder tools set
/// <see cref="Depth"/> to reach their run folders (logs\2026-10-07\1405-login\startset.log
/// is depth 2); flat tools leave it at 0.
/// </summary>
/// <param name="Id">Stable id, unique within the tool.</param>
/// <param name="Label">What the viewer calls this source.</param>
/// <param name="Root">The folder, with environment variables such as %ProgramData% allowed.</param>
/// <param name="Patterns">File-name masks (*.log, install.log) matched in the root and its subfolders.</param>
/// <param name="Depth">How many subfolder levels below the root to search.</param>
public sealed record LogSource(
    string Id,
    string Label,
    string Root,
    IReadOnlyList<string> Patterns,
    int Depth = 0)
{
    public string ResolvedRoot => Environment.ExpandEnvironmentVariables(Root);
}
