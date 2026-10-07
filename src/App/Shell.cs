using System.ComponentModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;

namespace LogDeck.App;

/// <summary>The Reveal, Open and Copy actions. Each only reads; none changes anything.</summary>
internal static class Shell
{
    /// <summary>Opens Explorer with <paramref name="path"/> selected, or opens the folder itself.</summary>
    public static void Reveal(string path)
    {
        try
        {
            if (File.Exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            else if (Directory.Exists(path))
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            else if (Path.GetDirectoryName(path) is { } parent && Directory.Exists(parent))
                Process.Start(new ProcessStartInfo(parent) { UseShellExecute = true });
        }
        catch (Win32Exception) { }
    }

    /// <summary>Opens the file in its default app, or Notepad when it has none (.jsonl).</summary>
    public static void OpenInEditor(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            try { Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true }); }
            catch (Win32Exception) { }
        }
    }

    public static void Copy(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
        Clipboard.Flush();
    }
}
