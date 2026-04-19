using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
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
        var hostWindow = TopLevel.GetTopLevel(control) as Window;
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
    public static Control? FindControlUnderCursorAnyWindow(Window fallbackMainWindow) =>
        UiPointerClientPosition.TryGetPointerOverControlAnywhere()
        ?? UiPointerClientPosition.TryGetControlUnderPointer(fallbackMainWindow);
}
