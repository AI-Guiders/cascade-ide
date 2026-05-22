using CascadeIDE.Features.Chat;

using Xunit;



namespace CascadeIDE.Tests;



public sealed class ChatSlashAutocompleteTests

{

    [Fact]

    public void GetSuggestions_EmptySlash_ReturnsRootNamespacesOnly()

    {

        var suggestions = ChatSlashAutocomplete.GetSuggestions("/");

        Assert.NotEmpty(suggestions);

        Assert.All(suggestions, s => Assert.EndsWith(" ", s.InsertText));

        Assert.Contains(suggestions, s => s.InsertText.Equals("/intercom ", StringComparison.Ordinal));

        Assert.Contains(suggestions, s => s.InsertText.Equals("/build ", StringComparison.Ordinal));

        Assert.Contains(suggestions, s => s.ListTitle == "intercom");

        Assert.Contains(suggestions, s => s.ListTitle == "build");

    }



    [Fact]

    public void GetSuggestions_NotSlash_ReturnsEmpty()

    {

        Assert.Empty(ChatSlashAutocomplete.GetSuggestions("hello"));

    }



    [Fact]

    public void GetSuggestions_SlashOnSecondLine_WithCaret_ReturnsRootNamespaces()

    {

        var text = "привет\n/";

        var suggestions = ChatSlashAutocomplete.GetSuggestions(text, caretIndex: text.Length);

        Assert.NotEmpty(suggestions);

        Assert.Contains(suggestions, s => s.InsertText.Equals("/intercom ", StringComparison.Ordinal));

    }



    [Fact]

    public void GetSuggestions_SlashAfterTextOnSameLine_ReturnsEmpty()

    {

        Assert.Empty(ChatSlashAutocomplete.GetSuggestions("hello /help", caretIndex: 11));

    }



    [Fact]

    public void GetSuggestions_PrefixBuild_FiltersNamespaceCommands()

    {

        var suggestions = ChatSlashAutocomplete.GetSuggestions("/b");

        Assert.Contains(suggestions, s => s.InsertText == "/build ");

        Assert.DoesNotContain(suggestions, s => s.InsertText.StartsWith("/build run", StringComparison.Ordinal));

        Assert.DoesNotContain(suggestions, s => s.SlashPath == "/overview");

    }



    [Fact]

    public void GetSuggestions_BuildNamespace_ListsActions()

    {

        var suggestions = ChatSlashAutocomplete.GetSuggestions("/build ");

        Assert.Equal(3, suggestions.Count);

        Assert.Contains(suggestions, s => s.InsertText == "/build run");

        Assert.Contains(suggestions, s => s.InsertText == "/build ui");

        Assert.Contains(suggestions, s => s.InsertText == "/build structured");

    }



    [Fact]

    public void GetSuggestions_BuildActionPrefix_FiltersRun()

    {

        var suggestions = ChatSlashAutocomplete.GetSuggestions("/build r");

        Assert.Single(suggestions);

        Assert.Equal("/build run", suggestions[0].InsertText);

    }



    [Fact]

    public void GetSuggestions_IntercomPrefix_CompletesDomainOnly()

    {

        var suggestions = ChatSlashAutocomplete.GetSuggestions("/i");

        Assert.Contains(suggestions, s => s.InsertText == "/intercom ");

        Assert.DoesNotContain(suggestions, s => s.InsertText.Contains("topic rename", StringComparison.Ordinal));

    }



    [Fact]

    public void GetSuggestions_IntercomDomain_ListsObjects()

    {

        var suggestions = ChatSlashAutocomplete.GetSuggestions("/intercom ");

        Assert.Contains(suggestions, s => s.InsertText == "/intercom topic ");

        Assert.Contains(suggestions, s => s.InsertText == "/intercom message ");

        Assert.DoesNotContain(suggestions, s => s.InsertText == "/intercom topic rename ");

        Assert.Contains(suggestions, s => s.ListTitle == "topic");

        Assert.Contains(suggestions, s => s.ListTitle == "message");

        Assert.All(suggestions, s => Assert.DoesNotContain('/', s.ListTitle));

    }



