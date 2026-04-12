using System.Globalization;
using Avalonia.Data.Converters;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

/// <summary>Краткий значок строки таблицы готовности окружения (галочка / предупреждение / …).</summary>
public sealed class EnvironmentReadinessLevelToGlyphTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EnvironmentReadinessLevel level)
            return "·";

        return level switch
        {
            EnvironmentReadinessLevel.Ok => "✓",
            EnvironmentReadinessLevel.Warning => "⚠",
            EnvironmentReadinessLevel.Info => "ℹ",
            EnvironmentReadinessLevel.Unavailable => "✕",
            _ => "·"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
