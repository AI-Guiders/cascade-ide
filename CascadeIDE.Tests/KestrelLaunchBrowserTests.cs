using System.Collections.Generic;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class KestrelLaunchBrowserTests
{
    [Fact]
    public void ResolveUrl_Without_LaunchUrl_Uses_First_AspNetCore_Url()
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASPNETCORE_URLS"] = "https://localhost:7208;http://localhost:33300"
        };
        var u = KestrelLaunchBrowser.ResolveUrlToOpen(env, launchUrl: null);
        Assert.Equal("https://localhost:7208", u);
    }

    [Fact]
    public void ResolveUrl_Absolute_LaunchUrl_Ignores_Env()
    {
        var u = KestrelLaunchBrowser.ResolveUrlToOpen(
            environment: null,
            "http://localhost:33300/graphql");
        Assert.Equal("http://localhost:33300/graphql", u);
    }

    [Fact]
    public void ResolveUrl_Leading_Slash_Combines_With_Base_Authority()
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASPNETCORE_URLS"] = "http://localhost:33300"
        };
        var u = KestrelLaunchBrowser.ResolveUrlToOpen(env, "/graphql");
        Assert.Equal("http://localhost:33300/graphql", u);
    }

    [Fact]
    public void ResolveUrl_Leading_Slash_With_Query_Uses_UriBuilder()
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASPNETCORE_URLS"] = "http://localhost:33300"
        };
        var u = KestrelLaunchBrowser.ResolveUrlToOpen(env, "/graphql?op=1");
        Assert.Equal("http://localhost:33300/graphql?op=1", u);
    }
}
