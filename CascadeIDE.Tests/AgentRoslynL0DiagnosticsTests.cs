using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

[Trait("Category", "AgentEnvironment")]
public sealed class AgentRoslynL0DiagnosticsTests
{
    [Fact]
    public async Task RunAsync_skipped_when_language_or_resolver_missing()
    {
        var noLang = new AgentRoslynL0Diagnostics(null, () => []);
        var outcome = await noLang.RunAsync();
        Assert.Contains("no language service", outcome.Detail, StringComparison.Ordinal);

        var noResolver = new AgentRoslynL0Diagnostics(new CSharpLanguageService(), null);
        outcome = await noResolver.RunAsync();
        Assert.Contains("no language service", outcome.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_open_tabs_detects_syntax_error()
    {
        var path = Path.Combine(Path.GetTempPath(), "cide-l0-open-" + Guid.NewGuid().ToString("N") + ".cs");
        try
        {
            var broken = "class Broken { void M( { }";
            var l0 = new AgentRoslynL0Diagnostics(
                new CSharpLanguageService(),
                () => [(path, broken)],
                new AgentEnvironmentLadderSettings { L0CsScope = AgentL0CsScopeParser.OpenTabsOnly });

            var outcome = await l0.RunAsync();

            Assert.False(outcome.Green);
            Assert.True(outcome.ErrorCount > 0);
            Assert.Contains("1 file", outcome.Detail, StringComparison.Ordinal);
        }
        finally
        {
            try { File.Delete(path); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RunAsync_git_dirty_loads_cs_from_disk_when_not_open()
    {
        var dir = Directory.CreateTempSubdirectory("cide-l0-git-");
        try
        {
            var onlyGit = Path.Combine(dir.FullName, "OnlyGit.cs");
            await File.WriteAllTextAsync(onlyGit, "namespace T; public class Ok { }");

            var git = new GitNameOnlyRunner
            {
                UnstagedOutput = "OnlyGit.cs\nreadme.txt",
            };

            var l0 = new AgentRoslynL0Diagnostics(
                new CSharpLanguageService(),
                () => [],
                new AgentEnvironmentLadderSettings { L0CsScope = AgentL0CsScopeParser.OpenTabsAndGitDirtyCs },
                git,
                () => dir.FullName);

            var outcome = await l0.RunAsync();

            Assert.True(outcome.Green);
            Assert.Contains("1 file", outcome.Detail, StringComparison.Ordinal);
            Assert.Equal(2, git.Invocations.Count);
        }
        finally
        {
            try { dir.Delete(recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RunAsync_open_tab_buffer_overrides_disk_for_same_path()
    {
        var dir = Directory.CreateTempSubdirectory("cide-l0-priority-");
        try
        {
            var cs = Path.Combine(dir.FullName, "Same.cs");
            await File.WriteAllTextAsync(cs, "namespace T; public class Ok { }");

            var git = new GitNameOnlyRunner { UnstagedOutput = "Same.cs" };
            var l0 = new AgentRoslynL0Diagnostics(
                new CSharpLanguageService(),
                () => [(cs, "class Broken { void M( { }")],
                new AgentEnvironmentLadderSettings { L0CsScope = AgentL0CsScopeParser.OpenTabsAndGitDirtyCs },
                git,
                () => dir.FullName);

            var outcome = await l0.RunAsync();

            Assert.False(outcome.Green);
            Assert.True(outcome.ErrorCount > 0);
        }
        finally
        {
            try { dir.Delete(recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RunAsync_git_dirty_respects_max_files_cap()
    {
        var dir = Directory.CreateTempSubdirectory("cide-l0-cap-");
        try
        {
            for (var i = 0; i < 5; i++)
                await File.WriteAllTextAsync(Path.Combine(dir.FullName, $"F{i}.cs"), $"namespace T{i}; public class C{i} {{}}");

            var git = new GitNameOnlyRunner
            {
                UnstagedOutput = "F0.cs\nF1.cs\nF2.cs\nF3.cs\nF4.cs",
            };

            var l0 = new AgentRoslynL0Diagnostics(
                new CSharpLanguageService(),
                () => [],
                new AgentEnvironmentLadderSettings
                {
                    L0CsScope = AgentL0CsScopeParser.OpenTabsAndGitDirtyCs,
                    L0GitDirtyMaxFiles = 2,
                },
                git,
                () => dir.FullName);

            var outcome = await l0.RunAsync();

            Assert.True(outcome.Green);
            Assert.Contains("2 file", outcome.Detail, StringComparison.Ordinal);
        }
        finally
        {
            try { dir.Delete(recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RunAsync_open_tabs_only_does_not_call_git()
    {
        var git = new GitNameOnlyRunner { UnstagedOutput = "Ghost.cs" };
        var l0 = new AgentRoslynL0Diagnostics(
            new CSharpLanguageService(),
            () => [],
            new AgentEnvironmentLadderSettings { L0CsScope = AgentL0CsScopeParser.OpenTabsOnly },
            git,
            () => @"C:\any");

        var outcome = await l0.RunAsync();

        Assert.Empty(git.Invocations);
        Assert.Contains("no .cs inputs", outcome.Detail, StringComparison.Ordinal);
    }

    private sealed class GitNameOnlyRunner : IGitCommandRunner
    {
        public string? UnstagedOutput { get; set; }

        public string? StagedOutput { get; set; }

        public List<IReadOnlyList<string>> Invocations { get; } = [];

        public Task<(bool Success, int ExitCode, string Output)> RunAsync(
            IReadOnlyList<string> args,
            string workingDirectory,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add(args);
            var isCached = args.Any(a => string.Equals(a, "--cached", StringComparison.Ordinal));
            var isNameOnly = args.Any(a => string.Equals(a, "--name-only", StringComparison.Ordinal));
            if (isNameOnly && isCached)
                return Task.FromResult((true, 0, StagedOutput ?? ""));
            if (isNameOnly)
                return Task.FromResult((true, 0, UnstagedOutput ?? ""));
            return Task.FromResult((true, 0, ""));
        }
    }
}
