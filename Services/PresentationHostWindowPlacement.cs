using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Services;

/// <summary>
/// Размещение вторичных <c>TopLevel</c> по презентации (ADR 0017): целевой экран по индексу,
/// восстановление из <c>settings.toml</c> с прижатием к рабочим областям. Общий код для PFD/MFD-хостов.
/// </summary>
public static class PresentationHostWindowPlacement
{
    private const double MinRestoreWidth = 320;
    private const double MinRestoreHeight = 240;

    /// <summary>Геометрия из настроек (все четыре поля заданы). Позиция — пиксели; ширина/высота — DIP (как <see cref="Window.Width"/>).</summary>
    public readonly record struct PresentationHostWindowBounds(int PixelX, int PixelY, double Width, double Height);

    /// <summary>Восстановить из <paramref name="saved"/> при пересечении с рабочей областью; иначе — <see cref="PlaceNearMain"/>.</summary>
    /// <param name="presentationScreenIndex">Индекс дисплея в порядке <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>; <c>null</c> — эвристика «не тот же, что главное окно».</param>
    /// <param name="maximizeOnDedicatedScreens">См. <see cref="DisplaySettings.MaximizePresentationHostWindowsOnDedicatedScreens"/>.</param>
    public static void PlaceOrRestore(
        Window mainWindow,
        Window hostWindow,
        PresentationHostWindowBounds? saved,
        int? presentationScreenIndex = null,
        bool maximizeOnDedicatedScreens = true)
    {
        if (saved is { } b && TryClampToWorkingAreas(mainWindow, b, out var pos, out var w, out var h, out var matchedScreen))
        {
            if (presentationScreenIndex is >= 0 and var tIdx)
            {
                var screens = mainWindow.Screens?.All;
                if (screens is { Count: >= 2 })
                {
                    var ordered = PresentationMonitorTopology.OrderScreensForPresentation(screens);
                    if (tIdx < ordered.Count && matchedScreen is not null
                        && !ReferenceEquals(matchedScreen, ordered[tIdx]))
                    {
                        PlaceNearMain(mainWindow, hostWindow, presentationScreenIndex, maximizeOnDedicatedScreens);
                        return;
                    }

                    if (tIdx < ordered.Count && matchedScreen is not null
                        && ReferenceEquals(matchedScreen, ordered[tIdx]))
                    {
                        PresentHostOnDedicatedScreen(hostWindow, matchedScreen, maximizeOnDedicatedScreens);
                        return;
                    }
                }
            }

            hostWindow.Position = pos;
            hostWindow.Width = w;
            hostWindow.Height = h;
            return;
        }

        PlaceNearMain(mainWindow, hostWindow, presentationScreenIndex, maximizeOnDedicatedScreens);
    }

    /// <param name="presentationScreenIndex">См. <see cref="PlaceOrRestore"/>.</param>
    /// <param name="maximizeOnDedicatedScreens">См. <see cref="PlaceOrRestore"/>.</param>
    public static void PlaceNearMain(
        Window mainWindow,
        Window hostWindow,
        int? presentationScreenIndex = null,
        bool maximizeOnDedicatedScreens = true)
    {
        var screens = mainWindow.Screens?.All;
        if (screens is null || screens.Count == 0)
        {
            hostWindow.Position = new PixelPoint(mainWindow.Position.X + 48, mainWindow.Position.Y + 48);
            return;
        }

        if (screens.Count < 2)
        {
            hostWindow.Position = new PixelPoint(mainWindow.Position.X + 48, mainWindow.Position.Y + 48);
            return;
        }

        if (presentationScreenIndex is >= 0 and var targetIdx)
        {
            var ordered = PresentationMonitorTopology.OrderScreensForPresentation(screens);
            if (targetIdx < ordered.Count)
            {
                PresentHostOnDedicatedScreen(hostWindow, ordered[targetIdx], maximizeOnDedicatedScreens);
                return;
            }
        }

        var mainScreen = mainWindow.Screens?.ScreenFromWindow(mainWindow);
        Screen? candidate = null;
        foreach (var sc in screens)
        {
            if (ReferenceEquals(sc, mainScreen))
                continue;
            candidate = sc;
            break;
        }

        if (candidate is null)
            candidate = screens[0];

        PresentHostOnDedicatedScreen(hostWindow, candidate, maximizeOnDedicatedScreens);
    }

    /// <summary>
    /// Один TopLevel на выделенном мониторе: по настройке — <see cref="WindowState.Maximized"/> или заполнение <see cref="Screen.WorkingArea"/> в DIP.
    /// </summary>
    private static void PresentHostOnDedicatedScreen(Window hostWindow, Screen screen, bool maximize)
    {
        if (maximize)
        {
            hostWindow.WindowState = WindowState.Normal;
            hostWindow.Position = screen.WorkingArea.Position;
            hostWindow.WindowState = WindowState.Maximized;
            return;
        }

        ApplyWorkingAreaPixelsToHostWindow(hostWindow, screen, screen.WorkingArea);
    }

