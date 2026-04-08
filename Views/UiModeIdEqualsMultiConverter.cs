using System.Globalization;
using Avalonia.Data.Converters;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>Сравнивает <see cref="MainWindowViewModel.UiMode"/> с id пункта меню (обе стороны через <see cref="MainWindowViewModel.NormalizeUiMode"/>).</summary>
public sealed class UiModeIdEqualsMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return false;
        var current = MainWindowViewModel.NormalizeUiMode(values[0] as string);
        var itemId = MainWindowViewModel.NormalizeUiMode(values[1]?.ToString());
        return string.Equals(current, itemId, StringComparison.OrdinalIgnoreCase);
    }

    public IList<object?> ConvertBack(object? value, IList<Type> targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
