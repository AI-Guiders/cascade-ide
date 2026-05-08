using CascadeIDE.Features.HybridIndex.Application;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>Разрешение области HCI для MCP <c>codebase_index_*</c> (паритет с оркестратором).</summary>
public static class IdeMcpHybridIndexScope
{
    public static bool TryResolveForCodebaseIndexCommand(
        string? argWorkspacePath,
        string? argSolutionPath,
        string? hybridIndexScopeMode,
        string? currentSolutionPath,
        Func<string?, string?> getWorkspaceDirectoryForSolution,
        out string hciWorkspaceRoot,
        out string? hciSolutionPath,
        out string? errorJson)
    {
        hciWorkspaceRoot = "";
        hciSolutionPath = null;
        errorJson = null;

        var reqWs = argWorkspacePath?.Trim();
        if (string.IsNullOrWhiteSpace(reqWs))
        {
            var sln = currentSolutionPath;
            var wsDir = getWorkspaceDirectoryForSolution(sln);
            if (string.IsNullOrWhiteSpace(wsDir))
            {
                errorJson = """{"error":"no_workspace","detail":"Open a solution or pass workspace_path"}""";
                return false;
            }

            (hciWorkspaceRoot, hciSolutionPath) =
                HybridIndexScopeResolver.ApplyScopeMode(hybridIndexScopeMode, wsDir, sln);
            return !string.IsNullOrWhiteSpace(hciWorkspaceRoot);
        }

        try
        {
            hciWorkspaceRoot = Path.GetFullPath(
                reqWs!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch
        {
            errorJson = """{"error":"invalid_workspace_path"}""";
            return false;
        }

        var slnArg = string.IsNullOrWhiteSpace(argSolutionPath) ? null : argSolutionPath.Trim();
        (hciWorkspaceRoot, hciSolutionPath) =
            HybridIndexScopeResolver.ApplyScopeMode(hybridIndexScopeMode, hciWorkspaceRoot, slnArg);
        if (string.IsNullOrWhiteSpace(hciWorkspaceRoot))
        {
            errorJson = """{"error":"invalid_workspace_path"}""";
            return false;
        }

        return true;
    }
}
