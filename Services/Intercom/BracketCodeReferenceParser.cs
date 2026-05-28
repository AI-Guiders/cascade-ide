#nullable enable

using System.Text.Json;
using System.Text.RegularExpressions;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>
/// Разбор bracket-ссылок H1/L2 (ADR 0128 §5.1, 0131): <c>F</c>/<c>M</c>/<c>L</c>/<c>S</c> — напр. <c>[M:Run S:for:2]</c>, <c>[F:…; M:…; S:for:2]</c>.
/// </summary>
public static class BracketCodeReferenceParser
{
    private static readonly Regex MemberToken = new(
        @"\bM:(?<member>[^\s;\]]+)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex FileToken = new(
        @"\bF:(?<file>[^\s;\]]+)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LineToken = new(
        @"\bL:(?<start>\d+)\s*(?:-\s*(?<end>\d+))?",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ScopeToken = new(
        @"\bS:(?<kind>[A-Za-z_][\w]*)\s*(?::|\()\s*(?<index>\d+)\s*\)?",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CsFileBeforeMember = new(
        @"(?<file>[^\s\[\]]+\.cs)\s+M:",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryParse(string? input, out BracketCodeReference reference, out string error)
    {
        reference = default;
        error = "";

        var text = (input ?? "").Trim();
        if (text.Length == 0)
        {
            error = "Пустая bracket-ссылка.";
            return false;
        }

        if (text.StartsWith('[') && text.EndsWith(']'))
            text = text[1..^1].Trim();

        if (text.Contains(';', StringComparison.Ordinal))
            return TryParseL2(text, out reference, out error);

        return TryParseH1(text, out reference, out error);
    }

    public static bool TryToAttachmentAnchor(
        in BracketCodeReference reference,
        string? activeFilePath,
        string? workspaceRoot,
        out AttachmentAnchor anchor,
        out string error) =>
        TryToAttachmentAnchor(
            reference,
            activeFilePath,
            workspaceRoot,
            solutionPath: null,
            indexDirectoryRelative: null,
            out anchor,
            out error);

    public static bool TryToAttachmentAnchor(
        in BracketCodeReference reference,
        string? activeFilePath,
        string? workspaceRoot,
        string? solutionPath,
        string? indexDirectoryRelative,
        out AttachmentAnchor anchor,
        out string error)
    {
        anchor = new AttachmentAnchor();
        error = "";

        if (!IntercomMemberFileInference.TryResolveRelativeFile(
                reference.File,
                reference.MemberKey,
                activeFilePath,
                workspaceRoot,
                solutionPath,
                indexDirectoryRelative,
                out var file,
                out error))
        {
            return false;
        }

        JsonElement? syntaxScope = null;
        if (!string.IsNullOrWhiteSpace(reference.ScopeKind))
        {
            var index = reference.ScopeIndexInParent is > 0 ? reference.ScopeIndexInParent.Value : 1;
            var parentMember = string.IsNullOrWhiteSpace(reference.MemberKey) ? null : reference.MemberKey.Trim();
            syntaxScope = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
            {
                ["kind"] = reference.ScopeKind.Trim(),
                ["indexInParent"] = index,
                ["parentMemberKey"] = parentMember,
            });
        }

        anchor = new AttachmentAnchor
        {
            File = file.Replace('\\', '/'),
            MemberKey = string.IsNullOrWhiteSpace(reference.MemberKey) ? null : reference.MemberKey.Trim(),
            LineStart = reference.LineStart,
            LineEnd = reference.LineEnd,
            SyntaxScope = syntaxScope,
        };

        return true;
    }

    private static bool TryParseL2(string text, out BracketCodeReference reference, out string error)
    {
        reference = default;
        error = "";

        string? file = null;
        string? member = null;
        int? lineStart = null;
        int? lineEnd = null;
        string? scopeKind = null;
        int? scopeIndex = null;

        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length == 0)
                continue;

            if (part.StartsWith("F:", StringComparison.OrdinalIgnoreCase))
            {
                file = part[2..].Trim();
                continue;
            }

            if (part.StartsWith("M:", StringComparison.OrdinalIgnoreCase))
            {
                member = part[2..].Trim();
                continue;
            }

            if (part.StartsWith("L:", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseLineRange(part[2..].Trim(), out lineStart, out lineEnd, out error))
                    return false;
                continue;
            }

            if (part.StartsWith("S:", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseScopePayload(part[2..].Trim(), out scopeKind, out scopeIndex, out error))
                    return false;
                continue;
            }

            error = $"Неизвестное поле L2: «{part}».";
            return false;
        }

        if (string.IsNullOrWhiteSpace(file)
            && string.IsNullOrWhiteSpace(member)
            && lineStart is null
            && string.IsNullOrWhiteSpace(scopeKind))
        {
            error = "L2-ссылка не содержит F:, M:, L: или S:.";
            return false;
        }

        reference = new BracketCodeReference(file, member, lineStart, lineEnd, scopeKind, scopeIndex);
        return true;
    }

