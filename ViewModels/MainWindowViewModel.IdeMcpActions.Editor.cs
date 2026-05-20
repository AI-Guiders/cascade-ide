using CascadeIDE.Features.IdeMcp.Application;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: редактор.</summary>
public partial class MainWindowViewModel
{
    void Services.IIdeMcpActions.OpenFile(string path) =>
        IdeMcpEditorDocumentActions.ScheduleOpenFile(
            UiScheduler.Default,
            path,
            normalizedPath =>
            {
                IsLoadingCurrentFile = true;
                try
                {
                    Documents.OpenOrActivateDocument(normalizedPath);
                }
                finally
                {
                    IsLoadingCurrentFile = false;
                }
            });

    void Services.IIdeMcpActions.LoadSolution(string path) =>
        IdeMcpEditorDocumentActions.ScheduleLoadSolution(UiScheduler.Default, path, LoadSolution);

    async Task<string> Services.IIdeMcpActions.LoadSolutionAndWaitAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LoadSolutionAsync(path).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await UiScheduler.Default.InvokeAsync(() =>
        {
            if (!string.IsNullOrWhiteSpace(Workspace.SolutionLoadError))
                return Workspace.SolutionLoadError.Trim();
            if (string.IsNullOrWhiteSpace(Workspace.SolutionPath) || Workspace.SolutionRoots.Count == 0)
                return "Не удалось загрузить решение.";
            return "OK";
        }).ConfigureAwait(false);
    }

    void Services.IIdeMcpActions.SelectInEditor(string? filePath, int startLine, int startColumn, int endLine, int endColumn)
    {
        UiScheduler.Default.Post(() =>
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var normalized = CanonicalFilePath.Normalize(filePath);
                if (!CanonicalFilePath.Equals(CurrentFilePath, normalized) && File.Exists(normalized))
                {
                    IsLoadingCurrentFile = true;
                    try
                    {
                        Documents.OpenOrActivateDocument(normalized);
                    }
                    finally { IsLoadingCurrentFile = false; }
                }
            }
            var text = EditorText ?? "";
            if (!IdeMcpEditorOrchestrator.TryComputeSelectionSpan(
                    text, startLine, startColumn, endLine, endColumn, out var start, out var len))
                return;
            EditorSelectionStart = start;
            EditorSelectionLength = len;
        });
    }

    async Task<string> Services.IIdeMcpActions.GetEditorStateAsync(int? maxPreviewChars)
    {
        var preview = maxPreviewChars ?? 2000;
        return await UiScheduler.Default.InvokeAsync(() =>
                IdeMcpEditorOrchestrator.SerializeEditorState(
                    _editorStateProvider?.Invoke(preview) ?? new Services.EditorStateDto()))
            .ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.GetEditorContentRangeAsync(int startLine, int endLine)
    {
        return await UiScheduler.Default.InvokeAsync(() =>
            {
                var content = _editorContentRangeProvider?.Invoke(startLine, endLine);
                return IdeMcpEditorOrchestrator.SerializeEditorContentRange(
                    CurrentFilePath, startLine, endLine, content);
            })
            .ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.GetOpenDocumentTextAsync(string? filePath, int? maxChars)
    {
        return await UiScheduler.Default.InvokeAsync(() =>
            {
                var tabs = Documents.CollectIdeMcpOpenDocumentTabSnapshots();
                return IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse(
                    filePath, CurrentFilePath, tabs, maxChars);
            })
            .ConfigureAwait(false);
    }

    void Services.IIdeMcpActions.ApplyEdit(string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText)
    {
        UiScheduler.Default.Post(() => _applyEditAction?.Invoke(filePath, startLine, startColumn, endLine, endColumn, newText));
    }

    void Services.IIdeMcpActions.GoToPosition(string? filePath, int line, int column, int? endLine, int? endColumn)
    {
        ((Services.IIdeMcpActions)this).SelectInEditor(filePath, line, column, endLine ?? line, endColumn ?? column);
    }

    void Services.IIdeMcpActions.RevealEditorRange(string? filePath, int startLine, int endLine, int? durationMs)
    {
        UiScheduler.Default.Post(() =>
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var normalized = CanonicalFilePath.Normalize(filePath);
                if (!CanonicalFilePath.Equals(CurrentFilePath, normalized) && File.Exists(normalized))
                {
                    IsLoadingCurrentFile = true;
                    try
                    {
                        Documents.OpenOrActivateDocument(normalized);
                    }
                    finally
                    {
                        IsLoadingCurrentFile = false;
                    }
                }
            }

            var path = string.IsNullOrEmpty(filePath) ? CurrentFilePath : CanonicalFilePath.Normalize(filePath);
            _revealEditorRangeAction?.Invoke(path, startLine, endLine, durationMs);
        });
    }
}
