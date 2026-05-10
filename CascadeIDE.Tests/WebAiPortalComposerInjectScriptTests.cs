using CascadeIDE.Features.WebAiPortal.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WebAiPortalComposerInjectScriptTests
{
    [Fact]
    public void Build_placeholder_replaced_and_handles_unicode()
    {
        var js = WebAiPortalComposerInjectScript.Build("café-привет");
        Assert.DoesNotContain("%%B64%%", js, StringComparison.Ordinal);
        Assert.Contains("atob(", js, StringComparison.Ordinal);
        Assert.Contains("TEXTAREA", js, StringComparison.Ordinal);
    }
}
