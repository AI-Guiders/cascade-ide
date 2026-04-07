using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Подсветка контрола рамкой для <c>ide_highlight_control</c> в том <see cref="Window"/>, где лежит контрол
/// (главное, вспомогательное и т.д.). Оверлей — <c>AgentHighlightLayer</c> / <c>AgentHighlightOverlay</c> в разметке окна.
/// </summary>
public static class UiAgentHighlight
{
    private static Border? _lastOverlay;
    private static IDisposable? _hideTimer;

    public static string ShowForControl(Control control)
    {
        var hostWindow = control.GetVisualRoot() as Window;
        if (hostWindow is null)
            return "No host window for control.";

        var canvas = hostWindow.FindControl<Canvas>("AgentHighlightLayer");
        var overlay = hostWindow.FindControl<Border>("AgentHighlightOverlay");
        if (canvas is null || overlay is null)
            return "Highlight overlay not found (AgentHighlightLayer / AgentHighlightOverlay).";

        if (_lastOverlay is not null && !ReferenceEquals(_lastOverlay, overlay))
            _lastOverlay.IsVisible = false;

        var pt = control.TranslatePoint(new Point(0, 0), canvas);
        if (pt is null)
            return "Could not get control position.";

        var w = control.Bounds.Width;
        var h = control.Bounds.Height;

        Canvas.SetLeft(overlay, pt.Value.X);
        Canvas.SetTop(overlay, pt.Value.Y);
        overlay.Width = w;
        overlay.Height = h;
        overlay.IsVisible = true;
        _lastOverlay = overlay;

        _hideTimer?.Dispose();
        _hideTimer = DispatcherTimer.RunOnce(() =>
        {
            overlay.IsVisible = false;
            _hideTimer = null;
        }, TimeSpan.FromSeconds(3));

        return "OK";
    }

    /// <summary>Контрол под курсором в любом окне приложения (паритет с <see cref="UiColorsUnderCursor"/>).</summary>
    public static Control? FindControlUnderCursorAnyWindow(Window fallbackMainWindow)
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

        var overFallback = (fallbackMainWindow as IInputRoot)?.PointerOverElement;
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
}
