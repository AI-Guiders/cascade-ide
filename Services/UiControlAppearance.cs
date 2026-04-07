using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Снимок эффективного вида любого контрола: тип, имя, границы, видимость, содержимое (текст),
/// фон, цвет текста, шрифт. Для ide_get_control_appearance — по курсору или по имени.
/// Вызывать из UI-потока.
/// </summary>
public static class UiControlAppearance
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <param name="topLevel">Окно (TopLevel).</param>
    /// <param name="controlName">Если задано — ищем контрол по имени в дереве; иначе — элемент под курсором.</param>
    public static string GetJson(TopLevel topLevel, string? controlName)
    {
        Control? control;
        if (!string.IsNullOrWhiteSpace(controlName))
        {
            control = topLevel is Window mw
                ? FindControlByNameAcrossAllWindows(mw, controlName)
                : FindControlByName(topLevel, controlName.Trim());
            if (control is null)
                return JsonSerializer.Serialize(new { error = "Контрол с именем не найден.", name = controlName }, Options);
        }
        else
        {
            var over = (topLevel as IInputRoot)?.PointerOverElement;
            control = over as Control ?? FindAncestorControl(over as Visual);
            if (control is null)
                return JsonSerializer.Serialize(new { hint = "Нет контрола под курсором. Укажите name из ide_get_ui_layout." }, Options);
        }

        var root = (control.GetVisualRoot() ?? topLevel) as Visual;
        var snapshot = BuildSnapshot(control, root);
        return JsonSerializer.Serialize(snapshot, Options);
    }

    /// <summary>Найти первый контрол в дереве с заданным именем (для применения layout и т.д.).</summary>
    public static Control? FindControlByName(Visual root, string name)
    {
        if (root is StyledElement se && string.Equals(se.Name, name, StringComparison.Ordinal) && root is Control c)
            return c;
        foreach (var child in root.GetVisualChildren())
        {
            var found = FindControlByName(child, name);
            if (found is not null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Поиск по имени во всех окнах процесса: сначала в <paramref name="mainWindow"/>, затем в остальных (вспомогательные окна, настройки, превью).
    /// Одинаковые имена в разных окнах: побеждает вхождение в главном окне.
    /// </summary>
    public static Control? FindControlByNameAcrossAllWindows(Window mainWindow, string name)
    {
        var trimmed = name.Trim();
        var c = FindControlByName(mainWindow, trimmed);
        if (c is not null)
            return c;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var w in desktop.Windows)
            {
                if (w is not Window win || ReferenceEquals(win, mainWindow))
                    continue;
                c = FindControlByName(win, trimmed);
                if (c is not null)
                    return c;
            }
        }

        return null;
    }

    private static Control? FindAncestorControl(Visual? visual)
    {
        for (var v = visual?.GetVisualParent(); v is not null; v = v.GetVisualParent())
        {
            if (v is Control c)
                return c;
        }
        return null;
    }

    private static Dictionary<string, object?> BuildSnapshot(Control control, Visual? root)
    {
        var name = (control as StyledElement)?.Name ?? "";
        double x = 0, y = 0, w = control.Bounds.Width, h = control.Bounds.Height;
        if (root is not null)
        {
            var topLeft = control.TranslatePoint(new Point(0, 0), root);
            if (topLeft is { } p)
            {
                x = p.X;
                y = p.Y;
            }
        }

        var content = GetContent(control);
        var background = BrushToHex(GetBrush(control, "Background"));
        var foreground = BrushToHex(GetBrush(control, "Foreground"));
        var (effectiveBackground, effectiveForeground) = GetEffectiveColors(control);
        var borderBrush = BrushToHex(GetBrush(control, "BorderBrush"));
        var fontFamily = GetString(control, "FontFamily");
        var fontSize = GetDouble(control, "FontSize");
        var borderThickness = GetThickness(control, "BorderThickness");
        var contentTruncated = TryDetectContentTruncated(control, content, w, fontSize, fontFamily);

        var dict = new Dictionary<string, object?>
        {
            ["type"] = control.GetType().Name,
            ["name"] = name,
            ["visible"] = control.IsVisible,
            ["bounds"] = new Dictionary<string, double> { ["x"] = Math.Round(x, 1), ["y"] = Math.Round(y, 1), ["w"] = Math.Round(w, 1), ["h"] = Math.Round(h, 1) },
            ["content"] = content,
            ["background"] = background,
            ["foreground"] = foreground,
            ["effective_background"] = effectiveBackground,
            ["effective_foreground"] = effectiveForeground,
            ["border_brush"] = borderBrush,
            ["border_thickness"] = borderThickness,
            ["font_family"] = fontFamily,
            ["font_size"] = fontSize
        };
        if (contentTruncated.HasValue)
            dict["content_truncated"] = contentTruncated.Value;
        dict["background_brush"] = UiThemeSnapshot.FormatBrushForJson(GetBrush(control, "Background"));
        dict["border_brush_display"] = UiThemeSnapshot.FormatBrushForJson(GetBrush(control, "BorderBrush"));
        if (control is Border br)
        {
            var cr = br.CornerRadius;
            dict["corner_radius"] = new Dictionary<string, double>
            {
                ["top_left"] = cr.TopLeft,
                ["top_right"] = cr.TopRight,
                ["bottom_right"] = cr.BottomRight,
                ["bottom_left"] = cr.BottomLeft
            };
            dict["box_shadow"] = br.BoxShadow.ToString();
        }

        return dict;
    }

    /// <summary>Снимок контрола по имени в дереве окна (те же поля, что у <c>ide_get_control_appearance</c>).</summary>
    public static Dictionary<string, object?>? TryBuildNamedRegionSnapshot(TopLevel topLevel, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var c = FindControlByName(topLevel, name.Trim());
        if (c is null)
            return null;
        return BuildSnapshot(c, topLevel as Visual);
    }

    /// <summary>Реальное состояние обрезки: для TextBlock — из контрола (TextLayout.TextLines.HasOverflowed); иначе — оценка по измерению.</summary>
    private static bool? TryDetectContentTruncated(Control control, string? content, double boundsWidth, double? fontSize, string? fontFamily)
    {
        if (string.IsNullOrEmpty(content) && control is not TextBlock)
            return null;

        if (control is TextBlock tb && tb.TextLayout is { } layout)
        {
            foreach (var line in layout.TextLines)
            {
                if (line.HasOverflowed)
                    return true;
            }
            return false;
        }

        if (string.IsNullOrEmpty(content) || boundsWidth <= 0)
            return null;
        var size = fontSize ?? 12;
        var family = ParseFontFamilyName(fontFamily) ?? "Inter";
        const double reservedForChrome = 48;
        var availableWidth = Math.Max(0, boundsWidth - reservedForChrome);
        if (availableWidth <= 0)
            return null;
        try
        {
            var typeface = new Typeface(family);
            using var fallbackLayout = new TextLayout(content!, typeface, null, size, null, TextAlignment.Left, TextWrapping.NoWrap, null, null, FlowDirection.LeftToRight, double.PositiveInfinity, double.PositiveInfinity);
            var textWidth = fallbackLayout.Width;
            return textWidth > availableWidth;
        }
        catch
        {
            return null;
        }
    }

    private static string? ParseFontFamilyName(string? composite)
    {
        if (string.IsNullOrWhiteSpace(composite))
            return null;
        if (composite.Contains("Inter", StringComparison.OrdinalIgnoreCase))
            return "Inter";
        if (composite.Contains("Consolas", StringComparison.OrdinalIgnoreCase))
            return "Consolas";
        var hash = composite.IndexOf('#');
        if (hash >= 0)
        {
            var part = composite[(hash + 1)..].Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(part))
                return part;
        }
        return composite.Split(',')[0].Trim();
    }

    /// <summary>Эффективные цвета с учётом поддерева и предков (шаблон, выделение): то, что реально рисуется. Для ide_get_colors_under_cursor и снимка контрола.</summary>
    public static (string? background, string? foreground) GetEffectiveColors(Control control, int maxDepth = 20, int maxAncestors = 15)
    {
        string? bg = null;
        string? fg = null;
        WalkForBrushes(control, 0, ref bg, ref fg, maxDepth);
        if (bg is null || fg is null)
            WalkAncestorsForBrushes(control.GetVisualParent(), 0, ref bg, ref fg, maxAncestors);
        return (bg, fg);
    }

    private static void WalkAncestorsForBrushes(Visual? visual, int depth, ref string? foundBg, ref string? foundFg, int maxDepth)
    {
        if (visual is null || depth > maxDepth)
            return;
        if (visual is Control c)
        {
            if (foundBg is null)
            {
                var brush = GetBrush(c, "Background");
                if (brush is SolidColorBrush)
                    foundBg = BrushToHex(brush);
            }
            if (foundFg is null)
            {
                var brush = GetBrush(c, "Foreground");
                if (brush is SolidColorBrush)
                    foundFg = BrushToHex(brush);
            }
        }
        WalkAncestorsForBrushes(visual.GetVisualParent(), depth + 1, ref foundBg, ref foundFg, maxDepth);
    }

    private static void WalkForBrushes(Visual? visual, int depth, ref string? foundBg, ref string? foundFg, int maxDepth)
    {
        if (visual is null || depth > maxDepth)
            return;
        if (visual is Control c)
        {
            if (foundBg is null)
            {
                var brush = GetBrush(c, "Background");
                if (brush is SolidColorBrush)
                    foundBg = BrushToHex(brush);
            }
            if (foundFg is null)
            {
                var brush = GetBrush(c, "Foreground");
                if (brush is SolidColorBrush)
                    foundFg = BrushToHex(brush);
            }
            if (foundBg is not null && foundFg is not null)
                return;
        }
        foreach (var child in visual.GetVisualChildren())
        {
            WalkForBrushes(child, depth + 1, ref foundBg, ref foundFg, maxDepth);
            if (foundBg is not null && foundFg is not null)
                return;
        }
    }

    private static string? GetContent(Control? c)
    {
        if (c is null) return null;
        const int maxShort = 300;
        const int maxTextBox = 12000;

        if (c is TextBox tbx)
        {
            var text = tbx.Text ?? "";
            return text.Length <= maxTextBox ? text : text[..maxTextBox] + "\n... (обрезано)";
        }
        if (c is TextBlock tb)
            return Trunc(tb.Text ?? "", maxShort);
        if (c is ContentControl cc && cc.Content is string contentStr)
            return Trunc(contentStr, maxShort);
        if (c is ContentControl cc2 && cc2.Content is not null)
            return Trunc(cc2.Content.ToString() ?? "", maxShort);
        if (c is Button btn)
            return Trunc(btn.Content?.ToString() ?? "", maxShort);
        if (c is MenuItem mi)
            return Trunc(mi.Header?.ToString() ?? "", maxShort);
        return null;
    }

    private static string Trunc(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= max ? s : s[..max] + "...";
    }

    private static IBrush? GetBrush(Control control, string propertyName)
    {
        var prop = control.GetType().GetProperty(propertyName);
        return prop?.GetValue(control) as IBrush;
    }

    private static string? BrushToHex(IBrush? brush)
    {
        if (brush is not SolidColorBrush scb)
            return null;
        var c = scb.Color;
        return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    private static string? GetString(Control control, string propertyName)
    {
        var prop = control.GetType().GetProperty(propertyName);
        var v = prop?.GetValue(control);
        return v?.ToString();
    }

    private static double? GetDouble(Control control, string propertyName)
    {
        var prop = control.GetType().GetProperty(propertyName);
        if (prop?.GetValue(control) is not IConvertible val)
            return null;
        try { return val.ToDouble(null); }
        catch { return null; }
    }

    private static object? GetThickness(Control control, string propertyName)
    {
        var prop = control.GetType().GetProperty(propertyName);
        var v = prop?.GetValue(control);
        if (v is null) return null;
        if (v is Thickness t)
            return new { left = t.Left, top = t.Top, right = t.Right, bottom = t.Bottom };
        return v.ToString();
    }
}
