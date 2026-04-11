using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

/// <summary>Цвет заголовка строки по уровню готовности (MFD «Окружение»).</summary>
public sealed class EnvironmentReadinessLevelToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EnvironmentReadinessLevel level)
            return Brushes.Gray;

        return level switch
        {
            EnvironmentReadinessLevel.Ok => new SolidColorBrush(Color.Parse("#2D8A5A")),
            EnvironmentReadinessLevel.Warning => new SolidColorBrush(Color.Parse("#C9A227")),
            EnvironmentReadinessLevel.Info => new SolidColorBrush(Color.Parse("#6B9BD2")),
            EnvironmentReadinessLevel.Unavailable => new SolidColorBrush(Color.Parse("#CC4444")),
            _ => Brushes.Gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
