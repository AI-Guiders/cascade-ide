using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>cide-editor bridge (ADR 0162 §4).</summary>
public static class CideEditorBridgeTypes
{
    public const string SetModel = "editor/setModel";
    public const string ApplyEdits = "editor/applyEdits";
    public const string SetDecorations = "editor/setDecorations";
    public const string SetTheme = "editor/setTheme";
    public const string SetStickyScroll = "editor/setStickyScroll";
    public const string SetGutterGlyphs = "editor/setGutterGlyphs";
    public const string SetIntelligence = "editor/setIntelligence";
    public const string RevealRange = "editor/revealRange";
    public const string SetSelectionByOffset = "editor/setSelectionByOffset";
    public const string SetAgentReveal = "editor/setAgentReveal";
    public const string ClearAgentReveal = "editor/clearAgentReveal";
    public const string SetEpochDim = "editor/setEpochDim";

    public const string DidChange = "editor/didChange";
    public const string DidChangeCursorSelection = "editor/didChangeCursorSelection";
    public const string DidScroll = "editor/didScroll";
    public const string DidGutterClick = "editor/didGutterClick";
    public const string Ready = "editor/ready";

    /// <summary>Editor → host: tunnel hotkey from WebView2 (Ctrl+P etc.).</summary>
    public const string HostShortcut = "host/shortcut";

    /// <summary>Alt+drag selection released — drop target hit-test on host (Intercom composer).</summary>
    public const string HostAttachDragComplete = "host/attach-drag-complete";

