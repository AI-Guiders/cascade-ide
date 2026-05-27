using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashParametricTests
{
    [Theory]
    [InlineData("5", 5, 5)]
    [InlineData("5 10", 5, 10)]
    [InlineData("5:10", 5, 10)]
    [InlineData("5;10", 5, 10)]
    public void TryParseLineRangeTail_AcceptsCommonForms(string tail, int start, int end)
    {
        Assert.True(ChatSlashParametricArgsBuilder.TryParseLineRangeTail(tail, out var s, out var e, out _));
        Assert.Equal(start, s);
        Assert.Equal(end, e);
    }

    [Fact]
    public void Catalog_lists_all_parametric_commands()
    {
        Assert.True(IntentMelodyCatalog.TryGetParametricRootByCommandId(IdeCommands.Select, out var select));
        Assert.Equal("els", select.Slug);
        Assert.True(IntentMelodyCatalog.TryGetParametricRootByCommandId(IdeCommands.ApplyEdit, out var edit));
        Assert.Equal("eld", edit.Slug);
        Assert.True(IntentMelodyCatalog.TryGetParametricRootByCommandId(IdeCommands.ShowWebAiPortalPage, out var portal));
        Assert.Equal("wai", portal.Slug);
    }

    [Fact]
    public void ResolveInput_EditorLineSelect_WithRange()
    {
        Assert.True(SlashLineResolver.TryResolveSlashLine("/editor line select 5 10", out var line));
        Assert.Equal("/editor line select", line.CanonicalPath);
        Assert.Equal("5 10", line.ArgTail);
    }

    [Fact]
    public void Catalog_ResolvesEditorLineSelect()
    {
        ChatSlashCatalogTestSupport.AssertResolves("/editor line select 5", "/editor line select", "5");
        Assert.True(
            ChatSlashCommandCatalog.TryResolveInput("/editor line select 5", out var d, out _));
        Assert.Equal(IdeCommands.Select, d.CommandId);
    }

    [Fact]
    public void Catalog_ResolvesPortalOpen()
    {
        ChatSlashCatalogTestSupport.AssertResolves(
            "/portal open https://example.com",
            "/portal open",
            "https://example.com");
        Assert.True(
            ChatSlashCommandCatalog.TryResolveInput("/portal open https://example.com", out var d, out _));
        Assert.Equal(IdeCommands.ShowWebAiPortalPage, d.CommandId);
    }

    [Fact]
    public async Task TryRun_EditorLineSelect_BuildsLineArgs()
    {
        const string file = @"C:\proj\a.cs";
        const string text = "one\ntwo\nthree\n";
        string? capturedId = null;
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? capturedArgs = null;

        var runner = new ChatSlashCommandRunner(
            (id, args, _) =>
            {
                capturedId = id;
                capturedArgs = args;
                return Task.FromResult("");
            },
            () => new ChatSlashEditorContext(file, text));

        var result = await runner.TryRunAsync("/editor line select 2 2");
        Assert.True(result.Success);
        Assert.Equal(IdeCommands.Select, capturedId);
        Assert.NotNull(capturedArgs);
        Assert.Equal(file, capturedArgs!["file_path"].GetString());
        Assert.Equal(2, capturedArgs["start_line"].GetInt32());
        Assert.Equal(2, capturedArgs["end_line"].GetInt32());
    }

    [Fact]
    public async Task TryRun_PortalOpen_WithUrl()
    {
        string? capturedId = null;
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? capturedArgs = null;

        var runner = new ChatSlashCommandRunner(
            (id, args, _) =>
            {
                capturedId = id;
                capturedArgs = args;
                return Task.FromResult("");
            },
            () => new ChatSlashEditorContext(null, null));

        var result = await runner.TryRunAsync("/portal open example.com");
        Assert.True(result.Success);
        Assert.Equal(IdeCommands.ShowWebAiPortalPage, capturedId);
        Assert.NotNull(capturedArgs);
        Assert.Equal("example.com", capturedArgs!["url"].GetString());
    }

    [Fact]
    public async Task TryRun_GitCommit_StripsOptionalQuotes()
    {
        string? message = null;
        var runner = new ChatSlashCommandRunner(
            (id, args, _) =>
            {
                Assert.Equal(IdeCommands.GitCommit, id);
                message = args!["message"].GetString();
                return Task.FromResult("");
            },
            () => new ChatSlashEditorContext(null, null));

        var result = await runner.TryRunAsync("/git commit \"feat: test\"");
        Assert.True(result.Success);
        Assert.Equal("feat: test", message);
    }

    [Fact]
    public async Task TryRun_PortalOpen_WithoutUrl_AllowsNullArgs()
    {
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? capturedArgs = new Dictionary<string, System.Text.Json.JsonElement>();

        var runner = new ChatSlashCommandRunner(
            (_, args, _) =>
            {
                capturedArgs = args;
                return Task.FromResult("");
            },
            () => new ChatSlashEditorContext(null, null));

        var result = await runner.TryRunAsync("/portal open");
        Assert.True(result.Success);
        Assert.Null(capturedArgs);
    }
}
