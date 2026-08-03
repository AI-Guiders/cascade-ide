#nullable enable

using System.Text.Json;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>Host-only: bracket → AttachmentAnchor (HybridIndex). Peel CRS links parse-only <see cref="BracketCodeReferenceParser"/> into GlassCore.</summary>
public static class BracketCodeReferenceAttachment
{
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
}
