using System.Threading.Channels;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class BlankSolutionCreatorTests
{
    private sealed class CapturingDotnetRunner : IDotnetCommandRunner
    {
        public List<(IReadOnlyList<string> Args, string WorkingDir)> Calls { get; } = [];

        public Task<(bool Success, int ExitCode, string Output)> RunAsync(
            IReadOnlyList<string> args,
            string workingDirectory,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((args, workingDirectory));
            return Task.FromResult((true, 0, ""));
        }

        public Task<(bool Success, int ExitCode)> RunWithChunkWriterAsync(
            IReadOnlyList<string> args,
            string workingDirectory,
            ChannelWriter<string> chunkWriter,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    /// <summary>Имитирует <c>dotnet new sln</c>: создаёт <c>{name}.sln</c> в каталоге <c>-o</c>.</summary>
    private sealed class DotnetNewSlnSimRunner : IDotnetCommandRunner
    {
        public List<(IReadOnlyList<string> Args, string WorkingDir)> Calls { get; } = [];

        public Task<(bool Success, int ExitCode, string Output)> RunAsync(
            IReadOnlyList<string> args,
            string workingDirectory,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((args, workingDirectory));
            // new sln -n NAME -o OUT
            var name = args.Count > 3 ? args[3] : "";
            var outDir = args.Count > 5 ? args[5] : "";
            Directory.CreateDirectory(outDir);
            var slnPath = Path.Combine(outDir, name + ".sln");
            File.WriteAllText(slnPath, "Microsoft Visual Studio Solution File, Format Version 12.00\r\n");
            return Task.FromResult((true, 0, ""));
        }

        public Task<(bool Success, int ExitCode)> RunWithChunkWriterAsync(
            IReadOnlyList<string> args,
            string workingDirectory,
            ChannelWriter<string> chunkWriter,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task TryCreateAsync_Rejects_ExistingFile()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "cide-blank-sln-test-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tmp);
        var sln = Path.Combine(tmp, "Already.sln");
        await File.WriteAllTextAsync(sln, "", CancellationToken.None);

        try
        {
            var r = await BlankSolutionCreator.TryCreateAsync(sln, new CapturingDotnetRunner(), CancellationToken.None);
            Assert.False(r.Ok);
            Assert.Contains("уже существует", r.ErrorMessage ?? "", StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                File.Delete(sln);
                Directory.Delete(tmp, recursive: true);
            }
            catch
            {
                // ignore
            }
        }
    }

    [Fact]
    public async Task TryCreateAsync_Invokes_DotnetNewSln_WithNameAndOutputDir()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "cide-blank-sln-new-" + Guid.NewGuid().ToString("n"));
        var sln = Path.Combine(tmp, "MyApp.sln");
        var runner = new DotnetNewSlnSimRunner();

        try
        {
            var r = await BlankSolutionCreator.TryCreateAsync(sln, runner, CancellationToken.None);
            Assert.True(r.Ok, r.ErrorMessage);
            Assert.Equal(sln, r.SolutionPath, StringComparer.OrdinalIgnoreCase);
            Assert.True(File.Exists(sln));

            var call = Assert.Single(runner.Calls);
            Assert.Equal(tmp, call.WorkingDir, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(["new", "sln", "-n", "MyApp", "-o", tmp], call.Args);
        }
        finally
        {
            try
            {
                if (File.Exists(sln))
                    File.Delete(sln);
                if (Directory.Exists(tmp))
                    Directory.Delete(tmp, recursive: true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
