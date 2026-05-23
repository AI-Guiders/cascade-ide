using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    public void OpenFile(string path) =>
        IdeMcpEditorDocumentOrchestrator.ScheduleOpenFile(
            UiScheduler.Default,
            path,
            normalizedPath =>
            {
                _host.IsLoadingCurrentFile = true;
                try
                {
                    _host.Documents.OpenOrActivateDocument(normalizedPath);
                }
                finally
                {
                    _host.IsLoadingCurrentFile = false;
                }
            });

    public void LoadSolution(string path) =>
        IdeMcpEditorDocumentOrchestrator.ScheduleLoadSolution(UiScheduler.Default, path, LoadSolution);

    public async Task<string> LoadSolutionAndWaitAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _host.LoadSolutionAsync(path).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await UiScheduler.Default.InvokeAsync(() =>
        {
            if (!string.IsNullOrWhiteSpace(_host.Workspace.SolutionLoadError))
                return _host.Workspace.SolutionLoadError.Trim();
            if (string.IsNullOrWhiteSpace(_host.Workspace.SolutionPath) || _host.Workspace.SolutionRoots.Count == 0)
                return "РќРµ СѓРґР°Р»РѕСЃСЊ Р·Р°РіСЂСѓР·РёС‚СЊ СЂРµС€РµРЅРёРµ.";
            return "OK";
        }).ConfigureAwait(false);
    }

    public void SelectInEditor(string? filePath, int startLine, int startColumn, int endLine, int endColumn)
    {
        UiScheduler.Default.Post(() =>
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var normalized = CanonicalFilePath.Normalize(filePath);
                if (!CanonicalFilePath.Equals(_host.CurrentFilePath, normalized) && File.Exists(normalized))
                {
                    _host.IsLoadingCurrentFile = true;
                    try
                    {
                        _host.Documents.OpenOrActivateDocument(normalized);
                    }
                    finally { _host.IsLoadingCurrentFile = false; }
                }
            }
            var text = _host.EditorText ?? "";
            if (!IdeMcpEditorOrchestrator.TryComputeSelectionSpan(
                    text, startLine, startColumn, endLine, endColumn, out var start, out var len))
                return;
            _host.EditorSelectionStart = start;
            _host.EditorSelectionLength = len;
        });
    }

    public async Task<string> GetEditorStateAsync(int? maxPreviewChars)
    {
        var preview = maxPreviewChars ?? 2000;
        return await UiScheduler.Default.InvokeAsync(() =>
                IdeMcpEditorOrchestrator.SerializeEditorState(
                    _host.McpEditorStateProvider?.Invoke(preview) ?? new Services.EditorStateDto()))
            .ConfigureAwait(false);
    }

    public async Task<string> GetEditorContentRangeAsync(int startLine, int endLine)
    {
        return await UiScheduler.Default.InvokeAsync(() =>
            {
                var content = _host.McpEditorContentRangeProvider?.Invoke(startLine, endLine);
                return IdeMcpEditorOrchestrator.SerializeEditorContentRange(
                    _host.CurrentFilePath, startLine, endLine, content);
            })
            .ConfigureAwait(false);
    }

    public async Task<string> GetOpenDocumentTextAsync(string? filePath, int? maxChars)
    {
        return await UiScheduler.Default.InvokeAsync(() =>
            {
                var tabs = _host.Documents.CollectIdeMcpOpenDocumentTabSnapshots();
                return IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse(
                    filePath, _host.CurrentFilePath, tabs, maxChars);
            })
            .ConfigureAwait(false);
    }

    public void ApplyEdit(string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText)
    {
        UiScheduler.Default.Post(() => _host.McpApplyEditAction?.Invoke(filePath, startLine, startColumn, endLine, endColumn, newText));
    }

    public void GoToPosition(string? filePath, int line, int column, int? endLine, int? endColumn)
    {
        this.SelectInEditor(filePath, line, column, endLine ?? line, endColumn ?? column);
    }

    public void RevealEditorRange(string? filePath, int startLine, int endLine, int? durationMs)
    {
        UiScheduler.Default.Post(() =>
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var normalized = CanonicalFilePath.Normalize(filePath);
                if (!CanonicalFilePath.Equals(_host.CurrentFilePath, normalized) && File.Exists(normalized))
                {
                    _host.IsLoadingCurrentFile = true;
                    try
                    {
                        _host.Documents.OpenOrActivateDocument(normalized);
                    }
                    finally
                    {
                        _host.IsLoadingCurrentFile = false;
                    }
                }
            }

            var path = string.IsNullOrEmpty(filePath) ? _host.CurrentFilePath : CanonicalFilePath.Normalize(filePath);
            _host.McpRevealEditorRangeAction?.Invoke(path, startLine, endLine, durationMs);
        });
    }

}
