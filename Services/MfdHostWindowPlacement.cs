using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Services;

/// <summary>Размещение окна-хоста зоны Mfd (ADR 0017): соседний экран, восстановление из настроек с прижатием к рабочим областям.</summary>
public static class MfdHostWindowPlacement
{
    // Минимум только для restore из settings.toml (защита от битых значений). Первый показ без сохранённой геометрии — PlaceNearMain → вся WorkingArea второго экрана.
    private const double MinRestoreWidth = 320;
    private const double MinRestoreHeight = 240;

    /// <summary>Запись геометрии из <see cref="Models.CascadeIdeSettings"/> (все четыре поля заданы).</summary>
    public readonly record struct MfdHostWindowBounds(int PixelX, int PixelY, double Width, double Height);

    /// <summary>Восстановить из <paramref name="saved"/> при пересечении с рабочей областью; иначе — как <see cref="PlaceNearMain(Window,Window,int?)"/>.</summary>
    /// <param name="mfdPresentationScreenIndex">Индекс дисплея в порядке <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>; <c>null</c> — эвристика «не тот же, что главное окно».</param>
    public static void PlaceOrRestore(
        Window mainWindow,
        Window mfdHostWindow,
        MfdHostWindowBounds? saved,
        int? mfdPresentationScreenIndex = null)
    {
        if (saved is { } b && TryClampToWorkingAreas(mainWindow, b, out var pos, out var w, out var h))
        {
            mfdHostWindow.Position = pos;
            mfdHostWindow.Width = w;
            mfdHostWindow.Height = h;
            return;
        }

        PlaceNearMain(mainWindow, mfdHostWindow, mfdPresentationScreenIndex);
    }

    /// <param name="mfdPresentationScreenIndex">См. <see cref="PlaceOrRestore"/>.</param>
    public static void PlaceNearMain(Window mainWindow, Window mfdHostWindow, int? mfdPresentationScreenIndex = null)
    {
        var screens = mainWindow.Screens?.All;
        if (screens is null || screens.Count == 0)
        {
            mfdHostWindow.Position = new PixelPoint(mainWindow.Position.X + 48, mainWindow.Position.Y + 48);
            return;
        }

        if (screens.Count < 2)
        {
            mfdHostWindow.Position = new PixelPoint(mainWindow.Position.X + 48, mainWindow.Position.Y + 48);
            return;
        }

        if (mfdPresentationScreenIndex is >= 0 and var targetIdx)
        {
            var ordered = PresentationMonitorTopology.OrderScreensForPresentation(screens);
            if (targetIdx < ordered.Count)
            {
                var wa = ordered[targetIdx].WorkingArea;
                mfdHostWindow.Position = wa.Position;
                mfdHostWindow.Width = wa.Width;
                mfdHostWindow.Height = wa.Height;
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

        var waFallback = candidate.WorkingArea;
        mfdHostWindow.Position = waFallback.Position;
        mfdHostWindow.Width = waFallback.Width;
        mfdHostWindow.Height = waFallback.Height;
    }

    private static bool TryClampToWorkingAreas(
        Window mainWindow,
        MfdHostWindowBounds b,
        out PixelPoint position,
        out double width,
        out double height)
    {
        position = default;
        width = 0;
        height = 0;

        var screens = mainWindow.Screens?.All;
        if (screens is null || screens.Count == 0)
            return false;

        var w = Math.Max(MinRestoreWidth, b.Width);
        var h = Math.Max(MinRestoreHeight, b.Height);

        var rw = (int)Math.Round(w);
        var rh = (int)Math.Round(h);
        var rect = new PixelRect(b.PixelX, b.PixelY, rw, rh);

        Screen? best = null;
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

        if (best is null || bestArea <= 0)
            return false;

        var wa = best.WorkingArea;
        var iw = Math.Min((int)Math.Round(w), wa.Width);
        var ih = Math.Min((int)Math.Round(h), wa.Height);
        iw = Math.Max((int)MinRestoreWidth, iw);
        ih = Math.Max((int)MinRestoreHeight, ih);

        var x = rect.X;
        var y = rect.Y;
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

        position = new PixelPoint(x, y);
        width = iw;
        height = ih;
        return true;
    }
}
