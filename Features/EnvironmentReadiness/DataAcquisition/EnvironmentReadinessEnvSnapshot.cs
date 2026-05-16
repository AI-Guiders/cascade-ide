#nullable enable

using CascadeIDE.Contracts;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.EnvironmentReadiness.DataAcquisition;

/// <summary>
/// Снимок переменных окружения, которые Cascade IDE реально читает для заметок/knowledge и отладки (ADR 0023, DAL).
/// Значения не логируем целиком в UI — только факты проверки пути.
/// </summary>
public readonly record struct EnvironmentReadinessEnvSnapshot(
    string? AgentNotesFile,
    string? AgentNotesConfigPath,
    string? NetcoreDbgPath)
{
    public static EnvironmentReadinessEnvSnapshot FromCurrentProcess() =>
        new(
            Environment.GetEnvironmentVariable(WellKnownEnv.AgentNotesFile),
            null,
            Environment.GetEnvironmentVariable(WellKnownEnv.NetcoreDbgPath));

    public static EnvironmentReadinessEnvSnapshot FromSettings(CascadeIdeSettings settings) =>
        new(
            Environment.GetEnvironmentVariable(WellKnownEnv.AgentNotesFile),
            AgentNotesRuntimeLoader.ResolveConfigPath(settings),
            Environment.GetEnvironmentVariable(WellKnownEnv.NetcoreDbgPath));
}

/// <summary>Имена переменных — в одном месте (совпадают с AgentNotes.Core и разрешением netcoredbg в отладке).</summary>
[IoBoundary]
public static class WellKnownEnv
{
    public const string AgentNotesFile = "AGENT_NOTES_FILE";
    [Obsolete("Use [agent_notes].config_path in settings.toml (SSOT with MCP --config).")]
    public const string AgentNotesCanonPath = "AGENT_NOTES_CANON_PATH";
    public const string NetcoreDbgPath = "NETCOREDBG_PATH";
}
