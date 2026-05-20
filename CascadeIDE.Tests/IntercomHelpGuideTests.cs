#nullable enable

using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomHelpGuideTests
{
    [Fact]
    public void FormatFull_mentions_brackets_attach_and_local_hint()
    {
        var text = IntercomHelpGuide.FormatFull();
        Assert.Contains("[M:", text, StringComparison.Ordinal);
        Assert.Contains("/attach", text, StringComparison.Ordinal);
        Assert.Contains("/help", text, StringComparison.Ordinal);
        Assert.Contains("audience: self", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(IntercomHelpGuide.BundledRelativePath, text, StringComparison.Ordinal);
    }
}
