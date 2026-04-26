namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Зарезервировано под **Editor HUD** (inline) в смысле ADR 0085:
/// squiggles, inlays, Quick Info, gutter — плоскость документа, не <see cref="Presentation.EditorHudBannerTextComposer"/> (file-level баннер).
/// Данные — стабилизированные снимки и LSP/DAL; рендер сейчас в <c>Views/DockDocumentView</c> (strangler).
/// </summary>
public static class EditorInlineHudLayer
{
    // Намеренно пусто: якорь для расширения без смешения с HUD banner.
}
