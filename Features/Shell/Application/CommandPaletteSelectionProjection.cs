using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>Выбор строки в палитре команд без привязки к <c>ObservableCollection</c>.</summary>
[PresentationProjection]
public static class CommandPaletteSelectionProjection
{
    /// <summary>После полного пересчёта списка: первая строка или «нет выбора».</summary>
    public static int InitialSelectedIndex(int entryCount) => entryCount > 0 ? 0 : -1;

    /// <summary>Если индекс ушёл за хвост после добавления/удаления строк — зажать на последнюю валидную.</summary>
    public static int ClampUpperOrKeep(int selectedIndex, int entryCount)
    {
        if (entryCount <= 0)
            return -1;
        return selectedIndex >= entryCount ? Math.Max(0, entryCount - 1) : selectedIndex;
    }

    public static bool TryMoveCircular(int currentIndex, int delta, int entryCount, out int nextIndex)
    {
        nextIndex = currentIndex;
        if (entryCount <= 0)
            return false;
        var next = currentIndex + delta;
        if (next < 0)
            next = entryCount - 1;
        else if (next >= entryCount)
            next = 0;
        nextIndex = next;
        return true;
    }

    /// <summary>PgUp/PgDn: шаг по <paramref name="pageStep"/> с зажатием в диапазоне.</summary>
    public static bool TryPageMove(int currentIndex, int directionSign, int pageStep, int entryCount, out int nextIndex)
    {
        nextIndex = currentIndex;
        if (entryCount <= 0)
            return false;
        var step = pageStep * Math.Sign(directionSign);
        if (step == 0)
            return false;
        nextIndex = Math.Clamp(currentIndex + step, 0, entryCount - 1);
        return true;
    }
}
