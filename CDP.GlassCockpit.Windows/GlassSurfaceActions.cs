#nullable enable
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// WPF Aim/Drive for agent_surface/v0 (CIDE ide_* parity intent). Toolkit-local — not Avalonia SSOT.
/// </summary>
internal static class GlassSurfaceActions
{
    static readonly Dictionary<Window, (Canvas Layer, Border Overlay)> Overlays = new();
    static DispatcherTimer? _hideTimer;

    public static FrameworkElement? FindByName(IReadOnlyList<(string Role, Window Window)> windows, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var needle = name.Trim();
        foreach (var (_, win) in windows)
        {
            var hit = FindInTree(win, needle);
            if (hit is not null)
                return hit;
        }

        return null;
    }

    static FrameworkElement? FindInTree(DependencyObject root, string name)
    {
        if (root is FrameworkElement fe && string.Equals(fe.Name, name, StringComparison.Ordinal))
            return fe;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var hit = FindInTree(VisualTreeHelper.GetChild(root, i), name);
            if (hit is not null)
                return hit;
        }

        return null;
    }

    public static string Highlight(IReadOnlyList<(string Role, Window Window)> windows, string? name)
    {
        var fe = FindByName(windows, name);
        if (fe is null)
            return string.IsNullOrWhiteSpace(name) ? "Missing name= for highlight." : $"Control not found: {name}.";

        var win = Window.GetWindow(fe);
        if (win is null)
            return "No host window for control.";

        var (layer, overlay) = EnsureOverlay(win);
        Point tl;
        try
        {
            // Overlay is a sibling under Window content — not an ancestor of the control.
            tl = fe.TransformToVisual(layer).Transform(new Point(0, 0));
        }
        catch
        {
            return "Could not get control position.";
        }

        Canvas.SetLeft(overlay, tl.X);
        Canvas.SetTop(overlay, tl.Y);
        overlay.Width = Math.Max(1, fe.ActualWidth);
        overlay.Height = Math.Max(1, fe.ActualHeight);
        overlay.Visibility = Visibility.Visible;

        _hideTimer?.Stop();
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        var captured = overlay;
        _hideTimer.Tick += (_, _) =>
        {
            captured.Visibility = Visibility.Collapsed;
            _hideTimer?.Stop();
            _hideTimer = null;
        };
        _hideTimer.Start();
        return "OK";
    }

    static (Canvas Layer, Border Overlay) EnsureOverlay(Window win)
    {
        if (Overlays.TryGetValue(win, out var existing))
            return existing;

        var layer = new Canvas
        {
            IsHitTestVisible = false,
            ClipToBounds = false
        };
        Panel.SetZIndex(layer, 10_000);
        var overlay = new Border
        {
            Visibility = Visibility.Collapsed,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
            BorderThickness = new Thickness(2),
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xD7, 0x00)),
            CornerRadius = new CornerRadius(2),
            IsHitTestVisible = false
        };
        layer.Children.Add(overlay);

        switch (win.Content)
        {
            case Grid g:
                g.Children.Add(layer);
                break;
            case Panel p:
                p.Children.Add(layer);
                break;
            default:
            {
                var wrap = new Grid();
                var old = win.Content as UIElement;
                win.Content = wrap;
                if (old is not null)
                    wrap.Children.Add(old);
                wrap.Children.Add(layer);
                break;
            }
        }

        Overlays[win] = (layer, overlay);
        win.Closed += (_, _) => Overlays.Remove(win);
        return (layer, overlay);
    }

    public static string Focus(IReadOnlyList<(string Role, Window Window)> windows, string? name)
    {
        var fe = FindByName(windows, name);
        if (fe is null)
            return string.IsNullOrWhiteSpace(name) ? "Missing name= for focus." : $"Control not found: {name}.";
        fe.Focusable = true;
        Keyboard.Focus(fe);
        fe.Focus();
        return "OK";
    }

    public static string Click(IReadOnlyList<(string Role, Window Window)> windows, string? name)
    {
        var fe = FindByName(windows, name);
        if (fe is null)
            return string.IsNullOrWhiteSpace(name) ? "Missing name= for click." : $"Control not found: {name}.";
        if (fe is not Button btn)
            return $"Control is not a Button (type: {fe.GetType().Name}). Only Button click is supported.";

        btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        return "OK";
    }

    public static string SetText(IReadOnlyList<(string Role, Window Window)> windows, string? name, string? text)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Missing name= for set_text.";
        var fe = FindByName(windows, name);
        if (fe is null)
            return $"Control not found: {name}.";
        if (fe is TextBox tb)
        {
            tb.Text = text ?? "";
            return "OK";
        }

        return $"Control does not support text input (not TextBox): {fe.GetType().Name}.";
    }

    public static string SendKeys(IReadOnlyList<(string Role, Window Window)> windows, string? name, string? keysSpec)
    {
        if (string.IsNullOrWhiteSpace(keysSpec))
            return "Missing keys (e.g. Ctrl+Enter).";
        var fe = FindByName(windows, name);
        if (fe is null)
            return string.IsNullOrWhiteSpace(name) ? "Missing name= for send_keys." : $"Control not found: {name}.";
        if (!TryParseKeys(keysSpec.Trim(), out var key, out var mods, out var err))
            return err ?? "Invalid keys.";

        fe.Focusable = true;
        Keyboard.Focus(fe);
        fe.Focus();
        var source = PresentationSource.FromVisual(fe);
        if (source is null)
            return "No PresentationSource for control (window not ready).";
        var target = (Keyboard.FocusedElement as UIElement) ?? fe;
        _ = mods; // chord modifiers reserved; primary key raised on focused element
        var down = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
        {
            RoutedEvent = Keyboard.KeyDownEvent,
            Source = target
        };
        target.RaiseEvent(down);
        var up = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
        {
            RoutedEvent = Keyboard.KeyUpEvent,
            Source = target
        };
        target.RaiseEvent(up);
        return "OK";
    }

    public static string Appearance(IReadOnlyList<(string Role, Window Window)> windows, string? name)
    {
        var fe = FindByName(windows, name);
        if (fe is null)
            return string.IsNullOrWhiteSpace(name) ? "Missing name= for appearance." : $"Control not found: {name}.";

        var payload = new Dictionary<string, object?>
        {
            ["type"] = fe.GetType().Name,
            ["name"] = fe.Name ?? "",
            ["visible"] = fe.IsVisible,
            ["width"] = fe.ActualWidth,
            ["height"] = fe.ActualHeight,
            ["content"] = fe switch
            {
                TextBlock tb => tb.Text,
                TextBox tbx => tbx.Text,
                Button b => b.Content?.ToString(),
                _ => null
            },
            ["background"] = BrushToString(fe is Control c ? c.Background : null),
            ["foreground"] = BrushToString(fe is Control c2 ? c2.Foreground : null)
        };
        return JsonSerializer.Serialize(payload);
    }

    public static string ColorsUnderCursor(IReadOnlyList<(string Role, Window Window)> windows)
    {
        // WPF: element under mouse across surface windows.
        foreach (var (_, win) in windows)
        {
            if (!win.IsVisible)
                continue;
            try
            {
                var pos = Mouse.GetPosition(win);
                var hit = win.InputHitTest(pos) as DependencyObject;
                while (hit is not null && hit is not FrameworkElement)
                    hit = VisualTreeHelper.GetParent(hit);
                if (hit is FrameworkElement fe)
                {
                    var payload = new Dictionary<string, object?>
                    {
                        ["type"] = fe.GetType().Name,
                        ["name"] = fe.Name ?? "",
                        ["background"] = BrushToString(fe is Control c ? c.Background : null),
                        ["foreground"] = BrushToString(fe is Control c2 ? c2.Foreground : null),
                        ["window"] = win.Title
                    };
                    return JsonSerializer.Serialize(payload);
                }
            }
            catch
            {
                /* next window */
            }
        }

        return JsonSerializer.Serialize(new { error = "No control under cursor." });
    }

    static string? BrushToString(Brush? brush) =>
        brush is SolidColorBrush sc
            ? $"#{sc.Color.A:X2}{sc.Color.R:X2}{sc.Color.G:X2}{sc.Color.B:X2}"
            : brush?.ToString();

    static bool TryParseKeys(string spec, out Key key, out ModifierKeys mods, out string? error)
    {
        mods = ModifierKeys.None;
        key = Key.None;
        error = null;
        var parts = spec.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            error = "Empty key spec.";
            return false;
        }

        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl" or "control":
                    mods |= ModifierKeys.Control;
                    break;
                case "alt":
                    mods |= ModifierKeys.Alt;
                    break;
                case "shift":
                    mods |= ModifierKeys.Shift;
                    break;
                case "win" or "windows" or "meta":
                    mods |= ModifierKeys.Windows;
                    break;
                default:
                    error = $"Unknown modifier: {parts[i]}";
                    return false;
            }
        }

        var keyName = parts[^1];
        if (keyName.Length == 1 && char.IsLetterOrDigit(keyName[0]))
            keyName = keyName.ToUpperInvariant();
        if (!Enum.TryParse(keyName, ignoreCase: true, out key) || key == Key.None)
        {
            error = $"Unknown key: {parts[^1]}";
            return false;
        }

        return true;
    }
}
