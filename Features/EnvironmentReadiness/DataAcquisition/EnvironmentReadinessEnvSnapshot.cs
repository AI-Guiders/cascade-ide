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
    public const string NetcoreDbgPath = "NETCOREDBG_PATH";

    /// <summary>Sentinel <c>executable_env</c> / <c>cursor_acp_path_env</c> (ADR 0149) — не переменная <c>PATH</c> процесса.</summary>
    public const string SettingsPathLookupSentinel = SettingsEnvResolver.PathLookupSentinel;

    /// <summary>Опционально: именованная переменная с абсолютным путём, если не в PATH.</summary>
    public const string CascadeIntercomServerExe = "CASCADE_INTERCOM_SERVER_EXE";
    public const string CascadeIntercomBaseUrl = "CASCADE_INTERCOM_BASE_URL";
    public const string CursorAcpAgentPath = "CURSOR_ACP_AGENT_PATH";
}
