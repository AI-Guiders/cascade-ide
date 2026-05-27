using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Views;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SettingsCategoryCatalogTests
{
    [Fact]
    public void DiscoverFromAssembly_includes_all_builtin_panels()
    {
        var discovered = SettingsPanelRegistry.DiscoverFromAssembly();
        Assert.Contains(discovered, c => c.Panel == SettingsPanelIds.AiChat);
        Assert.Contains(discovered, c => c.Panel == SettingsPanelIds.Themes);
        Assert.Contains(discovered, c => c.Panel == SettingsPanelIds.Hci);
        Assert.Contains(discovered, c => c.Panel == SettingsPanelIds.WorkspaceNavigationMap);
    }

    [Fact]
    public void LoadOrdered_without_toml_overlay_uses_discovery()
    {
        var ordered = SettingsPanelRegistry.LoadOrdered();
        Assert.True(ordered.Count >= 6);
        Assert.Equal(SettingsPanelIds.Themes, ordered[0].Panel);
    }

    [Fact]
    public void BuildNavigation_inserts_group_headers()
    {
        var nav = SettingsShellView.BuildNavigation(SettingsPanelRegistry.DiscoverFromAssembly());
        Assert.Contains(nav, i => i is Features.Settings.SettingsNavigationGroupHeader g && g.Title == "Агент");
        Assert.Contains(nav, i => i is Features.Settings.SettingsNavigationCategory c && c.Panel == SettingsPanelIds.CSharpLsp);
    }

    [Fact]
    public void Merge_overlay_can_hide_panel()
    {
        var baseList = SettingsCategoryCatalog.LoadBuiltinFallbackList();
        var overlay = new[]
        {
            new SettingsCategoryDefinition { Panel = SettingsPanelIds.Hci, Hidden = true, Order = 1 },
        };
        var merged = SettingsPanelRegistry.Merge(baseList, overlay);
        Assert.DoesNotContain(merged, c => c.Panel == SettingsPanelIds.Hci);
    }
}
