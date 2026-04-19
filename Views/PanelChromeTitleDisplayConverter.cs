using System.Globalization;
using Avalonia.Data.Converters;

namespace CascadeIDE.Views;

/// <summary>Строка заголовка панели с учётом <see cref="PanelChromeHeader.UppercaseTitle"/> (для живой смены языка через привязку к <c>Title</c>).</summary>
public sealed class PanelChromeTitleDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return "";
        var title = values[0] as string ?? "";
        var upper = values[1] is bool b && b;
        return upper ? title.ToUpperInvariant() : title;
    }

    public IList<object?> ConvertBack(object? value, IList<Type> targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
