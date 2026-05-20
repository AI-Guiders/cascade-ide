#nullable enable

using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatBracketAutocompleteTests
{
    [Fact]
    public void TryGetEditState_detects_unclosed_bracket_at_caret()
    {
        var text = "see [M:Ru";
        Assert.True(ChatBracketAutocomplete.TryGetEditState(text, text.Length, out var state));
        Assert.Equal(ChatBracketAutocomplete.Axis.Member, state.ActiveAxis);
        Assert.Equal("Ru", state.AxisPrefix);
    }

    [Fact]
    public void GetSuggestions_member_prefix_after_M_colon()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(ChatBracketAutocompleteTests) + "_" + Guid.NewGuid().ToString("N"))).FullName;
        var file = Path.Combine(dir, "Foo.cs");
        File.WriteAllText(file, "class C { public void Run() { } }");

        var rel = "Foo.cs";
        var text = $"[{rel} M:R";
        var suggestions = ChatBracketAutocomplete.GetSuggestions(
            text,
            text.Length,
            file,
            dir,
            workspaceFiles: null);

        Assert.Contains(suggestions, s => s.Display == "Run");
    }

    [Fact]
    public void GetSuggestions_scope_after_S_colon()
    {
        var text = "[M:Run S:f";
        var suggestions = ChatBracketAutocomplete.GetSuggestions(text, text.Length, null, null, null);
        Assert.Contains(suggestions, s => s.Display.StartsWith("S:for:", StringComparison.Ordinal));
    }
}
