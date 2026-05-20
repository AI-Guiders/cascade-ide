#nullable enable

using System.Text;
using System.Text.Json;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models.Editor;
using CascadeIDE.Models.Intercom;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services.Intercom;

/// <summary>Resolve @ send: file, lines, excerpt, shape (ADR 0128).</summary>
public static class IntercomAttachmentResolveAtSend
{
    public const int MaxExcerptLines = 120;
    public const int MaxExcerptBytes = 16 * 1024;

    public sealed record EditorSnapshot(
        string? CurrentFilePath,
        string? EditorText,
        int? SelectionStart,
        int? SelectionLength,
        int? CaretOffset);

    public static bool TryResolveSelection(
        in EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out AttachmentAnchor anchor,
        out string error)
    {
        anchor = new AttachmentAnchor();
        error = "";

        if (string.IsNullOrWhiteSpace(editor.CurrentFilePath))
        {
            error = "Нет активного файла в редакторе.";
            return false;
        }

        if (editor.SelectionLength is not > 0)
        {
            error = "Нет выделения в редакторе — выдели фрагмент или используй /attach scope.";
            return false;
        }

        var start = editor.SelectionStart ?? 0;
        var len = editor.SelectionLength ?? 0;
        var (lineStart, _) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(editor.EditorText, start);
        var (lineEnd, _) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(
            editor.EditorText,
            Math.Clamp(start + len - 1, 0, (editor.EditorText ?? "").Length));

        var rel = AttachmentAnchorPaths.ToWorkspaceRelative(editor.CurrentFilePath, workspaceRoot)
            ?? editor.CurrentFilePath;
        anchor = new AttachmentAnchor
        {
            AttachmentShape = "selection",
            File = rel.Replace('\\', '/'),
            LineStart = lineStart,
            LineEnd = lineEnd,
            DisplayLabel = buildSelectionLabel(rel, lineStart, lineEnd),
        };

        return finalize(anchor, workspaceRoot, solutionPath, null, out anchor, out error);
    }

    public static bool TryResolveScope(
        in EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out AttachmentAnchor anchor,
        out string error)
    {
        anchor = new AttachmentAnchor();
        error = "";

        if (!AttachmentAnchorCaretScopeResolver.TryResolveAtCaret(
                editor.CurrentFilePath,
                editor.EditorText,
                editor.CaretOffset ?? editor.SelectionStart,
                workspaceRoot,
                out anchor,
                out error))
        {
            return false;
        }

        return finalize(anchor, workspaceRoot, solutionPath, null, out anchor, out error);
    }

    public static bool TryResolveFile(
        string pathArg,
        int? lineStart,
        int? lineEnd,
        string? workspaceRoot,
        string? solutionPath,
        out AttachmentAnchor anchor,
        out string error)
    {
        anchor = new AttachmentAnchor();
        error = "";

        var path = pathArg.Trim();
        if (path.Length == 0)
        {
            error = "Укажи путь: /attach file <path> [start] [end].";
            return false;
        }

        var rel = AttachmentAnchorPaths.ToWorkspaceRelative(path, workspaceRoot) ?? path.Replace('\\', '/');
        var isText = isTextFile(rel);
        if (!isText && (lineStart.HasValue || lineEnd.HasValue))
        {
            error = "Для бинарного файла нельзя задать диапазон строк.";
            return false;
        }

        if (!isText)
        {
            anchor = new AttachmentAnchor
            {
                AttachmentShape = "whole-file",
                File = rel.Replace('\\', '/'),
                DisplayLabel = Path.GetFileName(rel),
            };
            return finalize(anchor, workspaceRoot, solutionPath, null, out anchor, out error);
        }

        if (lineStart is null)
        {
            anchor = new AttachmentAnchor
            {
                AttachmentShape = "whole-file",
                File = rel.Replace('\\', '/'),
                DisplayLabel = Path.GetFileName(rel),
            };
            return finalize(anchor, workspaceRoot, solutionPath, null, out anchor, out error);
        }

        var end = lineEnd ?? lineStart;
        anchor = new AttachmentAnchor
        {
            AttachmentShape = "text-range",
            File = rel.Replace('\\', '/'),
            LineStart = lineStart,
            LineEnd = end,
            DisplayLabel = $"{Path.GetFileName(rel)}:{lineStart}-{end}",
        };
        return finalize(anchor, workspaceRoot, solutionPath, null, out anchor, out error);
    }

