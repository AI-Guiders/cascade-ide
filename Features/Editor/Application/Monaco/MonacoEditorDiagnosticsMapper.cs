using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Features.Editor.Application;

public static class MonacoEditorDiagnosticsMapper
{
    public static IReadOnlyList<CideEditorDecoration> ToDecorations(IReadOnlyList<EditorDiagnosticStrip> strips)
    {
        if (strips.Count == 0)
            return Array.Empty<CideEditorDecoration>();

        var list = new List<CideEditorDecoration>(strips.Count);
        foreach (var strip in strips)
        {
            var className = strip.Severity switch
            {
                DiagnosticSeverity.Error => "squiggly-error",
                DiagnosticSeverity.Warning => "squiggly-warning",
                _ => "squiggly-info",
            };
            list.Add(new CideEditorDecoration(
                strip.Start,
                strip.Length,
                className,
                $"{strip.Id}: {strip.Message}"));
        }

        return list;
    }
}
