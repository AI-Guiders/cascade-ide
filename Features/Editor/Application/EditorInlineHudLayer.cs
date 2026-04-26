namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Фасад **Editor HUD (inline)** по ADR 0085 / 0103: squiggles, hover/Quick Info, gutter — не file-level баннер
/// (<see cref="Presentation.EditorHudBannerTextComposer"/>).
/// Реализации: <see cref="Presentation.EditorInlineHoverToolTipController"/>; полная инвентаризация —
/// <c>docs/design/editor-hud-inline-migration-inventory-v1.md</c>.
/// </summary>
public static class EditorInlineHudLayer
{
    // Доп. фабрики (напр. установка IBackgroundRenderer) — по мере strangler; см. inventory.
}