    /// <summary>
    /// <see cref="Screen.WorkingArea"/> — в пикселях; <see cref="Window.Width"/>/<see cref="Window.Height"/> — в DIP (Avalonia 11).
    /// </summary>
    private static void ApplyWorkingAreaPixelsToHostWindow(Window hostWindow, Screen screen, PixelRect workingAreaPixels)
    {
        hostWindow.WindowState = WindowState.Normal;
        var s = screen.Scaling;
        if (s <= 0)
            s = 1;
        hostWindow.Position = workingAreaPixels.Position;
        hostWindow.Width = workingAreaPixels.Width / s;
        hostWindow.Height = workingAreaPixels.Height / s;
    }

    private static bool TryClampToWorkingAreas(
        Window mainWindow,
        PresentationHostWindowBounds b,
        out PixelPoint position,
        out double width,
        out double height,
        out Screen? matchedScreen)
    {
        position = default;
        width = 0;
        height = 0;
        matchedScreen = null;

        var screens = mainWindow.Screens?.All;
        if (screens is null || screens.Count == 0)
            return false;

        var wDip = Math.Max(MinRestoreWidth, b.Width);
        var hDip = Math.Max(MinRestoreHeight, b.Height);
        var rect = SavedBoundsToPixelRect(mainWindow.Screens, b, wDip, hDip);

        if (!TryGetScreenWithLargestOverlap(rect, screens, out var best))
            return false;

        matchedScreen = best;
        ClampSavedPixelRectToWorkingArea(rect, best.WorkingArea, best.Scaling, out position, out width, out height);
        return true;
    }

    /// <summary>Сохранённые Width/Height — DIP; перевод в пиксели для пересечения с <see cref="Screen.WorkingArea"/>.</summary>
    private static PixelRect SavedBoundsToPixelRect(Screens? screens, PresentationHostWindowBounds b, double wDip, double hDip)
    {
        var scale = screens?.ScreenFromPoint(new PixelPoint(b.PixelX, b.PixelY))?.Scaling ?? 1.0;
        if (scale <= 0)
            scale = 1.0;
        var rw = (int)Math.Round(wDip * scale);
        var rh = (int)Math.Round(hDip * scale);
        return new PixelRect(b.PixelX, b.PixelY, rw, rh);
    }

    private static bool TryGetScreenWithLargestOverlap(
        PixelRect rect,
        IReadOnlyList<Screen> screens,
        [NotNullWhen(true)] out Screen? best)
    {
        best = null;
        var bestArea = 0L;
        foreach (var sc in screens)
        {
            var inter = rect.Intersect(sc.WorkingArea);
            var area = (long)inter.Width * inter.Height;
            if (area > bestArea)
            {
                bestArea = area;
                best = sc;
            }
        }

        return best is not null && bestArea > 0;
    }

    private static void ClampSavedPixelRectToWorkingArea(
        PixelRect savedRect,
        PixelRect wa,
        double scalingRaw,
        out PixelPoint position,
        out double widthDip,
        out double heightDip)
    {
        var scale = scalingRaw > 0 ? scalingRaw : 1.0;
        var minWPx = (int)Math.Round(MinRestoreWidth * scale);
        var minHPx = (int)Math.Round(MinRestoreHeight * scale);
        var rw = savedRect.Width;
        var rh = savedRect.Height;
        var iw = Math.Min(rw, wa.Width);
        var ih = Math.Min(rh, wa.Height);
        iw = Math.Max(minWPx, iw);
        ih = Math.Max(minHPx, ih);
        if (iw > wa.Width)
            iw = wa.Width;
        if (ih > wa.Height)
            ih = wa.Height;

        var topLeft = ClampTopLeftInsideWorkingArea(savedRect.X, savedRect.Y, iw, ih, wa);
        position = topLeft;
        widthDip = iw / scale;
        heightDip = ih / scale;
    }

    private static PixelPoint ClampTopLeftInsideWorkingArea(int x, int y, int iw, int ih, PixelRect wa)
    {
        if (x < wa.X)
            x = wa.X;
        if (y < wa.Y)
            y = wa.Y;
        if (x + iw > wa.Right)
            x = wa.Right - iw;
        if (y + ih > wa.Bottom)
            y = wa.Bottom - ih;
        if (x < wa.X)
            x = wa.X;
        if (y < wa.Y)
            y = wa.Y;
        return new PixelPoint(x, y);
    }
}
