using System.Globalization;
using Avalonia.Data.Converters;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Views;

/// <summary>Привязка <see cref="UiModeFamily"/> к bool: равенство переданному имени enum (ConverterParameter).</summary>
public sealed class UiModeFamilyEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not UiModeFamily family)
            return false;
        var s = parameter?.ToString();
        if (string.IsNullOrEmpty(s))
            return false;
        return Enum.TryParse<UiModeFamily>(s, ignoreCase: true, out var expected) && family == expected;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True, если <see cref="UiModeFamily"/> **не** равен параметру (для <c>!IsPowerMode</c> и т.п.).</summary>
public sealed class UiModeFamilyNotEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not UiModeFamily family)
            return false;
        var s = parameter?.ToString();
        if (string.IsNullOrEmpty(s))
            return false;
        return Enum.TryParse<UiModeFamily>(s, ignoreCase: true, out var expected) && family != expected;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
