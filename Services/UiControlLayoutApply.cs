using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Применить к контролу параметры layout на лету (margin, grid row/column, canvas left/top, dock).
/// Вызывать из UI-потока. Для ide_set_control_layout.
/// </summary>
public static class UiControlLayoutApply
{
    /// <summary>Применить layout из JSON. Возвращает "OK" или сообщение об ошибке.</summary>
    /// <param name="root">Корень дерева (обычно окно).</param>
    /// <param name="controlName">Имя контрола (из ide_get_ui_layout).</param>
    /// <param name="layoutJson">JSON: margin, grid_row, grid_column, grid_row_span, grid_column_span, canvas_left, canvas_top, dock, visible (true/false — скрыть/показать).</param>
    public static string Apply(Visual root, string controlName, string layoutJson)
    {
        if (string.IsNullOrWhiteSpace(controlName))
            return "Missing control name.";

        var control = UiControlAppearance.FindControlByName(root, controlName.Trim());
        if (control is null)
            return $"Control not found: {controlName}.";

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            var rootEl = doc.RootElement;

            if (rootEl.TryGetProperty("margin", out var marginEl))
            {
                var t = ParseThickness(marginEl);
                if (t is { } thickness)
                    control.Margin = thickness;
            }

            if (rootEl.TryGetProperty("grid_row", out var rowEl) && rowEl.TryGetInt32(out var row))
                Grid.SetRow(control, row);
            if (rootEl.TryGetProperty("grid_column", out var colEl) && colEl.TryGetInt32(out var col))
                Grid.SetColumn(control, col);
            if (rootEl.TryGetProperty("grid_row_span", out var rowSpanEl) && rowSpanEl.TryGetInt32(out var rowSpan))
                Grid.SetRowSpan(control, rowSpan);
            if (rootEl.TryGetProperty("grid_column_span", out var colSpanEl) && colSpanEl.TryGetInt32(out var colSpan))
                Grid.SetColumnSpan(control, colSpan);

            if (rootEl.TryGetProperty("canvas_left", out var leftEl) && leftEl.TryGetDouble(out var canvasLeft))
                Canvas.SetLeft(control, canvasLeft);
            if (rootEl.TryGetProperty("canvas_top", out var topEl) && topEl.TryGetDouble(out var canvasTop))
                Canvas.SetTop(control, canvasTop);

            if (rootEl.TryGetProperty("dock", out var dockEl))
            {
                var dockStr = dockEl.GetString()?.Trim();
                if (Enum.TryParse<Dock>(dockStr, ignoreCase: true, out var dock))
                    DockPanel.SetDock(control, dock);
            }

            if (rootEl.TryGetProperty("visible", out var visEl) && (visEl.ValueKind == JsonValueKind.True || visEl.ValueKind == JsonValueKind.False))
                control.IsVisible = visEl.GetBoolean();

            return "OK";
        }
        catch (JsonException ex)
        {
            return "Invalid layout JSON: " + ex.Message;
        }
    }

    private static Thickness? ParseThickness(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Array && el.GetArrayLength() >= 4)
        {
            var a = el;
            return new Thickness(a[0].GetDouble(), a[1].GetDouble(), a[2].GetDouble(), a[3].GetDouble());
        }
        if (el.ValueKind == JsonValueKind.Object)
        {
            var left = el.TryGetProperty("left", out var l) ? l.GetDouble() : 0;
            var top = el.TryGetProperty("top", out var t) ? t.GetDouble() : 0;
            var right = el.TryGetProperty("right", out var r) ? r.GetDouble() : 0;
            var bottom = el.TryGetProperty("bottom", out var b) ? b.GetDouble() : 0;
            return new Thickness(left, top, right, bottom);
        }
        return null;
    }
}
