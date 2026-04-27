using Avalonia.Headless.XUnit;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using CascadeIDE.Features.Editor.Application.Presentation;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EditorDocumentBackgroundVisualsHandleTests
{
    /// <summary>
    /// Регресс: AvaloniaEdit для DocumentLength=0 обходит все записи <see cref="VarInlayHintElementGenerator" />
    /// в <see cref="TextView.ElementGenerators" />; дубликат одной ссылки N раз раньше давал N одинаковых inlay.
    /// Install/Dispose + дедуп в <see cref="VarInlayHintElementGenerator" /> (один inlay на document offset
    /// на <see cref="AvaloniaEdit.Rendering.VisualLine" />) визуально оставляют одну inlay. Повторный Install
    /// оставляет ровно одну ссылку на generator в списке.
    /// </summary>
    [AvaloniaFact]
    public void Install_TwiceAfterArtificialGeneratorDuplicates_LeavesSingleVarInlayGenerator()
    {
        const string src = """
            class C {
              void M() {
                var t = 0;
              }
            }
            """;
        var i = src.IndexOf("var", StringComparison.Ordinal);
        var anchor = i + 3;
        static IReadOnlyList<EditorDiagnosticStrip> NoStrips() => [];
        IReadOnlyList<EditorTrailingInlayPart> Inlays() => [new EditorTrailingInlayPart(anchor, "  int")];

        var editor = new TextEditor { Text = src };
        var tv = editor.TextArea.TextView;

        using var h1 = EditorDocumentBackgroundVisualsHandle.Install(
            editor,
            static () => [],
            static () => -1,
            NoStrips,
            Inlays);

        AssertVarInlayGeneratorCount(tv, 1);
        var g = Assert.Single(tv.ElementGenerators.OfType<VarInlayHintElementGenerator>());
        for (var k = 0; k < 4; k++)
            tv.ElementGenerators.Insert(0, g);
        AssertVarInlayGeneratorCount(tv, 5);

        using var h2 = EditorDocumentBackgroundVisualsHandle.Install(
            editor,
            static () => [],
            static () => -1,
            NoStrips,
            Inlays);
        AssertVarInlayGeneratorCount(tv, 1);
    }

    [AvaloniaFact]
    public void DuplicateVarInlayGeneratorRefs_StillRenderSingleZeroLengthInlayOnSameLine()
    {
        const string src = """
            class C {
              void M() {
                var t = 0;
              }
            }
            """;
        var i = src.IndexOf("var", StringComparison.Ordinal);
        var anchor = i + 3;
        IReadOnlyList<EditorTrailingInlayPart> Inlays() => [new EditorTrailingInlayPart(anchor, "  int")];
        var editor = new TextEditor { Text = src };
        var tv = editor.TextArea.TextView;

        static IReadOnlyList<EditorDiagnosticStrip> NoStrips() => [];

        using (EditorDocumentBackgroundVisualsHandle.Install(
                   editor,
                   static () => [],
                   static () => -1,
                   NoStrips,
                   Inlays))
        {
            var g = Assert.Single(tv.ElementGenerators.OfType<VarInlayHintElementGenerator>());
            for (var k = 0; k < 4; k++)
                tv.ElementGenerators.Insert(0, g);
            AssertVarInlayGeneratorCount(tv, 5);

            tv.Redraw();
            var docLine = editor.Document.GetLineByOffset(anchor);
            var visualLine = tv.GetOrConstructVisualLine(docLine);
            int zeroDocumentLen = visualLine.Elements.Count(e => e.DocumentLength == 0);
            Assert.Equal(1, zeroDocumentLen);
        }

        using (EditorDocumentBackgroundVisualsHandle.Install(
                   editor,
                   static () => [],
                   static () => -1,
                   NoStrips,
                   Inlays))
        {
            tv.Redraw();
            var docLine = editor.Document.GetLineByOffset(anchor);
            var visualLine = tv.GetOrConstructVisualLine(docLine);
            int zeroDocumentLen = visualLine.Elements.Count(e => e.DocumentLength == 0);
            Assert.Equal(1, zeroDocumentLen);
        }
    }

    [AvaloniaFact]
    public void Dispose_RemovesEveryDuplicateReferenceOfSameGenerator()
    {
        var editor = new TextEditor { Text = "var x = 1;" };
        var tv = editor.TextArea.TextView;
        var anchor = "var x = 1;".IndexOf("var", StringComparison.Ordinal) + 3;
        static IReadOnlyList<EditorDiagnosticStrip> NoStrips() => [];
        IReadOnlyList<EditorTrailingInlayPart> Inlays() => [new EditorTrailingInlayPart(anchor, "  int")];

        var h = EditorDocumentBackgroundVisualsHandle.Install(
            editor,
            static () => [],
            static () => -1,
            NoStrips,
            Inlays);
        var g = Assert.Single(tv.ElementGenerators.OfType<VarInlayHintElementGenerator>());
        for (var k = 0; k < 4; k++)
            tv.ElementGenerators.Insert(0, g);
        AssertVarInlayGeneratorCount(tv, 5);

        h.Dispose();
        AssertVarInlayGeneratorCount(tv, 0);
    }

    private static void AssertVarInlayGeneratorCount(TextView tv, int expected) =>
        Assert.Equal(expected, tv.ElementGenerators.OfType<VarInlayHintElementGenerator>().Count());
}
