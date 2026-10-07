using LogDeck.App.Views;
using LogDeck.Core.Models;
using LogDeck.Core.Services;
using Microsoft.UI.Xaml.Media;

namespace LogDeck.App.ViewModels;

/// <summary>A tool in the sidebar.</summary>
public sealed class ToolItem(ToolModule module, ToolStatus status)
{
    public ToolModule Module { get; } = module;
    public ToolStatus Status { get; } = status;
    public string Name => Module.Name;
    public string Glyph => Module.Glyph;

    /// <summary>Shown in the sidebar: the tool's category, or why it is dimmed.</summary>
    public string Subtitle => Status switch
    {
        { Installed: true, Version: { } version } => $"{Module.Category}  ·  {version}",
        { Installed: true } => Module.Category,
        { HasLogFolder: true } => $"{Module.Category}  ·  not installed, logs remain",
        _ => $"{Module.Category}  ·  not installed",
    };

    public double Opacity => Status.Installed || Status.HasLogFolder ? 1.0 : 0.5;
}

/// <summary>A log source in the picker above the session list.</summary>
public sealed class SourceItem(SourceScan scan)
{
    public SourceScan Scan { get; } = scan;
    public LogSource Source => Scan.Source;

    public string Display => Scan.State switch
    {
        SourceState.Ready => $"{Source.Label}  ({Scan.Count})",
        SourceState.NeedsAdmin => $"{Source.Label}  (administrator)",
        _ => $"{Source.Label}  (none)",
    };

    public override string ToString() => Display;
}

/// <summary>One log file, usually one run's session.</summary>
public sealed class SessionItem(LogFileEntry entry)
{
    public LogFileEntry Entry { get; } = entry;
    public string Title => Entry.Title;
    public string Subtitle => Entry.Subtitle;
}

/// <summary>One line in the viewer.</summary>
public sealed class LineItem(int number, string raw)
{
    public int Number { get; } = number;
    public string NumberText => Number.ToString();
    public string Raw { get; } = raw;

    /// <summary>CMTrace records are shown as "date time  component  message"; other lines as written.</summary>
    public string Text { get; } = CmTrace.Format(raw);

    public LineLevel Level { get; } = LineClassifier.Classify(raw);
    public Brush Foreground => LogBrushes.For(Level);
}
