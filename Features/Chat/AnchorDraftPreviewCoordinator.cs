#nullable enable

using AvaloniaEdit;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Debounced ghost-рамка в редакторе для незакрытого bracket в composer/CCL (ADR 0138 фаза B).
/// </summary>
public sealed class AnchorDraftPreviewCoordinator
{
    public const int DefaultDebounceMs = 200;

    private readonly Func<string?> _getActiveFilePath;
    private readonly Func<string> _getWorkspaceRoot;
    private readonly Func<string?> _getSolutionPath;
    private readonly Func<string?> _getIndexDirectoryRelative;
    private readonly Func<string?, TextEditor?> _getEditorForAbsoluteFilePath;

    private CancellationTokenSource? _debounceCts;
    private TextEditor? _lastEditor;
    private int _generation;

    public AnchorDraftPreviewCoordinator(
        Func<string?> getActiveFilePath,
        Func<string> getWorkspaceRoot,
        Func<string?> getSolutionPath,
        Func<string?> getIndexDirectoryRelative,
        Func<string?, TextEditor?> getEditorForAbsoluteFilePath)
    {
        _getActiveFilePath = getActiveFilePath;
        _getWorkspaceRoot = getWorkspaceRoot;
        _getSolutionPath = getSolutionPath;
        _getIndexDirectoryRelative = getIndexDirectoryRelative;
        _getEditorForAbsoluteFilePath = getEditorForAbsoluteFilePath;
    }

    public void Schedule(string? composerText, int caretIndex, int debounceMs = DefaultDebounceMs)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var cts = _debounceCts;
        var generation = ++_generation;
        var capturedText = composerText ?? "";
        var capturedCaret = caretIndex;
        _ = refreshDebouncedAsync(capturedText, capturedCaret, generation, debounceMs, cts);
    }

    public void Clear()
    {
        _debounceCts?.Cancel();
        _generation++;
        clearEditorHighlight();
    }

    private async Task refreshDebouncedAsync(
        string text,
        int caret,
        int generation,
        int debounceMs,
        CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(debounceMs, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!tryBuildBracketText(text, caret, out var bracketText))
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                if (generation != _generation || cts.IsCancellationRequested)
                    return;

                clearEditorHighlight();
            }).ConfigureAwait(false);
            return;
        }

        var activeFile = _getActiveFilePath();
        var workspace = _getWorkspaceRoot();
        var solution = _getSolutionPath();
        var indexDir = _getIndexDirectoryRelative();

        var preview = await Task.Run(
            () =>
            {
                if (!AnchorDraftPreviewResolver.TryResolve(
                        bracketText,
                        activeFile,
                        workspace,
                        solution,
                        indexDir,
                        out var resolved,
                        out _))
                {
                    return (AnchorDraftPreviewResolver.ResolvedPreview?)null;
                }

                return resolved;
            },
            cts.Token).ConfigureAwait(false);

        if (preview is null)
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                if (generation != _generation || cts.IsCancellationRequested)
                    return;

                clearEditorHighlight();
            }).ConfigureAwait(false);
            return;
        }

        await UiScheduler.Default.InvokeAsync(() =>
        {
            if (generation != _generation || cts.IsCancellationRequested)
                return;

            if (!ChatBracketAutocomplete.TryGetEditState(text, caret, out _))
            {
                clearEditorHighlight();
                return;
            }

            var editor = _getEditorForAbsoluteFilePath(preview.AbsoluteFilePath);
            if (editor is null)
            {
                clearEditorHighlight();
                return;
            }

            if (_lastEditor is not null && !ReferenceEquals(_lastEditor, editor))
                EditorAgentRangeReveal.Hide(_lastEditor);

            _lastEditor = editor;
            EditorAgentRangeReveal.ShowPersistent(editor, preview.StartLine, preview.EndLine);
        }).ConfigureAwait(false);
    }

    private static bool tryBuildBracketText(string text, int caret, out string bracketText)
    {
        bracketText = "";
        if (!ChatBracketAutocomplete.TryGetEditState(text, caret, out var state))
            return false;

        var inner = text[(state.BracketStart + 1)..state.CaretIndex];
        if (inner.Trim().Length == 0 && state.ActiveAxis == ChatBracketAutocomplete.Axis.Start)
            return false;

        bracketText = $"[{inner}]";
        return true;
    }

    private void clearEditorHighlight()
    {
        if (_lastEditor is null)
            return;

        EditorAgentRangeReveal.Hide(_lastEditor);
        _lastEditor = null;
    }
}
