#nullable enable

using System.Text.RegularExpressions;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Reverse anchors: scan ADR/KB bodies for code paths and brackets (ADR 0156 §2.3).</summary>
public static partial class DocReverseAnchorResolver
{
    public const string ProvenanceBracket = "bracket";
    public const string ProvenanceDocBody = "doc_body";
    public const string ProvenanceWorkspaceToml = "workspace_toml";

    public static IReadOnlyList<DocReverseAnchorMatch> Resolve(
        string? workspaceRoot,
        string? navigationAbsolutePath,
        IReadOnlyList<string> forwardDocRepoPaths,
        IReadOnlyList<WorkspaceExplicitCodeAnchor>? explicitAnchors = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(navigationAbsolutePath))
            return [];

        var root = workspaceRoot.Trim();
        var anchorRel = WorkspaceAdrMapResolver.TryComputeRepoRelativePath(root, navigationAbsolutePath);
        if (anchorRel is null)
            return [];

        var normalizedAnchor = NormalizePath(anchorRel);
        var anchorFileName = Path.GetFileName(normalizedAnchor);
        var results = new List<DocReverseAnchorMatch>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var docFileOverrides = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (explicitAnchors is { Count: > 0 })
        {
            foreach (var entry in explicitAnchors)
            {
                if (!PathsMatch(entry.CodeAnchor.File, normalizedAnchor, anchorFileName))
                    continue;

                var key = OverrideKey(entry.DocPath, entry.CodeAnchor.File);
                docFileOverrides.Add(key);
                AddMatch(
                    results,
                    seen,
                    entry.DocPath,
                    WorkspaceAdrMapResolver.GuessAdrPreviewTitle(entry.DocPath),
                    entry.CodeAnchor,
                    markdown: null,
                    lineHint: entry.CodeAnchor.LineStart,
                    entry.Provenance,
                    entry.Kind);
            }
        }

        foreach (var docRel in forwardDocRepoPaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var absDoc = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, docRel);
            if (absDoc is null || !WorkspaceTextFileReader.TryReadAllText(absDoc, out var md))
                continue;

