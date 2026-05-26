using System.Diagnostics;
using System.Text.Json;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>
/// <c>[agent.environment] shell_escape_tier</c> (ADR 0148): блокирует обход AEE через «сырые» MCP-команды
/// <c>build</c>/<c>run_tests</c> (отдельный <c>dotnet</c> вне лестницы verify).
/// </summary>
public static class ShellEscapePolicy
{
    private static readonly JsonSerializerOptions JsonCompact = new() { WriteIndented = false };

    /// <summary>Возвращает JSON-ошибку для MCP, если команда заблокирована.</summary>
    public static string? TryBlockJson(string commandId, string? tier)
    {
        if (!IsBypassingAeeDotnetCommand(commandId))
            return null;

        var t = (tier ?? "deny").Trim();
        if (string.Equals(t, "allow_with_audit", StringComparison.OrdinalIgnoreCase))
        {
            Trace.TraceInformation("[AEE] shell_escape_tier=allow_with_audit command={0}", commandId);
            return null;
        }

        if (string.Equals(t, "l3_only", StringComparison.OrdinalIgnoreCase))
        {
            if (IsTestOnlyCommand(commandId))
                return null;

            return BlockPayload(
                tier: t,
                commandId,
                "Tier l3_only: разрешены только run_tests/run_affected_tests; сборка/format — через ide_agent_verify или смените shell_escape_tier.");
        }

        if (string.Equals(t, "deny", StringComparison.OrdinalIgnoreCase))
            return BlockPayload(
                tier: t,
                commandId,
                "Tier deny: используйте ide_agent_verify с нужной политикой вместо прямых build/run_tests MCP.");

        return BlockPayload(t, commandId, $"Неизвестный shell_escape_tier «{t}»; считается блокирующим.");
    }

    private static bool IsBypassingAeeDotnetCommand(string commandId) =>
        commandId is IdeCommands.Build
            or IdeCommands.BuildStructured
            or IdeCommands.RunTests
            or IdeCommands.RunAffectedTests
            or IdeCommands.RunCodeCleanup;

    private static bool IsTestOnlyCommand(string commandId) =>
        commandId is IdeCommands.RunTests or IdeCommands.RunAffectedTests;

    private static string BlockPayload(string tier, string commandId, string message) =>
        JsonSerializer.Serialize(
            new
            {
                error = "shell_escape_blocked",
                shell_escape_tier = tier,
                command_id = commandId,
                message,
                use_instead = "ide_agent_verify",
            },
            JsonCompact);
}
