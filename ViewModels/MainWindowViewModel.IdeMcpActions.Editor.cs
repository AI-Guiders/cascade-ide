using System.IO;
using System.Text.Json;
using Avalonia.Threading;
using Dock.Model.Mvvm.Controls;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    void Services.IIdeMcpActions.OpenFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        var pathCopy = path;
        Dispatcher.UIThread.Post(() =>
        {
            if (!File.Exists(pathCopy))
                return;
            var normalizedPath = Path.GetFullPath(pathCopy);
            IsLoadingCurrentFile = true;
            try
            {
                OpenOrActivateDocument(normalizedPath);
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
        Dispatcher.UIThread.Post(() => LoadSolution(pathCopy));
    }

    void Services.IIdeMcpActions.SelectInEditor(string? filePath, int startLine, int startColumn, int endLine, int endColumn)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!string.IsNullOrEmpty(filePath) && filePath != CurrentFilePath && File.Exists(filePath))
            {
                IsLoadingCurrentFile = true;
                try
                {
                    OpenOrActivateDocument(filePath);
                }
                finally { IsLoadingCurrentFile = false; }
            }
            var text = EditorText ?? "";
            int start = LineColumnToOffset(text, startLine, startColumn);
            int end = LineColumnToOffset(text, endLine, endColumn);
            if (start < 0 || end < 0)
                return;
            int len = Math.Max(0, end - start);
            EditorSelectionStart = start;
            EditorSelectionLength = len;
        });
    }

    private static int LineColumnToOffset(string text, int line, int column)
    {
        if (line < 1 || column < 1)
            return -1;
        var lines = text.Split('\n');
        if (line > lines.Length)
            return -1;
        int offset = 0;
        for (int i = 0; i < line - 1; i++)
            offset += lines[i].Length + 1; // +1 for \n
        int lineLen = lines[line - 1].Length;
        int col = Math.Min(column, lineLen + 1);
        return offset + (col - 1);
    }

    async Task<string> Services.IIdeMcpActions.GetEditorStateAsync(int? maxPreviewChars)
    {
        var tcs = new TaskCompletionSource<string>();
        var preview = maxPreviewChars ?? 2000;
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var dto = _editorStateProvider?.Invoke(preview) ?? new Services.EditorStateDto();
                tcs.SetResult(JsonSerializer.Serialize(dto));
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
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var content = _editorContentRangeProvider?.Invoke(startLine, endLine);
                var obj = new
                {
                    file_path = CurrentFilePath,
                    start_line = startLine,
                    end_line = endLine,
                    content = content ?? ""
                };
                tcs.SetResult(JsonSerializer.Serialize(obj));
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
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var target = string.IsNullOrWhiteSpace(filePath) ? CurrentFilePath : filePath.Trim();
                if (string.IsNullOrEmpty(target))
                {
                    tcs.SetResult(JsonSerializer.Serialize(new { error = "no_path", message = "file_path не задан и нет текущего открытого файла." }));
                    return;
                }

                var doc = FindOpenDocumentModelByPath(target);
                if (doc is null)
                {
                    tcs.SetResult(JsonSerializer.Serialize(new
                    {
                        error = "not_open",
                        message = "Файл не среди открытых вкладок.",
                        file_path_requested = target
                    }));
                    return;
                }

                var fullText = doc.Content ?? "";
                var len = fullText.Length;
                var truncated = false;
                var outText = fullText;
                if (maxChars is > 0 && len > maxChars.Value)
                {
                    outText = fullText[..maxChars.Value];
                    truncated = true;
                }

                tcs.SetResult(JsonSerializer.Serialize(new
                {
                    file_path = doc.FilePath,
                    length = len,
                    truncated,
                    is_dirty = doc.IsDirty,
                    text = outText
                }));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>Модель открытого документа по пути (вкладка в <see cref="DockDocuments"/>).</summary>
    private OpenDocumentViewModel? FindOpenDocumentModelByPath(string path)
    {
        foreach (var item in DockDocuments)
        {
            if (item is not DockDocumentViewModel dvm)
                continue;
            if (PathsReferToSameFile(dvm.Doc.FilePath, path))
                return dvm.Doc;
        }

        return null;
    }

    private static bool PathsReferToSameFile(string a, string b)
    {
        try
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    void Services.IIdeMcpActions.ApplyEdit(string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText)
    {
        Dispatcher.UIThread.Post(() => _applyEditAction?.Invoke(filePath, startLine, startColumn, endLine, endColumn, newText));
    }

    void Services.IIdeMcpActions.GoToPosition(string? filePath, int line, int column, int? endLine, int? endColumn)
    {
        ((Services.IIdeMcpActions)this).SelectInEditor(filePath, line, column, endLine ?? line, endColumn ?? column);
    }
}
