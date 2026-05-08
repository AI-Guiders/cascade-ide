using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeMcpGitWorkspaceSessionTests
{
    [Fact]
    public void TryCreate_null_workspace_returns_error_json()
    {
        var runner = new NoOpGitRunner();
        Assert.False(IdeMcpGitWorkspaceSession.TryCreate(runner, null, out _, out var err));
        Assert.Contains("Workspace path", err, StringComparison.Ordinal);
        Assert.Contains("\"success\":false", err, StringComparison.Ordinal);
    }

    [Fact]
    public void TryCreate_nonexistent_directory_returns_error_json()
    {
        var runner = new NoOpGitRunner();
        Assert.False(IdeMcpGitWorkspaceSession.TryCreate(runner, @"Z:\nonexistent\cascade-git-test", out _, out var err));
        Assert.Contains("\"exit_code\":-1", err, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GitStatusAsync_passes_workspace_to_runner_and_success_json()
    {
        var dir = Directory.CreateTempSubdirectory("cascade-git-mcp-test-");
        try
        {
            var runner = new CapturingGitRunner { Result = (true, 0, "ok-branch") };
            Assert.True(IdeMcpGitWorkspaceSession.TryCreate(runner, dir.FullName, out var session, out var bindErr), bindErr);
            var json = await session!.GitStatusAsync();
            Assert.Contains("\"success\":true", json, StringComparison.Ordinal);
            Assert.Single(runner.WorkingDirectories);
            Assert.Equal(dir.FullName, runner.WorkingDirectories[0]);
        }
        finally
        {
            try
            {
                dir.Delete(recursive: true);
            }
            catch
            {
                // Best-effort cleanup on temp dir.
            }
        }
    }

    private sealed class NoOpGitRunner : IGitCommandRunner
    {
        public Task<(bool Success, int ExitCode, string Output)> RunAsync(
            IReadOnlyList<string> args,
            string workingDirectory,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((true, 0, ""));
    }

    private sealed class CapturingGitRunner : IGitCommandRunner
    {
        public List<string> WorkingDirectories { get; } = [];

        public (bool Success, int ExitCode, string Output) Result = (true, 0, "");

        public Task<(bool Success, int ExitCode, string Output)> RunAsync(
            IReadOnlyList<string> args,
            string workingDirectory,
            CancellationToken cancellationToken = default)
        {
            WorkingDirectories.Add(workingDirectory);
            return Task.FromResult(Result);
        }
    }
}
