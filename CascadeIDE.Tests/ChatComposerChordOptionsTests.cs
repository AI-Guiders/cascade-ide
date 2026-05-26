using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatComposerChordOptionsTests
{
    [Theory]
    [InlineData("Enter", "Ctrl+Enter")]
    [InlineData("Ctrl+Enter", "Enter")]
    [InlineData("Shift+Enter", "Ctrl+Enter")]
    public void ComplementaryChord_never_equals_send_when_using_standard_labels(string send, string newline)
    {
        Assert.NotEqual(send, newline, StringComparer.Ordinal);
        Assert.Contains(newline, ChatComposerChordOptions.Ordered, StringComparer.Ordinal);
    }
}
