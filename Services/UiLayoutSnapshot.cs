using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Сбор узла дерева UI для ide_get_ui_layout. Вызывать из UI-потока, передать корень (Window).
/// </summary>
public static class UiLayoutSnapshot
{
    private const int MaxDepth = 14;

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

        return new Dictionary<string, object?>
        {
            ["type"] = typeName,
            ["name"] = name ?? "",
            ["visible"] = visible,
            ["bounds"] = new Dictionary<string, double> { ["x"] = Math.Round(x, 1), ["y"] = Math.Round(y, 1), ["w"] = Math.Round(w, 1), ["h"] = Math.Round(h, 1) },
            ["content"] = content,
            ["children"] = children.Count > 0 ? children : null
        };
    }

    private static string? GetContent(Control? c)
    {
        if (c is null)
            return null;
        if (c is ContentControl cc && cc.Content is string s)
            return Trunc(s);
        if (c is ContentControl cc2 && cc2.Content is not null)
            return Trunc(cc2.Content.ToString() ?? "");
        if (c is TextBlock tb)
            return Trunc(tb.Text ?? "");
        if (c is TextBox tbx)
            return Trunc(tbx.Text ?? "");
        if (c is Button btn)
            return Trunc(btn.Content?.ToString() ?? "");
        if (c is MenuItem mi)
            return Trunc(mi.Header?.ToString() ?? "");
        return null;
    }

    private static string Trunc(string s, int max = 80)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= max ? s : s[..max] + "...";
    }
}
