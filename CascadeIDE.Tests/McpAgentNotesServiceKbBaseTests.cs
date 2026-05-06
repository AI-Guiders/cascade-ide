using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class McpAgentNotesServiceKbBaseTests
{
    [Fact]
    public void ReadKnowledgeFile_LoadsEmbeddedSmoke_WhenAgentNotesCanonEnvUnset()
    {
        var oldCanon = Environment.GetEnvironmentVariable("AGENT_NOTES_CANON_PATH");
        try
        {
            Environment.SetEnvironmentVariable("AGENT_NOTES_CANON_PATH", null);

            var svc = new McpAgentNotesService();
            var text = svc.ReadKnowledgeFile("kb-base-cide-smoke.md").Trim();

            Assert.Equal("kb-base-cide-smoke", text);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AGENT_NOTES_CANON_PATH", oldCanon);
        }
    }
}
