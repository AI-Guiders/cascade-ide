using System.Text.Json;
using CascadeIDE.Features.Workspace.Application;

namespace CascadeIDE.ViewModels;

internal sealed partial class IdeMcpCommandExecutor
{
    internal static string? TryGetWorkspaceRoot(IIdeMcpActions actions)
    {
        try
        {
            var json = actions.GetSolutionInfo();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out _))
                return null;
            var sln = doc.RootElement.TryGetProperty("solution_path", out var sp) ? sp.GetString() : null;
            if (string.IsNullOrWhiteSpace(sln))
                return null;
            return WorkspaceDirectoryFromSolutionPath.Resolve(sln);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
