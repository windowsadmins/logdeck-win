namespace LogDeck.Core.Models;

/// <summary>
/// One management tool: how to tell it is installed, where it writes its logs, and the
/// other folders an admin troubleshooting it is likely to want. A module is data only;
/// <see cref="Services.ToolDetector"/> and <see cref="Services.LogScanner"/> do the I/O.
/// </summary>
/// <param name="Id">Stable lower-case id, also accepted by --tool on the command line.</param>
/// <param name="Name">The tool's name as the sidebar shows it.</param>
/// <param name="Category">What the tool does, shown under the name.</param>
/// <param name="Glyph">A Segoe Fluent Icons glyph for the sidebar.</param>
/// <param name="DetectionPaths">The tool counts as installed when any of these exists.</param>
/// <param name="Sources">The log locations, in the order the viewer offers them.</param>
/// <param name="SupportPaths">Install, data and configuration folders worth revealing.</param>
public sealed record ToolModule(
    string Id,
    string Name,
    string Category,
    string Glyph,
    IReadOnlyList<string> DetectionPaths,
    IReadOnlyList<LogSource> Sources,
    IReadOnlyList<SupportPath> SupportPaths);

/// <summary>A folder or file shown on the Tools tab with Reveal and Copy path actions.</summary>
public sealed record SupportPath(string Label, string Path)
{
    public string ResolvedPath => Environment.ExpandEnvironmentVariables(Path);
}
