using CascadeIDE.Features.Documents;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Unifies reveal/goto paths into Monaco (ADR 0163 §2.4).</summary>
public sealed class EditorNavigationService : IEditorNavigationService
{
    private readonly MainWindowViewModel _vm;

    public EditorNavigationService(MainWindowViewModel vm) => _vm = vm;

    public async Task<bool> NavigateAsync(EditorNavigationTarget target, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.FilePath))
            return false;

        await UiScheduler.Default.InvokeAsync(() =>
        {
            _vm.Documents.OpenOrActivateDocument(target.FilePath);
            return true;
        }).ConfigureAwait(true);

        var dock = await WaitForDockAsync(_vm, target.FilePath, cancellationToken).ConfigureAwait(true);
        if (dock is null)
            return false;

        return await ApplyPresentationAsync(dock, target, cancellationToken).ConfigureAwait(true);
    }

    public bool TryNavigateReveal(
        string? filePath,
        int startLine,
        int endLine,
        int? durationMs,
        EditorNavigationSource source = EditorNavigationSource.Mcp)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var presentation = durationMs is > 0
            ? EditorNavigationPresentation.RevealTransient
            : EditorNavigationPresentation.SelectAndReveal;

        _ = NavigateAsync(new EditorNavigationTarget(
            filePath,
            startLine,
            endLine,
            Presentation: presentation,
            DurationMs: durationMs,
            Source: source));

        return true;
    }

    public bool TryNavigateGoTo(
        string? filePath,
        int line,
        int column,
        int? endLine = null,
        int? endColumn = null,
        EditorNavigationSource source = EditorNavigationSource.Mcp)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var end = endLine ?? line;
        _ = NavigateAsync(new EditorNavigationTarget(
            filePath,
            line,
            end,
            StartColumn: column,
            EndColumn: endColumn,
            Presentation: endLine is not null || endColumn is not null
                ? EditorNavigationPresentation.SelectAndReveal
                : EditorNavigationPresentation.ScrollOnly,
            Source: source));

        return true;
    }

    private static async Task<DockDocumentView?> WaitForDockAsync(
        MainWindowViewModel vm,
        string filePath,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dock = await UiScheduler.Default.InvokeAsync(() =>
                EditorActiveDockResolver.TryGetDockDocumentView(vm, filePath)).ConfigureAwait(true);
            if (dock is not null)
                return dock;
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<bool> ApplyPresentationAsync(
        DockDocumentView dock,
        EditorNavigationTarget target,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (target.Presentation)
        {
            case EditorNavigationPresentation.RevealTransient:
                await dock.RevealAgentRangeAsync(target.StartLine, target.EndLine, persistent: false).ConfigureAwait(true);
                return true;

            case EditorNavigationPresentation.RevealPersistent:
                await dock.RevealAgentRangeAsync(target.StartLine, target.EndLine, persistent: true).ConfigureAwait(true);
                return true;

            case EditorNavigationPresentation.ScrollOnly:
                await dock.GotoLineColumnAsync(
                    target.StartLine,
                    target.StartColumn ?? 1).ConfigureAwait(true);
                return true;

            case EditorNavigationPresentation.SelectAndReveal:
            default:
                if (target.DurationMs is > 0)
                {
                    return dock.TryRevealEditorRange(target.StartLine, target.EndLine, target.DurationMs);
                }

                await dock.GotoLineColumnAsync(
                    target.StartLine,
                    target.StartColumn ?? 1).ConfigureAwait(true);

                if (target.EndLine > target.StartLine || target.EndColumn is not null)
                {
                    var text = dock.GetEditorTextSnapshot();
                    var (startOffset, length) = SelectionOffsetsFromLines(
                        text,
                        target.StartLine,
                        target.StartColumn ?? 1,
                        target.EndLine,
                        target.EndColumn);
                    if (length > 0)
                        await dock.SetSelectionAsync(startOffset, length).ConfigureAwait(true);
                }

                return true;
        }
    }

    private static (int start, int length) SelectionOffsetsFromLines(
        string text,
        int startLine,
        int startColumn,
        int endLine,
        int? endColumn)
    {
        var start = OffsetFromLineColumn(text, startLine, startColumn);
        var endCol = endColumn ?? int.MaxValue;
        var end = OffsetFromLineColumn(text, endLine, endCol, endOfLineIfOverflow: true);
        return (start, Math.Max(0, end - start));
    }

    private static int OffsetFromLineColumn(string text, int lineOneBased, int columnOneBased, bool endOfLineIfOverflow = false)
    {
        if (string.IsNullOrEmpty(text) || lineOneBased < 1)
            return 0;

        var lineStart = 0;
        var line = 1;
        for (var i = 0; i < text.Length && line < lineOneBased; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        if (line != lineOneBased)
            return text.Length;

        var lineEnd = text.IndexOf('\n', lineStart);
        if (lineEnd < 0)
            lineEnd = text.Length;
        var lineLen = lineEnd - lineStart;
        var col = Math.Max(1, columnOneBased);
        if (endOfLineIfOverflow && col > lineLen + 1)
            return lineEnd;
        var offset = lineStart + Math.Min(col - 1, lineLen);
        return Math.Min(offset, text.Length);
    }
}
