using System.Globalization;
using System.Text;
using System.Text.Json;

using CascadeIDE.Models.Editor;

namespace CascadeIDE.Features.WebAiPortal.Application;

/// <summary>
/// Текст для подмешивания в веб-чат/буфер: при лимите длины — сводка и короткие строки команд моста (без JSON), плюс напоминание про json-cascade.
/// </summary>
public static class WebAiPortalChatMixInFormatter
{
    public const int DefaultMaxChatCharacters = 1_200;

    public readonly record struct MixInForComposer(bool UsedCompactMixer, string TextForComposer);

    private static readonly JsonSerializerOptions JsonRelaxed =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions JsonSnakeNaming =
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

    private const string BridgeFooter =
        "Мост: один блок json-cascade; command_id и параметры — как в строках ▸ (см. промпт ADR 0108).";

    private sealed class SearchResultSnap
    {
        public string? Err { get; set; }
        public string? Query { get; set; }
        public List<HitSnap>? Hits { get; set; }
    }

    private sealed class HitSnap
    {
        public long HitId { get; set; }
        public string? Path { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
        public string? Snippet { get; set; }
    }

    private sealed class StatusResultSnap
    {
        public bool DatabaseExists { get; set; }
        public int DocumentCount { get; set; }
        public bool DocumentCountMayBeStale { get; set; }
        public string? IndexedAtIso { get; set; }
        public string? WorkspaceRoot { get; set; }
        public string? LastReindexError { get; set; }
        public EffectiveSnap? EffectiveSettings { get; set; }
    }

    private sealed class EffectiveSnap
    {
        public List<string>? EffectiveExtensions { get; set; }
    }

    /// <summary>Снимок ответа <c>get_editor_content_range</c>: канонический диапазон строк + контент.</summary>
    private sealed record EditorRangeSnap(string? FilePath, LineRange Lines, string Content);

    private sealed class EditorRangeSerdeDto
    {
        public string? FilePath { get; set; }

        public int StartLine { get; set; }

        public int EndLine { get; set; }

        public string? Content { get; set; }
    }

    /// <summary>
    /// Текст для буфера/вставки в композитор. При <paramref name="preferCompact"/> и превышении лимита — компактная подсказка вместо полного ответа.
    /// </summary>
    public static MixInForComposer BuildForComposer(
        bool preferCompact,
        int maxChatCharacters,
        string? executedCommandId,
        string fullResponseText)
    {
        if (!preferCompact || fullResponseText.Length <= maxChatCharacters)
            return new MixInForComposer(false, fullResponseText);

        if (!string.IsNullOrEmpty(executedCommandId)
            && string.Equals(executedCommandId, IdeCommands.CodebaseIndexSearch, StringComparison.Ordinal)
            && TryParseSearchResult(fullResponseText, out var searchSnap)
            && searchSnap is not null)
            return new MixInForComposer(true, BuildCompactAfterCodebaseSearch(searchSnap, fullResponseText.Length));

        if (!string.IsNullOrEmpty(executedCommandId)
            && string.Equals(executedCommandId, IdeCommands.CodebaseIndexStatus, StringComparison.Ordinal)
            && TryParseStatusResult(fullResponseText, out var statusSnap)
            && statusSnap is not null)
            return new MixInForComposer(true, BuildCompactAfterStatus(statusSnap, fullResponseText.Length));

        if (!string.IsNullOrEmpty(executedCommandId)
            && string.Equals(executedCommandId, IdeCommands.GetEditorContentRange, StringComparison.Ordinal))
        {
            if (TryParseEditorRange(fullResponseText, out var rangeSnap) && rangeSnap is not null)
                return new MixInForComposer(true, BuildCompactAfterEditorRange(rangeSnap, fullResponseText.Length));
            if (looksLikeHugePlainTextFallback(fullResponseText))
                return new MixInForComposer(true, BuildCompactPlainTextSnippet(fullResponseText, IdeCommands.GetEditorContentRange));
        }

        if (!string.IsNullOrEmpty(executedCommandId)
            && string.Equals(executedCommandId, IdeCommands.GetEditorState, StringComparison.Ordinal)
            && TryParseEditorState(fullResponseText, out var stParsed)
            && stParsed is { } edited)
            return new MixInForComposer(true, BuildCompactAfterEditorState(edited, fullResponseText.Length));

        return new MixInForComposer(true, BuildGenericOversizedHint(executedCommandId, fullResponseText.Length));
    }

