#nullable enable

using CascadeIDE.Models.Editor;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Services;

/// <summary>Сводный resolve для reveal: Roslyn member/scope, fallback на lines @ send.</summary>
public static class EditorRevealRangeResolution
{
    public static bool TryResolveLines(
        EditorRevealRangeRequest request,
        string? workspaceRoot,
        out LineRange lines,
        out string detail,
        out bool usedFallback,
        string? solutionPath = null)
    {
        lines = default;
        detail = "";
        usedFallback = false;

        var filePath = request.File.Value;
        if (!AttachmentAnchorPaths.TryResolveAbsolute(filePath, workspaceRoot, out var absolute, out var pathErr))
        {
            detail = pathErr;
            return false;
        }

        if (request.SyntaxScope is not null || !string.IsNullOrWhiteSpace(request.MemberKey))
        {
            var cacheContext = IntercomAttachResolveCacheContext.From(
                workspaceRoot,
                solutionPath,
                filePath);
            if (AttachmentAnchorRoslynResolver.TryResolveLineRange(
                    null,
                    absolute,
                    request.MemberKey,
                    request.SyntaxScope,
                    cacheContext,
                    out var resolved,
                    out var roslynDetail))
            {
                lines = resolved;
                detail = roslynDetail;
                return true;
            }

            if (request.Lines is { } fallback)
            {
                lines = fallback;
                detail = roslynDetail;
                usedFallback = true;
                return true;
            }

            detail = roslynDetail;
            return false;
        }

        if (request.Lines is { } explicitLines)
        {
            lines = explicitLines;
            detail = "lines";
            return true;
        }

        detail = "no_target";
        return false;
    }
}
