using AgentClientProtocol;
using CascadeIDE.Services.CursorAcp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CascadeAcpMcpServerCatalogTests
{
    [Fact]
    public void FromExternalServersJson_AutonomousStdio_EnabledOnly()
    {
        const string json = """
            [
              {"name":"x","command":"cmd","enabled":false},
              {"name":"git","command":"git-mcp","arguments":["--stdio"],"enabled":true}
            ]
            """;
        var servers = CascadeAcpMcpServerCatalog.FromExternalServersJson(json);
        Assert.Single(servers);
        var s = Assert.IsType<StdioMcpServer>(servers[0]);
        Assert.Equal("git", s.Name);
        Assert.Equal("git-mcp", s.Command);
        Assert.Equal(["--stdio"], s.Args);
        Assert.Empty(s.Env);
    }

    [Fact]
    public void FromExternalServersJson_NativeStdio_Deserializes()
    {
        const string json = """
            [{"name":"n","type":"stdio","command":"c","args":[],"env":[]}]
            """;
        var servers = CascadeAcpMcpServerCatalog.FromExternalServersJson(json);
        Assert.Single(servers);
        Assert.IsType<StdioMcpServer>(servers[0]);
    }

    [Fact]
    public void MergeForAcpNewSession_InjectOff_ReturnsUserOnly()
    {
        const string json = """
            [{"name":"git","command":"x","enabled":true}]
            """;
        var merged = CascadeAcpMcpServerCatalog.MergeForAcpNewSession(json, acpAutoInjectIdeMcp: false);
        Assert.Single(merged);
        Assert.Equal("git", merged[0].Name);
    }

    [Fact]
    public void MergeForAcpNewSession_InjectOn_PrependsIdeWhenNoNameConflict()
    {
        const string json = """
            [{"name":"git","command":"x","enabled":true}]
            """;
        var merged = CascadeAcpMcpServerCatalog.MergeForAcpNewSession(json, acpAutoInjectIdeMcp: true);
        Assert.Equal(2, merged.Length);
        var ide = Assert.IsType<StdioMcpServer>(merged[0]);
        Assert.Equal(CascadeAcpMcpServerCatalog.AutoInjectIdeMcpServerName, ide.Name);
        Assert.False(string.IsNullOrEmpty(ide.Command));
        Assert.Equal(["--mcp-stdio"], ide.Args);
        Assert.Equal("git", merged[1].Name);
    }

    [Fact]
    public void MergeForAcpNewSession_UserCascadeIdeWins_NoDuplicate()
    {
        const string json = """
            [
              {"name":"cascade-ide","command":"custom.exe","arguments":["--mcp-stdio"],"enabled":true}
            ]
            """;
        var merged = CascadeAcpMcpServerCatalog.MergeForAcpNewSession(json, acpAutoInjectIdeMcp: true);
        Assert.Single(merged);
        var s = Assert.IsType<StdioMcpServer>(merged[0]);
        Assert.Equal("cascade-ide", s.Name);
        Assert.Equal("custom.exe", s.Command);
    }
}
