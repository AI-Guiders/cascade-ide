using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>Codegen trie из intent-catalog.toml совпадает с runtime-каталогом (ADR 0150).</summary>
public sealed class SlashRouteCatalogCodegenSyncTests
{
    [Fact]
    public void Generated_paths_match_runtime_slash_routes()
    {
        var runtime = IntentMelodyAliases.GetCatalogSnapshot().SlashRoutes.Keys
            .Select(IntentSlashCatalog.NormalizeSlashPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var generated = SlashRouteCatalogPathsGenerated.PathsLongestFirst
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(runtime.Count, generated.Count);
        foreach (var path in runtime)
            Assert.Contains(path, generated);
    }

    [Fact]
    public void Generated_arg_tail_kinds_match_runtime_index()
    {
        foreach (var path in SlashRouteCatalogPathsGenerated.PathsLongestFirst)
        {
            Assert.True(SlashRouteCatalogPathsGenerated.TryGetArgTailKind(path, out var gen));
            var runtime = SlashRouteCatalogIndex.GetArgTailKind(path);
            Assert.Equal(runtime, (SlashArgTailKind)gen);
        }
    }

    [Fact]
    public void Generated_trie_resolves_three_token_paths()
    {
        ChatSlashAutocomplete.ParseTypedBodyForResolver(
            "map type file",
            out var tokens,
            out var endsWithSpace);

        Assert.True(SlashRouteCatalogPathsGenerated.TryResolveLongestPrefix(
            tokens,
            endsWithSpace,
            out var path,
            out var tail,
            out var exact,
            out _));

        Assert.Equal("/map type file", path);
        Assert.Equal("", tail);
        Assert.True(exact);
    }
}
