using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Views;

namespace CascadeIDE.Services;

/// <summary>
/// Сбор узла дерева UI для ide_get_ui_layout. Вызывать из UI-потока, передать корень (Window).
/// </summary>
public static class UiLayoutSnapshot
{
    private const int MaxDepth = 14;
    /// <summary>Лимит текста в <c>content</c> для TextBlock и длинных подписей (паритет с чтением вспомогательных окон).</summary>
    private const int LayoutContentMaxChars = 480;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string BuildJson(Visual root)
    {
        var node = BuildNode(root, root as Visual, 0);
        return JsonSerializer.Serialize(node, Options);
    }

    /// <summary>
    /// Полный снимок layout для MCP: по одному дереву на каждое открытое <see cref="Window"/> (главное, вспомогательное, настройки, превью).
    /// Паритет с человеком/агентом при мультиоконности (ADR 0012, 0017).
    /// </summary>
    public static string BuildJsonAllWindows(Window mainWindow)
    {
        var list = new List<object>();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var w in desktop.Windows)
            {
                if (w is Window win)
                    list.Add(BuildWindowEntry(win, mainWindow));
            }
        }
        else
        {
            list.Add(BuildWindowEntry(mainWindow, mainWindow));
        }

        var payload = new Dictionary<string, object?> { ["windows"] = list };
        return JsonSerializer.Serialize(payload, Options);
    }

    /// <summary>Роль окна для MCP (снимок layout, PNG по всем окнам).</summary>
    public static string GetWindowRole(Window win, Window mainWindow) =>
        ReferenceEquals(win, mainWindow)
            ? "main"
            : win is AuxiliaryWorkspaceWindow
                ? "auxiliary"
                : "other";

    private static Dictionary<string, object?> BuildWindowEntry(Window win, Window mainWindow)
    {
        var role = GetWindowRole(win, mainWindow);

        return new Dictionary<string, object?>
        {
            ["role"] = role,
            ["window_type"] = win.GetType().Name,
            ["title"] = win.Title ?? "",
            ["is_active"] = win.IsActive,
            ["root"] = BuildNode(win, win, 0)
        };
    }

    private static object BuildNode(Visual root, Visual? visual, int depth)
    {
        if (visual is null || depth > MaxDepth)
            return new { type = "?", skip = true };

        var control = visual as Control;
        var name = (visual as StyledElement)?.Name ?? "";
        var visible = control?.IsVisible ?? true;
        var typeName = visual.GetType().Name;

        double x = 0, y = 0, w = visual.Bounds.Width, h = visual.Bounds.Height;
        if (root is Visual rootVisual)
        {
            var topLeft = visual.TranslatePoint(new Point(0, 0), rootVisual);
            if (topLeft is { } p)
            {
                x = p.X;
                y = p.Y;
            }
        }

        var content = GetContent(control);
        var children = new List<object>();
        if (depth < MaxDepth)
        {
            foreach (var child in visual.GetVisualChildren())
                children.Add(BuildNode(root, child, depth + 1));
        }

        var node = new Dictionary<string, object?>
        {
            ["type"] = typeName,
            ["name"] = name ?? "",
            ["visible"] = visible,
            ["bounds"] = new Dictionary<string, double> { ["x"] = Math.Round(x, 1), ["y"] = Math.Round(y, 1), ["w"] = Math.Round(w, 1), ["h"] = Math.Round(h, 1) },
            ["content"] = content,
            ["children"] = children.Count > 0 ? children : null
        };
        // Канонический id из ADR (в т.ч. eicas — канал, не якорь-колонка).
        if (visual is AttentionZoneContainer zc)
            node["attention_zone"] = zc.Zone.ToCanonicalId();
        return node;
    }

    private static string? GetContent(Control? c)
    {
        if (c is null)
            return null;
        if (c is ContentControl cc && cc.Content is string s)
            return Trunc(s, LayoutContentMaxChars);
        if (c is ContentControl cc2 && cc2.Content is not null)
            return Trunc(cc2.Content.ToString() ?? "", LayoutContentMaxChars);
        if (c is TextBlock tb)
            return Trunc(tb.Text ?? "", LayoutContentMaxChars);
        if (c is TextBox tbx)
            return Trunc(tbx.Text ?? "", LayoutContentMaxChars);
        if (c is Button btn)
            return Trunc(btn.Content?.ToString() ?? "", LayoutContentMaxChars);
        if (c is MenuItem mi)
            return Trunc(mi.Header?.ToString() ?? "", LayoutContentMaxChars);
        return null;
    }

    private static string Trunc(string s, int max = 80)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= max ? s : s[..max] + "...";
    }
}