            ScanDocument(docRel, md, normalizedAnchor, anchorFileName, results, seen, docFileOverrides);
        }

        return results
            .OrderBy(x => x.DocPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.DocLineHint ?? int.MaxValue)
            .ToList();
    }

    private static void ScanDocument(
        string docRel,
        string markdown,
        string anchorRel,
        string anchorFileName,
        List<DocReverseAnchorMatch> results,
        HashSet<string> seen,
        HashSet<string> docFileOverrides)
    {
        var title = WorkspaceAdrMapResolver.GuessAdrPreviewTitle(docRel);

        foreach (var (reference, line) in BracketCodeReferenceParser.EnumerateInProse(markdown))
        {
            if (string.IsNullOrWhiteSpace(reference.File))
                continue;

            var anchor = new CodeAnchor(
                NormalizePath(reference.File!),
                reference.LineStart,
                reference.LineEnd,
                reference.MemberKey,
                reference.ScopeKind is null ? null : $"{reference.ScopeKind}:{reference.ScopeIndexInParent}");

            if (!PathsMatch(anchor.File, anchorRel, anchorFileName))
                continue;

            if (docFileOverrides.Contains(OverrideKey(docRel, anchor.File)))
                continue;

            AddMatch(results, seen, docRel, title, anchor, markdown, line, ProvenanceBracket);
        }

        var proseText = string.Concat(MarkdownProseSegments.EnumerateProse(markdown));
        if (proseText.Length == 0)
            return;

        foreach (Match m in BacktickPathRegex().Matches(proseText))
        {
            var path = m.Groups["path"].Value;
            if (!LooksLikeCodePath(path) || !PathsMatch(path, anchorRel, anchorFileName))
                continue;

            if (docFileOverrides.Contains(OverrideKey(docRel, NormalizePath(path))))
                continue;

            AddMatch(
                results,
                seen,
                docRel,
                title,
                new CodeAnchor(NormalizePath(path)),
                markdown,
                LineNumberInMarkdown(markdown, m.Index),
                ProvenanceDocBody);
        }

        foreach (Match m in MarkdownCodeLinkRegex().Matches(proseText))
        {
            var path = m.Groups["path"].Value;
            if (!PathsMatch(path, anchorRel, anchorFileName))
                continue;

            if (docFileOverrides.Contains(OverrideKey(docRel, NormalizePath(path))))
                continue;

            AddMatch(
                results,
                seen,
                docRel,
                title,
                new CodeAnchor(NormalizePath(path)),
                markdown,
                LineNumberInMarkdown(markdown, m.Index),
                ProvenanceDocBody);
        }

        foreach (Match m in FileLineRangeRegex().Matches(proseText))
        {
            var path = m.Groups["path"].Value;
            if (!PathsMatch(path, anchorRel, anchorFileName))
                continue;

            if (docFileOverrides.Contains(OverrideKey(docRel, NormalizePath(path))))
                continue;

            int? ls = int.TryParse(m.Groups["start"].Value, out var s) ? s : null;
            int? le = m.Groups["end"].Success && int.TryParse(m.Groups["end"].Value, out var e) ? e : null;
            AddMatch(
                results,
                seen,
                docRel,
                title,
                new CodeAnchor(NormalizePath(path), ls, le),
                markdown,
                LineNumberInMarkdown(markdown, m.Index),
                ProvenanceDocBody);
        }
    }

    private static string OverrideKey(string docPath, string file) =>
        $"{NormalizePath(docPath)}|{NormalizePath(file)}";

    private static int LineNumberInMarkdown(string markdown, int indexInProseConcat)
    {
        var limit = Math.Min(indexInProseConcat, markdown.Length);
        var line = 1;
        for (var i = 0; i < limit; i++)
        {
            if (markdown[i] == '\n')
                line++;
        }

        return line;
    }

    private static void AddMatch(
        List<DocReverseAnchorMatch> results,
        HashSet<string> seen,
        string docRel,
        string title,
        CodeAnchor anchor,
        string? markdown,
        int? lineHint,
        string provenance,
        string? kind = null)
    {
        var key = $"{docRel}|{anchor.File}|{anchor.LineStart}|{anchor.MemberKey}|{provenance}";
        if (!seen.Add(key))
            return;

        var excerpt = markdown is { Length: > 0 } && lineHint is > 0
            ? BuildExcerpt(markdown, lineHint.Value)
            : "";

        results.Add(new DocReverseAnchorMatch(
            docRel,
            title,
            anchor,
            excerpt,
            provenance,
            lineHint,
            kind ?? WorkspaceCorrespondenceCodeAnchorsLoader.DefaultKind));
    }

    private static string BuildExcerpt(string markdown, int lineOneBased)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        if (lineOneBased < 1 || lineOneBased > lines.Length)
            return "";

        var raw = lines[lineOneBased - 1].Trim();
        return raw.Length <= 96 ? raw : raw[..93] + "…";
    }

    private static bool PathsMatch(string candidatePath, string anchorRel, string anchorFileName)
    {
        var c = NormalizePath(candidatePath);
        if (c.Equals(anchorRel, StringComparison.OrdinalIgnoreCase))
            return true;

        if (c.EndsWith('/' + anchorRel, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(Path.GetFileName(c), anchorFileName, StringComparison.OrdinalIgnoreCase)
            && anchorRel.EndsWith('/' + anchorFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeCodePath(string path) =>
        path.Contains('.', StringComparison.Ordinal)
        && (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".fs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".vb", StringComparison.OrdinalIgnoreCase)
            || path.Contains('/', StringComparison.Ordinal)
            || path.Contains('\\', StringComparison.Ordinal));

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').Trim().TrimStart('/');

    [GeneratedRegex(@"`(?<path>[\w./\\-]+\.(?:cs|fs|vb))`", RegexOptions.IgnoreCase)]
    private static partial Regex BacktickPathRegex();

    [GeneratedRegex(@"\[(?<label>[^\]]+)\]\((?<path>[^)\s#]+\.(?:cs|fs|vb)[^)]*)\)", RegexOptions.IgnoreCase)]
    private static partial Regex MarkdownCodeLinkRegex();

    [GeneratedRegex(@"(?<path>[\w./\\-]+\.(?:cs|fs|vb)):(?<start>\d+)(?:\s*[-–]\s*(?<end>\d+))?", RegexOptions.IgnoreCase)]
    private static partial Regex FileLineRangeRegex();
}