    private static bool looksLikeHugePlainTextFallback(string text)
    {
        if (text.Length <= DefaultMaxChatCharacters || text.TrimStart().StartsWith('{'))
            return false;
        return true;
    }

    private static string BuildCompactPlainTextSnippet(string slab, string? commandId)
    {
        var who = commandId ?? "ответ";
        const int preview = 620;
        var body = NormalizeOneLinePreview(slab, preview);
        return $"""
Сырой текст ({who}) слишком длинный для веб-чата (~{slab.Length} симв.).
Начало (усечено):
---
{body}
---
Узкий диапазон через мост или get_editor_state без превью.

{BridgeLine(IdeCommands.GetEditorState, new Dictionary<string, object?> { ["max_preview_chars"] = 0 })}
{BridgeFooter}
""";
    }

    private static bool TryParseSearchResult(string json, out SearchResultSnap? snap)
    {
        snap = null;
        try
        {
            snap = JsonSerializer.Deserialize<SearchResultSnap>(json, JsonRelaxed);
            return snap is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseStatusResult(string json, out StatusResultSnap? snap)
    {
        snap = null;
        try
        {
            snap = JsonSerializer.Deserialize<StatusResultSnap>(json, JsonRelaxed);
            return snap is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>JSON вида IdeMcpEditorOrchestrator: snake_case ключи или camelCase после стандартного Serialize.</summary>
    private static bool TryParseEditorRange(string json, out EditorRangeSnap? snap)
    {
        snap = null;
        EditorRangeSerdeDto? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<EditorRangeSerdeDto>(json, JsonSnakeNaming);
            if (dto is not null && trySnapFromDto(dto, out snap))
                return true;
            dto = JsonSerializer.Deserialize<EditorRangeSerdeDto>(json, JsonRelaxed);
            if (dto is not null && trySnapFromDto(dto, out snap))
                return true;
        }
        catch
        {
            // пробуем ручной разбор ниже
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!TryGetLengthyString(root, "content", out var contentText) || string.IsNullOrEmpty(contentText))
                return false;
            _ = TryGetString(root, "file_path", out var fp)
                || TryGetString(root, "FilePath", out fp);
            var sl = TryGetInt(root, "start_line", out var s0) ? s0 : (TryGetInt(root, "StartLine", out var s1) ? s1 : 1);
            var el = TryGetInt(root, "end_line", out var e0) ? e0 : (TryGetInt(root, "EndLine", out var e1) ? e1 : sl);
            return tryEditorRangeFromParts(fp, sl, el, contentText, out snap);
        }
        catch
        {
            return false;
        }

        static bool trySnapFromDto(EditorRangeSerdeDto d, out EditorRangeSnap? s) =>
            tryEditorRangeFromParts(d.FilePath, d.StartLine, d.EndLine, d.Content ?? "", out s);

        static bool tryEditorRangeFromParts(string? fp, int startRaw, int endRaw, string content, out EditorRangeSnap? s)
        {
            s = null;
            if (string.IsNullOrEmpty(content))
                return false;
            var lo = Math.Min(startRaw, endRaw);
            var hi = Math.Max(startRaw, endRaw);
            if (!LineNumber.TryCreate(lo, out var lnLo) || !LineNumber.TryCreate(hi, out var lnHi)
                || !LineRange.TryCreate(lnLo, lnHi, out var lines))
                return false;

            s = new EditorRangeSnap(fp, lines, content);
            return true;
        }
    }

    private static bool TryGetString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var e))
            return false;
        value = e.GetString();
        return true;
    }

    private static bool TryGetInt(JsonElement root, string name, out int v)
    {
        v = 0;
        if (!root.TryGetProperty(name, out var e))
            return false;
        return e.TryGetInt32(out v);
    }

