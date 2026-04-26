using System;
using System.Collections.Generic;
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
        var inlayGen = new VarInlayHintElementGenerator(getTrailingInlays);
        // Первый в списке: тот же document offset, что и «показать пробел» (SingleCharacter), — иначе inlay не получит Construct.
        editor.TextArea.TextView.ElementGenerators.Insert(0, inlayGen);
        var list = new IBackgroundRenderer[]
        {
            new BreakpointLineBackgroundRenderer(getBreakpointLines),
            new DebugCurrentLineBackgroundRenderer(getDebugCurrentLine),
            new DebugInstructionArrowBackgroundRenderer(getDebugCurrentLine),
            new EditorDiagnosticBackgroundRenderer(getDiagnosticStrips),
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
            tv.ElementGenerators.Remove(_inlayGen);
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
