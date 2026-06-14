using System.Diagnostics;
using CascadeIDE.Models;
using CascadeIDE.Models.Forge;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Forge.Infrastructure;

/// <summary>Open forge artifact from <c>[FRG:…]</c>; optional code tail → editor (ADR-0159 phase 3).</summary>
public static class ForgeLensOpenService
{
    public static string BuildViewUrl(string baseUrl, ForgeArtifactRef artifact)
    {
        var root = baseUrl.TrimEnd('/');
        return artifact.Kind switch
        {
            ForgeArtifactKind.Issue => $"{root}/view/repos/{Uri.EscapeDataString(artifact.Repo)}/issues/{artifact.Number}",
            ForgeArtifactKind.MergeRequest => $"{root}/view/repos/{Uri.EscapeDataString(artifact.Repo)}/merge-requests/{artifact.Number}",
            ForgeArtifactKind.Repo => $"{root}/view/repos/{Uri.EscapeDataString(artifact.Repo)}",
            _ => root + "/view",
        };
    }

    public static bool TryOpenExternal(string viewUrl, out string error)
    {
        error = "";
        try
        {
            Process.Start(new ProcessStartInfo(viewUrl) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryNavigateCodeTail(
        ForgeArtifactRef artifact,
        string? workspaceRoot,
        string? activeFilePath,
        string? solutionPath,
        string? indexDirectoryRelative,
        IIdeMcpActions actions,
        IntercomSettings settings,
        bool select,
        out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(artifact.CodeBracket))
            return true;

        if (!BracketCodeReferenceParser.TryParse(artifact.CodeBracket, out var reference, out error))
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

        error = IntercomAttachmentNavigator.Apply(
            actions,
            settings,
            workspaceRoot,
            anchor,
            selectExplicit: select,
            shiftSelect: false,
            durationMs: null,
            solutionPath);
        return !error.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
    }
}
