using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashCommandRunnerTests
{
    [Fact]
    public async Task TryRun_Overview_SucceedsWithoutOkText()
    {
        var runner = new ChatSlashCommandRunner((_, _, _) => Task.FromResult("{\"ok\":true}"));
        var result = await runner.TryRunAsync("/intercom overview");

        Assert.True(result.Handled);
        Assert.True(result.Success);
        Assert.Equal("/intercom overview", result.SlashPath);
        Assert.Null(result.DetailText);
    }

    [Fact]
    public async Task TryRun_Unknown_FailsWithDetail()
    {
        var runner = new ChatSlashCommandRunner((_, _, _) => Task.FromResult("{}"));
        var result = await runner.TryRunAsync("/not-a-real-command-xyz");

        Assert.True(result.Handled);
        Assert.False(result.Success);
        Assert.NotNull(result.DetailText);
    }

    [Fact]
    public async Task TryRun_FileOpen_normalizes_path()
    {
        string? capturedPath = null;
        var runner = new ChatSlashCommandRunner(
            (id, args, _) =>
            {
                if (id == IdeCommands.OpenFile && args is not null && args.TryGetValue("path", out var p))
                    capturedPath = p.GetString();
                return Task.FromResult("");
            },
            getWorkspaceRoot: () => @"D:\ws");

        var tempFile = Path.Combine(Path.GetTempPath(), "cide-slash-open-" + Guid.NewGuid().ToString("N") + ".txt");
        await File.WriteAllTextAsync(tempFile, "x");
        try
        {
            var result = await runner.TryRunAsync($"/file open \"{tempFile}\"");
            Assert.True(result.Success);
            Assert.Equal(Path.GetFullPath(tempFile), Path.GetFullPath(capturedPath!));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task TryRun_BuildRun_PreservesArgsTail()
    {
        var runner = new ChatSlashCommandRunner((id, _, _) =>
        {
            Assert.Equal(IdeCommands.Build, id);
            return Task.FromResult("");
        });

        var result = await runner.TryRunAsync("/build run");
        Assert.True(result.Success);
        Assert.Equal("/build run", result.SlashPath);
    }
}
