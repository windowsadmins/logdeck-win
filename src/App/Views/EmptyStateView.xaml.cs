using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LogDeck.App.Views;

/// <summary>The message a pane shows when it has nothing to list.</summary>
public sealed partial class EmptyStateView : UserControl
{
    public EmptyStateView()
    {
        InitializeComponent();
    }

    /// <summary>Raised by the "Restart as administrator" button.</summary>
    public event EventHandler? AdminRequested;

    /// <param name="offerAdmin">Show the restart button: the content needs administrator rights.</param>
    public void Show(string glyph, string title, string message, bool offerAdmin = false)
    {
        Icon.Glyph = glyph;
        TitleText.Text = title;
        MessageText.Text = message;
        MessageText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        AdminButton.Visibility = offerAdmin && !Elevation.IsElevated ? Visibility.Visible : Visibility.Collapsed;
        Visibility = Visibility.Visible;
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    private void AdminButton_Click(object sender, RoutedEventArgs e) => AdminRequested?.Invoke(this, EventArgs.Empty);

    public static class Glyphs
    {
        public const string Document = "";
        public const string Folder = "";
        public const string Admin = "";
        public const string Search = "";
        public const string Tools = "";
        public const string Warning = "";
    }
}
