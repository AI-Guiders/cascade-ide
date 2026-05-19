#nullable enable

using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.Models.Intercom;

/// <summary>Канонический якорь вложения Intercom (ADR 0128 §3). Wire/event log — те же имена полей.</summary>
public sealed record AttachmentAnchor
{
    public string? Id { get; init; }

    public string? AttachmentShape { get; init; }

    public string? DisplayLabel { get; init; }

    /// <summary>Workspace-relative или абсолютный путь после resolve @ send.</summary>
    public string? File { get; init; }

    public int? LineStart { get; init; }

    public int? LineEnd { get; init; }

    public string? MemberKey { get; init; }

    /// <summary>JSON-объект syntaxScope (kind, indexInParent, parentMemberKey, …) — v2+.</summary>
    public JsonElement? SyntaxScope { get; init; }

    public string? Excerpt { get; init; }

    public static bool TryParseFromJsonElement(JsonElement root, out AttachmentAnchor anchor, out string error)
    {
        anchor = new AttachmentAnchor();
        error = "";

        if (root.ValueKind != JsonValueKind.Object)
        {
            error = "anchor_json должен быть JSON-объектом.";
            return false;
        }

        anchor = new AttachmentAnchor
        {
            Id = readString(root, "id"),
            AttachmentShape = readString(root, "attachmentShape") ?? readString(root, "attachment_shape"),
            DisplayLabel = readString(root, "displayLabel") ?? readString(root, "display_label"),
            File = readString(root, "file"),
            LineStart = readInt(root, "lineStart") ?? readInt(root, "line_start"),
            LineEnd = readInt(root, "lineEnd") ?? readInt(root, "line_end"),
            MemberKey = readString(root, "memberKey") ?? readString(root, "member_key"),
            SyntaxScope = root.TryGetProperty("syntaxScope", out var ss) ? ss
                : root.TryGetProperty("syntax_scope", out var ss2) ? ss2 : null,
            Excerpt = readString(root, "excerpt"),
        };

        if (string.IsNullOrWhiteSpace(anchor.File))
        {
            error = "В anchor отсутствует file.";
            return false;
        }

        return true;
    }

    public static bool TryParseFlatArgs(
        IReadOnlyDictionary<string, JsonElement>? args,
        out AttachmentAnchor anchor,
        out int? durationMs,
        out string error)
    {
        anchor = new AttachmentAnchor();
        durationMs = null;
        error = "";

        if (args is null)
        {
            error = "Отсутствуют аргументы.";
            return false;
        }

        durationMs = McpCommandJsonArgs.OptionalInt32(args, "duration_ms");

        var file = McpCommandJsonArgs.String(args, "file");
        if (string.IsNullOrWhiteSpace(file))
        {
            error = "Отсутствует file (или передайте anchor_json).";
            return false;
        }

        anchor = new AttachmentAnchor
        {
            Id = McpCommandJsonArgs.String(args, "id"),
            AttachmentShape = McpCommandJsonArgs.String(args, "attachment_shape"),
            DisplayLabel = McpCommandJsonArgs.String(args, "display_label"),
            File = file,
            LineStart = McpCommandJsonArgs.OptionalInt32(args, "line_start"),
            LineEnd = McpCommandJsonArgs.OptionalInt32(args, "line_end"),
            MemberKey = McpCommandJsonArgs.String(args, "member_key"),
            Excerpt = McpCommandJsonArgs.String(args, "excerpt"),
        };

        if (args.TryGetValue("syntax_scope", out var scopeEl) && scopeEl.ValueKind == JsonValueKind.Object)
            anchor = anchor with { SyntaxScope = scopeEl };

        return true;
    }

    public static bool TryParseFlatArgs(IReadOnlyDictionary<string, JsonElement>? args, out AttachmentAnchor anchor, out string error)
    {
        if (!TryParseFlatArgs(args, out anchor, out _, out error))
            return false;
        return true;
    }

    private static string? readString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static int? readInt(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Number || !el.TryGetInt32(out var v))
            return null;
        return v;
    }
}

/// <summary>Разбор <c>intercom.reveal_attachment</c> (ADR 0128 §8, 0130 фаза 1).</summary>
public static class IntercomRevealAttachmentMcpArgs
{
    public static bool TryParse(
        IReadOnlyDictionary<string, JsonElement>? args,
        out AttachmentAnchor anchor,
        out bool? selectExplicit,
        out int? durationMs,
        out string error)
    {
        anchor = new AttachmentAnchor();
        selectExplicit = null;
        durationMs = null;
        error = "";

        if (args is null)
        {
            error = "Отсутствуют аргументы.";
            return false;
        }

        if (args.TryGetValue("anchor_json", out var anchorEl))
        {
            if (!AttachmentAnchor.TryParseFromJsonElement(anchorEl, out anchor, out error))
                return false;
        }
        else if (!AttachmentAnchor.TryParseFlatArgs(args, out anchor, out durationMs, out error))
        {
            return false;
        }

        if (durationMs is null)
            durationMs = McpCommandJsonArgs.OptionalInt32(args, "duration_ms");

        if (args.TryGetValue("select", out var selEl))
        {
            selectExplicit = selEl.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null,
            };
            if (selectExplicit is null)
            {
                error = "select должен быть true или false.";
                return false;
            }
        }

        return true;
    }
}

/// <summary>Args для <c>editor.select_code</c> / <c>editor.reveal_code</c> (ADR 0131).</summary>
public static class EditorCodeRefMcpArgs
{
    public static bool TryParse(
        IReadOnlyDictionary<string, JsonElement>? args,
        out string codeRef,
        out string? activeFile,
        out int? durationMs,
        out string error)
    {
        codeRef = "";
        activeFile = null;
        durationMs = null;
        error = "";

        if (args is null)
        {
            error = "Отсутствуют аргументы.";
            return false;
        }

        codeRef = McpCommandJsonArgs.String(args, "code_ref") ?? "";
        if (string.IsNullOrWhiteSpace(codeRef))
        {
            error = "Отсутствует code_ref.";
            return false;
        }

        activeFile = McpCommandJsonArgs.String(args, "active_file");
        durationMs = McpCommandJsonArgs.OptionalInt32(args, "duration_ms");
        return true;
    }
}
