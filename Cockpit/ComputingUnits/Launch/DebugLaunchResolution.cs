#nullable enable

namespace CascadeIDE.Cockpit.ComputingUnits.Launch;

/// <summary>Резолв целевой DLL, аргументов и среды из launch profile (F5, MCP) перед DAP.</summary>
public readonly record struct DebugLaunchResolution(
    string TargetDllPath,
    IReadOnlyList<string>? ProgramArgs,
    IReadOnlyDictionary<string, string>? Environment,
    string? WorkingDirectoryRelativeToSolution,
    bool OpenLaunchBrowser,
    string? LaunchUrl);
