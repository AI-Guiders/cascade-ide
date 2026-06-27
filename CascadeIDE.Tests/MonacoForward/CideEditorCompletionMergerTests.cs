using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class CideEditorCompletionMergerTests
{
    [Fact]
    public void Merge_adds_roslyn_StringBuilder_when_lsp_only_has_SByte()
    {
        var lsp = new[]
        {
            new CideEditorCompletionItem("SByte", "SByte", "struct System.SByte", "struct"),
        };
        var roslyn = new[]
        {
            new CideEditorCompletionItem("SByte", "SByte", null, "struct"),
            new CideEditorCompletionItem("StringBuilder", "StringBuilder", null, "class"),
        };

        var merged = CideEditorCompletionMerger.Merge(lsp, roslyn, "SB");

        Assert.Contains(merged, i => i.Label == "SByte");
        Assert.Contains(merged, i => i.Label == "StringBuilder");
    }

    [Fact]
    public void Extract_reads_identifier_prefix_on_new()
    {
        const string src = "class C { void M() { var x = new SB } }";
        var idx = src.IndexOf("SB", StringComparison.Ordinal);
        Assert.Equal("SB", CSharpCompletionPrefix.Extract(src, line1: 1, column1: idx + 3));
    }
}
