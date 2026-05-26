#nullable enable

using System.Text.RegularExpressions;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>Разбор code-ref хвоста find/relate (selection, L:, bracket, bare M:).</summary>
public static class IntercomCodeRefParser
{
    private static readonly Regex CsFileBeforeMember = new(
        @"(?<file>[^\s\[\]]+\.cs)\s+M:",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryParse(
        string? tail,
        in IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out IntercomCodeRefQuery query,
        out string error,
        string? indexDirectoryRelative = null)
    {
        query = new IntercomCodeRefQuery("", null, null);
        error = "";

        var text = (tail ?? "").Trim();
        if (text.Length == 0)
        {
            error = "Укажи фрагмент кода: selection, L:10-20, [M:…] или M:Member.";
            return false;
        }

        if (string.Equals(text, "selection", StringComparison.OrdinalIgnoreCase))
            return fromSelection(editor, workspaceRoot, solutionPath, out query, out error);

        if (text.StartsWith("L:", StringComparison.OrdinalIgnoreCase))
            return fromLineLiteral(text[2..].Trim(), editor, out query, out error);

        if (text.StartsWith("[", StringComparison.Ordinal) || looksLikeBareBracketAxes(text))
        {
            var bracketText = text.StartsWith("[", StringComparison.Ordinal) ? text : $"[{text}]";
            return fromBracket(
                bracketText,
                editor,
                workspaceRoot,
                solutionPath,
                indexDirectoryRelative,
                out query,
                out error);
        }

        error = "Ожидается selection, L:строки, [M:…] или M:Member.";
        return false;
    }

    /// <summary>Resolve code-ref в канонический <see cref="AttachmentAnchor"/> для event log relate (ADR 0137).</summary>
    public static bool TryResolveAnchor(
        string? tail,
        in IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out AttachmentAnchor anchor,
        out string error,
        string? indexDirectoryRelative = null)
    {
        anchor = new AttachmentAnchor();
        if (!TryParse(tail, editor, workspaceRoot, solutionPath, out var query, out error, indexDirectoryRelative))
            return false;

        if (query.ResolvedAnchor is { } resolved)
        {
            anchor = ensureAnchorId(resolved);
            return true;
        }

        if (string.IsNullOrWhiteSpace(query.File))
        {
            error = "У ссылки нет file.";
            return false;
        }

        anchor = ensureAnchorId(new AttachmentAnchor
        {
            AttachmentShape = "text-range",
            DisplayLabel = query.HasLineRange
                ? $"{query.File} L{query.LineStart}-{query.LineEnd}"
                : query.File,
            File = query.File.Replace('\\', '/'),
            LineStart = query.LineStart,
            LineEnd = query.LineEnd,
            ResolvedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
            ResolveOutcome = "resolved",
        });
        return true;
    }

    public static bool TryResolveAnchorFromMcp(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? args,
        in IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out AttachmentAnchor anchor,
        out string error,
        string? indexDirectoryRelative = null)
    {
        anchor = new AttachmentAnchor();
        error = "";

        if (args is not null && args.TryGetValue("anchor_json", out var anchorEl))
        {
            if (!AttachmentAnchor.TryParseFromJsonElement(anchorEl, out anchor, out error))
                return false;

            anchor = ensureAnchorId(anchor);
            return true;
        }

        return TryResolveAnchorFromMcpQuery(
            args,
            editor,
            workspaceRoot,
            solutionPath,
            indexDirectoryRelative,
            out anchor,
            out error);
    }

    private static bool TryResolveAnchorFromMcpQuery(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? args,
        in IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        string? indexDirectoryRelative,
        out AttachmentAnchor anchor,
        out string error)
    {
        if (!TryParseFromMcp(args, editor, workspaceRoot, solutionPath, out var query, out error, indexDirectoryRelative))
        {
            anchor = new AttachmentAnchor();
            return false;
        }

        return TryResolveAnchorFromQuery(query, out anchor, out error);
    }

    private static bool TryResolveAnchorFromQuery(
        IntercomCodeRefQuery query,
        out AttachmentAnchor anchor,
        out string error) =>
        query.ResolvedAnchor is { } resolved
            ? TryResolveAnchorFromResolved(resolved, out anchor, out error)
            : TryResolveAnchorFromPositionalQuery(query, out anchor, out error);

    private static bool TryResolveAnchorFromResolved(
        AttachmentAnchor resolved,
        out AttachmentAnchor anchor,
        out string error)
    {
        error = "";
        anchor = ensureAnchorId(resolved);
        return true;
    }

    private static bool TryResolveAnchorFromPositionalQuery(
        IntercomCodeRefQuery query,
        out AttachmentAnchor anchor,
        out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(query.File))
        {
            error = "У ссылки нет file.";
            anchor = new AttachmentAnchor();
            return false;
        }

        anchor = ensureAnchorId(new AttachmentAnchor
        {
            AttachmentShape = "text-range",
            DisplayLabel = query.HasLineRange
                ? $"{query.File} L{query.LineStart}-{query.LineEnd}"
                : query.File,
            File = query.File.Replace('\\', '/'),
            LineStart = query.LineStart,
            LineEnd = query.LineEnd,
            ResolvedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
            ResolveOutcome = "resolved",
        });
        return true;
    }

    private static AttachmentAnchor ensureAnchorId(AttachmentAnchor anchor) =>
        string.IsNullOrWhiteSpace(anchor.Id)
            ? anchor with { Id = Guid.NewGuid().ToString("N") }
            : anchor;

