using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Features.Chat;

public readonly record struct ChatSlashAgentResult(bool Success, string Message);

public static class ChatSlashAgentActions
{
    public static bool TryExecute(
        string slashPath,
        string? argsTail,
        IAgentEnvironmentService agentEnvironment,
        Func<string?> getSolutionPath,
        out ChatSlashAgentResult result)
    {
        result = default;
        if (!IntentSlashCatalog.TryGetRoute(slashPath, out var route)
            || route.ExecutionKind != ChatSlashCommandExecutionKind.LocalAgent)
        {
            return false;
        }

        var normalized = slashPath.Trim().ToLowerInvariant();
        return normalized switch
        {
            "/agent verify" => RunVerify(argsTail, agentEnvironment, getSolutionPath, out result),
            "/agent cancel" => RunCancel(agentEnvironment, out result),
            "/agent status" => RunStatus(agentEnvironment, out result),
            "/agent last" => RunLast(agentEnvironment, out result),
            "/agent sandbox" => RunSandbox(argsTail, agentEnvironment, getSolutionPath, out result),
            _ => Unknown(normalized, out result),
        };
    }

    private static bool RunVerify(
        string? argsTail,
        IAgentEnvironmentService agentEnvironment,
        Func<string?> getSolutionPath,
        out ChatSlashAgentResult result)
    {
        var solution = getSolutionPath();
        if (string.IsNullOrWhiteSpace(solution))
        {
            result = new(false, "Открой solution (.sln/.slnx) перед /agent verify.");
            return true;
        }

        if (!AgentVerifyPolicyParser.TryParse(argsTail, out var policy))
        {
            result = new(false, "Политика: minimal | standard | strict | ci_parity (по умолчанию standard).");
            return true;
        }

        var start = agentEnvironment.StartVerify(solution, policy);
        if (!start.Accepted)
        {
            result = new(false, start.Error ?? "Verify не запущен.");
            return true;
        }

        result = new(
            true,
            $"Verify запущен (run {start.RunId![..8]}…, snapshot {start.VerifySnapshotId}). Смотри /agent status и /agent last.");
        return true;
    }

    private static bool RunCancel(IAgentEnvironmentService agentEnvironment, out ChatSlashAgentResult result)
    {
        var cancelled = agentEnvironment.CancelActive();
        result = new(true, cancelled ? "Активный verify отменён." : "Нет активного verify.");
        return true;
    }

    private static bool RunStatus(IAgentEnvironmentService agentEnvironment, out ChatSlashAgentResult result)
    {
        var status = agentEnvironment.GetStatus();
        if (!status.IsActive)
        {
            result = new(true, "AEE: нет активного verify. Последний результат — /agent last.");
            return true;
        }

        var tail = status.WritesInvalidatedVerifyEpoch ? " · правки во время verify — перезапусти verify." : "";
        result = new(
            true,
            $"AEE: verify {status.RunId![..8]}… · policy {status.Policy} · snapshot {status.VerifySnapshotId}"
            + (!string.IsNullOrEmpty(status.SandboxRunDirectory)
                ? $" · sandbox {status.SandboxRunDirectory}"
                : "")
            + $"{tail}");
        return true;
    }

    private static bool RunLast(IAgentEnvironmentService agentEnvironment, out ChatSlashAgentResult result)
    {
        var last = agentEnvironment.GetLastRun();
        if (last is null)
        {
            result = new(true, "Пока нет завершённых verify runs в этой сессии.");
            return true;
        }

        result = new(true, last.FormatChatTrace());
        return true;
    }

    private static bool RunSandbox(
        string? argsTail,
        IAgentEnvironmentService agentEnvironment,
        Func<string?> getWorkspaceRoot,
        out ChatSlashAgentResult result)
    {
        if (!AgentSandboxProfileParser.TryParse(argsTail, out var profile))
        {
            result = new(false, "Профиль: agent_ephemeral | agent_worktree | in_place");
            return true;
        }

        var prep = agentEnvironment.PrepareSandbox(profile, getWorkspaceRoot());
        result = prep.Success
            ? new(true, $"Sandbox {profile}: {prep.Path}\n{prep.Detail}")
            : new(false, prep.Detail ?? "Sandbox prepare failed.");
        return true;
    }

    private static bool Unknown(string path, out ChatSlashAgentResult result)
    {
        result = new(false, $"Неизвестная agent-команда: {path}. Доступно: /agent verify|cancel|status|last|sandbox");
        return true;
    }
}
