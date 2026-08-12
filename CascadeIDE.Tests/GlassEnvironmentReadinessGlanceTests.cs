#nullable enable
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassEnvironmentReadinessGlanceTests
{
    [Fact]
    public void Format_ready_when_dotnet_ok_and_envs_unset()
    {
        var body = GlassEnvironmentReadinessGlance.Format(
            new GlassEnvironmentReadinessGlance.EnvProbeStatus(
                new GlassEnvironmentReadinessGlance.EnvProbeRow("AGENT_NOTES_FILE", "unset", null),
                new GlassEnvironmentReadinessGlance.EnvProbeRow("NETCOREDBG_PATH", "unset", null),
                new GlassEnvironmentReadinessGlance.EnvProbeRow("dotnet", "ok", "dotnet.exe")));

        Assert.Contains("EnvironmentReadiness glance · READY", body);
        Assert.Contains("dotnet · ok · dotnet.exe", body);
        Assert.Contains("■ Glass env probe", body);
        Assert.Contains("□ Avalonia EnvReady", body);
    }

    [Fact]
    public void Format_degraded_when_dbg_path_missing()
    {
        var body = GlassEnvironmentReadinessGlance.Format(
            new GlassEnvironmentReadinessGlance.EnvProbeStatus(
                new GlassEnvironmentReadinessGlance.EnvProbeRow("AGENT_NOTES_FILE", "unset", null),
                new GlassEnvironmentReadinessGlance.EnvProbeRow("NETCOREDBG_PATH", "missing", "bogus.exe"),
                new GlassEnvironmentReadinessGlance.EnvProbeRow("dotnet", "ok", "dotnet.exe")));

        Assert.Contains("EnvironmentReadiness glance · DEGRADED", body);
        Assert.Contains("NETCOREDBG_PATH · missing", body);
    }

    [Fact]
    public void Format_missing_when_dotnet_absent()
    {
        var body = GlassEnvironmentReadinessGlance.Format(
            new GlassEnvironmentReadinessGlance.EnvProbeStatus(
                new GlassEnvironmentReadinessGlance.EnvProbeRow("AGENT_NOTES_FILE", "unset", null),
                new GlassEnvironmentReadinessGlance.EnvProbeRow("NETCOREDBG_PATH", "unset", null),
                new GlassEnvironmentReadinessGlance.EnvProbeRow("dotnet", "missing", "not on PATH")));

        Assert.Contains("EnvironmentReadiness glance · MISSING", body);
        Assert.Contains("dotnet · missing", body);
    }
}
