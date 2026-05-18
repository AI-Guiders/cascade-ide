using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashSolutionNewParserTests
{
    [Fact]
    public void TryParse_solution_new_console_with_name()
    {
        var parse = ChatSlashCommandParser.TryParse("/solution new console MyApp");

        Assert.True(parse.IsSlashLine);
        Assert.False(parse.IsRejected);
        Assert.Equal("solution", parse.Head);
        Assert.Equal("new", parse.Action);
        Assert.Equal("console", parse.SubAction);
        Assert.Equal("MyApp", parse.ArgsTail);
    }

    [Fact]
    public void TryResolve_solution_new_console_maps_to_create_project()
    {
        var parse = ChatSlashCommandParser.TryParse("/solution new console App1");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var descriptor));
        Assert.Equal("create_project_in_solution", descriptor.CommandId);
        Assert.Equal("/solution new console", descriptor.SlashPath);
    }
}
