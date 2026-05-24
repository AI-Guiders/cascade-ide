using IntercomService.Services;
using Xunit;

namespace IntercomService.Tests;

public sealed class OAuthRedirectAllowlistTests
{
    [Theory]
    [InlineData("http://127.0.0.1:54321/callback", true)]
    [InlineData("http://localhost:8080/callback", true)]
    [InlineData("https://evil.example/callback", false)]
    [InlineData("http://example.com/callback", false)]
    public void IsAllowed_loopback_only(string redirectUri, bool expected) =>
        Assert.Equal(expected, OAuthRedirectAllowlist.IsAllowed(redirectUri));
}
