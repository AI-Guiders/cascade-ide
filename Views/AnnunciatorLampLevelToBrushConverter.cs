using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

/// <summary>Цвет заголовка строки по уровню лампы (таблица / MFD).</summary>
public sealed class AnnunciatorLampLevelToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AnnunciatorLampLevel level)
            return Brushes.Gray;

        return new SolidColorBrush(CockpitPrimitivesPalette.Annunciator.RowAccent(level));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
