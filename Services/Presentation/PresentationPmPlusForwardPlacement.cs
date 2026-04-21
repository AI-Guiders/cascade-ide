using Avalonia.Controls;
using Avalonia.Platform;

namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Плейсмент <c>(xP+yM)(F)</c> / <c>(F)(xP+yM)</c> на физических дисплеях при ≥2 мониторах (ADR 0017).
/// Лобовое (<c>F</c>) — на <see cref="Screens.Primary"/>; сплит <c>P+M</c> — по соседству с primary:
/// если в строке сначала <c>P+M</c>, то на экран слева от primary; если сначала <c>F</c> — на экран справа.
/// Так строковый порядок остаётся симметричным, а при трёх мониторах не происходит наложения на один дисплей
/// из-за того, что главное окно по умолчанию открывается на primary, а не на «первом» в LTR.
/// </summary>
public static class PresentationPmPlusForwardPlacement
{
    /// <summary>
    /// Индексы в массиве <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/> для главного (F) и окна P+M.
    /// </summary>
    public static bool TryGetOrderedIndices(
        Screens screens,
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> parseScreens,
        out int forwardOrderedIndex,
        out int pmOrderedIndex)
    {
        forwardOrderedIndex = 0;
        pmOrderedIndex = 0;

        if (!PresentationLayoutAnalyzer.IsPmPlusForwardTwoScreenPreset(parseScreens))
            return false;

        if (screens.All.Count < 2)
            return false;

        if (!PresentationLayoutAnalyzer.TryGetMainWindowPresentationScreenIndex(parseScreens, out var forwardGroupIdx))
            return false;

        var ordered = PresentationMonitorTopology.OrderScreensForPresentation(screens.All);
        var pIdx = FindPrimaryOrderedIndex(ordered, screens.Primary);

        // forwardGroupIdx == 0 → в строке сначала F, затем P+M.
        var pmGroupBeforeForward = forwardGroupIdx == 1;

        ComputeForwardAndPmOrderedIndices(ordered.Count, pIdx, pmGroupBeforeForward, out forwardOrderedIndex, out pmOrderedIndex);
        return true;
    }

    /// <summary>Чистая геометрия: primary по центру ряда, P+M слева или справа от него.</summary>
    internal static void ComputeForwardAndPmOrderedIndices(
        int orderedScreenCount,
        int primaryOrderedIndex,
        bool pmGroupBeforeForwardInPresentation,
        out int forwardOrderedIndex,
        out int pmOrderedIndex)
    {
        forwardOrderedIndex = primaryOrderedIndex;
        var pIdx = primaryOrderedIndex;

        if (pmGroupBeforeForwardInPresentation)
        {
            // (P+M)(F): P+M слева от лобового (primary), если есть.
            pmOrderedIndex = pIdx > 0
                ? pIdx - 1
                : (orderedScreenCount > pIdx + 1 ? pIdx + 1 : pIdx);
        }
        else
        {
            // (F)(P+M): P+M справа от primary, если есть.
            pmOrderedIndex = pIdx < orderedScreenCount - 1
                ? pIdx + 1
                : (pIdx > 0 ? pIdx - 1 : pIdx);
        }

        if (pmOrderedIndex == forwardOrderedIndex && orderedScreenCount >= 2)
            pmOrderedIndex = forwardOrderedIndex == 0 ? 1 : forwardOrderedIndex - 1;
    }

    private static int FindPrimaryOrderedIndex(IReadOnlyList<Screen> ordered, Screen? primary)
    {
        if (primary is null)
            return 0;

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ReferenceEquals(ordered[i], primary))
                return i;
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].WorkingArea == primary.WorkingArea)
                return i;
        }

        return 0;
    }
}
