using System.Text.Json;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP workspace actions.
/// Keeps payload shaping and JSON guards out of MainWindowViewModel.
/// </summary>
public static class IdeMcpWorkspaceOrchestrator
{
    public static string SerializeIdeState(object state) =>
        JsonSerializer.Serialize(state);

    public static string SerializeCockpitSurface(object snapshot) =>
        JsonSerializer.Serialize(snapshot);

    public static string SerializeSolutionInfo(
        string? solutionPath,
        string? currentFilePath,
        IReadOnlyList<string> projectPaths,
        string? selectedSolutionPath) =>
        JsonSerializer.Serialize(new
        {
            solution_path = solutionPath ?? "",
            current_file_path = currentFilePath ?? "",
            project_paths = projectPaths,
            selected_solution_path = selectedSolutionPath ?? ""
        });

    public static string SerializeBuildOutput(string? text, string background, string foreground) =>
        JsonSerializer.Serialize(new
        {
            text = text ?? "",
            theme = new
            {
                background,
                foreground
            }
        });

    public static string SerializeWorkspaceNotLoadedError() =>
        JsonSerializer.Serialize(new { error = "No workspace loaded." });

    public static string SerializeInvalidWorkspaceRootError() =>
        JsonSerializer.Serialize(new { error = "Invalid workspace root." });

    public static JsonElement ParseDiagnosticsOrEmpty(string diagnosticsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(diagnosticsJson);
        }
        catch
        {
            return JsonSerializer.SerializeToElement(Array.Empty<object>());
        }
    }

    public static string BuildTruncatedOutputPreview(string? buildOutput, int maxChars)
    {
        var text = buildOutput ?? "";
        return text.Length > maxChars ? text[..maxChars] + "\n... (output truncated)" : text;
    }
}
