namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>
/// Единая C#-проекция HUD inline → CECB push (ADR 0163 §2.8, monaco-presentation-projection-v1).
/// Политика 0085: var-inlay не на строках с диагностикой.
/// </summary>
public static class MonacoEditorPresentationProjector
{
    public sealed record Push(
        int ModelVersion,
        IReadOnlyList<CideEditorDecoration> DiagnosticDecorations,
        IReadOnlyList<CideEditorInlayHint> InlayHints);

    public static Push ProjectEditorHud(
        int modelVersion,
        string sourceText,
        IReadOnlyList<EditorDiagnosticStrip> strips,
        IReadOnlyList<EditorTrailingInlayPart> varInlayParts)
    {
        var decorations = MonacoEditorDiagnosticsMapper.ToDecorations(strips);
        var inlays = MergeInlayHints(sourceText, strips, varInlayParts);
        return new Push(modelVersion, decorations, inlays);
    }

    public static Push ProjectDiagnosticsOnly(int modelVersion, string sourceText, IReadOnlyList<EditorDiagnosticStrip> strips)
    {
        var decorations = MonacoEditorDiagnosticsMapper.ToDecorations(strips);
        var inlays = MonacoEditorDiagnosticsMapper.ToDiagnosticInlays(strips);
        return new Push(modelVersion, decorations, inlays);
    }

    public static IReadOnlyList<CideEditorInlayHint> MergeInlayHints(
        string sourceText,
        IReadOnlyList<EditorDiagnosticStrip> strips,
        IReadOnlyList<EditorTrailingInlayPart> varInlayParts)
    {
        var diagnosticLines = strips.Select(s => s.Line1).ToHashSet();
        var varHints = MonacoEditorInlayMapper.ToHints(sourceText, varInlayParts)
            .Where(h => !diagnosticLines.Contains(h.Line));
        var diagHints = MonacoEditorDiagnosticsMapper.ToDiagnosticInlays(strips);
        return varHints.Concat(diagHints).ToList();
    }
}
