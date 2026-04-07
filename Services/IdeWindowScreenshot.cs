using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace CascadeIDE.Services;

/// <summary>
/// Рендер окна в PNG (для MCP <c>capture_window</c>, в т.ч. все окна при <c>scope=all</c>).
/// Вызывать только с UI-потока.
/// </summary>
public static class IdeWindowScreenshot
{
    /// <summary>PNG в памяти и размеры в пикселях.</summary>
    public static (byte[] PngBytes, int WidthPx, int HeightPx) CaptureWindowPng(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var scale = window.RenderScaling;
        var cw = window.ClientSize.Width;
        var ch = window.ClientSize.Height;
        if (cw <= 0 || ch <= 0)
            cw = window.Bounds.Width;
        if (ch <= 0)
            ch = window.Bounds.Height;
        var pw = Math.Max(1, (int)Math.Ceiling(cw * scale));
        var ph = Math.Max(1, (int)Math.Ceiling(ch * scale));
        var pixelSize = new PixelSize(pw, ph);
        using var rtb = new RenderTargetBitmap(pixelSize);
        rtb.Render(window);
        using var ms = new MemoryStream();
        rtb.Save(ms);
        return (ms.ToArray(), pw, ph);
    }

    /// <summary>
    /// JSON для MCP: base64 PNG и опционально сохранение под workspace.
    /// </summary>
    public static string BuildCaptureJson(byte[] pngBytes, int widthPx, int heightPx, string? savedFullPath)
    {
        var b64 = Convert.ToBase64String(pngBytes);
        return JsonSerializer.Serialize(new
        {
            format = "png_base64",
            width = widthPx,
            height = heightPx,
            data = b64,
            saved_path = savedFullPath
        });
    }

    /// <summary>
    /// Сохранить PNG под корнем workspace, если пути заданы и путь остаётся внутри корня.
    /// </summary>
    public static string? TrySaveUnderWorkspace(string workspaceRoot, string relativePath, byte[] pngBytes)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(relativePath))
            return null;
        var root = Path.GetFullPath(workspaceRoot.Trim());
        var combined = Path.GetFullPath(Path.Combine(root, relativePath.Trim().Replace('/', Path.DirectorySeparatorChar)));
        if (!IsStrictSubPath(root, combined))
            return null;
        var dir = Path.GetDirectoryName(combined);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(combined, pngBytes);
        return combined;
    }

    private static bool IsStrictSubPath(string rootDir, string candidateFile)
    {
        rootDir = Path.GetFullPath(rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                  + Path.DirectorySeparatorChar;
        candidateFile = Path.GetFullPath(candidateFile);
        return candidateFile.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase);
    }
}
