using Avalonia.Platform;

namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Сопоставление порядка групп в строке <c>presentation</c> физическим дисплеям: общая сетка координат, сортировка слева направо,
/// затем сверху вниз (ADR 0017 п. 5 — соседство через геометрию, не primary ОС).
/// </summary>
public static class PresentationMonitorTopology
{
    /// <summary>Упорядочить дисплеи для сопоставления с группами <c>(…) (…) …</c> слева направо, сверху вниз.</summary>
    public static IReadOnlyList<Screen> OrderScreensForPresentation(IReadOnlyList<Screen> screens)
    {
        if (screens.Count <= 1)
            return screens;

        var arr = screens.ToArray();
        Array.Sort(arr, static (a, b) =>
        {
            var wa = a.WorkingArea;
            var wb = b.WorkingArea;
            var c = wa.X.CompareTo(wb.X);
            return c != 0 ? c : wa.Y.CompareTo(wb.Y);
        });
        return arr;
    }
}
