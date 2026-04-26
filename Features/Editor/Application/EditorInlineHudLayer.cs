using AvaloniaEdit;
using CascadeIDE.Features.Editor.Application.Presentation;

namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Фасад **Editor HUD (inline)** по ADR 0085 / 0103: squiggles, hover/Quick Info, gutter — не file-level баннер
/// (<see cref="Presentation.EditorHudBannerTextComposer"/>).
/// Реализации: <see cref="Presentation.EditorInlineHoverToolTipController"/>, <see cref="Presentation.EditorDocumentBackgroundVisualsHandle"/>;
/// полная инвентаризация — <c>docs/design/editor-hud-inline-migration-inventory-v1.md</c>.
/// </summary>
public static class EditorInlineHudLayer
{
    public static EditorDocumentBackgroundVisualsHandle InstallDocumentBackgroundVisuals(
        TextEditor editor,
        Func<IReadOnlyList<int>> getBreakpointLines,
        Func<int> getDebugCurrentLine,
        Func<IReadOnlyList<EditorDiagnosticStrip>> getDiagnosticStrips) =>
        EditorDocumentBackgroundVisualsHandle.Install(
            editor, getBreakpointLines, getDebugCurrentLine, getDiagnosticStrips);
}
