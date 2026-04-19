using System.Globalization;
using Avalonia.Data.Converters;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

/// <summary>Краткий значок строки таблицы по уровню лампы (галочка / предупреждение / …).</summary>
public sealed class AnnunciatorLampLevelToGlyphTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AnnunciatorLampLevel level)
            return "·";

        return level switch
        {
            AnnunciatorLampLevel.Ok => "✓",
            AnnunciatorLampLevel.Caution => "⚠",
            AnnunciatorLampLevel.Advisory => "ℹ",
            AnnunciatorLampLevel.Critical => "✕",
            _ => "·"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
