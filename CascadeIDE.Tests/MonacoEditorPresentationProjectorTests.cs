using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MonacoEditorPresentationProjectorTests
{
    [Fact]
    public void Diagnostics_use_line_first_decorations()
    {
        var strips = new[]
        {
            new EditorDiagnosticStrip(0, 5, DiagnosticSeverity.Error, "CS0246", "Type not found", 14, 1),
        };

        var decos = MonacoEditorDiagnosticsMapper.ToDecorations(strips);
        Assert.Single(decos);
        Assert.Equal(14, decos[0].StartLine);
        Assert.True(decos[0].IsWholeLine);
        Assert.Equal("squiggly-error", decos[0].ClassName);
    }

    [Fact]
    public void Diagnostic_inlays_use_at_end_of_line_flag()
    {
        var strips = new[]
        {
            new EditorDiagnosticStrip(0, 5, DiagnosticSeverity.Error, "CS0246", "Type not found", 3, 1),
        };

        var inlays = MonacoEditorDiagnosticsMapper.ToDiagnosticInlays(strips);
        Assert.Single(inlays);
        Assert.True(inlays[0].AtEndOfLine);
        Assert.Equal("diagnostic-error", inlays[0].Kind);
    }

    [Fact]
    public void MergeInlayHints_suppresses_var_on_diagnostic_lines()
    {
        const string text = "var x = 1;\nvar bad = y;";
        var strips = new[]
        {
            new EditorDiagnosticStrip(10, 3, DiagnosticSeverity.Error, "CS0103", "y", 2, 11),
        };
        var varParts = new[]
        {
            new EditorTrailingInlayPart(3, " int"),
            new EditorTrailingInlayPart(15, " error"),
        };

        var hints = MonacoEditorPresentationProjector.MergeInlayHints(text, strips, varParts);
        Assert.Equal(2, hints.Count);
        Assert.DoesNotContain(hints, h => h.Line == 2 && h.Kind == "type");
        Assert.Contains(hints, h => h.Line == 2 && h.AtEndOfLine);
        Assert.Contains(hints, h => h.Line == 1 && !h.AtEndOfLine);
    }
}
