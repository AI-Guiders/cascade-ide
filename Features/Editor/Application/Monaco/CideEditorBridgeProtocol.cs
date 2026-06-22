using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>cide-editor bridge (ADR 0162 §4).</summary>
public static class CideEditorBridgeTypes
{
    public const string SetModel = "editor/setModel";
    public const string ApplyEdits = "editor/applyEdits";
    public const string SetDecorations = "editor/setDecorations";
    public const string SetTheme = "editor/setTheme";
    public const string DidChange = "editor/didChange";
    public const string DidChangeCursorSelection = "editor/didChangeCursorSelection";
    public const string Ready = "editor/ready";
}

public sealed record CideEditorSetModelMessage(
    string Uri,
    string LanguageId,
    string Text,
    int Version);

public sealed record CideEditorApplyEdit(
    int StartOffset,
    int Length,
    string Text);

public sealed record CideEditorApplyEditsMessage(
    IReadOnlyList<CideEditorApplyEdit> Edits,
    int ExpectedVersion);

public sealed record CideEditorDecoration(
    int StartOffset,
    int Length,
    string ClassName,
    string? HoverMessage);

public sealed record CideEditorSetDecorationsMessage(
    string SetId,
    IReadOnlyList<CideEditorDecoration> Decorations);

public sealed record CideEditorInboundMessage(
    string Type,
    [property: JsonPropertyName("version")] int? Version,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("caretOffset")] int? CaretOffset,
    [property: JsonPropertyName("selectionStart")] int? SelectionStart,
    [property: JsonPropertyName("selectionLength")] int? SelectionLength);

public static class CideEditorBridgeJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string WrapOutbound(string type, object payload) =>
        JsonSerializer.Serialize(new { type, payload }, Options);

    public static CideEditorInboundMessage? TryParseInbound(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeEl))
                return null;
            var type = typeEl.GetString() ?? "";
            int? version = root.TryGetProperty("version", out var v) && v.TryGetInt32(out var vi) ? vi : null;
            string? text = root.TryGetProperty("text", out var t) ? t.GetString() : null;
            int? caret = root.TryGetProperty("caretOffset", out var c) && c.TryGetInt32(out var ci) ? ci : null;
            int? selStart = root.TryGetProperty("selectionStart", out var ss) && ss.TryGetInt32(out var ssi) ? ssi : null;
            int? selLen = root.TryGetProperty("selectionLength", out var sl) && sl.TryGetInt32(out var sli) ? sli : null;
            return new CideEditorInboundMessage(type, version, text, caret, selStart, selLen);
        }
        catch
        {
            return null;
        }
    }
}

public static class CideEditorLanguageIds
{
    public static string FromFilePath(string? filePath)
    {
        var ext = string.IsNullOrWhiteSpace(filePath)
            ? ""
            : Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "csharp",
            ".csx" => "csharp",
            ".json" => "json",
            ".md" => "markdown",
            ".toml" => "ini",
            ".xml" => "xml",
            ".axaml" => "xml",
            ".html" => "html",
            ".htm" => "html",
            ".css" => "css",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".py" => "python",
            ".yaml" or ".yml" => "yaml",
            ".sql" => "sql",
            ".sh" => "shell",
            ".ps1" => "powershell",
            _ => "plaintext",
        };
    }
}

public static class MonacoEditorAssetLocator
{
    public static string GetIndexHtmlPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDir, "Assets", "cide-editor", "index.html");
        if (File.Exists(candidate))
            return candidate;

        // Development: repo layout next to project output.
        var dev = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "cide-editor", "index.html"));
        if (File.Exists(dev))
            return dev;

        return candidate;
    }

    public static Uri GetIndexUri()
    {
        var path = GetIndexHtmlPath();
        return new Uri(new Uri("file:///"), path.Replace('\\', '/'));
    }
}
