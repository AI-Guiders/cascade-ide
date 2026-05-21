#nullable enable

using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatThreadNavigatorFilterTests
{
    [Fact]
    public void FilterNavigatorRows_MatchesTitle()
    {
        var rows = new[]
        {
            new ChatThreadPresentation.PickerRow(Guid.NewGuid(), "MFD UI", "1 · abc", 0),
            new ChatThreadPresentation.PickerRow(Guid.NewGuid(), "Channel", "2 · def", 0),
        };

        var filtered = ChatThreadPresentation.FilterNavigatorRows(rows, "mfd");
        Assert.Single(filtered);
        Assert.Equal("MFD UI", filtered[0].Title);
    }

    [Fact]
    public void FilterNavigatorRows_EmptyQuery_ReturnsAll()
    {
        var rows = new[]
        {
            new ChatThreadPresentation.PickerRow(Guid.NewGuid(), "A", "", 0),
            new ChatThreadPresentation.PickerRow(Guid.NewGuid(), "B", "", 0),
        };

        Assert.Equal(2, ChatThreadPresentation.FilterNavigatorRows(rows, "").Count);
        Assert.Equal(2, ChatThreadPresentation.FilterNavigatorRows(rows, null).Count);
    }
}
