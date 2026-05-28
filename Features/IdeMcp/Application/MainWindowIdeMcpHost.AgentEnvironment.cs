#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Models;
using CascadeIDE.Services;

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
        var path = ResolveSolutionPath(solutionPath);
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult("{\"error\":\"solution_path required\"}");

        var p = ParsePolicy(policy);
        var sandbox = ParseSandbox(sandboxProfile);
        var result = _host.AgentEnvironment.StartVerify(path, p, sandbox);
        return Task.FromResult(JsonSerializer.Serialize(result));
    }

    public Task<string> IdeAgentVerifyBatchAsync(
        string? policy,
        string? sandboxProfile,
        string? solutionPath,
        bool useWorktree,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var path = ResolveSolutionPath(solutionPath);
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult("{\"error\":\"solution_path required\"}");

        var request = new AgentVerifyBatchRequest(
            ParsePolicy(policy),
            ParseSandboxOrDefault(sandboxProfile),
            path,
            useWorktree);
        var result = _host.AgentEnvironment.StartVerifyBatch(request);
        return Task.FromResult(JsonSerializer.Serialize(result));
    }

    public Task<string> IdeAgentCancelAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var ok = _host.AgentEnvironment.CancelActive();
        return Task.FromResult(JsonSerializer.Serialize(new { cancelled = ok }));
    }

    public Task<string> IdeAgentStatusAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(JsonSerializer.Serialize(_host.AgentEnvironment.GetStatus()));
    }

    public Task<string> IdeAgentLastAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var last = _host.AgentEnvironment.GetLastRun();
        return Task.FromResult(last is null ? "{}" : JsonSerializer.Serialize(last));
    }

    public Task<string> IdeAgentSandboxPrepareAsync(
        string? profile,
        string? workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var result = _host.AgentEnvironment.PrepareSandbox(
            ParseSandboxOrDefault(profile),
            string.IsNullOrWhiteSpace(workspaceRoot) ? _host.GetWorkspacePath() : workspaceRoot);
        return Task.FromResult(JsonSerializer.Serialize(result));
    }

    private string? ResolveSolutionPath(string? solutionPath) =>
        string.IsNullOrWhiteSpace(solutionPath) ? _host.Workspace.SolutionPath : solutionPath.Trim();

    private static AgentVerifyPolicy ParsePolicy(string? policy) =>
        string.IsNullOrWhiteSpace(policy)
            ? AgentVerifyPolicy.Standard
            : Enum.TryParse<AgentVerifyPolicy>(policy, ignoreCase: true, out var p)
                ? p
                : AgentVerifyPolicy.Standard;

    private static AgentSandboxProfile? ParseSandbox(string? profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
            return null;

        return Enum.TryParse<AgentSandboxProfile>(profile, ignoreCase: true, out var p) ? p : null;
    }

    private static AgentSandboxProfile ParseSandboxOrDefault(string? profile) =>
        ParseSandbox(profile) ?? AgentSandboxProfile.AgentEphemeral;
}
