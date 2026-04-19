using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Cockpit.PrimitivesKit;

namespace CascadeIDE.Views;

public sealed class EicasSeverityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is EicasSeverity s
            ? new SolidColorBrush(CockpitPrimitivesPalette.Eicas.Foreground(s))
            : Brushes.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
