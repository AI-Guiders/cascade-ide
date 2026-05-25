using System.Text.Json;
using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{
    public Task<string> IdeAgentVerifyAsync(
        string? policy,
        string? sandboxProfile,
        string? solutionPath,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var svc = _host.AgentEnvironment;
        var solution = string.IsNullOrWhiteSpace(solutionPath)
            ? _host.Workspace.SolutionPath
            : solutionPath;

        if (!AgentVerifyPolicyParser.TryParse(
                policy ?? _host.GetCascadeSettingsForExecutor().Agent.Environment.DefaultVerifyPolicy,
                out var p))
            p = AgentVerifyPolicy.Standard;

        AgentSandboxProfile? sandbox = null;
        if (!string.IsNullOrWhiteSpace(sandboxProfile)
            && AgentSandboxProfileParser.TryParse(sandboxProfile, out var sp))
        {
            sandbox = sp;
        }

        var start = svc.StartVerify(solution ?? "", p, sandbox);
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            accepted = start.Accepted,
            run_id = start.RunId,
            verify_snapshot_id = start.VerifySnapshotId,
            error = start.Error,
        }));
    }

    public Task<string> IdeAgentCancelAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var cancelled = _host.AgentEnvironment.CancelActive();
        return Task.FromResult(JsonSerializer.Serialize(new { cancelled }));
    }

    public Task<string> IdeAgentStatusAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var s = _host.AgentEnvironment.GetStatus();
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            active = s.IsActive,
            run_id = s.RunId,
            verify_snapshot_id = s.VerifySnapshotId,
            policy = s.Policy,
            sandbox_profile = s.SandboxProfile,
        }));
    }

    public Task<string> IdeAgentLastAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var last = _host.AgentEnvironment.GetLastRun();
        if (last is null)
            return Task.FromResult("{\"last\":null}");

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            last = new
            {
                run_id = last.RunId,
                green = last.Green,
                max_rung = last.MaxRungReached,
                completed_at_utc = last.CompletedAtUtc,
                trace = last.FormatChatTrace(),
            },
        }));
    }

    public Task<string> IdeAgentSandboxPrepareAsync(
        string? profile,
        string? workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (!AgentSandboxProfileParser.TryParse(
                profile ?? _host.GetCascadeSettingsForExecutor().Agent.Environment.DefaultSandboxProfile,
                out var p))
        {
            p = AgentSandboxProfile.AgentEphemeral;
        }

        var root = string.IsNullOrWhiteSpace(workspaceRoot) ? _host.GetWorkspacePath() : workspaceRoot;
        var result = _host.AgentEnvironment.PrepareSandbox(p, root);
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            success = result.Success,
            path = result.Path,
            detail = result.Detail,
        }));
    }
}
