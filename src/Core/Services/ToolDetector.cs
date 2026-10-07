using System.Diagnostics;
using LogDeck.Core.Models;

namespace LogDeck.Core.Services;

/// <summary>Whether a tool is on this machine, and what it left behind.</summary>
/// <param name="Installed">Any of the module's detection paths exists.</param>
/// <param name="FoundPath">The first detection path that exists.</param>
/// <param name="Version">The file version of <paramref name="FoundPath"/> when it is an exe.</param>
/// <param name="HasLogFolder">Any log source's folder exists, installed or not.</param>
public sealed record ToolStatus(bool Installed, string? FoundPath, string? Version, bool HasLogFolder);

public static class ToolDetector
{
    public static ToolStatus Detect(ToolModule module)
    {
        string? found = null;
        foreach (var path in module.DetectionPaths)
        {
            var resolved = Environment.ExpandEnvironmentVariables(path);
            if (File.Exists(resolved) || Directory.Exists(resolved))
            {
                found = resolved;
                break;
            }
        }

        var hasLogs = module.Sources.Any(s => Directory.Exists(s.ResolvedRoot) || LogScanner.HiddenByAccess(s.ResolvedRoot));
        return new ToolStatus(found is not null, found, found is null ? null : FileVersion(found), hasLogs);
    }

    private static string? FileVersion(string path)
    {
        if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            return null;
        try
        {
            // Prefer the file version: some tools stamp the release only there and leave the
            // product version at 1.0.0.
            var info = FileVersionInfo.GetVersionInfo(path);
            var version = new[] { info.FileVersion, info.ProductVersion }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .OrderByDescending(v => ReleaseVersion.IsDateVersion(v))
                .FirstOrDefault();
            return version is null ? null : ReleaseVersion.Display(version);
        }
        catch
        {
            return null;
        }
    }
}
