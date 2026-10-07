using System.Reflection;
using System.Runtime.InteropServices;
using LogDeck.App.Views;
using LogDeck.Core.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Window = Microsoft.UI.Xaml.Window;

namespace LogDeck.App;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    public MainWindow()
    {
        InitializeComponent();

        Title = "LogDeck";

        // DPI-aware sizing clamped to the available work area.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        var workArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
            AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(
            Math.Min((int)(1360 * scale), (int)(workArea.Width * 0.96)),
            Math.Min((int)(900 * scale), (int)(workArea.Height * 0.96))));

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LogDeck.ico");
        AppWindow.SetIcon(iconPath);
        TitleBarIcon.Source = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "LogDeck.png")));

        // Extend content into the title bar for a seamless, theme-matching look.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new MicaBackdrop();

        VersionText.Text = ReleaseVersion.Display(
            Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        ElevatedBadge.Visibility = Elevation.IsElevated
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        // Logs is the default; --tab tools opens the other tab.
        var tab = CommandLine.Value("--tab")?.ToLowerInvariant() ?? "logs";
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>()
            .FirstOrDefault(i => (string?)i.Tag == tab) ?? NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            var pageType = item.Tag?.ToString() switch
            {
                "tools" => typeof(ToolsPage),
                _ => typeof(LogsPage),
            };
            ContentFrame.Navigate(pageType);
        }
    }
}
