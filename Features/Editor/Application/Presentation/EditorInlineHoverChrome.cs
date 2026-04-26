using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaEdit;

namespace CascadeIDE.Features.Editor.Application.Presentation;

/// <summary>
/// Тайминги и смещения **inline** hover/Quick Info / диагностики по указателю (ADR 0085 Editor HUD, не file-level баннер).
/// Не <b>IDS</b> (ADR 0079): тултип — привязка к <see cref="TextEditor"/>, не глобальный стек IDE-оверлеев; величины в одном месте (<see cref="ApplyToolTipServiceTo"/>, <see cref="PointerPositionDebounce"/>).
/// </summary>
public static class EditorInlineHoverChrome
{
    public const int PointerPositionDebounceMilliseconds = 120;

    /// <summary>Задержка показа <see cref="ToolTip"/>, мс; совпадает с Avalonia <c>ToolTip.ShowDelay</c> на <see cref="TextEditor"/>.</summary>
    public const int ToolTipShowDelayMilliseconds = 280;

    public const double ToolTipVerticalOffsetDip = 10;
    public const double ToolTipHorizontalOffsetDip = 10;

    public static TimeSpan PointerPositionDebounce => TimeSpan.FromMilliseconds(PointerPositionDebounceMilliseconds);

    /// <summary>Учитывает, что <see cref="AvaloniaEdit.TextEditor"/> якорь большой: <see cref="PlacementMode.Pointer"/> — у курсора (как в IDE).</summary>
    public static void ApplyToolTipServiceTo(Control host)
    {
        ToolTip.SetPlacement(host, PlacementMode.Pointer);
        ToolTip.SetShowDelay(host, ToolTipShowDelayMilliseconds);
        ToolTip.SetVerticalOffset(host, ToolTipVerticalOffsetDip);
        ToolTip.SetHorizontalOffset(host, ToolTipHorizontalOffsetDip);
    }
}
