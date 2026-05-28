#nullable enable

extern alias svgctrl;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SvgImage = svgctrl::Avalonia.Svg.SvgImage;
using SvgSource = svgctrl::Avalonia.Svg.SvgSource;

namespace CascadeIDE.Views;

/// <summary>Загрузка inline/file изображений для Markdown preview.</summary>
public static class MarkdownPreviewImageFactory
{
    public static Control? TryCreate(string? url, string? alt, string? sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return TryCreateFromDataUri(trimmed, alt);

        var absolute = ResolveFilePath(trimmed, sourceFilePath);
        if (absolute is null || !File.Exists(absolute))
            return BuildPlaceholder(alt, trimmed);

        var ext = Path.GetExtension(absolute);
        if (ext.Equals(".svg", StringComparison.OrdinalIgnoreCase))
            return TryCreateSvgFromFile(absolute, alt);

        try
        {
            using var stream = File.OpenRead(absolute);
            var bitmap = new Bitmap(stream);
            return WrapImage(new Image { Source = bitmap }, alt);
        }
        catch
        {
            return BuildPlaceholder(alt, trimmed);
        }
    }

    private static Control? TryCreateFromDataUri(string dataUri, string? alt)
    {
        var comma = dataUri.IndexOf(',', StringComparison.Ordinal);
        if (comma < 0)
            return BuildPlaceholder(alt, dataUri);

        var meta = dataUri[5..comma];
        var payload = dataUri[(comma + 1)..];
        var isBase64 = meta.Contains(";base64", StringComparison.OrdinalIgnoreCase);
        if (!isBase64)
            return BuildPlaceholder(alt, "non-base64 data URI");

        try
        {
            var bytes = Convert.FromBase64String(payload);
            if (meta.Contains("image/svg+xml", StringComparison.OrdinalIgnoreCase))
                return TryCreateSvgFromBytes(bytes, alt);

            using var stream = new MemoryStream(bytes);
            var bitmap = new Bitmap(stream);
            return WrapImage(new Image { Source = bitmap }, alt);
        }
        catch
        {
            return BuildPlaceholder(alt, dataUri);
        }
    }

    private static Control? TryCreateSvgFromFile(string path, string? alt)
    {
        try
        {
            var source = SvgSource.Load(path, null);
            if (source?.Picture is null)
                return BuildPlaceholder(alt, path);

            return WrapImage(new SvgImage { Source = source }, alt);
        }
        catch
        {
            return BuildPlaceholder(alt, path);
        }
    }

    private static Control? TryCreateSvgFromBytes(byte[] bytes, string? alt)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            var source = SvgSource.Load(stream, null);
            if (source?.Picture is null)
                return BuildPlaceholder(alt, "svg");

            return WrapImage(new SvgImage { Source = source }, alt);
        }
        catch
        {
            return BuildPlaceholder(alt, "svg");
        }
    }

    private static Control WrapImage(object image, string? alt)
    {
        var host = image is Control control
            ? control
            : new ContentControl { Content = image };

        host.MaxWidth = 960;
        host.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;

        if (string.IsNullOrWhiteSpace(alt))
            return host;

        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                host,
                new TextBlock
                {
                    Text = alt,
                    Opacity = 0.7,
                    FontSize = 11,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
    }

    private static Control BuildPlaceholder(string? alt, string detail) =>
        new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(alt)
                ? $"[Image: {detail}]"
                : $"[Image: {alt}] ({detail})",
            Opacity = 0.75,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };

    private static string? ResolveFilePath(string url, string? sourceFilePath)
    {
        if (Path.IsPathRooted(url))
            return url;

        if (string.IsNullOrWhiteSpace(sourceFilePath))
            return null;

        var dir = Path.GetDirectoryName(sourceFilePath);
        if (string.IsNullOrWhiteSpace(dir))
            return null;

        return Path.GetFullPath(Path.Combine(dir, url.Replace('/', Path.DirectorySeparatorChar)));
    }
}
