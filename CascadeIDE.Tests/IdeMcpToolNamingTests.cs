using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeMcpToolNamingTests
{
    [Theory]
    [InlineData("intercom.reveal_attachment", "ide_intercom_reveal_attachment")]
    [InlineData("cockpit.open_command_line", "ide_cockpit_open_command_line")]
    [InlineData("editor.select_code", "ide_editor_select_code")]
    public void ToToolName_replaces_dots_with_underscores(string commandId, string expectedTool)
    {
        Assert.Equal(expectedTool, IdeMcpToolNaming.ToToolName(commandId));
        Assert.True(expectedTool.Length <= IdeMcpToolNaming.MaxToolNameLength);
        Assert.DoesNotContain('.', IdeMcpToolNaming.ToToolName(commandId));
    }

    [Fact]
    public void ToToolName_chat_toggle_spine_uses_short_alias()
    {
        var tool = IdeMcpToolNaming.ToToolName(IdeCommands.ChatToggleProductSpineInAgentContext);
        Assert.Equal("ide_chat_toggle_spine_ctx", tool);
        Assert.True(tool.Length <= IdeMcpToolNaming.MaxToolNameLength);
    }

    [Theory]
    [InlineData("ide_intercom_reveal_attachment", "intercom.reveal_attachment")]
    [InlineData("ide_chat_toggle_spine_ctx", "chat_toggle_product_spine_in_agent_context")]
    public void TryToCommandId_roundtrips(string toolName, string commandId)
    {
        Assert.True(IdeMcpToolNaming.TryToCommandId(toolName, out var resolved));
        Assert.Equal(commandId, resolved);
    }

    [Fact]
    public void BuildGeneratedProxyTools_skips_dotted_duplicate_when_rich_tool_exists()
    {
        var existing = new HashSet<string>(StringComparer.Ordinal) { "ide_intercom_reveal_attachment" };
        var proxies = IdeMcpToolCatalog.BuildGeneratedProxyTools(existing).ToList();
        Assert.DoesNotContain(proxies, t => t.Name.Contains('.', StringComparison.Ordinal));
        Assert.DoesNotContain(proxies, t => t.Name == "ide_intercom.reveal_attachment");
    }
}
