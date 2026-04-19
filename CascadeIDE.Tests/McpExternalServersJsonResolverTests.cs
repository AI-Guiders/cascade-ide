using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class McpExternalServersJsonResolverTests
{
    [Fact]
    public void Resolve_uses_inline_when_path_empty()
    {
        var settings = new CascadeIdeSettings
        {
            Mcp = new McpSettings { ExternalServersJson = """[{"name":"a","command":"x","arguments":[],"enabled":true}]""" },
        };
        Assert.Equal(settings.Mcp.ExternalServersJson, McpExternalServersJsonResolver.ResolveEffectiveJson(settings));
    }

    [Fact]
    public void Resolve_uses_file_when_exists()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascade-mcp-json-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "servers.json");
        var payload = """[{"name":"fromfile","command":"cmd.exe","arguments":[],"enabled":true}]""";
        File.WriteAllText(file, payload);
        try
        {
            var settings = new CascadeIdeSettings
            {
                Mcp = new McpSettings { ExternalServersJson = "[]", ExternalServersJsonPath = file },
            };
            Assert.Equal(payload, McpExternalServersJsonResolver.ResolveEffectiveJson(settings));
        }
        finally
        {
            try { File.Delete(file); } catch { /* ignore */ }
            try { Directory.Delete(dir); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Resolve_falls_back_to_inline_when_file_missing()
    {
        var settings = new CascadeIdeSettings
        {
            Mcp = new McpSettings
            {
                ExternalServersJson = """[{"name":"inline","command":"y","arguments":[],"enabled":true}]""",
                ExternalServersJsonPath = Path.Combine(Path.GetTempPath(), "nonexistent-" + Guid.NewGuid().ToString("N") + ".json"),
            },
        };
        Assert.Equal(settings.Mcp.ExternalServersJson, McpExternalServersJsonResolver.ResolveEffectiveJson(settings));
    }
}
