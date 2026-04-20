using System.Globalization;
using Avalonia.Data.Converters;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

/// <summary>
/// True, если строка — заголовок секции (Dev Tools / переменные окружения).
/// Параметр <c>Invert</c> инвертирует результат (для скрытия обычной карточки на секции).
/// </summary>
public sealed class EnvironmentReadinessIsSectionRowConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isSection = value is string s
            && (s == EnvironmentReadinessCellIds.DevToolsSection || s == EnvironmentReadinessCellIds.EnvSection);
        if (string.Equals(parameter as string, "Invert", StringComparison.Ordinal))
            isSection = !isSection;
        return isSection;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True для непустой строки (подпись детали у секции).</summary>
public sealed class StringNotEmptyToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && s.Length > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
