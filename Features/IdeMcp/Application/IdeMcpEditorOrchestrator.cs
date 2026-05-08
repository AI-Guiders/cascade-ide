using System.Text.Json;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP editor actions.
/// Keeps MCP payload shaping out of MainWindowViewModel.
/// </summary>
public static class IdeMcpEditorOrchestrator
{
    /// <summary>Снимок открытой вкладки для <see cref="BuildGetOpenDocumentTextResponse"/> (без зависимости от ViewModels).</summary>
    public readonly record struct OpenDocumentTabSnapshot(string FilePath, string Content, bool IsDirty);

    /// <summary>
    /// Разрешить путь, найти вкладку по тем же правилам, что и обозреватель, вернуть JSON для MCP <c>get_open_document_text</c>.
    /// </summary>
    public static string BuildGetOpenDocumentTextResponse(
        string? filePathArgument,
        string? currentFilePath,
        IReadOnlyList<OpenDocumentTabSnapshot> openTabs,
        int? maxChars)
    {
        var target = string.IsNullOrWhiteSpace(filePathArgument) ? currentFilePath : filePathArgument.Trim();
        if (string.IsNullOrEmpty(target))
            return SerializeOpenDocumentMissingPathError();

        foreach (var tab in openTabs)
        {
            if (EditorTextCoordinateUtilities.PathsReferToSameFile(tab.FilePath, target))
                return SerializeOpenDocumentText(tab.FilePath, tab.Content, tab.IsDirty, maxChars);
        }

        return SerializeOpenDocumentNotOpenError(target);
    }

    /// <summary>
    /// Maps 1-based line/column range to a selection span in <paramref name="editorText"/>.
    /// Returns false if either offset is invalid (e.g. out of range).
    /// </summary>
    public static bool TryComputeSelectionSpan(
        string? editorText,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        out int selectionStart,
        out int selectionLength)
    {
        var text = editorText ?? "";
        var start = EditorTextCoordinateUtilities.LineColumnToOffset(text, startLine, startColumn);
        var end = EditorTextCoordinateUtilities.LineColumnToOffset(text, endLine, endColumn);
        if (start < 0 || end < 0)
        {
            selectionStart = 0;
            selectionLength = 0;
            return false;
        }

        selectionStart = start;
        selectionLength = Math.Max(0, end - start);
        return true;
    }

    public static string SerializeEditorState(Services.EditorStateDto dto) =>
        JsonSerializer.Serialize(dto);

    public static string SerializeEditorContentRange(string? filePath, int startLine, int endLine, string? content) =>
        JsonSerializer.Serialize(new
        {
            file_path = filePath,
            start_line = startLine,
            end_line = endLine,
            content = content ?? ""
        });

    public static string SerializeOpenDocumentMissingPathError() =>
        JsonSerializer.Serialize(new { error = "no_path", message = "file_path не задан и нет текущего открытого файла." });

    public static string SerializeOpenDocumentNotOpenError(string filePathRequested) =>
        JsonSerializer.Serialize(new
        {
            error = "not_open",
            message = "Файл не среди открытых вкладок.",
            file_path_requested = filePathRequested
        });

    public static string SerializeOpenDocumentText(
        string? filePath,
        string? fullText,
        bool isDirty,
        int? maxChars)
    {
        var source = fullText ?? "";
        var len = source.Length;
        var truncated = false;
        var text = source;
        if (maxChars is > 0 && len > maxChars.Value)
        {
            text = source[..maxChars.Value];
            truncated = true;
        }

        return JsonSerializer.Serialize(new
        {
            file_path = filePath,
            length = len,
            truncated,
            is_dirty = isDirty,
            text
        });
    }
}
