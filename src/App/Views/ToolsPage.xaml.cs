using LogDeck.Core.Models;
using LogDeck.Core.Modules;
using LogDeck.Core.Services;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LogDeck.App.Views;

/// <summary>
/// The Tools tab: every tool LogDeck knows, whether it is installed, and where each of its
/// log sources and folders is, with Reveal and Copy path for each.
/// </summary>
public sealed partial class ToolsPage : Page
{
    public ToolsPage()
    {
        InitializeComponent();
        Build();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Build();

    private void Build()
    {
        Cards.Children.Clear();
        var tools = ToolCatalog.All.Select(m => (Module: m, Status: ToolDetector.Detect(m)))
            .OrderByDescending(t => t.Status.Installed)
            .ToList();
        Summary.Text = $"{tools.Count(t => t.Status.Installed)} of {tools.Count} installed";

        foreach (var (module, status) in tools)
            Cards.Children.Add(Card(module, status));
    }

    private static Border Card(ToolModule module, ToolStatus status)
    {
        var body = new StackPanel { Spacing = 10 };

        var header = new Grid { ColumnSpacing = 12 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new FontIcon
        {
            Glyph = module.Glyph,
            FontSize = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Resource("AccentTextFillColorPrimaryBrush"),
        });
        var title = new StackPanel();
        title.Children.Add(new TextBlock { Text = module.Name, FontWeight = FontWeights.SemiBold, FontSize = 16 });
        title.Children.Add(new TextBlock { Text = module.Category, FontSize = 12, Foreground = Resource("TextFillColorSecondaryBrush") });
        Grid.SetColumn(title, 1);
        header.Children.Add(title);
        var badge = new TextBlock
        {
            Text = status switch
            {
                { Installed: true, Version: { } v } => $"Installed  ·  {v}",
                { Installed: true } => "Installed",
                { HasLogFolder: true } => "Not installed, logs remain",
                _ => "Not installed",
            },
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Resource(status.Installed ? "SystemFillColorSuccessBrush" : "TextFillColorTertiaryBrush"),
        };
        Grid.SetColumn(badge, 2);
        header.Children.Add(badge);
        body.Children.Add(header);

        if (status.FoundPath is { } found)
            body.Children.Add(PathRow("Detected", found, null));

        foreach (var source in module.Sources)
        {
            var scan = LogScanner.Scan(source);
            var state = scan.State switch
            {
                SourceState.Ready => scan.Count == 1 ? "1 log" : $"{scan.Count} logs",
                SourceState.NeedsAdmin => "Run as administrator to read these",
                SourceState.Missing => "Not on this computer",
                _ => "No logs yet",
            };
            body.Children.Add(PathRow(source.Label, Describe(source), state, source.ResolvedRoot));
        }

        foreach (var support in module.SupportPaths)
            body.Children.Add(PathRow(support.Label, support.ResolvedPath,
                Directory.Exists(support.ResolvedPath) || File.Exists(support.ResolvedPath) ? null : "Not on this computer"));

        return new Border
        {
            Child = body,
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Background = Resource("CardBackgroundFillColorDefaultBrush"),
            BorderBrush = Resource("CardStrokeColorDefaultBrush"),
        };
    }

    /// <summary>The source's folder and what it matches there: C:\…\logs\*\*\install.log.</summary>
    private static string Describe(LogSource source)
    {
        var levels = string.Concat(Enumerable.Repeat(@"\*", source.Depth));
        return $@"{source.ResolvedRoot}{levels}\{string.Join(", ", source.Patterns)}";
    }

    private static Grid PathRow(string label, string shown, string? state, string? actionPath = null)
    {
        var path = actionPath ?? shown;
        var row = new Grid { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Foreground = Resource("TextFillColorSecondaryBrush") });

        var text = new TextBlock
        {
            Text = shown,
            FontFamily = (FontFamily)Application.Current.Resources["LogFontFamily"],
            FontSize = 12,
            IsTextSelectionEnabled = true,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTipService.SetToolTip(text, shown);
        Grid.SetColumn(text, 1);
        row.Children.Add(text);

        if (state is not null)
        {
            var stateText = new TextBlock
            {
                Text = state,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Resource(state.StartsWith("Run as", StringComparison.Ordinal) ? "SystemFillColorCautionBrush" : "TextFillColorTertiaryBrush"),
            };
            Grid.SetColumn(stateText, 2);
            row.Children.Add(stateText);
        }

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        actions.Children.Add(IconButton("\uEC50", "Reveal in File Explorer", () => Shell.Reveal(path)));
        actions.Children.Add(IconButton("\uE8C8", "Copy path", () => Shell.Copy(path)));
        Grid.SetColumn(actions, 3);
        row.Children.Add(actions);
        return row;
    }

    private static Button IconButton(string glyph, string tip, Action action)
    {
        var button = new Button { Content = new FontIcon { Glyph = glyph, FontSize = 12 }, Padding = new Thickness(6) };
        ToolTipService.SetToolTip(button, tip);
        button.Click += (_, _) => action();
        return button;
    }

    private static Brush Resource(string key) => (Brush)Application.Current.Resources[key];
}
