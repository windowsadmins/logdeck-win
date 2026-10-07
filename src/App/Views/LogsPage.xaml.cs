using System.Collections.ObjectModel;
using LogDeck.App.ViewModels;
using LogDeck.Core.Models;
using LogDeck.Core.Modules;
using LogDeck.Core.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace LogDeck.App.Views;

/// <summary>
/// The Logs tab: the tools in the sidebar, the selected tool's sources and their sessions
/// newest first, and the selected log with live tail, filter and severity colours.
/// </summary>
public sealed partial class LogsPage : Page
{
    private static readonly TimeSpan TailInterval = TimeSpan.FromSeconds(1);
    private const int RescanEveryTicks = 5;

    private readonly DispatcherQueueTimer _timer;
    private List<ToolItem> _allTools = [];
    private List<LineItem> _lines = [];
    private ObservableCollection<LineItem> _shown = [];
    private LogTail? _tail;
    private LogFileEntry? _openFile;
    private int _openVersion;
    private bool _reading;
    private int _ticks;
    private bool _restoringSelection;

    public LogsPage()
    {
        InitializeComponent();

        ToolsEmpty.AdminRequested += RestartElevated;
        SessionsEmpty.AdminRequested += RestartElevated;
        ViewerEmpty.AdminRequested += RestartElevated;

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TailInterval;
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();

        LoadTools(CommandLine.Value("--tool"));
    }

    private ToolItem? SelectedTool => ToolList.SelectedItem as ToolItem;
    private SourceItem? SelectedSource => SourcePicker.SelectedItem as SourceItem;

    // ── Tools ────────────────────────────────────────────────────

    private void LoadTools(string? selectId = null)
    {
        selectId ??= SelectedTool?.Module.Id;
        _allTools = ToolCatalog.All
            .Select(m => new ToolItem(m, ToolDetector.Detect(m)))
            .OrderByDescending(t => t.Status.Installed)
            .ThenByDescending(t => t.Status.HasLogFolder)
            .ToList();
        ShowTools(selectId);
    }

    private void ShowTools(string? selectId)
    {
        var showAll = ShowAllToggle.IsChecked == true;
        var visible = _allTools.Where(t => showAll || t.Status.Installed || t.Status.HasLogFolder).ToList();
        ToolList.ItemsSource = visible;

        if (visible.Count == 0)
        {
            ToolsEmpty.Show(EmptyStateView.Glyphs.Tools, "No tools found",
                "None of the management tools LogDeck knows is installed on this computer. Turn on the filter button above to list them all.");
            ShowSources(null);
            return;
        }

        ToolsEmpty.Hide();
        ToolList.SelectedItem = visible.FirstOrDefault(t => string.Equals(t.Module.Id, selectId, StringComparison.OrdinalIgnoreCase))
            ?? visible[0];
    }

    private void ToolList_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowSources(SelectedTool);

