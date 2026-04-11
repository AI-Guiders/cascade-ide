using System.Globalization;
using Avalonia.Data.Converters;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

/// <summary>Сравнивает <see cref="SecondaryShellPage"/> с именем в <c>ConverterParameter</c> (как у enum).</summary>
public sealed class SecondaryShellPageEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SecondaryShellPage current || parameter is not string s)
            return false;
        return Enum.TryParse<SecondaryShellPage>(s.Trim(), ignoreCase: true, out var p) && current == p;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