    public static bool TryResolveBracket(
        in BracketCodeReference reference,
        string? activeFilePath,
        string? workspaceRoot,
        string? solutionPath,
        out AttachmentAnchor anchor,
        out string error) =>
        TryResolveBracket(reference, activeFilePath, workspaceRoot, solutionPath, null, out anchor, out error);

    public static bool TryResolveBracket(
        in BracketCodeReference reference,
        string? activeFilePath,
        string? workspaceRoot,
        string? solutionPath,
        IntercomAttachmentRoslynResolveSession? resolveSession,
        out AttachmentAnchor anchor,
        out string error)
    {
        if (!BracketCodeReferenceParser.TryToAttachmentAnchor(reference, activeFilePath, workspaceRoot, out anchor, out error))
            return false;

        var shape = !string.IsNullOrWhiteSpace(reference.MemberKey)
            ? reference.ScopeKind is not null ? "syntax-scope" : "member"
            : reference.LineStart is not null ? "text-range" : "whole-file";

        anchor = anchor with
        {
            AttachmentShape = shape,
            DisplayLabel = buildBracketLabel(reference, anchor.File),
        };

        return finalize(anchor, workspaceRoot, solutionPath, resolveSession, out anchor, out error);
    }

    public static bool TryAssignIdAndResolve(
        AttachmentAnchor draft,
        string shortId,
        string? workspaceRoot,
        string? solutionPath,
        out AttachmentAnchor anchor,
        out string error) =>
        TryAssignIdAndResolve(draft, shortId, workspaceRoot, solutionPath, null, out anchor, out error);

    public static bool TryAssignIdAndResolve(
        AttachmentAnchor draft,
        string shortId,
        string? workspaceRoot,
        string? solutionPath,
        IntercomAttachmentRoslynResolveSession? resolveSession,
        out AttachmentAnchor anchor,
        out string error)
    {
        anchor = draft with
        {
            Id = shortId,
            ResolvedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
        };
        return finalize(anchor, workspaceRoot, solutionPath, resolveSession, out anchor, out error);
    }

    private static bool finalize(
        AttachmentAnchor draft,
        string? workspaceRoot,
        string? solutionPath,
        IntercomAttachmentRoslynResolveSession? resolveSession,
        out AttachmentAnchor anchor,
        out string error)
    {
        anchor = draft;
        error = "";

        if (string.IsNullOrWhiteSpace(anchor.File))
        {
            error = "Не удалось определить file.";
            return false;
        }

        anchor = anchor with { ResolvedAtUtc = anchor.ResolvedAtUtc ?? DateTimeOffset.UtcNow.ToString("O") };

        if (!AttachmentAnchorPaths.TryResolveAbsolute(anchor.File, workspaceRoot, out var absolute, out var pathErr))
        {
            error = pathErr;
            return false;
        }

        if (string.IsNullOrWhiteSpace(anchor.DisplayLabel))
            anchor = anchor with { DisplayLabel = Path.GetFileName(anchor.File) };

        AttachmentSyntaxScope? syntaxScope = null;
        if (AttachmentSyntaxScope.TryParse(anchor.SyntaxScope, out var parsedScope))
            syntaxScope = parsedScope;

        if (!string.IsNullOrWhiteSpace(anchor.MemberKey) || syntaxScope is not null)
        {
            if (!AttachmentAnchorRoslynResolver.TryResolveLineRange(
                    resolveSession,
                    absolute,
                    anchor.MemberKey,
                    syntaxScope,
                    out var lines,
                    out var roslynDetail))
            {
                error = formatRoslynResolveError(anchor, syntaxScope, roslynDetail);
                return false;
            }

            anchor = anchor with
            {
                LineStart = lines.Start.Value,
                LineEnd = lines.End.Value,
            };
        }

        var excerpt = tryBuildExcerpt(absolute, anchor.LineStart, anchor.LineEnd, resolveSession);

        anchor = anchor with { Excerpt = excerpt };
        return true;
    }

