namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Именованный слой Editor HUD (ADR 0103) <b>для одного открытого документа</b>: движок + стабилизированный контекст
/// для баннера. Inline squiggles/inlays (0085) остаются в view до отдельного переноса; граница данных — <see cref="EditorSemanticSnapshot"/>.
/// </summary>
public sealed class EditorDocumentHudLayer
{
    private readonly EditorHudEngine _engine = new();

    public void ConfigureDiagnostics(Func<string?, IReadOnlyList<EditorDiagnosticStrip>>? getStripsForFile) =>
        _engine.ConfigureDiagnostics(getStripsForFile);

    /// <summary>Контекст для VM; <c>null</c>, если нет полос/пути (закрытый DAL-источник или пустой путь).</summary>
    public EditorHudStabilizedContext? BuildStabilizedContext(EditorInputDelta delta)
    {
        _engine.OnStabilizedInput(delta);
        if (string.IsNullOrEmpty(delta.FilePath) || _engine.LastSnapshot is not { } snap)
            return null;
        return new EditorHudStabilizedContext(delta.FilePath, snap);
    }
}
