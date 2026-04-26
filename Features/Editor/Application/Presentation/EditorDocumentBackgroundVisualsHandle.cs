using System;
using System.Collections.Generic;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor.Application.Presentation;

/// <summary>
/// Регистрирует <see cref="IBackgroundRenderer"/> для документа (брейкпоинты, отладка, squiggles, EOL inlay).
/// <see cref="Dispose"/> снимает те же инстансы с <see cref="AvaloniaEdit.TextEditor.TextArea.TextView.BackgroundRenderers"/>.
/// </summary>
public sealed class EditorDocumentBackgroundVisualsHandle : IDisposable
{
    private readonly TextEditor _editor;
    private readonly IBackgroundRenderer[] _renderers;
    private bool _disposed;

    private EditorDocumentBackgroundVisualsHandle(TextEditor editor, IBackgroundRenderer[] renderers)
    {
        _editor = editor;
        _renderers = renderers;
    }

    public static EditorDocumentBackgroundVisualsHandle Install(
        TextEditor editor,
        Func<IReadOnlyList<int>> getBreakpointLines,
        Func<int> getDebugCurrentLine,
        Func<IReadOnlyList<EditorDiagnosticStrip>> getDiagnosticStrips,
        Func<IReadOnlyList<EditorTrailingInlayPart>>? getTrailingInlays = null)
    {
        getTrailingInlays ??= static () => [];
        var list = new IBackgroundRenderer[]
        {
            new BreakpointLineBackgroundRenderer(getBreakpointLines),
            new DebugCurrentLineBackgroundRenderer(getDebugCurrentLine),
            new DebugInstructionArrowBackgroundRenderer(getDebugCurrentLine),
            new EditorDiagnosticBackgroundRenderer(getDiagnosticStrips),
            new EditorInlayHintBackgroundRenderer(getTrailingInlays)
        };
        var br = editor.TextArea.TextView.BackgroundRenderers;
        foreach (var r in list)
            br.Add(r);
        return new EditorDocumentBackgroundVisualsHandle(editor, list);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        var br = _editor.TextArea?.TextView.BackgroundRenderers;
        if (br is null)
            return;
        foreach (var r in _renderers)
        {
            var i = br.IndexOf(r);
            if (i >= 0)
                br.RemoveAt(i);
        }
    }
}
