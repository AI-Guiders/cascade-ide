using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace CascadeIDE.Services;

/// <summary>Размещение окна-хоста зоны Mfd на соседнем экране (ADR 0017 v1; без tie-break при нескольких кандидатах).</summary>
public static class MfdHostWindowPlacement
{
    public static void PlaceNearMain(Window mainWindow, Window mfdHostWindow)
    {
        var screens = mainWindow.Screens?.All;
        if (screens is null || screens.Count < 2)
        {
            mfdHostWindow.Position = new PixelPoint(mainWindow.Position.X + 48, mainWindow.Position.Y + 48);
            return;
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

        var wa = candidate.WorkingArea;
        mfdHostWindow.Position = wa.Position;
        mfdHostWindow.Width = wa.Width;
        mfdHostWindow.Height = wa.Height;
    }
}
