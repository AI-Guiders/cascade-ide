using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class LaunchSettingsJsonImportHeuristicTests
{
    [Theory]
    [InlineData("http://localhost:5000", true)]
    [InlineData("https://127.0.0.1:1", true)]
    [InlineData("http://a;https://b", true)]
    [InlineData("  http://x  ;  ; https://y ", true)]
    [InlineData("ftp://x", false)]
    [InlineData("something-htt-else", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void ApplicationUrlsSuggestKestrelListener_Matches_Expected(string? urls, bool expected) =>
        Assert.Equal(expected, LaunchSettingsJsonImport.ApplicationUrlsSuggestKestrelListener(urls));
}
