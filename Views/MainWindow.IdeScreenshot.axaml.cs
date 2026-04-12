using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CascadeIDE.Cockpit.Surface;
using CascadeIDE.Services;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private Task<string> CaptureWindowForMcpCoreAsync(string? workspacePath, string? outputRelativePath, string? scope)
    {
        if (string.Equals(scope?.Trim(), "all", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(CaptureAllIdeWindowsJson(workspacePath, outputRelativePath));

        var (bytes, w, h) = IdeWindowScreenshot.CaptureWindowPng(this);
        string? saved = null;
        if (!string.IsNullOrWhiteSpace(workspacePath) && !string.IsNullOrWhiteSpace(outputRelativePath))
            saved = IdeWindowScreenshot.TrySaveUnderWorkspace(workspacePath, outputRelativePath, bytes);
        var json = IdeWindowScreenshot.BuildCaptureJson(bytes, w, h, saved);
        return Task.FromResult(json);
    }

    private string CaptureAllIdeWindowsJson(string? workspacePath, string? outputRelativePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            var (b, w, h) = IdeWindowScreenshot.CaptureWindowPng(this);
            string? s = null;
            if (!string.IsNullOrWhiteSpace(workspacePath) && !string.IsNullOrWhiteSpace(outputRelativePath))
                s = IdeWindowScreenshot.TrySaveUnderWorkspace(workspacePath, outputRelativePath, b);
            var one = new[]
            {
                new
                {
                    role = "main",
                    window_type = GetType().Name,
                    title = Title ?? "",
                    format = "png_base64",
                    width = w,
                    height = h,
                    data = Convert.ToBase64String(b),
                    saved_path = s
                }
            };
            return JsonSerializer.Serialize(new { format = "png_multi_window", windows = one }, options);
        }

        var list = new List<object>();
        var idx = 0;
        foreach (var w in desktop.Windows)
        {
            if (w is not Window win)
                continue;
            var (bytes, pw, ph) = IdeWindowScreenshot.CaptureWindowPng(win);
            var saved = TrySaveIndexedCapture(workspacePath, outputRelativePath, idx, bytes);
            var role = UiLayoutSnapshot.GetWindowRole(win, this);
            list.Add(new
            {
                role,
                window_type = win.GetType().Name,
                title = win.Title ?? "",
                format = "png_base64",
                width = pw,
                height = ph,
                data = Convert.ToBase64String(bytes),
                saved_path = saved
            });
            idx++;
        }

        return JsonSerializer.Serialize(new { format = "png_multi_window", windows = list }, options);
    }

    private static string? TrySaveIndexedCapture(string? workspacePath, string? outputRelativePath, int index, byte[] pngBytes)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || string.IsNullOrWhiteSpace(outputRelativePath))
            return null;
        var rel = outputRelativePath.Trim();
        if (rel.Contains("{n}", StringComparison.Ordinal))
        {
            rel = rel.Replace("{n}", index.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
            return IdeWindowScreenshot.TrySaveUnderWorkspace(workspacePath, rel, pngBytes);
        }

        var sep = rel.Replace('/', Path.DirectorySeparatorChar);
        var dir = Path.GetDirectoryName(sep);
        var fileName = Path.GetFileName(sep);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(stem))
            stem = "ide-window";
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext))
            ext = ".png";
        var folder = string.IsNullOrEmpty(dir) ? "" : dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var combined = $"{folder}{stem}-{index}{ext}";
        return IdeWindowScreenshot.TrySaveUnderWorkspace(workspacePath, combined, pngBytes);
    }
}
