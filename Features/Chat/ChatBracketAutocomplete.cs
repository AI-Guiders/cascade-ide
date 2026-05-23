#nullable enable

using System.Text.RegularExpressions;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Автокомплит внутри <c>[F:… M:… L:… S:…]</c> в composer Intercom (ADR 0128 §5.1).</summary>
public static class ChatBracketAutocomplete
{
    public const int DefaultFileLimit = 25;
    public const int DefaultMemberLimit = 30;
    public const int DefaultTemplateLimit = 12;

    private static readonly Regex CsFileBeforeMember = new(
        @"(?<file>[^\s\[\]]+\.cs)\s+M:(?<prefix>[^\s;\]]*)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] ScopeKinds = ["for", "foreach", "if", "while", "switch", "try", "lock", "using"];

    public enum Axis
    {
        None = 0,
        Start,
        File,
        Member,
        Lines,
        Scope,
    }

    public sealed record EditState(int BracketStart, int CaretIndex, string Segment, Axis ActiveAxis, string AxisPrefix);

    public static bool TryGetEditState(string? text, int caretIndex, out EditState state)
    {
        state = default!;
        if (string.IsNullOrEmpty(text))
            return false;

        caretIndex = Math.Clamp(caretIndex, 0, text.Length);
        var bracketStart = findOpenBracketStart(text, caretIndex);
        if (bracketStart < 0)
            return false;

        var inner = text[(bracketStart + 1)..caretIndex];
        if (inner.Contains('\n') || inner.Contains('\r'))
            return false;

        var segment = inner;
        var semi = inner.LastIndexOf(';');
        if (semi >= 0)
            segment = inner[(semi + 1)..].TrimStart();

        var (axis, prefix) = detectAxis(segment);
        state = new EditState(bracketStart, caretIndex, segment, axis, prefix);
        return true;
    }

    public static IReadOnlyList<ChatBracketSuggestion> GetSuggestions(
        string? text,
        int caretIndex,
        string? activeFilePath,
        string? workspaceRoot,
        IWorkspaceFileSlashCompletionProvider? workspaceFiles,
        int fileLimit = DefaultFileLimit,
        int memberLimit = DefaultMemberLimit)
    {
        if (!TryGetEditState(text, caretIndex, out var state))
            return [];

        return state.ActiveAxis switch
        {
            Axis.File => suggestFiles(state, workspaceFiles, workspaceRoot, fileLimit),
            Axis.Member => suggestMembers(state, activeFilePath, workspaceRoot, memberLimit),
            Axis.Lines => suggestLines(state),
            Axis.Scope => suggestScope(state),
            Axis.Start => suggestStart(state, activeFilePath, workspaceRoot, workspaceFiles, fileLimit, memberLimit),
            _ => [],
        };
    }

    private static IReadOnlyList<ChatBracketSuggestion> suggestStart(
        EditState state,
        string? activeFilePath,
        string? workspaceRoot,
        IWorkspaceFileSlashCompletionProvider? workspaceFiles,
        int fileLimit,
        int memberLimit)
    {
        var list = new List<ChatBracketSuggestion>();
        var seg = state.Segment.Trim();

        if (seg.Length == 0)
        {
            list.Add(template(state, "M:", "Member (метод, свойство…)", "Attach", "M:", false));
            list.Add(template(state, "F:", "File (workspace-relative)", "Attach", "F:", false));
            list.Add(template(state, "S:", "Syntax scope (for/if/…)", "Attach", "S:for:1", false));
            list.Add(template(state, "L:", "Lines 1-based", "Attach", "L:", false));

            // Не тянуть Roslyn/parse всего .cs на один символ «[» — только шаблоны осей; члены после M: / file M:.
            return list;
        }

        if (seg.Contains('.', StringComparison.Ordinal) && !seg.Contains(' ', StringComparison.Ordinal))
        {
            return suggestFiles(state, workspaceFiles, workspaceRoot, fileLimit);
        }

        if (char.IsLetterOrDigit(seg[0]))
        {
            var fileSeg = state with { ActiveAxis = Axis.File, AxisPrefix = seg };
            return suggestFiles(fileSeg, workspaceFiles, workspaceRoot, fileLimit);
        }

        return list;
    }

    private static IReadOnlyList<ChatBracketSuggestion> suggestFiles(
        EditState state,
        IWorkspaceFileSlashCompletionProvider? workspaceFiles,
        string? workspaceRoot,
        int limit)
    {
        if (workspaceFiles is null)
            return [];

        var prefix = state.AxisPrefix;
        if (state.Segment.StartsWith("F:", StringComparison.OrdinalIgnoreCase))
            prefix = state.Segment[2..];

        var matches = workspaceFiles.GetMatches(prefix, limit);
        var list = new List<ChatBracketSuggestion>(matches.Count);
        foreach (var m in matches)
        {
            var path = m.InsertPath;
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var inner = buildInnerWithAxisPrefix(state, "F:", path);
            list.Add(new ChatBracketSuggestion(
                path,
                m.Help,
                "File",
                inner,
                AddClosingBracket: true));
        }

        return list;
    }

    private static IReadOnlyList<ChatBracketSuggestion> suggestMembers(
        EditState state,
        string? activeFilePath,
        string? workspaceRoot,
        int limit)
    {
        string? file = null;
        var memberPrefix = state.AxisPrefix;

        if (CsFileBeforeMember.Match(state.Segment) is { Success: true } fm)
        {
            file = fm.Groups["file"].Value.Trim();
            memberPrefix = fm.Groups["prefix"].Value;
        }
        else if (state.Segment.StartsWith("M:", StringComparison.OrdinalIgnoreCase))
        {
            memberPrefix = state.Segment[2..];
            file = activeFilePath;
        }
        else
        {
            var fIdx = state.Segment.IndexOf(" F:", StringComparison.OrdinalIgnoreCase);
            if (fIdx >= 0)
            {
                var fPart = state.Segment[(fIdx + 3)..].Trim();
                var sp = fPart.IndexOf(' ');
                file = sp > 0 ? fPart[..sp].Trim() : fPart;
            }
            else
            {
                file = activeFilePath;
            }

            var mIdx = state.Segment.LastIndexOf(" M:", StringComparison.OrdinalIgnoreCase);
            if (mIdx >= 0)
                memberPrefix = state.Segment[(mIdx + 3)..];
        }

        if (string.IsNullOrWhiteSpace(file))
            return [];

        var rel = AttachmentAnchorPaths.ToWorkspaceRelative(file, workspaceRoot) ?? file.Replace('\\', '/');
        var matches = BracketMemberCompletionProvider.GetMatches(rel, workspaceRoot, memberPrefix, limit);
        return matches
            .Select(m =>
            {
                var inner = buildInnerWithMember(state, rel, m.Name);
                return new ChatBracketSuggestion(
                    m.Name,
                    $"{m.Help} · {rel}",
                    "Member",
                    inner,
                    AddClosingBracket: true);
            })
            .ToList();
    }

    private static IReadOnlyList<ChatBracketSuggestion> suggestLines(EditState state)
    {
        var prefix = state.AxisPrefix.Trim();
        var templates = new[] { "1", "10", "10-20", "50-100" };
        return templates
            .Where(t => prefix.Length == 0 || t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(t => template(state, t, "Line range @ send", "Lines", $"L:{t}", true))
            .ToList();
    }

    private static IReadOnlyList<ChatBracketSuggestion> suggestScope(EditState state)
    {
        var payload = state.AxisPrefix.Trim();
        string? kindFilter = null;
        var indexPrefix = "";

        if (payload.Contains(':'))
        {
            var parts = payload.Split(':', 2);
            kindFilter = parts[0].Trim().ToLowerInvariant();
            indexPrefix = parts.Length > 1 ? parts[1] : "";
        }
        else if (payload.Contains('('))
        {
            kindFilter = payload[..payload.IndexOf('(')].Trim().ToLowerInvariant();
            indexPrefix = payload[(payload.IndexOf('(') + 1)..].TrimEnd(')');
        }
        else
        {
            kindFilter = payload.ToLowerInvariant();
        }

        var list = new List<ChatBracketSuggestion>();
        foreach (var kind in ScopeKinds)
        {
            if (kindFilter.Length > 0 && !kind.StartsWith(kindFilter, StringComparison.Ordinal))
                continue;

            for (var i = 1; i <= 3; i++)
            {
                var idxText = i.ToString();
                if (indexPrefix.Length > 0 && !idxText.StartsWith(indexPrefix, StringComparison.Ordinal))
                    continue;

                var token = $"S:{kind}:{i}";
                list.Add(template(state, token, $"Syntax scope ({kind}, #{i} in member body)", "Scope", token, true));
            }
        }

        return list;
    }

    private static ChatBracketSuggestion template(
        EditState state,
        string display,
        string help,
        string? group,
        string axisPayload,
        bool close) =>
        new(display, help, group, buildInnerReplacingAxis(state, axisPayload), close);

    private static string buildInnerWithAxisPrefix(EditState state, string axis, string value)
    {
        var seg = state.Segment;
        if (seg.StartsWith("F:", StringComparison.OrdinalIgnoreCase))
            return $"F:{value}";

        if (seg.Contains(';'))
        {
            var idx = seg.LastIndexOf(';');
            return seg[..(idx + 1)] + $" F:{value}";
        }

        return $"F:{value}";
    }

    private static string buildInnerWithMember(EditState state, string file, string memberName)
    {
        if (CsFileBeforeMember.IsMatch(state.Segment) || state.Segment.Contains(".cs", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileName(file);
            return $"{fileName} M:{memberName}";
        }

        if (state.Segment.StartsWith("M:", StringComparison.OrdinalIgnoreCase))
            return $"M:{memberName}";

        if (state.Segment.Contains(" F:", StringComparison.OrdinalIgnoreCase))
        {
            var fIdx = state.Segment.IndexOf(" F:", StringComparison.OrdinalIgnoreCase);
            return state.Segment[..(fIdx + 3)].TrimEnd() + $" {Path.GetFileName(file)} M:{memberName}".Trim();
        }

        return $"M:{memberName}";
    }

    private static string buildInnerReplacingAxis(EditState state, string newTail)
    {
        if (state.Segment.Contains(';'))
        {
            var idx = state.Segment.LastIndexOf(';');
            return state.Segment[..(idx + 1)] + " " + newTail;
        }

        return newTail;
    }

    private static (Axis axis, string prefix) detectAxis(string segment)
    {
        var seg = segment.Trim();
        if (seg.Length == 0)
            return (Axis.Start, "");

        if (CsFileBeforeMember.IsMatch(seg))
            return (Axis.Member, CsFileBeforeMember.Match(seg).Groups["prefix"].Value);

        var sMatch = Regex.Match(seg, @"\bS:(?<p>[^\s;\]]*)$", RegexOptions.IgnoreCase);
        if (sMatch.Success)
            return (Axis.Scope, sMatch.Groups["p"].Value);

        var lMatch = Regex.Match(seg, @"\bL:(?<p>[^\s;\]]*)$", RegexOptions.IgnoreCase);
        if (lMatch.Success)
            return (Axis.Lines, lMatch.Groups["p"].Value);

        var mMatch = Regex.Match(seg, @"\bM:(?<p>[^\s;\]]*)$", RegexOptions.IgnoreCase);
        if (mMatch.Success)
            return (Axis.Member, mMatch.Groups["p"].Value);

        var fMatch = Regex.Match(seg, @"\bF:(?<p>[^\s;\]]*)$", RegexOptions.IgnoreCase);
        if (fMatch.Success)
            return (Axis.File, fMatch.Groups["p"].Value);

        if (seg.Contains(".cs", StringComparison.OrdinalIgnoreCase) && seg.Contains(' '))
            return (Axis.Member, "");

        if (seg.Contains('.', StringComparison.Ordinal) && !seg.Contains(' ', StringComparison.Ordinal))
            return (Axis.File, seg);

        return (Axis.Start, seg);
    }

    private static int findOpenBracketStart(string text, int caretIndex)
    {
        var depth = 0;
        for (var i = caretIndex - 1; i >= 0; i--)
        {
            if (text[i] == ']')
            {
                depth++;
                continue;
            }

            if (text[i] != '[')
                continue;

            if (depth > 0)
            {
                depth--;
                continue;
            }

            return i;
        }

        return -1;
    }
}
