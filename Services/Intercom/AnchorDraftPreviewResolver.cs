#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;

namespace CascadeIDE.Services.Intercom;

/// <summary>Resolve draft bracket → file + line range (без записи anchor в лог).</summary>
public static class AnchorDraftPreviewResolver
{
    public sealed record ResolvedPreview(string AbsoluteFilePath, int StartLine, int EndLine);

    public static bool TryResolve(
        string bracketText,
        string? activeFilePath,
        string? workspaceRoot,
        string? solutionPath,
        string? indexDirectoryRelative,
        out ResolvedPreview resolved,
        out string error)
    {
        resolved = default!;
        error = "";

        var text = (bracketText ?? "").Trim();
        if (text.Length == 0)
        {
            error = "empty_bracket";
            return false;
        }

        if (!BracketCodeReferenceParser.TryParse(text, out var reference, out error))
            return false;

        if (!BracketCodeReferenceParser.TryToAttachmentAnchor(
                reference,
                activeFilePath,
                workspaceRoot,
                solutionPath,
                indexDirectoryRelative,
                out var anchor,
                out error))
        {
            return false;
        }

        if (!IntercomAttachmentResolveAtSend.TryResolveBracketDraft(
                reference,
                activeFilePath,
                workspaceRoot,
                solutionPath,
                indexDirectoryRelative,
                out anchor,
                out error))
        {
            return false;
        }

        var plan = IntercomAttachmentRevealPlan.Create(anchor, workspaceRoot, solutionPath);
        if (string.IsNullOrWhiteSpace(plan.AbsoluteFilePath))
        {
            error = plan.Message;
            return false;
        }

        if (plan.Lines is not { } lines)
        {
            error = plan.OpenFileOnly ? "open_file_only" : plan.Message;
            return false;
        }

        resolved = new ResolvedPreview(
            plan.AbsoluteFilePath,
            lines.Start.Value,
            lines.End.Value);
        return true;
    }
}
