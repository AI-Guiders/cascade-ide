using CascadeIDE.Services;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Diagnostic strips → line-first CECB decorations and EOL inlays.</summary>
public static class MonacoEditorDiagnosticsMapper
{
    private const int MaxInlineMessageLength = 120;

    public static IReadOnlyList<CideEditorDecoration> ToDecorations(IReadOnlyList<EditorDiagnosticStrip> strips)
    {
        if (strips.Count == 0)
            return Array.Empty<CideEditorDecoration>();

        var list = new List<CideEditorDecoration>(strips.Count);
        foreach (var group in strips.GroupBy(s => s.Line1).OrderBy(g => g.Key))
        {
            var items = group.ToList();
            var primary = items.OrderByDescending(s => SeverityRank(s.Severity)).First();
            var className = primary.Severity switch
            {
                DiagnosticSeverity.Error => "squiggly-error",
                DiagnosticSeverity.Warning => "squiggly-warning",
                _ => "squiggly-info",
            };
            var hover = items.Count == 1
                ? $"{primary.Id}: {primary.Message}"
                : string.Join("\n", items.Select(s => $"{s.Id}: {s.Message}"));

            list.Add(new CideEditorDecoration(
                StartOffset: 0,
                Length: 0,
                ClassName: className,
                HoverMessage: hover,
                IsWholeLine: true,
                StartLine: group.Key,
                StartColumn: 1,
                EndLine: group.Key));
        }

        return list;
    }

    /// <summary>Trailing diagnostic labels at EOL (VS / Error Lens style).</summary>
    public static IReadOnlyList<CideEditorInlayHint> ToDiagnosticInlays(IReadOnlyList<EditorDiagnosticStrip> strips)
    {
        if (strips.Count == 0)
            return Array.Empty<CideEditorInlayHint>();

        var list = new List<CideEditorInlayHint>(strips.Count);
        foreach (var group in strips.GroupBy(s => s.Line1).OrderBy(g => g.Key))
        {
            var items = group.ToList();
            var primary = items.OrderByDescending(s => SeverityRank(s.Severity)).First();
            var label = FormatInlineLabel(primary, items.Count);
            var kind = primary.Severity switch
            {
                DiagnosticSeverity.Error => "diagnostic-error",
                DiagnosticSeverity.Warning => "diagnostic-warning",
                _ => "diagnostic-info",
            };
            list.Add(new CideEditorInlayHint(group.Key, 1, label, kind, AtEndOfLine: true));
        }

        return list;
    }

    private static string FormatInlineLabel(EditorDiagnosticStrip primary, int countOnLine)
    {
        var message = primary.Message.Trim();
        if (message.Length > MaxInlineMessageLength)
            message = message[..(MaxInlineMessageLength - 1)] + "…";

        var label = $" {primary.Id}: {message}";
        if (countOnLine > 1)
            label += $" (+{countOnLine - 1})";
        return label;
    }

    private static int SeverityRank(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => 3,
        DiagnosticSeverity.Warning => 2,
        DiagnosticSeverity.Info => 1,
        _ => 0,
    };
}
