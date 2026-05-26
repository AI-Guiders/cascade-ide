using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace CascadeIDE.Tests;

[Trait("Category", "BuildVerifyWorker")]
public sealed class BuildVerifyWorkerCliTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CascadeIDE.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("CascadeIDE.sln not found from test output path.");
    }

    private static string WorkerDllPath()
    {
        var inOutput = Path.Combine(AppContext.BaseDirectory, "CascadeIDE.BuildVerifyWorker.dll");
        if (File.Exists(inOutput))
            return inOutput;

        var fallback = Path.Combine(
            RepoRoot(),
            "tools",
            "CascadeIDE.BuildVerifyWorker",
            "bin",
            "Debug",
            "net10.0",
            "CascadeIDE.BuildVerifyWorker.dll");
        if (File.Exists(fallback))
            return fallback;

        throw new FileNotFoundException(
            "BuildVerifyWorker assembly not found; build tools/CascadeIDE.BuildVerifyWorker first.");
    }

    [Fact]
    public void Worker_missing_args_returns_exit_2()
    {
        var (exit, _, _) = RunWorker([]);
        Assert.Equal(2, exit);
    }

    [Fact]
    public void Worker_unknown_mode_returns_exit_2()
    {
        var csproj = SampleCsprojPath();
        var (exit, _, _) = RunWorker(["publish", csproj]);
        Assert.Equal(2, exit);
    }

    [Fact]
    public void Worker_missing_project_returns_exit_2()
    {
        var missing = Path.Combine(RepoRoot(), "samples", "DebugTarget", "does-not-exist.csproj");
        var (exit, _, _) = RunWorker(["build", missing]);
        Assert.Equal(2, exit);
    }

    [Fact]
    public void Worker_build_sample_csproj_returns_success_json()
    {
        var csproj = SampleCsprojPath();
        var (exit, stdout, _) = RunWorker(["build", csproj]);

        Assert.Equal(0, exit);
        using var doc = JsonDocument.Parse(stdout);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    private static string SampleCsprojPath() =>
        Path.Combine(RepoRoot(), "samples", "DebugTarget", "DebugTarget.csproj");

    private static (int ExitCode, string StdOut, string StdErr) RunWorker(string[] workerArgs)
    {
        var dll = WorkerDllPath();
        var psi = new ProcessStartInfo("dotnet", $"exec \"{dll}\" {string.Join(' ', workerArgs.Select(QuoteArg))}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start worker process.");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(TimeSpan.FromMinutes(5));
        return (proc.ExitCode, stdout.Trim(), stderr.Trim());
    }

    private static string QuoteArg(string arg) =>
        arg.Contains(' ', StringComparison.Ordinal) || arg.Contains('"', StringComparison.Ordinal)
            ? $"\"{arg.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : arg;
}
