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

        var hasLogs = module.Sources.Any(s => Directory.Exists(s.ResolvedRoot));
        return new ToolStatus(found is not null, found, found is null ? null : FileVersion(found), hasLogs);
    }

    private static string? FileVersion(string path)
    {
        if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            return null;
        try
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            var version = info.ProductVersion ?? info.FileVersion;
            return string.IsNullOrWhiteSpace(version) ? null : ReleaseVersion.Display(version);
        }
        catch
        {
            return null;
        }
    }
}
