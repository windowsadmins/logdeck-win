using LogDeck.Core.Services;
using Microsoft.UI.Xaml.Media;

namespace LogDeck.App.Views;

/// <summary>One colour per log level, the same as the Managed tools' own Logs tabs.</summary>
internal static class LogBrushes
{
    private static SolidColorBrush? _error, _warning, _success, _debug, _header;

    public static Brush For(LineLevel level) => level switch
    {
        LineLevel.Error => _error ??= new SolidColorBrush(Microsoft.UI.Colors.IndianRed),
        LineLevel.Warning => _warning ??= new SolidColorBrush(Microsoft.UI.Colors.Goldenrod),
        LineLevel.Success => _success ??= new SolidColorBrush(Microsoft.UI.Colors.MediumSeaGreen),
        LineLevel.Debug => _debug ??= new SolidColorBrush(Windows.UI.Color.FromArgb(204, 128, 128, 128)),
        LineLevel.Header => _header ??= new SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue),
        _ => (Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorPrimaryBrush"],
    };
}
