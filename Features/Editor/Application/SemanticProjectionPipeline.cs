using Microsoft.CodeAnalysis;

namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Вертикальный срез DAL/диагностик → снимок для Editor HUD (ADR 0103).
/// Источник полос — <see cref="WorkspaceDiagnosticsCoordinator"/>, не прямой CCU.
/// </summary>
public static class SemanticProjectionPipeline
{
    public static EditorSemanticSnapshot FromDiagnosticStrips(IReadOnlyList<EditorDiagnosticStrip> strips)
    {
        var e = 0;
        var w = 0;
        var i = 0;
        var h = 0;
        foreach (var s in strips)
        {
            switch (s.Severity)
            {
                case DiagnosticSeverity.Error:
                    e++;
                    break;
                case DiagnosticSeverity.Warning:
                    w++;
                    break;
                case DiagnosticSeverity.Info:
                    i++;
                    break;
                case DiagnosticSeverity.Hidden:
                    h++;
                    break;
            }
        }

        return new EditorSemanticSnapshot(e, w, i, h);
    }
}
