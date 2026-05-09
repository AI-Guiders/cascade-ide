#nullable enable

using CascadeIDE.Contracts;

namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

/// <summary>
/// CCU formatter для ошибок MCP-запуска отладки: единый формат "# Error: ...".
/// </summary>
[ComputingUnit]
public sealed class LaunchMcpErrorFormatUnit : ICockpitComputeUnit
{
    public static LaunchMcpErrorFormatUnit Default { get; } = new();

    private LaunchMcpErrorFormatUnit()
    {
    }

    public string FormatResolveFailure(
        LaunchReadinessSnapshot readiness,
        string? explicitProfileName,
        string solutionDirectory)
    {
        if (!readiness.CanAttemptResolve)
        {
            return "# Error: " + (explicitProfileName is { Length: > 0 }
                ? "profile_not_found: " + explicitProfileName
                : "active_profile_missing");
        }

        if (!string.IsNullOrWhiteSpace(explicitProfileName) && !string.IsNullOrWhiteSpace(readiness.ProfileProjectRelative))
        {
            var candidate = CanonicalFilePath.Normalize(Path.Combine(solutionDirectory, readiness.ProfileProjectRelative));
            return "# Error: project_not_found: " + candidate;
        }

        return "# Error: active_profile_missing";
    }
}
