using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CascadeIDE.Tests;

public class SemanticProjectionPipelineTests
{
    [Fact]
    public void FromDiagnosticStrips_AggregatesSeverities()
    {
        IReadOnlyList<EditorDiagnosticStrip> strips =
        [
            new(0, 1, DiagnosticSeverity.Error, "E1", "m", 1, 1),
            new(0, 1, DiagnosticSeverity.Error, "E2", "m", 1, 1),
            new(0, 1, DiagnosticSeverity.Warning, "W1", "m", 1, 1),
            new(0, 1, DiagnosticSeverity.Info, "I1", "m", 1, 1),
            new(0, 1, DiagnosticSeverity.Hidden, "H1", "m", 1, 1),
        ];

        var s = SemanticProjectionPipeline.FromDiagnosticStrips(strips);
        Assert.Equal(2, s.ErrorCount);
        Assert.Equal(1, s.WarningCount);
        Assert.Equal(1, s.InfoCount);
        Assert.Equal(1, s.HintCount);
    }

    [Fact]
    public void FromDiagnosticStrips_Empty_ZeroCounts()
    {
        var s = SemanticProjectionPipeline.FromDiagnosticStrips([]);
        Assert.Equal(0, s.ErrorCount);
        Assert.Equal(0, s.WarningCount);
        Assert.Equal(0, s.InfoCount);
        Assert.Equal(0, s.HintCount);
    }
}
