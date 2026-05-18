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
    public void TryParse_EditorLineSelect_WithRange()
    {
        var r = ChatSlashCommandParser.TryParse("/editor line select 5 10");
        Assert.True(r.IsSlashLine);
        Assert.False(r.IsRejected);
        Assert.Equal("editor", r.Head);
        Assert.Equal("line", r.Action);
        Assert.Equal("select", r.SubAction);
        Assert.Equal("5 10", r.ArgsTail);
    }

    [Fact]
    public void Catalog_ResolvesEditorLineSelect()
    {
        var parse = ChatSlashCommandParser.TryParse("/editor line select 5");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal("/editor line select", d.SlashPath);
        Assert.Equal(IdeCommands.Select, d.CommandId);
    }

    [Fact]
    public void Catalog_ResolvesPortalOpen()
    {
        var parse = ChatSlashCommandParser.TryParse("/portal open https://example.com");
        Assert.True(ChatSlashCommandCatalog.TryResolve(parse, out var d));
        Assert.Equal("/portal open", d.SlashPath);
        Assert.Equal(IdeCommands.ShowWebAiPortalPage, d.CommandId);
        Assert.Equal("https://example.com", parse.ArgsTail);
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
