using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Контракт discoverability: навигация на фиксированные страницы вторичного контура
/// должна быть и в палитре, и в Command Melody (<c>c:</c>). Новую такую команду —
/// сюда + <see cref="IdeCommandRegistry"/> + <c>intent-melody-aliases.toml</c>.
/// </summary>
public sealed class IdeCommandPaletteMelodyContractTests
{
    private static readonly string[] MfdPageNavigationCommandIds =
    [
        IdeCommands.ShowEnvironmentReadinessPage,
        IdeCommands.ShowHybridIndexPage,
        IdeCommands.ShowWebAiPortalPage,
    ];

    [Fact]
    public void Mfd_page_navigation_commands_have_palette_catalog_entries()
    {
        var catalogIds = IdeCommandPaletteCatalog.All.Select(e => e.CommandId).ToHashSet(StringComparer.Ordinal);
        foreach (var id in MfdPageNavigationCommandIds)
            Assert.Contains(id, catalogIds);
    }

    [Fact]
    public void Mfd_page_navigation_commands_have_at_least_one_intent_melody_alias()
    {
        var counts = IntentMelodyAliases.AllPairs()
            .GroupBy(p => p.CommandId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var id in MfdPageNavigationCommandIds)
        {
            Assert.True(
                counts.TryGetValue(id, out var n) && n >= 1,
                $"Ожидался хотя бы один alias в IntentMelody для «{id}» (файл intent-melody-aliases.toml).");
        }
    }
}
