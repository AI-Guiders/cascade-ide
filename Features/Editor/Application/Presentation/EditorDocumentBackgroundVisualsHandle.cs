using System.Runtime.CompilerServices;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor.Application.Presentation;

/// <summary>
/// Регистрирует <see cref="IBackgroundRenderer"/> и (для C#) <see cref="VarInlayHintElementGenerator" />.
/// <see cref="Dispose"/> снимает те же инстансы с <see cref="AvaloniaEdit.TextEditor.TextArea.TextView" />.
/// </summary>
public sealed class EditorDocumentBackgroundVisualsHandle : IDisposable
{
    /// <summary>
    /// Один <see cref="VarInlayHintElementGenerator" /> на <see cref="TextView" />: иначе AvaloniaEdit
    /// для <c>DocumentLength == 0</c> не делает break и N экземпляров в <see cref="TextView.ElementGenerators" />
    /// дают N одинаковых inlay на одном offset.
    /// </summary>
    private static readonly ConditionalWeakTable<TextView, VarInlayHintElementGenerator> InlayGeneratorsByTextView = new();

    private readonly TextEditor _editor;
    private readonly IBackgroundRenderer[] _renderers;
    private readonly VarInlayHintElementGenerator _inlayGen;
    private bool _disposed;

    private EditorDocumentBackgroundVisualsHandle(
        TextEditor editor,
        IBackgroundRenderer[] renderers,
        VarInlayHintElementGenerator inlayGen)
    {
        _editor = editor;
        _renderers = renderers;
        _inlayGen = inlayGen;
    }

    public static EditorDocumentBackgroundVisualsHandle Install(
        TextEditor editor,
        Func<IReadOnlyList<int>> getBreakpointLines,
        Func<int> getDebugCurrentLine,
        Func<IReadOnlyList<EditorDiagnosticStrip>> getDiagnosticStrips,
        Func<IReadOnlyList<EditorTrailingInlayPart>>? getTrailingInlays = null)
    {
        getTrailingInlays ??= static () => [];
        var tv = editor.TextArea.TextView;
        var gens = tv.ElementGenerators;
        for (int i = gens.Count - 1; i >= 0; i--)
        {
            if (gens[i] is VarInlayHintElementGenerator)
                gens.RemoveAt(i);
        }
        if (!InlayGeneratorsByTextView.TryGetValue(tv, out var inlayGen))
        {
            inlayGen = new VarInlayHintElementGenerator(getTrailingInlays);
            InlayGeneratorsByTextView.Add(tv, inlayGen);
        }
        else
            inlayGen.SetInlayProvider(getTrailingInlays);
        // Та же ссылка в списке N раз = N inlay (AvaloniaEdit обходит все записи; Remove по типу не снимает дубль ссылок)
        while (gens.Remove(inlayGen)) { }
        gens.Insert(0, inlayGen);
        if (InlayHintTrace.IsDebug)
        {
            for (int i = 0; i < gens.Count; i++)
                InlayHintTrace.LogDebug($"ElementGenerators[{i}]={gens[i].GetType().Name} hash={gens[i].GetHashCode():X8}");
        }

        var list = new IBackgroundRenderer[]
        {
            new BreakpointLineBackgroundRenderer(getBreakpointLines),
            new DebugCurrentLineBackgroundRenderer(getDebugCurrentLine),
            new DebugInstructionArrowBackgroundRenderer(getDebugCurrentLine),
            new EditorDiagnosticBackgroundRenderer(getDiagnosticStrips),
            // Inline diagnostic text after EOL: KnownLayer.Text, поверх глифов.
            new EditorEndOfLineDiagnosticTextRenderer(getDiagnosticStrips),
        };
        var br = editor.TextArea.TextView.BackgroundRenderers;
        foreach (var r in list)
            br.Add(r);
        return new EditorDocumentBackgroundVisualsHandle(editor, list, inlayGen);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        var tv = _editor.TextArea?.TextView;
        if (tv is not null)
        {
            _inlayGen.SetInlayProvider(static () => []);
            while (tv.ElementGenerators.Remove(_inlayGen)) { }
            var br = tv.BackgroundRenderers;
            foreach (var r in _renderers)
            {
                var i = br.IndexOf(r);
                if (i >= 0)
                    br.RemoveAt(i);
            }
        }
    }
}