    private void ShowAll_Click(object sender, RoutedEventArgs e) => ShowTools(SelectedTool?.Module.Id);

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        var source = SelectedSource?.Source.Id;
        var file = _openFile?.Path;
        LoadTools();
        if (source is not null) SelectSource(source);
        if (file is not null) SelectSession(file);
    }

    // ── Sources ──────────────────────────────────────────────────

    private void ShowSources(ToolItem? tool)
    {
        if (tool is null)
        {
            SourcePicker.ItemsSource = null;
            ShowSessions(null);
            return;
        }

        var items = tool.Module.Sources.Select(s => new SourceItem(LogScanner.Scan(s))).ToList();
        SourcePicker.ItemsSource = items;
        var preferred = CommandLine.Value("--source");
        SourcePicker.SelectedItem =
            items.FirstOrDefault(i => string.Equals(i.Source.Id, preferred, StringComparison.OrdinalIgnoreCase))
            ?? items.FirstOrDefault(i => i.Scan.State == SourceState.Ready)
            ?? items.FirstOrDefault(i => i.Scan.State == SourceState.NeedsAdmin)
            ?? items.FirstOrDefault();
    }

    private void SelectSource(string id)
    {
        if (SourcePicker.ItemsSource is IEnumerable<SourceItem> items && items.FirstOrDefault(i => i.Source.Id == id) is { } item)
            SourcePicker.SelectedItem = item;
    }

    private void SourcePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringSelection) return;
        ShowSessions(SelectedSource);
    }

    private void RevealSource_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSource is { } source) Shell.Reveal(source.Source.ResolvedRoot);
    }

    // ── Sessions ─────────────────────────────────────────────────

    private void ShowSessions(SourceItem? source)
    {
        RevealSourceButton.IsEnabled = source is not null && Directory.Exists(source.Source.ResolvedRoot);

        if (source is null)
        {
            SessionList.ItemsSource = null;
            SessionsEmpty.Show(EmptyStateView.Glyphs.Folder, "No tool selected", "Pick a tool on the left to see its logs.");
            CloseLog();
            return;
        }

        var scan = source.Scan;
        SessionList.ItemsSource = scan.Files.Select(f => new SessionItem(f)).ToList();
        var root = source.Source.ResolvedRoot;

        switch (scan.State)
        {
            case SourceState.Ready:
                SessionsEmpty.Hide();
                SessionList.SelectedIndex = 0;
                break;
            case SourceState.NeedsAdmin:
                SessionsEmpty.Show(EmptyStateView.Glyphs.Admin, "Run as administrator to read these",
                    Elevation.IsElevated
                        ? $"{root} cannot be read even as administrator."
                        : $"{root} is readable by administrators only.",
                    offerAdmin: true);
                CloseLog();
                break;
            case SourceState.Missing:
                SessionsEmpty.Show(EmptyStateView.Glyphs.Folder, "No logs on this computer",
                    $"{root} does not exist. The tool writes it on its first run.");
                CloseLog();
                break;
            default:
                SessionsEmpty.Show(EmptyStateView.Glyphs.Document, "No logs yet",
                    $"{root} holds no {string.Join(" or ", source.Source.Patterns)} files yet.");
                CloseLog();
                break;
        }
    }

    private void SelectSession(string path)
    {
        if (SessionList.ItemsSource is IEnumerable<SessionItem> items
            && items.FirstOrDefault(i => string.Equals(i.Entry.Path, path, StringComparison.OrdinalIgnoreCase)) is { } item)
            SessionList.SelectedItem = item;
    }

    private void SessionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringSelection) return;
        if (SessionList.SelectedItem is SessionItem session) _ = OpenLogAsync(session.Entry);
        else if (SelectedSource?.Scan.State == SourceState.Ready) ShowNoLogSelected();
    }

    /// <summary>
    /// Every few seconds, list the selected source again so a new run shows up without a
    /// refresh. The selection and the open log are kept.
    /// </summary>
    private void RescanSelectedSource()
    {
        if (SelectedSource is not { } current || SelectedTool is null) return;
        var scan = LogScanner.Scan(current.Source);
        var before = current.Scan;
        if (scan.State == before.State && scan.Count == before.Count
            && scan.Files.FirstOrDefault()?.Path == before.Files.FirstOrDefault()?.Path)
            return;

        var items = (SourcePicker.ItemsSource as IEnumerable<SourceItem>)?.ToList() ?? [];
        var index = items.IndexOf(current);
        if (index < 0) return;
        var replacement = new SourceItem(scan);
        items[index] = replacement;

        _restoringSelection = true;
        try
        {
            SourcePicker.ItemsSource = items;
            SourcePicker.SelectedItem = replacement;
            if (scan.State == SourceState.Ready && _openFile is { } open && scan.Files.Any(f => f.Path == open.Path))
            {
                SessionList.ItemsSource = scan.Files.Select(f => new SessionItem(f)).ToList();
                SessionsEmpty.Hide();
                SelectSession(open.Path);
                return;
            }
        }
        finally
        {
            _restoringSelection = false;
        }
        ShowSessions(replacement);
    }

    // ── Viewer ───────────────────────────────────────────────────

    private async Task OpenLogAsync(LogFileEntry entry)
    {
        var version = ++_openVersion;
        var tail = new LogTail(entry.Path);
        _tail = null;
        _openFile = entry;
        SetFileActions(true);
        ViewerHeader.Visibility = Visibility.Visible;
        ViewerPath.Text = entry.Path;
        ViewerInfo.Text = "Reading…";

        TailRead read;
        List<LineItem> lines;
        try
        {
            (read, lines) = await Task.Run(() =>
            {
                var r = tail.ReadInitial();
                return (r, r.Lines.Select((l, i) => new LineItem(i + 1, l)).ToList());
            });
        }
        catch (UnauthorizedAccessException)
        {
            if (version != _openVersion) return;
            ShowLines([]);
            ViewerInfo.Text = string.Empty;
            ViewerEmpty.Show(EmptyStateView.Glyphs.Admin, "Run as administrator to read this log",
                Elevation.IsElevated
                    ? "This log cannot be read even as administrator."
                    : "This log is readable by administrators only.",
                offerAdmin: true);
            return;
        }
        catch (Exception ex) when (ex is IOException or System.Security.SecurityException)
        {
            if (version != _openVersion) return;
            ShowLines([]);
            ViewerInfo.Text = string.Empty;
            ViewerEmpty.Show(EmptyStateView.Glyphs.Warning, "Could not open this log", ex.Message);
            return;
        }

        if (version != _openVersion) return;
        _tail = tail;
        _lines = lines;
        UpdateInfo(read.SkippedBytes);
        ApplyFilter(scrollToEnd: true);
    }

    private void CloseLog()
    {
        _openVersion++;
        _tail = null;
        _openFile = null;
        _lines = [];
        ShowLines([]);
        ViewerHeader.Visibility = Visibility.Collapsed;
        SetFileActions(false);
        ShowNoLogSelected();
    }

    private void ShowNoLogSelected() =>
        ViewerEmpty.Show(EmptyStateView.Glyphs.Document, "No log selected", "Select a log on the left to read it here.");

    private void SetFileActions(bool enabled)
    {
        CopyPathButton.IsEnabled = enabled;
        RevealButton.IsEnabled = enabled;
        OpenButton.IsEnabled = enabled;
    }

    private void UpdateInfo(long skippedBytes)
    {
        if (_openFile is null) return;
        var size = TryLength(_openFile.Path) ?? _openFile.Size;
        var info = $"{LogFileEntry.FormatSize(size)}  ·  {_lines.Count:N0} lines";
        if (skippedBytes > 0) info += $"  ·  last {LogFileEntry.FormatSize(size - skippedBytes)} shown";
        ViewerInfo.Text = info;
    }

    private static long? TryLength(string path)
    {
        try { return new FileInfo(path).Length; } catch { return null; }
    }

    private void OnTick()
    {
        if (++_ticks % RescanEveryTicks == 0) RescanSelectedSource();
        if (LiveToggle.IsChecked == true) _ = TailAsync();
    }

    private async Task TailAsync()
    {
        if (_tail is not { } tail || _reading) return;
        var version = _openVersion;
        _reading = true;
        try
        {
            var read = await Task.Run(() =>
            {
                try { return tail.ReadNew(); }
                catch { return null; }
            });
            if (read is null || version != _openVersion || (read.Lines.Count == 0 && !read.Reset)) return;

            if (read.Reset)
            {
                _lines = read.Lines.Select((l, i) => new LineItem(i + 1, l)).ToList();
                UpdateInfo(read.SkippedBytes);
                ApplyFilter(scrollToEnd: true);
                return;
            }

            var level = LevelPicker.SelectedIndex;
            var filter = FilterBox.Text;
            foreach (var raw in read.Lines)
            {
                var item = new LineItem(_lines.Count + 1, raw);
                _lines.Add(item);
                if (Matches(item, filter, level)) _shown.Add(item);
            }
            UpdateInfo(0);
            UpdateViewerEmpty();
            ScrollToEnd();
        }
        finally
        {
            _reading = false;
        }
    }

    // ── Filter ───────────────────────────────────────────────────

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter(scrollToEnd: false);

    private void LevelPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LineList is not null) ApplyFilter(scrollToEnd: false);
    }

    private void ApplyFilter(bool scrollToEnd)
    {
        var level = LevelPicker.SelectedIndex;
        var filter = FilterBox.Text;
        ShowLines(_lines.Where(l => Matches(l, filter, level)));
        UpdateViewerEmpty();
        if (scrollToEnd) ScrollToEnd();
    }

    private static bool Matches(LineItem line, string filter, int level)
    {
        if (level == 1 && line.Level is not (LineLevel.Error or LineLevel.Warning)) return false;
        if (level == 2 && line.Level is not LineLevel.Error) return false;
        return string.IsNullOrEmpty(filter) || line.Text.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Shows the last line. Queued behind layout: right after ItemsSource changes the list
    /// has not measured its items yet, and scrolling then lands at the top.
    /// </summary>
    private void ScrollToEnd()
    {
        if (_shown.Count == 0) return;
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (_shown.Count > 0 && ReferenceEquals(LineList.ItemsSource, _shown))
                LineList.ScrollIntoView(_shown[^1], ScrollIntoViewAlignment.Leading);
        });
    }

    private void ShowLines(IEnumerable<LineItem> lines)
    {
        _shown = new ObservableCollection<LineItem>(lines);
        LineList.ItemsSource = _shown;
    }

    private void UpdateViewerEmpty()
    {
        if (_openFile is null) return;
        if (_shown.Count > 0) ViewerEmpty.Hide();
        else if (_lines.Count == 0) ViewerEmpty.Show(EmptyStateView.Glyphs.Document, "This log is empty", "New lines appear here as they are written while Live is on.");
        else ViewerEmpty.Show(EmptyStateView.Glyphs.Search, "No matching lines", "No line matches the filter and level chosen above.");
    }

    // ── Actions ──────────────────────────────────────────────────

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (_openFile is { } file) Shell.Copy(file.Path);
    }

    private void Reveal_Click(object sender, RoutedEventArgs e)
    {
        if (_openFile is { } file) Shell.Reveal(file.Path);
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (_openFile is { } file) Shell.OpenInEditor(file.Path);
    }

    private void CopyLines_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        CopySelected();
        args.Handled = true;
    }

    private void CopySelected_Click(object sender, RoutedEventArgs e) => CopySelected();

    private void CopyAll_Click(object sender, RoutedEventArgs e)
    {
        if (_shown.Count > 0) Shell.Copy(string.Join(Environment.NewLine, _shown.Select(l => l.Raw)));
    }

    private void CopySelected()
    {
        var selected = LineList.SelectedItems.OfType<LineItem>().OrderBy(l => l.Number).Select(l => l.Raw).ToList();
        if (selected.Count > 0) Shell.Copy(string.Join(Environment.NewLine, selected));
    }

    private void RestartElevated(object? sender, EventArgs e) => Elevation.RestartElevated(SelectedTool?.Module.Id);
}
