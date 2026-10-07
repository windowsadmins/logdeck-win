using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace LogDeck.App;

/// <summary>
/// LogDeck runs unelevated. When a log is readable by administrators only, the viewer says
/// so and offers to start a second, elevated copy; nothing is ever written either way.
/// </summary>
internal static class Elevation
{
    public static bool IsElevated { get; } = Check();

    private static bool Check()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Starts LogDeck elevated on <paramref name="toolId"/> and closes this copy. False when
    /// the UAC prompt was declined or the start failed; this copy then stays open.
    /// </summary>
    public static bool RestartElevated(string? toolId)
    {
        var exe = Environment.ProcessPath;
        if (exe is null) return false;
        var args = toolId is null ? string.Empty : $"--tool {toolId}";
        try
        {
            Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = true, Verb = "runas" });
            Microsoft.UI.Xaml.Application.Current.Exit();
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
