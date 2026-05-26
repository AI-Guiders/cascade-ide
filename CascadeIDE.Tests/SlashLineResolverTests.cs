using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SlashLineResolverTests
{
    [Theory]
    [InlineData("/intercom server start", "", SlashArgTailKind.Optional, true, true)]
    [InlineData("/intercom server start ", "", SlashArgTailKind.Optional, true, true)]
    [InlineData("/intercom server start http://127.0.0.1:5080", "http://127.0.0.1:5080", SlashArgTailKind.Optional, true, true)]
    [InlineData("/intercom server status", "", SlashArgTailKind.None, true, true)]
    [InlineData("/file open", "", SlashArgTailKind.Required, false, false)]
    [InlineData("/file open src/Foo.cs", "src/Foo.cs", SlashArgTailKind.Required, true, true)]
    [InlineData("/build run", "", SlashArgTailKind.None, true, true)]
    public void ResolveSlashLine_Matrix(
        string line,
        string expectedArgTail,
        SlashArgTailKind expectedKind,
        bool expectedRunnable,
        bool hideSegments)
    {
        Assert.True(SlashLineResolver.TryResolveSlashLine(line, out var r));
        Assert.Equal(expectedArgTail, r.ArgTail);
        Assert.Equal(expectedKind, r.ArgTailKind);
        Assert.Equal(expectedRunnable, r.IsRunnable);
        Assert.Equal(hideSegments, r.ShouldHideSegmentSuggestions);
    }

    [Fact]
    public void ResolveSlashLine_ServerStart_StripsVerbFromParserShape()
    {
        const string line = "/intercom server start http://127.0.0.1:5080";
        var parse = ChatSlashCommandParser.TryParse(line);
        Assert.Equal("server", parse.Action);
        Assert.Contains("start", parse.ArgsTail, StringComparison.Ordinal);

        Assert.True(SlashLineResolver.TryResolveSlashLine(line, out var r));
        Assert.Equal("/intercom server start", r.CanonicalPath);
        Assert.Equal("http://127.0.0.1:5080", r.ArgTail);
    }

    [Fact]
    public void GetArgTailKind_ServerStart_IsOptional_FromCatalog()
    {
        Assert.Equal(SlashArgTailKind.Optional, SlashRouteCatalogIndex.GetArgTailKind("/intercom server start"));
    }

    [Fact]
    public void Autocomplete_InsertsTrailingSpace_ForOptionalArgTail()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/intercom server ");
        var start = suggestions.First(s => s.ListTitle == "start");
        Assert.EndsWith(" ", start.InsertText);
    }
}
