using Microsoft.UI.Xaml;

namespace LogDeck.App;

public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LogDeck", "startup-crash.log");

    private Window? _window;

    public App()
    {
        UnhandledException += (_, e) =>
        {
            e.Handled = true;
            Log($"UnhandledException: {e.Exception?.GetType().FullName}\n{e.Exception?.Message}\n{e.Exception?.StackTrace}");
        };
        ApplyThemeArgument();
        InitializeComponent();
    }

    /// <summary>
    /// --theme light|dark overrides the Windows theme for this run. Set here because the
    /// application theme can only be chosen before the first window exists.
    /// </summary>
    private void ApplyThemeArgument()
    {
        switch (CommandLine.Value("--theme")?.ToLowerInvariant())
        {
            case "light": RequestedTheme = ApplicationTheme.Light; break;
            case "dark": RequestedTheme = ApplicationTheme.Dark; break;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            Log($"OnLaunched exception: {ex.GetType().FullName}\n{ex.Message}\n{ex.StackTrace}");
        }
    }

    // The one file LogDeck writes, and only when it crashes: in the user's own profile.
    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
            File.WriteAllText(CrashLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{message}\n");
        }
        catch { }
    }
}
