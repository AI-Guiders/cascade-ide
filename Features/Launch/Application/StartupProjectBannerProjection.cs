using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Launch.Application;

/// <summary>Текст баннера стартового проекта в хроме (ADR 0090 + F5).</summary>
[ComputingUnit("startup-project-ui")]
public static class StartupProjectBannerProjection
{
    public static string Format(
        bool hasStartupProject,
        bool showLaunchProfilePicker,
        string? selectedLaunchProfileId,
        string startupProjectShortLabel)
    {
        if (!hasStartupProject)
            return "";
        var basePart = $"Старт отладки (F5): {startupProjectShortLabel}";
        if (showLaunchProfilePicker && !string.IsNullOrEmpty(selectedLaunchProfileId))
            return $"{basePart} · {selectedLaunchProfileId}";
        return basePart;
    }
}
