using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Цвета элемента под курсором (PointerOverElement). Вызывать из UI-потока.
/// Для ide_get_colors_under_cursor — мгновенный фон и текст под мышью.
/// Учитываются все открытые окна IDE (главное, настройки, превью и т.д.).
/// </summary>
public static class UiColorsUnderCursor
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Собрать JSON: тип, имя, background_hex, foreground_hex элемента под курсором.
    /// Перебирает все открытые окна приложения и возвращает элемент из того окна, под курсором которого есть Control.</summary>
    public static string GetJson(TopLevel topLevel)
    {
        var control = FindControlUnderCursor(topLevel);
        if (control is null)
            return JsonSerializer.Serialize(new { hint = "Нет элемента под курсором или не Control." }, Options);

        var name = (control as StyledElement)?.Name ?? "";
        var bgHex = BrushToHex(GetBrush(control, "Background"));
        var fgHex = BrushToHex(GetBrush(control, "Foreground"));
        var (effectiveBg, effectiveFg) = UiControlAppearance.GetEffectiveColors(control);

        var obj = new Dictionary<string, object?>
        {
            ["type"] = control.GetType().Name,
            ["name"] = name,
            ["background"] = bgHex,
            ["foreground"] = fgHex,
            ["effective_background"] = effectiveBg,
            ["effective_foreground"] = effectiveFg
        };
        return JsonSerializer.Serialize(obj, Options);
    }

    /// <summary>Ищет Control под курсором во всех открытых окнах IDE; при недоступности списка окон — только в переданном TopLevel.</summary>
    private static Control? FindControlUnderCursor(TopLevel fallbackTopLevel)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window is not IInputRoot root)
                    continue;
                var over = root.PointerOverElement;
                var control = over as Control ?? FindAncestorControl(over as Visual);
                if (control is not null)
                    return control;
            }
        }

        var overFallback = (fallbackTopLevel as IInputRoot)?.PointerOverElement;
        return overFallback as Control ?? FindAncestorControl(overFallback as Visual);
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
}
