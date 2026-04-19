extern alias svgctrl;
using System.Globalization;
using Avalonia.Data.Converters;
using CascadeIDE.Models;
using SvgImage = svgctrl::Avalonia.Svg.SvgImage;
using SvgSource = svgctrl::Avalonia.Svg.SvgSource;

namespace CascadeIDE.Views;

/// <summary>Преобразует SolutionItem в иконку для дерева решения. Загружает SVG из Assets/Icons: solution, project, folder, file, а также по расширению (cs, ts, json, md, ...). Иконки по расширениям можно взять из пакета file-icon-vectors (см. docs/ASSETS-ICONS.md).</summary>
public sealed class SolutionItemIconConverter : IValueConverter
{
    private const string AvaresBase = "avares://CascadeIDE/Assets/Icons/";
    private static readonly string[] NodeKeys = ["solution", "project", "folder", "file"];
    private static readonly Dictionary<string, SvgSource> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();

    private static SvgSource? LoadSvg(string iconKey)
    {
        // file_cs -> cs.svg, file_json -> json.svg; иначе solution/project/folder/file
        var assetName = iconKey.StartsWith("file_", StringComparison.OrdinalIgnoreCase)
            ? iconKey[5..]
            : (NodeKeys.Contains(iconKey, StringComparer.OrdinalIgnoreCase) ? iconKey : "file");
        var path = AvaresBase + assetName + ".svg";
        SvgSource? source;
        lock (CacheLock)
        {
            if (Cache.TryGetValue(assetName, out var cached))
                return cached;
            source = SvgSource.Load(path, null);
            if (source?.Picture is not null)
                Cache[assetName] = source;
        }
        if (source is not null)
            return source;
        if (assetName != "file")
            return LoadSvg("file");
        return null;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is SolutionItem item ? item.IconKey : value?.ToString();
        if (string.IsNullOrEmpty(key))
            key = "file";
        var source = LoadSvg(key);
        if (source is null)
            return null;
        return new SvgImage { Source = source };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
