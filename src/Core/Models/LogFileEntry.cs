using System.Globalization;

namespace LogDeck.Core.Models;

/// <summary>One log file found under a source, usually one run's session.</summary>
/// <param name="Path">Full path of the file.</param>
/// <param name="Folder">The folder below the source root that holds it, empty for the root itself.</param>
/// <param name="LastWrite">When the file was last written, local time.</param>
/// <param name="Size">Length in bytes.</param>
public sealed record LogFileEntry(string Path, string Folder, DateTime LastWrite, long Size)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>The session folder when there is one (2026-10-07 / 1405-login), else the file name.</summary>
    public string Title => Folder.Length > 0 ? Folder.Replace('\\', '/').Replace("/", " / ") : FileName;

    /// <summary>The file name when the title is the session folder, then when it was written and its size.</summary>
    public string Subtitle
    {
        get
        {
            var when = LastWrite.ToString("MMM d, yyyy  HH:mm", CultureInfo.CurrentCulture);
            return Folder.Length > 0 ? $"{FileName}  ·  {when}  ·  {DisplaySize}" : $"{when}  ·  {DisplaySize}";
        }
    }

    public string DisplaySize => FormatSize(Size);

    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB",
    };
}
