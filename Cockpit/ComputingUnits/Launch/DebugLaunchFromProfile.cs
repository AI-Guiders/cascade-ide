#nullable enable
namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

/// <summary>Общие шаги: путь к .csproj, карта <see cref="LaunchProfileData"/> в <see cref="DebugLaunchResolution"/> (ADR 0090).</summary>
public static class DebugLaunchFromProfile
{
    /// <summary>
    /// Тот же <see cref="IReadOnlyDictionary{TKey, TValue}"/>, что в профиле, или <c>null</c>, если словарь пустой
    /// (DAP/браузер: как при F5, без пустой обёртки).
    /// </summary>
    public static IReadOnlyDictionary<string, string>? NonEmptyEnvironmentOrNull(LaunchProfileData prof) =>
        prof.Environment is { Count: > 0 } d ? d : null;

    public static DebugLaunchResolution ToResolution(LaunchProfileData profile, string targetDllPath) =>
        new(
            targetDllPath,
            profile.ProgramArgs is { Count: > 0 } ? profile.ProgramArgs : null,
            NonEmptyEnvironmentOrNull(profile),
            string.IsNullOrEmpty(profile.WorkingDirectoryRelative) ? null : profile.WorkingDirectoryRelative,
            profile.OpenLaunchBrowser,
            profile.LaunchUrl);
}
