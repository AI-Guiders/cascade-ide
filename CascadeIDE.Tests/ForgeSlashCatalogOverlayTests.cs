using CascadeIDE.Features.Chat;
using CascadeIDE.Services.Forge;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ForgeSlashCatalogOverlayTests
{
    public ForgeSlashCatalogOverlayTests()
    {
        ForgeSlashCatalogOverlay.Clear();
    }

    [Fact]
    public void Overlay_resolves_forge_issue_open_path()
    {
        ForgeSlashCatalogOverlay.ApplyForTests(
        [
            new ForgeCapabilitiesCommand
            {
                Domain = "forge",
                Object = "issue",
                Intent = "open",
                CommandId = "forge.issue.open",
                Path = "/issue open",
                PathAliases = ["/forge issue open"],
                Help = "Open issue by number.",
                ArgTail = "required",
            },
        ]);

        Assert.True(SlashLineResolver.TryResolveSlashLine("/forge issue open 3", out var resolution));
        Assert.Equal("/forge issue open", resolution.CanonicalPath);
        Assert.Equal("3", resolution.ArgTail);
        Assert.Equal(SlashArgTailKind.Required, resolution.ArgTailKind);
        Assert.True(resolution.IsRunnable);
    }

    [Fact]
    public void Catalog_resolves_overlay_descriptor()
    {
        ForgeSlashCatalogOverlay.ApplyForTests(
        [
            new ForgeCapabilitiesCommand
            {
                Object = "repo",
                Intent = "open",
                CommandId = "forge.repo.open",
                Path = "/repo open",
                PathAliases = ["/forge repo open"],
                Help = "Open repo view.",
                ArgTail = "optional",
            },
        ]);

        Assert.True(ChatSlashCommandCatalog.TryResolveInput("/forge repo open", out var descriptor, out _));
        Assert.Equal("forge.repo.open", descriptor.CommandId);
        Assert.Equal(ChatSlashCommandExecutionKind.ForgeCommand, descriptor.ExecutionKind);
    }

    [Fact]
    public void Clear_removes_overlay_paths()
    {
        ForgeSlashCatalogOverlay.ApplyForTests(
        [
            new ForgeCapabilitiesCommand
            {
                Object = "issue",
                Intent = "open",
                CommandId = "forge.issue.open",
                Path = "/issue open",
                PathAliases = ["/forge issue open"],
                Help = "Open issue.",
            },
        ]);

        ForgeSlashCatalogOverlay.Clear();
        Assert.False(SlashLineResolver.TryResolveSlashLine("/forge issue open 1", out _));
    }

    [Fact]
    public void Overlay_resolves_forge_artifact_goto()
    {
        ForgeSlashCatalogOverlay.ApplyForTests(
        [
            new ForgeCapabilitiesCommand
            {
                Object = "artifact",
                Intent = "goto",
                CommandId = "forge.artifact.goto",
                Path = "/artifact goto",
                PathAliases = ["/forge artifact goto"],
                Help = "Open [FRG:…] artifact.",
                ArgTail = "required",
            },
        ]);

        Assert.True(ChatSlashCommandCatalog.TryResolveInput("/forge artifact goto [FRG:pilot/issues/1]", out var descriptor, out _));
        Assert.Equal("forge.artifact.goto", descriptor.CommandId);
    }
}
