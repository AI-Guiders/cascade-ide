#nullable enable

using CascadeIDE.Contracts;
namespace CascadeIDE.Features.EnvironmentReadiness.DataAcquisition;

/// <summary>
/// Снимок переменных окружения, которые Cascade IDE реально читает для заметок/knowledge и отладки (ADR 0023, DAL).
/// Значения не логируем целиком в UI — только факты проверки пути.
/// </summary>
public readonly record struct EnvironmentReadinessEnvSnapshot(
    string? AgentNotesFile,
    string? AgentNotesCanonPath,
    string? NetcoreDbgPath)
{
    public static EnvironmentReadinessEnvSnapshot FromCurrentProcess() =>
        new(
            Environment.GetEnvironmentVariable(WellKnownEnv.AgentNotesFile),
            Environment.GetEnvironmentVariable(WellKnownEnv.AgentNotesCanonPath),
            Environment.GetEnvironmentVariable(WellKnownEnv.NetcoreDbgPath));
}

/// <summary>Имена переменных — в одном месте (совпадают с AgentNotes.Core и разрешением netcoredbg в отладке).</summary>
[IoBoundary]
public static class WellKnownEnv
{
    public const string AgentNotesFile = "AGENT_NOTES_FILE";
    public const string AgentNotesCanonPath = "AGENT_NOTES_CANON_PATH";
    public const string NetcoreDbgPath = "NETCOREDBG_PATH";
}
