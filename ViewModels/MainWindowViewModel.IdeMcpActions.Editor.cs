using CascadeIDE.Features.IdeMcp.Application;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: редактор.</summary>
public partial class MainWindowViewModel
{
    void Services.IIdeMcpActions.OpenFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        var pathCopy = path;
        UiScheduler.Default.Post(() =>
        {
            if (!File.Exists(pathCopy))
                return;
            var normalizedPath = Path.GetFullPath(pathCopy);
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
    }

    void Services.IIdeMcpActions.LoadSolution(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        var pathCopy = path;
        UiScheduler.Default.Post(() => LoadSolution(pathCopy));
    }

    void Services.IIdeMcpActions.SelectInEditor(string? filePath, int startLine, int startColumn, int endLine, int endColumn)
    {
        UiScheduler.Default.Post(() =>
        {
            if (!string.IsNullOrEmpty(filePath) && filePath != CurrentFilePath && File.Exists(filePath))
            {
                IsLoadingCurrentFile = true;
                try
                {
                    Documents.OpenOrActivateDocument(filePath);
                }
                finally { IsLoadingCurrentFile = false; }
            }
            var text = EditorText ?? "";
            int start = EditorTextCoordinateUtilities.LineColumnToOffset(text, startLine, startColumn);
            int end = EditorTextCoordinateUtilities.LineColumnToOffset(text, endLine, endColumn);
            if (start < 0 || end < 0)
                return;
            int len = Math.Max(0, end - start);
            EditorSelectionStart = start;
            EditorSelectionLength = len;
        });
    }

    async Task<string> Services.IIdeMcpActions.GetEditorStateAsync(int? maxPreviewChars)
    {
        var tcs = new TaskCompletionSource<string>();
        var preview = maxPreviewChars ?? 2000;
        UiScheduler.Default.Post(() =>
        {
            try
            {
                var dto = _editorStateProvider?.Invoke(preview) ?? new Services.EditorStateDto();
                tcs.SetResult(IdeMcpEditorOrchestrator.SerializeEditorState(dto));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return await tcs.Task.ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.GetEditorContentRangeAsync(int startLine, int endLine)
    {
        var tcs = new TaskCompletionSource<string>();
        UiScheduler.Default.Post(() =>
        {
            try
            {
                var content = _editorContentRangeProvider?.Invoke(startLine, endLine);
                tcs.SetResult(IdeMcpEditorOrchestrator.SerializeEditorContentRange(CurrentFilePath, startLine, endLine, content));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return await tcs.Task.ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.GetOpenDocumentTextAsync(string? filePath, int? maxChars)
    {
        var tcs = new TaskCompletionSource<string>();
        UiScheduler.Default.Post(() =>
        {
            try
            {
                var target = string.IsNullOrWhiteSpace(filePath) ? CurrentFilePath : filePath.Trim();
                if (string.IsNullOrEmpty(target))
                {
                    tcs.SetResult(IdeMcpEditorOrchestrator.SerializeOpenDocumentMissingPathError());
                    return;
                }

                var doc = FindOpenDocumentModelByPath(target);
                if (doc is null)
                {
                    tcs.SetResult(IdeMcpEditorOrchestrator.SerializeOpenDocumentNotOpenError(target));
                    return;
                }
                tcs.SetResult(IdeMcpEditorOrchestrator.SerializeOpenDocumentText(doc.FilePath, doc.Content, doc.IsDirty, maxChars));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>Модель открытого документа по пути (вкладка в <see cref="Documents.DockDocuments"/>).</summary>
    private OpenDocumentViewModel? FindOpenDocumentModelByPath(string path)
    {
        foreach (var item in Documents.DockDocuments)
        {
            if (item is not DockDocumentViewModel dvm)
                continue;
            if (EditorTextCoordinateUtilities.PathsReferToSameFile(dvm.Doc.FilePath, path))
                return dvm.Doc;
        }

        return null;
    }

    void Services.IIdeMcpActions.ApplyEdit(string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText)
    {
        UiScheduler.Default.Post(() => _applyEditAction?.Invoke(filePath, startLine, startColumn, endLine, endColumn, newText));
    }

    void Services.IIdeMcpActions.GoToPosition(string? filePath, int line, int column, int? endLine, int? endColumn)
    {
        ((Services.IIdeMcpActions)this).SelectInEditor(filePath, line, column, endLine ?? line, endColumn ?? column);
    }
}