    private static string formatRoslynResolveError(
        AttachmentAnchor anchor,
        AttachmentSyntaxScope? syntaxScope,
        string roslynDetail)
    {
        var file = string.IsNullOrWhiteSpace(anchor.File) ? "?" : Path.GetFileName(anchor.File);
        var target = !string.IsNullOrWhiteSpace(anchor.MemberKey)
            ? anchor.MemberKey
            : syntaxScope is not null
                ? $"{syntaxScope.Kind}#{syntaxScope.IndexInParent}"
                : "?";
        return string.IsNullOrWhiteSpace(roslynDetail)
            ? $"Не удалось разрешить вложение в {file}: {target}."
            : $"Не удалось разрешить вложение в {file}: {target} ({roslynDetail}).";
    }

    private static string? tryBuildExcerpt(
        string absolutePath,
        int? lineStart,
        int? lineEnd,
        IntercomAttachmentRoslynResolveSession? resolveSession)
    {
        string text;
        if (!AttachmentAnchorRoslynResolver.TryGetCachedText(resolveSession, absolutePath, out text))
        {
            if (!File.Exists(absolutePath))
                return null;

            try
            {
                text = File.ReadAllText(absolutePath);
            }
            catch
            {
                return null;
            }
        }

        if (lineStart is null || lineEnd is null)
        {
            return trimExcerpt(text);
        }

        var lines = text.Replace("\r", "").Split('\n');
        var start = Math.Clamp(lineStart.Value, 1, lines.Length);
        var end = Math.Clamp(lineEnd.Value, start, lines.Length);
        if (end - start + 1 > MaxExcerptLines)
            end = start + MaxExcerptLines - 1;

        var slice = string.Join(Environment.NewLine, lines[(start - 1)..end]);
        return trimExcerpt(slice);
    }

    private static string? trimExcerpt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var bytes = Encoding.UTF8.GetByteCount(text);
        if (bytes <= MaxExcerptBytes)
            return text;

        var maxChars = Math.Max(1, MaxExcerptBytes / 4);
        return text.Length <= maxChars ? text : text[..maxChars] + "…";
    }

    private static bool isTextFile(string path)
    {
        var ext = Path.GetExtension(path);
        if (ext.Length == 0)
            return true;
        return ext.Equals(".cs", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".json", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".xml", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".yml", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".ts", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".tsx", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".js", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jsx", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".css", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".sql", StringComparison.OrdinalIgnoreCase);
    }

    private static string buildSelectionLabel(string file, int lineStart, int lineEnd) =>
        lineStart == lineEnd
            ? $"{Path.GetFileName(file)}:{lineStart}"
            : $"{Path.GetFileName(file)}:{lineStart}-{lineEnd}";

    private static string buildBracketLabel(in BracketCodeReference reference, string? file)
    {
        if (!string.IsNullOrWhiteSpace(reference.MemberKey) && reference.ScopeKind is not null)
            return $"{reference.MemberKey} › {reference.ScopeKind} ({reference.ScopeIndexInParent})";
        if (!string.IsNullOrWhiteSpace(reference.MemberKey))
            return reference.MemberKey!;
        if (!string.IsNullOrWhiteSpace(file))
            return Path.GetFileName(file);
        return "attach";
    }
}
