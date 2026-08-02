#nullable enable
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

/// <summary>Drive: set_control_layout / set_panel_size / request_confirmation.</summary>
internal static partial class GlassSurfaceActions
{
    public static string SetControlLayout(IReadOnlyList<(string Role, Window Window)> windows, string? name, string? layoutJson)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Missing name= for set_control_layout.";
        if (string.IsNullOrWhiteSpace(layoutJson))
            return "Missing layout= JSON.";

        var fe = FindByName(windows, name);
        if (fe is null)
            return $"Control not found: {name}.";

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("margin", out var marginEl))
            {
                var t = ParseThickness(marginEl);
                if (t is { } thickness)
                    fe.Margin = thickness;
            }

            if (root.TryGetProperty("grid_row", out var rowEl) && rowEl.TryGetInt32(out var row))
                Grid.SetRow(fe, row);
            if (root.TryGetProperty("grid_column", out var colEl) && colEl.TryGetInt32(out var col))
                Grid.SetColumn(fe, col);
            if (root.TryGetProperty("grid_row_span", out var rowSpanEl) && rowSpanEl.TryGetInt32(out var rowSpan))
                Grid.SetRowSpan(fe, rowSpan);
            if (root.TryGetProperty("grid_column_span", out var colSpanEl) && colSpanEl.TryGetInt32(out var colSpan))
                Grid.SetColumnSpan(fe, colSpan);

            if (root.TryGetProperty("canvas_left", out var leftEl) && leftEl.TryGetDouble(out var canvasLeft))
                Canvas.SetLeft(fe, canvasLeft);
            if (root.TryGetProperty("canvas_top", out var topEl) && topEl.TryGetDouble(out var canvasTop))
                Canvas.SetTop(fe, canvasTop);

            if (root.TryGetProperty("dock", out var dockEl))
            {
                var dockStr = dockEl.GetString()?.Trim();
                if (Enum.TryParse<Dock>(dockStr, ignoreCase: true, out var dock))
                    DockPanel.SetDock(fe, dock);
            }

            if (root.TryGetProperty("visible", out var visEl)
                && (visEl.ValueKind == JsonValueKind.True || visEl.ValueKind == JsonValueKind.False))
                fe.Visibility = visEl.GetBoolean() ? Visibility.Visible : Visibility.Collapsed;

            return "OK";
        }
        catch (JsonException ex)
        {
            return "Invalid layout JSON: " + ex.Message;
        }
    }

    public static string SetPanelSize(
        IReadOnlyList<(string Role, Window Window)> windows,
        string? panel,
        string? widthRaw,
        string? heightRaw)
    {
        if (string.IsNullOrWhiteSpace(panel))
            return "Missing panel= (pfd_region|mfd_region|intercom).";

        double? width = null, height = null;
        if (!string.IsNullOrWhiteSpace(widthRaw)
            && double.TryParse(widthRaw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var w))
            width = w;
        if (!string.IsNullOrWhiteSpace(heightRaw)
            && double.TryParse(heightRaw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var h))
            height = h;

        var mainGrid = FindByName(windows, "MainGrid") as Grid;
        if (mainGrid is null)
            return "MainGrid not found.";

        switch (panel.Trim().ToLowerInvariant())
        {
            case "pfd_region":
                if (width is not { } w0)
                    return "pfd_region requires width (pixels).";
                if (mainGrid.ColumnDefinitions.Count <= 0)
                    return "Invalid grid.";
                mainGrid.ColumnDefinitions[0].Width = new GridLength(Math.Max(0, w0));
                return "OK";
            case "mfd_region":
                if (width is not { } w4)
                    return "mfd_region requires width (pixels).";
                if (mainGrid.ColumnDefinitions.Count <= 4)
                    return "Invalid grid.";
                mainGrid.ColumnDefinitions[4].Width = new GridLength(Math.Max(0, w4));
                return "OK";
            case "intercom" or "terminal":
                if (height is not { } hi)
                    return "intercom requires height (pixels).";
                var forward = FindByName(windows, "ForwardBody") as Grid;
                if (forward is null || forward.RowDefinitions.Count <= 2)
                    return "ForwardBody rows not found.";
                forward.RowDefinitions[2].Height = new GridLength(Math.Max(80, hi));
                return "OK";
            default:
                return $"Unknown panel: {panel}. Use: pfd_region, mfd_region, intercom.";
        }
    }

    public static string RequestConfirmation(IReadOnlyList<(string Role, Window Window)> windows, string? message)
    {
        Window? owner = null;
        foreach (var (_, win) in windows)
        {
            if (win.IsActive)
            {
                owner = win;
                break;
            }

            owner ??= win;
        }

        var text = string.IsNullOrWhiteSpace(message) ? "Confirm action?" : message.Trim();
        var result = owner is null
            ? MessageBox.Show(text, "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Question)
            : MessageBox.Show(owner, text, "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        return result == MessageBoxResult.OK ? "ok" : "cancel";
    }

    static Thickness? ParseThickness(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Array && el.GetArrayLength() >= 4)
            return new Thickness(el[0].GetDouble(), el[1].GetDouble(), el[2].GetDouble(), el[3].GetDouble());
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
