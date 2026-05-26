using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentEnvironmentServiceTests
{
    private static AgentEnvironmentService CreateService(InMemoryDataBus? bus = null) =>
        new(bus ?? new InMemoryDataBus(), new AgentEnvironmentSettings());

    [Fact]
    public void StartVerify_WithoutSolution_ReturnsError()
    {
        var result = CreateService().StartVerify("", AgentVerifyPolicy.Standard);

        Assert.False(result.Accepted);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void PolicyParser_AcceptsAliases()
    {
        Assert.True(AgentVerifyPolicyParser.TryParse("quick", out var p));
        Assert.Equal(AgentVerifyPolicy.Minimal, p);
    }

    [Fact]
    public void SandboxProfileParser_AcceptsEphemeral()
    {
        Assert.True(AgentSandboxProfileParser.TryParse("ephemeral", out var p));
        Assert.Equal(AgentSandboxProfile.AgentEphemeral, p);
    }

    [Fact]
    public void PrepareSandbox_CreatesRunDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-aee-" + Guid.NewGuid().ToString("N"));
        var manager = new AgentSandboxManager(Path.Combine(root, "runs"));
        var lease = manager.Prepare("test-run", AgentSandboxProfile.AgentEphemeral);
        Assert.True(Directory.Exists(lease.RunDirectory));
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void EnvironmentTaskDedup_CoalescesWithinWindow()
    {
        var dedup = new EnvironmentTaskDedup(1500);
        Assert.False(dedup.ShouldCoalesce("k"));
        Assert.True(dedup.ShouldCoalesce("k"));
    }

    [Fact]
    public void SettingsToml_DeserializesAgentEnvironment()
    {
        const string toml = """
            [agent.environment]
            default_verify_policy = "strict"
            coalesce_window_ms = 2000
            shell_escape_tier = "l3_only"

            [agent.environment.ladder]
            l0_enabled = false
            """;
        var s = CascadeIDE.Services.CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(toml)!;
        Assert.Equal("strict", s.Agent.Environment.DefaultVerifyPolicy);
        Assert.Equal(2000, s.Agent.Environment.CoalesceWindowMs);
        Assert.Equal("l3_only", s.Agent.Environment.ShellEscapeTier);
        Assert.False(s.Agent.Environment.Ladder.L0Enabled);
    }
}