    private static bool fromSelection(
        in IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out IntercomCodeRefQuery query,
        out string error)
    {
        if (!IntercomAttachmentResolveAtSend.TryResolveSelection(editor, workspaceRoot, solutionPath, out var anchor, out error))
        {
            query = new IntercomCodeRefQuery("", null, null);
            return false;
        }

        return fromAnchor(anchor, out query, out error);
    }

    private static bool fromLineLiteral(
        string lineTail,
        in IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        out IntercomCodeRefQuery query,
        out string error)
    {
        query = new IntercomCodeRefQuery("", null, null);
        error = "";

        if (string.IsNullOrWhiteSpace(editor.CurrentFilePath))
        {
            error = "Нет активного файла в редакторе.";
            return false;
        }

        if (!tryParseLineRangeTail(lineTail, out var start, out var end, out error))
            return false;

        var file = AttachmentAnchorPaths.ToWorkspaceRelative(editor.CurrentFilePath, null)
                   ?? editor.CurrentFilePath.Replace('\\', '/');
        var anchor = new AttachmentAnchor
        {
            AttachmentShape = "text-range",
            DisplayLabel = start == end ? $"{file} L{start}" : $"{file} L{start}-{end}",
            File = file,
            LineStart = start,
            LineEnd = end,
            ResolvedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
            ResolveOutcome = "resolved",
        };
        query = IntercomCodeRefQuery.FromAnchor(ensureAnchorId(anchor));
        return true;
    }

    private static bool fromBracket(
        string text,
        in IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        string? indexDirectoryRelative,
        out IntercomCodeRefQuery query,
        out string error)
    {
        if (!BracketCodeReferenceParser.TryParse(text, out var reference, out error))
        {
            query = new IntercomCodeRefQuery("", null, null);
            return false;
        }

        if (!BracketCodeReferenceParser.TryToAttachmentAnchor(
                reference,
                editor.CurrentFilePath,
                workspaceRoot,
                solutionPath,
                indexDirectoryRelative,
                out var anchor,
                out error))
        {
            query = new IntercomCodeRefQuery("", null, null);
            return false;
        }

        return fromAnchor(anchor, out query, out error);
    }

    private static bool fromAnchor(AttachmentAnchor anchor, out IntercomCodeRefQuery query, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(anchor.File))
        {
            error = "У ссылки нет file.";
            query = new IntercomCodeRefQuery("", null, null);
            return false;
        }

        query = IntercomCodeRefQuery.FromAnchor(anchor);
        return true;
    }

    private static bool looksLikeBareBracketAxes(string text) =>
        text.StartsWith("M:", StringComparison.OrdinalIgnoreCase)
        || text.StartsWith("F:", StringComparison.OrdinalIgnoreCase)
        || text.StartsWith("S:", StringComparison.OrdinalIgnoreCase)
        || CsFileBeforeMember.IsMatch(text);

    public static bool TryParseFromMcp(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? args,
        in IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out IntercomCodeRefQuery query,
        out string error,
        string? indexDirectoryRelative = null)
    {
        query = new IntercomCodeRefQuery("", null, null);
        error = "";

        if (args is null)
        {
            error = "Отсутствуют аргументы.";
            return false;
        }

        var codeRef = McpCommandJsonArgs.String(args, "code_ref");
        if (!string.IsNullOrWhiteSpace(codeRef))
        {
            return TryParse(
                codeRef,
                editor,
                workspaceRoot,
                solutionPath,
                out query,
                out error,
                indexDirectoryRelative);
        }

        if (args.TryGetValue("use_selection", out var selEl)
            && selEl.ValueKind is System.Text.Json.JsonValueKind.True)
        {
            return TryParse(
                "selection",
                editor,
                workspaceRoot,
                solutionPath,
                out query,
                out error,
                indexDirectoryRelative);
        }

        var file = McpCommandJsonArgs.String(args, "file");
        if (string.IsNullOrWhiteSpace(file))
        {
            error = "Укажи use_selection, code_ref или file+line_start.";
            return false;
        }

        var lineStart = McpCommandJsonArgs.OptionalInt32(args, "line_start");
        var lineEnd = McpCommandJsonArgs.OptionalInt32(args, "line_end") ?? lineStart;
        if (lineStart is null)
        {
            query = new IntercomCodeRefQuery(file.Replace('\\', '/'), null, null);
            return true;
        }

        if (lineStart < 1 || lineEnd < 1 || lineEnd < lineStart)
        {
            error = "line_start/line_end: 1-based, конец ≥ начала.";
            return false;
        }

        query = new IntercomCodeRefQuery(file.Replace('\\', '/'), lineStart, lineEnd);
        return true;
    }

    private static bool tryParseLineRangeTail(string lineTail, out int startLine, out int endLine, out string error)
    {
        startLine = 0;
        endLine = 0;
        error = "";

        var tail = lineTail.Trim();
        if (tail.Length == 0)
        {
            error = "Укажи номера строк (1-based): одну, две через пробел или start:end.";
            return false;
        }

        var normalized = tail.Replace(':', ' ').Replace(';', ' ');
        var parts = normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2)
        {
            error = "Ожидается одна строка или диапазон: «5», «5 10» или «5:10».";
            return false;
        }

        if (!int.TryParse(parts[0], out startLine) || startLine < 1)
        {
            error = $"Некорректный номер строки «{parts[0]}».";
            return false;
        }

        if (parts.Length == 1)
        {
            endLine = startLine;
            return true;
        }

        if (!int.TryParse(parts[1], out endLine) || endLine < 1)
        {
            error = $"Некорректный номер строки «{parts[1]}».";
            return false;
        }

        if (endLine < startLine)
        {
            error = "Конец диапазона не может быть меньше начала.";
            return false;
        }

        return true;
    }
}