    public const string RequestCompletion = "editor/requestCompletion";
    public const string CompletionResult = "editor/completionResult";
    public const string RequestHover = "editor/requestHover";
    public const string HoverResult = "editor/hoverResult";
    public const string RequestSignature = "editor/requestSignature";
    public const string SignatureResult = "editor/signatureResult";
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

/// <summary>
/// Decoration push DTO. Prefer <see cref="StartLine"/> for whole-line layers (diagnostics);
/// use <see cref="StartOffset"/> for token spans (highlights). See monaco-presentation-projection-v1.md.
/// </summary>
public sealed record CideEditorDecoration(
    int StartOffset,
    int Length,
    string ClassName,
    string? HoverMessage,
    bool IsWholeLine = false,
    string? GlyphMarginClassName = null,
    int? StartLine = null,
    int? StartColumn = null,
    int? EndLine = null,
    int? EndColumn = null);

public sealed record CideEditorSetSelectionMessage(int SelectionStart, int SelectionLength);

public sealed record CideEditorSetAgentRevealMessage(
    int StartLine,
    int EndLine,
    bool Persistent,
    int? DurationMs);

public sealed record CideEditorSetEpochDimMessage(bool Dimmed);

public sealed record CideEditorSetDecorationsMessage(
    string SetId,
    IReadOnlyList<CideEditorDecoration> Decorations,
    int? ExpectedModelVersion = null);

public sealed record CideEditorStickyScrollMessage(string? Label);

public sealed record CideEditorGutterGlyph(
    int LineOneBased,
    string TextGlyph,
    string? ToolTip,
    string VisualKind);

public sealed record CideEditorSetGutterGlyphsMessage(
    IReadOnlyList<CideEditorGutterGlyph> Glyphs);

public sealed record CideEditorSetIntelligenceMessage(bool Enabled);

public sealed record CideEditorRevealRangeMessage(
    int StartLine,
    int EndLine,
    int? Column,
    bool Select = true);

public sealed record CideEditorDefinitionLocation(
    string FilePath,
    int Line,
    int Column);

public sealed record CideEditorReferenceLocation(
    string FilePath,
    int Line,
    int Column,
    int? EndLine = null,
    int? EndColumn = null);

public sealed record CideEditorReferencesResultMessage(
    int RequestId,
    IReadOnlyList<CideEditorReferenceLocation> Locations);

public sealed record CideEditorFormatResultMessage(
    int RequestId,
    string? Text);

public sealed record CideEditorCodeActionItem(
    string Title,
    string Kind,
    string? Text,
    int? ActionIndex = null);

public sealed record CideEditorDocumentTextChange(
    string FilePath,
    string Text,
    bool IsNewFile = false,
    string? PreviousFilePath = null);

public sealed record CideEditorWorkspaceEditResultMessage(
    int RequestId,
    bool Ok,
    string? Error,
    IReadOnlyList<CideEditorDocumentTextChange> Changes);

public sealed record CideEditorCodeActionResultMessage(
    int RequestId,
    IReadOnlyList<CideEditorCodeActionItem> Actions);

public sealed record CideEditorDefinitionResultMessage(
    int RequestId,
    CideEditorDefinitionLocation? Location);

public sealed record CideEditorSetThemeMessage(string ThemeName);

public sealed record CideEditorCompletionItem(
    string Label,
    string InsertText,
    string? Detail,
    string? Kind = null);

public sealed record CideEditorCompletionResultMessage(
    int RequestId,
    IReadOnlyList<CideEditorCompletionItem> Items);

public sealed record CideEditorHoverResultMessage(
    int RequestId,
    string? Markdown);

public sealed record CideEditorSignatureResultMessage(
    int RequestId,
    string? Signature);

public sealed record CideEditorInlayHint(
    int Line,
    int Column,
    string Label,
    string Kind = "type",
    bool AtEndOfLine = false);

public sealed record CideEditorInlayHintsResultMessage(
    int RequestId,
    IReadOnlyList<CideEditorInlayHint> Hints);

public sealed record CideEditorCodeLensItem(
    string Id,
    int Line,
    int Column,
    string Title);

public sealed record CideEditorCodeLensResultMessage(
    int RequestId,
    IReadOnlyList<CideEditorCodeLensItem> Lenses);

public sealed record CideEditorSemanticTokensLegend(
    IReadOnlyList<string> TokenTypes,
    IReadOnlyList<string> TokenModifiers);

public sealed record CideEditorSemanticTokensData(
    IReadOnlyList<uint> Data,
    string? ResultId);

public sealed record CideEditorSemanticTokensResultMessage(
    int RequestId,
    IReadOnlyList<uint> Data,
    string? ResultId);

public sealed record CideEditorInboundMessage(
    string Type,
    [property: JsonPropertyName("version")] int? Version,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("caretOffset")] int? CaretOffset,
    [property: JsonPropertyName("selectionStart")] int? SelectionStart,
    [property: JsonPropertyName("selectionLength")] int? SelectionLength,
    [property: JsonPropertyName("requestId")] int? RequestId,
    [property: JsonPropertyName("line")] int? Line,
    [property: JsonPropertyName("column")] int? Column,
    [property: JsonPropertyName("topLine")] int? TopLine,
    [property: JsonPropertyName("lensId")] string? LensId,
    [property: JsonPropertyName("filePath")] string? FilePath,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("actionIndex")] int? ActionIndex,
    [property: JsonPropertyName("newName")] string? NewName,
    [property: JsonPropertyName("endLine")] int? EndLine,
    [property: JsonPropertyName("endColumn")] int? EndColumn,
    [property: JsonPropertyName("id")] string? ShortcutId);

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
            int? version = TryInt(root, "version");
            string? text = root.TryGetProperty("text", out var t) ? t.GetString() : null;
            int? caret = TryInt(root, "caretOffset");
            int? selStart = TryInt(root, "selectionStart");
            int? selLen = TryInt(root, "selectionLength");
            int? requestId = TryInt(root, "requestId");
            int? line = TryInt(root, "line");
            int? column = TryInt(root, "column");
            int? topLine = TryInt(root, "topLine");
            string? lensId = root.TryGetProperty("lensId", out var lens) ? lens.GetString() : null;
            string? filePath = root.TryGetProperty("filePath", out var fp) ? fp.GetString() : null;
            string? error = root.TryGetProperty("error", out var err) ? err.GetString() : null;
            int? actionIndex = TryInt(root, "actionIndex");
            string? newName = root.TryGetProperty("newName", out var nn) ? nn.GetString() : null;
            int? endLine = TryInt(root, "endLine");
            int? endColumn = TryInt(root, "endColumn");
            string? shortcutId = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            return new CideEditorInboundMessage(
                type, version, text, caret, selStart, selLen, requestId, line, column, topLine, lensId, filePath, error,
                actionIndex, newName, endLine, endColumn, shortcutId);
        }
        catch
        {
            return null;
        }
    }

    private static int? TryInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.TryGetInt32(out var value) ? value : null;
}

public static class CideEditorLanguageIds
{
    public static string FromFilePath(string? filePath) =>
        EditorLanguageSupport.GetMonacoLanguageId(filePath);

    public static bool SupportsRoslynIntelligence(string? filePath) =>
        string.Equals(Path.GetExtension(filePath), ".cs", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Path.GetExtension(filePath), ".csx", StringComparison.OrdinalIgnoreCase);
}

public static class MonacoEditorAssetLocator
{
    public const string VirtualHostName = "cide-editor.local";

    public static string GetCideEditorRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDir, "Assets", "cide-editor");
        if (Directory.Exists(candidate))
            return candidate;

        var dev = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "cide-editor"));
        if (Directory.Exists(dev))
            return dev;

        return candidate;
    }

    public static string GetIndexHtmlPath()
    {
        var path = Path.Combine(GetCideEditorRoot(), "index.html");
        return File.Exists(path) ? path : path;
    }

    public static string GetMonacoVsPath()
    {
        var local = Path.Combine(GetCideEditorRoot(), "monaco", "min", "vs");
        return Directory.Exists(local) ? local.Replace('\\', '/') : "";
    }

    public static Uri GetIndexUri() =>
        new($"https://{VirtualHostName}/index.html");

    public static Uri GetFileIndexUri()
    {
        var path = Path.GetFullPath(GetIndexHtmlPath());
        return new Uri(path);
    }
}

