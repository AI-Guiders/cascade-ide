using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashSolutionNewParserTests
{
    [Fact]
    public void TryResolveInput_solution_new_console_with_name()
    {
        Assert.True(SlashLineResolver.TryResolveSlashLine("/solution new console MyApp", out var line));
        Assert.Equal("/solution new console", line.CanonicalPath);
        Assert.Equal("MyApp", line.ArgTail);
    }

    [Fact]
    public void TryResolve_solution_new_console_maps_to_create_project()
    {
        ChatSlashCatalogTestSupport.AssertResolves(
            "/solution new console App1",
            "/solution new console",
            "App1");
        Assert.True(
            ChatSlashCommandCatalog.TryResolveInput(
                "/solution new console App1",
                out var descriptor,
                out _));
        Assert.Equal(IdeCommands.CreateProjectInSolution, descriptor.CommandId);
    }
}
