using System.Threading.Channels;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ProjectInSolutionCreatorTests
{
    private sealed class SimRunner : IDotnetCommandRunner
    {
        public List<(IReadOnlyList<string> Args, string WorkingDir)> Calls { get; } = [];

        public Task<(bool Success, int ExitCode, string Output)> RunAsync(
            IReadOnlyList<string> arguments,
            string workingDirectory,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((arguments.ToList(), workingDirectory));
            var cmd = string.Join(' ', arguments);

            if (cmd.StartsWith("new console", StringComparison.Ordinal))
            {
                var oIndex = IndexOf(arguments, "-o");
                var dir = oIndex >= 0 && oIndex + 1 < arguments.Count ? arguments[oIndex + 1] : "";
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
                return Task.FromResult((true, 0, ""));
            }

            if (cmd.StartsWith("sln add", StringComparison.Ordinal))
                return Task.FromResult((true, 0, ""));

            return Task.FromResult((false, 1, "unexpected: " + cmd));
        }

        public Task<(bool Success, int ExitCode)> RunWithChunkWriterAsync(
            IReadOnlyList<string> args,
            string workingDirectory,
            ChannelWriter<string> chunkWriter,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        private static int IndexOf(IReadOnlyList<string> args, string value)
        {
            for (var i = 0; i < args.Count; i++)
            {
                if (string.Equals(args[i], value, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }

    [Fact]
    public async Task TryCreateAsync_runs_new_and_sln_add()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cide-sln-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var sln = Path.Combine(dir, "Test.sln");
        await File.WriteAllTextAsync(sln, "");

        var runner = new SimRunner();
        var result = await ProjectInSolutionCreator.TryCreateAsync(sln, "console", "MyApp", runner);

        Assert.True(result.Ok, result.ErrorMessage);
        Assert.Equal(2, runner.Calls.Count);
        Assert.Contains("new", runner.Calls[0].Args);
        Assert.Equal("sln", runner.Calls[1].Args[0]);
    }

    [Fact]
    public async Task TryCreateAsync_without_solution_fails()
    {
        var runner = new SimRunner();
        var result = await ProjectInSolutionCreator.TryCreateAsync(null, "console", "X", runner);

        Assert.False(result.Ok);
        Assert.Contains("открой", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