    [Fact]

    public void GetSuggestions_IntercomWithoutTrailingSpace_ListsObjectSegmentsOnly()

    {

        var suggestions = ChatSlashAutocomplete.GetSuggestions("/intercom");

        Assert.NotEmpty(suggestions);

        Assert.Contains(suggestions, s => s.ListTitle == "topic");

        Assert.DoesNotContain(suggestions, s => s.ListTitle == "selection");

        Assert.All(suggestions, s => Assert.DoesNotContain('/', s.ListTitle));

    }



    [Fact]
    public void CatalogListsIntercomTopicRename()
    {
        Assert.Contains(
            ChatSlashCommandCatalog.AllSuggestions(),
            s => s.SlashPath == "/intercom topic rename");
    }

    [Fact]
    public void GetSuggestions_IntercomTopicSpace_IncludesRenamePath()
    {
        var paths = ChatSlashAutocomplete.GetSuggestions("/intercom topic ")
            .Select(s => s.SlashPath)
            .ToList();
        Assert.Contains("/intercom topic rename", paths);
    }

    [Fact]
    public void GetSuggestions_IntercomTopicObject_ListsActions()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions("/intercom topic ");
        Assert.Contains(suggestions, s => s.SlashPath == "/intercom topic rename");
        Assert.Contains(suggestions, s => s.InsertText == "/intercom topic rename ");
        Assert.Contains(suggestions, s => s.InsertText == "/intercom topic create ");
        Assert.Contains(suggestions, s => s.InsertText == "/intercom topic open");
        Assert.DoesNotContain(suggestions, s => s.InsertText == "/intercom message select ");
        Assert.Contains(suggestions, s => s.ListTitle == "rename");
        Assert.Contains(suggestions, s => s.ListTitle == "create");
        Assert.All(suggestions, s => Assert.DoesNotContain('/', s.ListTitle));
    }

    [Fact]

    public void GetSuggestions_IntercomTopicRenamePrefix_CompletesActionWithArgSpace()

    {

        var suggestions = ChatSlashAutocomplete.GetSuggestions("/intercom topic r");

        Assert.Single(suggestions);

        Assert.Equal("/intercom topic rename ", suggestions[0].InsertText);

    }



    [Fact]
    public void GetHierarchyContext_RootSlash_ShowsDomainStep()
    {
        var ctx = ChatSlashAutocomplete.GetHierarchyContext("/");
        Assert.NotNull(ctx);
        Assert.Equal("/", ctx.PathPrefix);
        Assert.Equal("домен", ctx.NextStepLabel);
        Assert.Contains("домен", ctx.Breadcrumb, StringComparison.Ordinal);
    }

    [Fact]
    public void GetHierarchyContext_IntercomDomain_ShowsObjectStep()
    {
        var ctx = ChatSlashAutocomplete.GetHierarchyContext("/intercom ");
        Assert.NotNull(ctx);
        Assert.Equal("/intercom", ctx.PathPrefix);
        Assert.Equal("объект", ctx.NextStepLabel);
        Assert.Contains("intercom", ctx.Breadcrumb, StringComparison.Ordinal);
    }

    [Fact]
    public void GetHierarchyContext_IntercomTopic_ShowsIntentStep()
    {
        var ctx = ChatSlashAutocomplete.GetHierarchyContext("/intercom topic ");
        Assert.NotNull(ctx);
        Assert.Equal("/intercom topic", ctx.PathPrefix);
        Assert.Equal("действие", ctx.NextStepLabel);
    }

    [Fact]

    public void TryReplaceSlashLineAtCaret_PreservesTextBeforeSlashLine()

    {

        var text = "привет\n/intercom top";

        var ok = ChatSlashAutocomplete.TryReplaceSlashLineAtCaret(

            text,

            caretIndex: text.Length,

            "/intercom topic ",

            out var newText,

            out var newCaret);



        Assert.True(ok);

        Assert.Equal("привет\n/intercom topic ", newText);

        Assert.Equal(newText.Length, newCaret);

    }

}

