#nullable enable
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Applies CIDE column-definition strings (e.g. <c>220,4,*,4,340</c> / <c>0.3*,4,0.4*,4,0.3*</c>)
/// to a WPF <see cref="Grid"/> — replaces Avalonia <c>ColumnDefinitions.Parse</c> binding hacks.
/// </summary>
internal static class WpfMainGridColumns
{
    public static void Apply(Grid grid, string columnDefinitions)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var parts = (columnDefinitions ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            parts = "220,4,*,4,340".Split(',');

        // Keep existing column count if XAML already defined 5; rewrite widths in place.
        while (grid.ColumnDefinitions.Count < parts.Length)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        for (var i = 0; i < parts.Length; i++)
            grid.ColumnDefinitions[i].Width = ParseLength(parts[i]);

        // Extra columns (if any) collapse.
        for (var i = parts.Length; i < grid.ColumnDefinitions.Count; i++)
            grid.ColumnDefinitions[i].Width = new GridLength(0);
    }

    static GridLength ParseLength(string token)
    {
        if (string.Equals(token, "*", StringComparison.Ordinal))
            return new GridLength(1, GridUnitType.Star);

        if (token.EndsWith('*'))
        {
            var num = token[..^1];
            if (string.IsNullOrEmpty(num))
                return new GridLength(1, GridUnitType.Star);
            if (double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out var star))
                return new GridLength(star, GridUnitType.Star);
            return new GridLength(1, GridUnitType.Star);
        }

        if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var px))
            return new GridLength(px, GridUnitType.Pixel);

        return new GridLength(1, GridUnitType.Star);
    }
}