    private static bool TryParseH1(string text, out BracketCodeReference reference, out string error)
    {
        reference = default;
        error = "";

        string? file = null;
        if (FileToken.Match(text) is { Success: true } ff)
            file = ff.Groups["file"].Value.Trim();
        else if (CsFileBeforeMember.Match(text) is { Success: true } fm)
            file = fm.Groups["file"].Value.Trim();

        string? member = null;
        if (MemberToken.Match(text) is { Success: true } mm)
            member = mm.Groups["member"].Value.Trim();
        else if (text.StartsWith("M:", StringComparison.OrdinalIgnoreCase))
            member = text[2..].Trim();

        string? scopeKind = null;
        int? scopeIndex = null;
        if (ScopeToken.Match(text) is { Success: true } sm)
        {
            scopeKind = sm.Groups["kind"].Value.Trim();
            if (!int.TryParse(sm.Groups["index"].Value, out var scopeIdx) || scopeIdx < 1)
            {
                error = "Некорректный индекс S: (ожидается ≥ 1).";
                return false;
            }

            scopeIndex = scopeIdx;
        }

        int? lineStart = null;
        int? lineEnd = null;
        if (LineToken.Match(text) is { Success: true } lm)
        {
            if (!int.TryParse(lm.Groups["start"].Value, out var start) || start < 1)
            {
                error = "Некорректный диапазон L:.";
                return false;
            }

            lineStart = start;
            if (lm.Groups["end"].Success && int.TryParse(lm.Groups["end"].Value, out var end) && end >= start)
                lineEnd = end;
            else
                lineEnd = start;
        }

        if (string.IsNullOrWhiteSpace(file)
            && string.IsNullOrWhiteSpace(member)
            && lineStart is null
            && string.IsNullOrWhiteSpace(scopeKind))
        {
            error = "Ожидается [M:…], [M:… S:for:2], [file.cs M:…] или L2 [F:…; M:…; …].";
            return false;
        }

        reference = new BracketCodeReference(file, member, lineStart, lineEnd, scopeKind, scopeIndex);
        return true;
    }

    private static bool TryParseScopePayload(string payload, out string? kind, out int? indexInParent, out string error)
    {
        kind = null;
        indexInParent = null;
        error = "";
        var token = "S:" + payload;
        if (ScopeToken.Match(token) is not { Success: true } m)
        {
            error = "S: ожидает for:2 или if(1) (kind + 1-based index).";
            return false;
        }

        kind = m.Groups["kind"].Value.Trim();
        if (!int.TryParse(m.Groups["index"].Value, out var idx) || idx < 1)
        {
            error = "Некорректный индекс S: (ожидается ≥ 1).";
            return false;
        }

        indexInParent = idx;
        return true;
    }

    private static bool TryParseLineRange(string payload, out int? lineStart, out int? lineEnd, out string error)
    {
        lineStart = null;
        lineEnd = null;
        error = "";

        var normalized = payload.Replace(':', '-').Replace(' ', '-');
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2)
        {
            error = "L: ожидает «50» или «50-100».";
            return false;
        }

        if (!int.TryParse(parts[0], out var start) || start < 1)
        {
            error = $"Некорректная строка «{parts[0]}».";
            return false;
        }

        lineStart = start;
        if (parts.Length == 1)
        {
            lineEnd = start;
            return true;
        }

        if (!int.TryParse(parts[1], out var end) || end < start)
        {
            error = $"Некорректный конец диапазона «{parts[1]}».";
            return false;
        }

        lineEnd = end;
        return true;
    }

    /// <summary>Скан bracket-ссылок только в prose (вне fenced code), ADR 0129 §5.</summary>
    public static IReadOnlyList<(BracketCodeReference Reference, int LineNumber)> EnumerateInProse(string markdown)
    {
        var hits = new List<(BracketCodeReference, int)>();
        foreach (var prose in MarkdownProseSegments.EnumerateProse(markdown))
        {
            var line = 1;
            var lineStart = 0;
            for (var i = 0; i < prose.Length; i++)
            {
                if (prose[i] == '\n')
                {
                    line++;
                    lineStart = i + 1;
                }

                if (prose[i] != '[')
                    continue;

                if (!TryReadBracketSpan(prose, i, out var close))
                    continue;

                var inner = prose.Substring(i + 1, close - i - 1);
                if (!TryParse(inner, out var reference, out _))
                    continue;

                if (IsMarkdownLinkAfter(prose, close))
                    continue;

                if (string.IsNullOrWhiteSpace(reference.File))
                    continue;

                var lineNumber = line;
                for (var p = lineStart; p < i; p++)
                {
                    if (prose[p] == '\n')
                        lineNumber++;
                }

                hits.Add((reference, lineNumber));
                i = close;
            }
        }

        return hits;
    }

    internal static bool IsMarkdownLinkAfter(string text, int closeBracketIndex)
    {
        var j = closeBracketIndex + 1;
        while (j < text.Length && char.IsWhiteSpace(text[j]))
            j++;

        return j < text.Length && text[j] == '(';
    }

    internal static bool TryReadBracketSpan(string text, int openIndex, out int closeIndex)
    {
        closeIndex = -1;
        var depth = 0;
        for (var i = openIndex; i < text.Length; i++)
        {
            if (text[i] == '[')
                depth++;
            else if (text[i] == ']')
            {
                depth--;
                if (depth == 0)
                {
                    closeIndex = i;
                    return true;
                }
            }
        }

        return false;
    }
}

/// <summary>Результат parse bracket до resolve в <see cref="AttachmentAnchor"/>.</summary>
public readonly record struct BracketCodeReference(
    string? File,
    string? MemberKey,
    int? LineStart,
    int? LineEnd,
    string? ScopeKind = null,
    int? ScopeIndexInParent = null);
