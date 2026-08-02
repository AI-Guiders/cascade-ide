#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// WPF visual-tree layout for agent_surface/v0 Sense (parity shape with Avalonia UiLayoutSnapshot wire).
/// Toolkit-local adapter — Avalonia type is not SSOT.
/// </summary>
internal static class GlassUiLayoutSnapshot
{
    const int MaxDepth = 14;
    const int LayoutContentMaxChars = 480;

    static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string BuildJsonAllWindows(IReadOnlyList<(string Role, Window Window)> windows)
    {
        var list = new List<object>();
        foreach (var (role, win) in windows)
        {
            if (win is null)
                continue;
            list.Add(new Dictionary<string, object?>
            {
                ["role"] = role,
                ["window_type"] = win.GetType().Name,
                ["title"] = win.Title ?? "",
                ["is_active"] = win.IsActive,
                ["root"] = BuildNode(win, win, 0)
            });
        }

        var payload = new Dictionary<string, object?>
        {
            ["schema"] = "agent_surface/v0",
            ["windows"] = list
        };
        return JsonSerializer.Serialize(payload, Options);
    }

    static object BuildNode(Visual root, DependencyObject? visual, int depth)
    {
        if (visual is null || depth > MaxDepth)
            return new { type = "?", skip = true };

        var fe = visual as FrameworkElement;
        var name = fe?.Name ?? "";
        var visible = fe?.IsVisible ?? true;
        var typeName = visual.GetType().Name;

        double x = 0, y = 0, w = 0, h = 0;
        if (fe is not null)
        {
            w = fe.ActualWidth;
            h = fe.ActualHeight;
            try
            {
                var tl = fe.TransformToAncestor(root).Transform(new Point(0, 0));
                x = tl.X;
                y = tl.Y;
            }
            catch
            {
                /* not yet connected */
            }
        }

        var content = GetContent(fe);
        var children = new List<object>();
        if (depth < MaxDepth)
        {
            var count = VisualTreeHelper.GetChildrenCount(visual);
            for (var i = 0; i < count; i++)
                children.Add(BuildNode(root, VisualTreeHelper.GetChild(visual, i), depth + 1));
        }

        return new Dictionary<string, object?>
        {
            ["type"] = typeName,
            ["name"] = name,
            ["visible"] = visible,
            ["bounds"] = new Dictionary<string, double>
            {
                ["x"] = Math.Round(x, 1),
                ["y"] = Math.Round(y, 1),
                ["w"] = Math.Round(w, 1),
                ["h"] = Math.Round(h, 1)
            },
            ["content"] = content,
            ["children"] = children.Count > 0 ? children : null
        };
    }

    static string? GetContent(FrameworkElement? fe)
    {
        if (fe is null)
            return null;
        return fe switch
        {
            TextBlock tb => Trunc(tb.Text),
            TextBox tbx => Trunc(tbx.Text),
            Button btn => Trunc(btn.Content?.ToString()),
            Label lab => Trunc(lab.Content?.ToString()),
            ContentControl cc when cc.Content is string s => Trunc(s),
            ContentControl cc2 when cc2.Content is not null => Trunc(cc2.Content.ToString()),
            _ => null
        };
    }

    static string? Trunc(string? s, int max = LayoutContentMaxChars)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= max ? s : s[..max] + "...";
    }
}
