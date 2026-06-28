extern alias svgctrl;
using System.Globalization;
using Avalonia.Data.Converters;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using SvgImage = svgctrl::Avalonia.Svg.SvgImage;
using SvgSource = svgctrl::Avalonia.Svg.SvgSource;

namespace CascadeIDE.Views;

/// <summary>Преобразует SolutionItem в иконку для дерева решения (ADR 0167 icon set).</summary>
public sealed class SolutionItemIconConverter : IMultiValueConverter
{
    private const string AvaresBase = "avares://CascadeIDE/Assets/Icons/";
    private static readonly Dictionary<string, SvgSource> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();

    private static SvgSource? LoadSvg(string assetName)
    {
        foreach (var candidate in FallbackAssetNames(assetName))
        {
            var path = AvaresBase + candidate + ".svg";
            lock (CacheLock)
            {
                if (Cache.TryGetValue(candidate, out var cached))
                    return cached;
            }

            var source = SvgSource.Load(path, null);
            if (source?.Picture is null)
                continue;

            lock (CacheLock)
                Cache[candidate] = source;
            return source;
        }

        return null;
    }

    private static IEnumerable<string> FallbackAssetNames(string assetName)
    {
        yield return assetName;
        if (string.Equals(assetName, "file", StringComparison.OrdinalIgnoreCase))
            yield return "cs";
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = values.Count > 0 && values[0] is SolutionItem item
            ? item.IconKey
            : values.FirstOrDefault()?.ToString();
        if (string.IsNullOrEmpty(key))
            key = "file";

        var powerMonochrome = values.Count > 1 && values[1] is UiModeFamily family
            ? family.IsPowerFamily()
            : false;

        var assetName = SolutionExplorerIconKeys.ResolveAssetName(key, powerMonochrome);
        var source = LoadSvg(assetName);
        return source is null ? null : new SvgImage { Source = source };
    }

    public object? ConvertBack(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
