#nullable enable

using System.Text.RegularExpressions;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>
/// Разбор bracket-ссылок H1/L2 (ADR 0128 §5, 0131): <c>[M:…]</c>, <c>[file.cs M:…]</c>, <c>[F:…; M:…; L:…]</c>.
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
        out string error)
    {
        anchor = new AttachmentAnchor();
        error = "";

        var file = reference.File?.Trim();
        if (string.IsNullOrWhiteSpace(file))
        {
            if (string.IsNullOrWhiteSpace(activeFilePath))
            {
                error = "Не задан файл в ссылке и нет активного файла в редакторе.";
                return false;
            }

            file = AttachmentAnchorPaths.ToWorkspaceRelative(activeFilePath, workspaceRoot) ?? activeFilePath;
        }

        anchor = new AttachmentAnchor
        {
            File = file.Replace('\\', '/'),
            MemberKey = string.IsNullOrWhiteSpace(reference.MemberKey) ? null : reference.MemberKey.Trim(),
            LineStart = reference.LineStart,
            LineEnd = reference.LineEnd,
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

            error = $"Неизвестное поле L2: «{part}».";
            return false;
        }

        if (string.IsNullOrWhiteSpace(file) && string.IsNullOrWhiteSpace(member) && lineStart is null)
        {
            error = "L2-ссылка не содержит F:, M: или L:.";
            return false;
        }

        reference = new BracketCodeReference(file, member, lineStart, lineEnd);
        return true;
    }

    private static bool TryParseH1(string text, out BracketCodeReference reference, out string error)
    {
        reference = default;
        error = "";

        string? file = null;
        if (CsFileBeforeMember.Match(text) is { Success: true } fm)
            file = fm.Groups["file"].Value.Trim();

        string? member = null;
        if (MemberToken.Match(text) is { Success: true } mm)
            member = mm.Groups["member"].Value.Trim();
        else if (text.StartsWith("M:", StringComparison.OrdinalIgnoreCase))
            member = text[2..].Trim();

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

        if (string.IsNullOrWhiteSpace(file) && string.IsNullOrWhiteSpace(member) && lineStart is null)
        {
            error = "Ожидается [M:…], [file.cs M:…] или [F:…; M:…; L:…].";
            return false;
        }

        reference = new BracketCodeReference(file, member, lineStart, lineEnd);
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
}

/// <summary>Результат parse bracket до resolve в <see cref="AttachmentAnchor"/>.</summary>
public readonly record struct BracketCodeReference(
    string? File,
    string? MemberKey,
    int? LineStart,
    int? LineEnd);
