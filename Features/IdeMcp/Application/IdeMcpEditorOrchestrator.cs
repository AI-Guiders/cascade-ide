using System.Text.Json;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP editor actions.
/// Keeps MCP payload shaping out of MainWindowViewModel.
/// </summary>
public static class IdeMcpEditorOrchestrator
{
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