    private static bool TryGetLengthyString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var e))
            return false;
        value = e.GetString();
        return !string.IsNullOrEmpty(value);
    }

    private static string BuildCompactAfterCodebaseSearch(SearchResultSnap s, int rawLength)
    {
        var hits = s.Hits ?? [];
        var sb = new StringBuilder(capacity: 512);
        sb.Append($"HCI search ~{rawLength} симв.: сводка; сырой JSON в чат не дублировать.\n");
        sb.Append($"Запрос: \"{TruncateOneLine(s.Query ?? "", 140)}\"\n");
        if (!string.IsNullOrEmpty(s.Err))
            sb.Append($"Ошибка: {TruncateOneLine(s.Err, 200)}\n");
        sb.Append($"Попаданий: {hits.Count}.\n");
        sb.Append("\nСписок (потом один explain на hit_id):\n");

        var n = Math.Min(hits.Count, 12);
        for (var i = 0; i < n; i++)
        {
            var h = hits[i];
            var sn = NormalizeOneLinePreview(h.Snippet, 100);
            sb.Append($"{i + 1}) hit_id={h.HitId}  ");
            sb.Append($"{TruncateOneLine(h.Path ?? "", 90)}");
            sb.Append($"  L{h.LineStart}-{h.LineEnd}");
            if (sn.Length > 0)
                sb.Append($"  ‹{sn}›");
            sb.Append('\n');
        }

        if (hits.Count > n)
            sb.Append($"… ещё {hits.Count - n} (узкий search top_n=4 path_prefix=…).\n");

        var sampleHitId = hits.Count > 0 ? hits[0].HitId : 0L;
        sb.Append("\nДальше один шаг (подставь hit_id из таблицы):\n");
        sb.Append(BridgeLine(IdeCommands.CodebaseIndexExplain, new Dictionary<string, object?> { ["hit_id"] = sampleHitId }));
        sb.Append("(пример по первой строке)\n\n");
        sb.Append("Открытый файл без поиска — ");
        sb.Append(BridgeLine(IdeCommands.GetEditorContentRange, new Dictionary<string, object?> { ["start_line"] = 1, ["end_line"] = 40 }).TrimEnd());
        sb.Append('\n');
        sb.Append(BridgeFooter);
        sb.Append('\n');
        return sb.ToString();
    }

    private static string BuildCompactAfterStatus(StatusResultSnap st, int rawLength)
    {
        var stale = st.DocumentCountMayBeStale ? ", возм. устарел" : "";
        var err = string.IsNullOrEmpty(st.LastReindexError)
            ? ""
            : $"\nОшибка reindex: {TruncateOneLine(st.LastReindexError, 160)}";
        var ws = TruncateOneLine(st.WorkspaceRoot ?? "—", 140);
        var indexed = TruncateOneLine(st.IndexedAtIso ?? "—", 48);
        var extN = st.EffectiveSettings?.EffectiveExtensions?.Count ?? 0;

        return $"""
HCI status ~{rawLength} симв.:
БД {(st.DatabaseExists ? "ok" : "нет")}{stale}; docs {st.DocumentCount}; индекс {indexed}
workspace {ws}; расширений FTS: {extN}{err}

Следующий шаг:
{BridgeLine(IdeCommands.CodebaseIndexSearch, new Dictionary<string, object?> { ["query"] = ".", ["top_n"] = 6 })}
{BridgeFooter}
""";
    }

    private static string BuildCompactAfterEditorRange(EditorRangeSnap range, int rawLength)
    {
        var content = range.Content ?? "";
        const int previewLimit = 700;
        var preview = content.Length <= previewLimit
            ? content
            : content[..previewLimit];
        preview = NormalizeOneLinePreview(preview, preview.Length);
        var path = string.IsNullOrWhiteSpace(range.FilePath) ? "(файл)" : range.FilePath!;
        var start = range.Lines.Start.Value;
        var end = range.Lines.End.Value;
        var half = Math.Max(1, (end - start + 1) / 2);
        var narrowEnd = Math.Min(end, start + half - 1);
        narrowEnd = Math.Max(narrowEnd, start);

        return $"""
Диапазон ~{rawLength} симв.: {path}, L{start}–{end}
Превью (одна строка): {preview}{(content.Length > previewLimit ? $" …+{content.Length - previewLimit}" : "")}

Уже меньше охват:
{BridgeLine(IdeCommands.GetEditorContentRange, new Dictionary<string, object?> { ["start_line"] = start, ["end_line"] = narrowEnd })}
{BridgeFooter}
""";
    }

    private static bool TryParseEditorState(string json, out EditorStateDto? dto)
    {
        dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<EditorStateDto>(json, JsonRelaxed);
            return dto is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildCompactAfterEditorState(EditorStateDto st, int rawLength)
    {
        var path = string.IsNullOrWhiteSpace(st.FilePath) ? "(нет пути)" : st.FilePath!;
        var basename = TryBasename(st.FilePath, out var bn) ? bn : path;
        var queryToken = basename.Length > 48 ? basename[..48] : basename;
        var startLine = Math.Max(1, st.CaretLine - 10);
        var endLine = Math.Max(st.CaretLine, st.CaretLine + 22);

        var sb = new StringBuilder();
        sb.Append($"get_editor_state ~{rawLength} симв.: сводка.\n");
        sb.Append($"Файл: {path}; каретка {st.CaretLine}:{st.CaretColumn}; размер {st.ContentLength}.\n");
        sb.Append("Дальше по одному (можно в этом порядке):\n");
        sb.Append(BridgeLine(IdeCommands.CodebaseIndexStatus, []));
        sb.Append(BridgeLine(IdeCommands.CodebaseIndexSearch, new Dictionary<string, object?> { ["query"] = queryToken, ["top_n"] = 8 }));
        sb.Append(BridgeLine(IdeCommands.GetEditorState, new Dictionary<string, object?> { ["max_preview_chars"] = 0 }));
        sb.Append(BridgeLine(IdeCommands.GetEditorContentRange, new Dictionary<string, object?> { ["start_line"] = startLine, ["end_line"] = endLine }));
        sb.Append('\n');
        sb.Append(BridgeFooter);
        sb.Append('\n');
        return sb.ToString();
    }

    private static string BuildGenericOversizedHint(string? commandId, int rawLength)
    {
        var who = string.IsNullOrEmpty(commandId) ? "ответ" : commandId;

        return $"""
Ответ IDE `{who}` ~{rawLength} симв. — в чат без сырого JSON.

Порциями:
{BridgeLine(IdeCommands.CodebaseIndexStatus, [])}{BridgeLine(IdeCommands.CodebaseIndexSearch, new Dictionary<string, object?> { ["query"] = "символ или тип", ["top_n"] = 8 })}
Редактор без лишнего: {BridgeLine(IdeCommands.GetEditorState, new Dictionary<string, object?> { ["max_preview_chars"] = 0 }).Trim()}
После HCI search: {BridgeLine(IdeCommands.CodebaseIndexExplain, new Dictionary<string, object?> { ["hit_id"] = 123L }).Trim()} (hit_id из таблицы попаданий)

{BridgeFooter}
""";
    }

    /// <summary>Одна строка вида <c>▸ command_id k=v …</c> (без JSON).</summary>
    private static string BridgeLine(string commandId, Dictionary<string, object?> args)
    {
        var sb = new StringBuilder();
        sb.Append("▸ ").Append(commandId);
        foreach (var kv in args.OrderBy(static x => x.Key, StringComparer.Ordinal))
        {
            if (kv.Value is null)
                continue;
            sb.Append(' ').Append(kv.Key).Append('=');
            AppendBridgeArg(sb, kv.Value);
        }

        sb.Append('\n');
        return sb.ToString();
    }

    private static void AppendBridgeArg(StringBuilder sb, object value)
    {
        switch (value)
        {
            case string s:
                if (s.AsSpan().ContainsAny(" \t\"".AsSpan()) || s.Length == 0)
                    sb.Append('"').Append(s.Replace("\"", "\\\"", StringComparison.Ordinal)).Append('"');
                else
                    sb.Append(s);
                break;
            case long l:
                sb.Append(l.ToString(CultureInfo.InvariantCulture));
                break;
            case int i:
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                break;
            case double d:
                sb.Append(d.ToString(CultureInfo.InvariantCulture));
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            default:
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }

    private static bool TryBasename(string? path, out string basename)
    {
        basename = "";
        if (string.IsNullOrWhiteSpace(path))
            return false;
        try
        {
            basename = Path.GetFileNameWithoutExtension(path.Trim()) ?? "";
            return basename.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string TruncateOneLine(string? text, int maxChars) =>
        NormalizeOneLinePreview(text ?? "", maxChars);

    /// <summary>Однострочный препросмотр: перевод строк → пробел, схлопывание пробелов.</summary>
    private static string NormalizeOneLinePreview(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || maxChars <= 0)
            return "";
        var flattened = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', ' ').Replace('\n', ' ');
        flattened = flattened.Replace('\t', ' ');
        while (flattened.Contains("  ", StringComparison.Ordinal))
            flattened = flattened.Replace("  ", " ", StringComparison.Ordinal);
        flattened = flattened.Trim();
        if (flattened.Length <= maxChars)
            return flattened;
        return flattened[..Math.Max(0, maxChars - 1)].TrimEnd() + "…";
    }
}
