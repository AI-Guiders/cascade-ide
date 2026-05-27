using System.Runtime.CompilerServices;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>
/// Подсветка «устаревшего verify epoch» на уровне всего открытого файла (ADR 0148 W3).
/// </summary>
public static class EditorAgentVerifyEpochDim
{
    private static readonly ConditionalWeakTable<TextEditor, DimState> s_states = new();

    public static void SetDimmed(TextEditor editor, bool dimmed)
    {
        var state = s_states.GetValue(editor, static e => new DimState(e));
        state.SetDimmed(dimmed);
    }

    public static void RefreshForPath(TextEditor editor, string? filePath, Func<string, bool> isPathStale)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            SetDimmed(editor, false);
            return;
        }

        try
        {
            var full = Path.GetFullPath(filePath);
            SetDimmed(editor, isPathStale(full));
        }
        catch
        {
            SetDimmed(editor, false);
        }
    }

    private sealed class DimState
    {
        private readonly TextEditor _editor;
        private readonly EditorAgentVerifyEpochDimBackgroundRenderer _renderer;
        private bool _dimmed;

        public DimState(TextEditor editor)
        {
            _editor = editor;
            _renderer = new EditorAgentVerifyEpochDimBackgroundRenderer(() => _dimmed);
            _editor.TextArea.TextView.BackgroundRenderers.Add(_renderer);
        }

        public void SetDimmed(bool dimmed)
        {
            if (_dimmed == dimmed)
                return;

            _dimmed = dimmed;
            UiScheduler.Default.Post(
                () => _editor.TextArea.TextView.InvalidateLayer(_renderer.Layer),
                DispatcherPriority.Background);
        }
    }
}
