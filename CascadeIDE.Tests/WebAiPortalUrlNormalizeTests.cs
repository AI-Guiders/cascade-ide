using CascadeIDE.Features.WebAiPortal.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WebAiPortalUrlNormalizeTests
{
    [Theory]
    [InlineData("", "about:blank")]
    [InlineData("   ", "about:blank")]
    [InlineData("example.com", "https://example.com/")]
    [InlineData("/example.com", "https://example.com/")]
    [InlineData("//example.com/path", "https://example.com/path")]
    [InlineData("https://example.com/a", "https://example.com/a")]
    public void TryBuildNavigationUri_HappyPath_Normalizes(string input, string expectedUriPrefix)
    {
        Assert.True(WebAiPortalUrlNormalize.TryBuildNavigationUri(input, out var uri, out var normalized));
        Assert.NotNull(uri);
        Assert.StartsWith(expectedUriPrefix.TrimEnd('/'), normalized.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("localhost:5000")]
    [InlineData("127.0.0.1:8080")]
    [InlineData("[::1]:3000")]
    public void TryBuildNavigationUri_Localhost_PrefersHttp(string input)
    {
        Assert.True(WebAiPortalUrlNormalize.TryBuildNavigationUri(input, out var uri, out _));
        Assert.Equal(Uri.UriSchemeHttp, uri!.Scheme);
    }

    [Theory]
    [InlineData("not a url :::")]
    [InlineData(":")]
    public void TryBuildNavigationUri_Invalid_ReturnsFalse(string input) =>
        Assert.False(WebAiPortalUrlNormalize.TryBuildNavigationUri(input, out _, out _));
}
