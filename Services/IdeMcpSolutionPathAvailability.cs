namespace CascadeIDE.Services;

/// <summary>Проверка пути решения перед MCP-сборкой/тестами — вне статического оркестратора (CASCOPE031).</summary>
public static class IdeMcpSolutionPathAvailability
{
    public static bool IsRunnableSolutionFile(string? solutionPathFromUi) =>
        !string.IsNullOrWhiteSpace(solutionPathFromUi)
        && File.Exists(solutionPathFromUi);
}
