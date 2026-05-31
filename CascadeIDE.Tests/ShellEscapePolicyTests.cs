using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ShellEscapePolicyTests
{
    [Fact]
    public void Deny_Allows_OpenFile()
    {
        Assert.Null(ShellEscapePolicy.TryBlockJson(IdeCommands.OpenFile, "deny"));
    }

    [Fact]
    public void Deny_Block_Build_ReturnsJson()
    {
        var json = ShellEscapePolicy.TryBlockJson(IdeCommands.Build, "deny");
        Assert.NotNull(json);
        Assert.Contains("shell_escape_blocked", json, StringComparison.Ordinal);
        Assert.Contains(IdeCommands.Build, json, StringComparison.Ordinal);
        Assert.Contains("ide_agent_verify", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TestsOnly_Allows_RunTests()
    {
        Assert.Null(ShellEscapePolicy.TryBlockJson(IdeCommands.RunTests, ShellEscapeTier.TestsOnly));
    }

    [Fact]
    public void TestsOnly_Block_Build()
    {
        var json = ShellEscapePolicy.TryBlockJson(IdeCommands.BuildStructured, ShellEscapeTier.TestsOnly);
        Assert.NotNull(json);
        Assert.Contains("shell_escape_blocked", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AllowWithAudit_Build_NotBlocked()
    {
        Assert.Null(ShellEscapePolicy.TryBlockJson(IdeCommands.RunCodeCleanup, "allow_with_audit"));
    }
}
