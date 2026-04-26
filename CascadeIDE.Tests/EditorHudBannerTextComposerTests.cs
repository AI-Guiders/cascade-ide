using CascadeIDE.Features.Editor.Application.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class EditorHudBannerTextComposerTests
{
    [Fact]
    public void FormatDiagnosticSummary_Mixed()
    {
        var s = EditorHudBannerTextComposer.FormatDiagnosticSummary(2, 3);
        Assert.Equal("2 ошибок, 3 предупреждений", s);
    }

    [Fact]
    public void FormatDiagnosticSummary_SingleError()
    {
        Assert.Equal("1 ошибка", EditorHudBannerTextComposer.FormatDiagnosticSummary(1, 0));
    }

    [Fact]
    public void FormatReferenceOccurrenceSummary_Zero_Null()
    {
        Assert.Null(EditorHudBannerTextComposer.FormatReferenceOccurrenceSummary(0));
    }

    [Fact]
    public void Combine_Both()
    {
        var t = EditorHudBannerTextComposer.Combine("a", "b");
        Assert.Equal("a · b", t);
    }
}
