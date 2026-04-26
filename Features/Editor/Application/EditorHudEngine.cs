namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Семантическая проекция для слоя: полосы диагностик → <see cref="EditorSemanticSnapshot"/> (ADR 0103).
/// Сборка per-док: <see cref="EditorDocumentHudLayer"/>; баннер (текст) — <see cref="Presentation.EditorHudBannerTextComposer"/> + VM.
/// </summary>
public sealed class EditorHudEngine
{
    private Func<string?, IReadOnlyList<EditorDiagnosticStrip>>? _getStripsForFile;

    /// <summary>Последняя проекция после <see cref="OnStabilizedInput"/>; для отладки и будущих inline-слоёв.</summary>
    public EditorSemanticSnapshot? LastSnapshot { get; private set; }

    /// <summary>Источник полос диагностик по пути (обычно <c>WorkspaceDiagnosticsCoordinator.GetStripsForFile</c>).</summary>
    public void ConfigureDiagnostics(Func<string?, IReadOnlyList<EditorDiagnosticStrip>>? getStripsForFile) =>
        _getStripsForFile = getStripsForFile;

    public void OnStabilizedInput(EditorInputDelta delta)
    {
        if (_getStripsForFile is null)
        {
            LastSnapshot = null;
            return;
        }

        var strips = _getStripsForFile(delta.FilePath);
        LastSnapshot = SemanticProjectionPipeline.FromDiagnosticStrips(strips);
    }
}
