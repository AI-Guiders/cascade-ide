#nullable enable

namespace CascadeIDE.Features.EnvironmentReadiness.DataAcquisition;

/// <summary>Минимальные fs-факты для readiness без I/O в Application (ADR 0102).</summary>
public static class EnvironmentReadinessFileFacts
{
    public static bool SolutionPathIsExistingFile(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path);
}
