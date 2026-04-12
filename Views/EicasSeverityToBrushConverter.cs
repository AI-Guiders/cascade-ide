using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CascadeIDE.Cockpit.Channels.Eicas;

namespace CascadeIDE.Views;

public sealed class EicasSeverityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is EicasSeverity s
            ? s switch
            {
                EicasSeverity.Warning => new SolidColorBrush(Color.Parse("#C02828")),
                EicasSeverity.Caution => new SolidColorBrush(Color.Parse("#B8860B")),
                _ => new SolidColorBrush(Color.Parse("#1565C0")),
            }
            : Brushes.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
