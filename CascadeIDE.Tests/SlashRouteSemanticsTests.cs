#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SlashRouteSemanticsTests
{
    [Theory]
    [InlineData("/build run", "solution", "build", "run", SlashPathRole.Alias)]
    [InlineData("/intercom topic list", "intercom", "topic", "list", SlashPathRole.Canonical)]
    [InlineData("/editor select code", "editor", "code", "select", SlashPathRole.Canonical)]
    [InlineData("/map type file", "map", "type", "set", SlashPathRole.Alias)]
    [InlineData("/help", "help", "", "", SlashPathRole.Canonical)]
    public void Resolve_matches_expected(
        string path,
        string domain,
        string obj,
        string intent,
        SlashPathRole role)
    {
        var fields = SlashRouteSemantics.Resolve(path);
        Assert.Equal(domain, fields.Domain, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(obj, fields.Object, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(intent, fields.Intent, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(role, fields.PathRole);
        Assert.True(SlashRouteSemantics.PathMatchesSemantic(path, fields));
    }

    [Fact]
    public void Bundled_catalog_semantics_match_inference()
    {
        foreach (var route in IntentSlashCatalog.SlashRoutes.Values)
        {
            var inferred = SlashRouteSemantics.Resolve(route.SlashPath, route.MapLevel);
            Assert.True(
                SlashRouteSemantics.PathMatchesSemantic(route.SlashPath, route.SemanticFields),
                route.SlashPath);
            Assert.Equal(inferred.Domain, route.Domain, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(inferred.Object, route.Object, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(inferred.Intent, route.Intent, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(inferred.PathRole, route.PathRole);
        }
    }

    [Fact]
    public void GetHierarchyContext_BuildRun_AfterObjectToken_ShowsIntentStep()
    {
        var ctx = ChatSlashAutocomplete.GetHierarchyContext("/build ");
        Assert.NotNull(ctx);
        Assert.Equal("действие", ctx.NextStepLabel);
    }
}
