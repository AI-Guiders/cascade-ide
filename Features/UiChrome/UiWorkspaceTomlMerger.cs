using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Слияние слоёв <see cref="UiWorkspaceToml"/> (ADR 0021 §2.1): <c>chrome</c>, <c>routing</c>, <c>workspace_navigation.presets</c>.
/// </summary>
public static class UiWorkspaceTomlMerger
{
    public static UiWorkspaceToml? Merge(UiWorkspaceToml? lower, UiWorkspaceToml? higher)
    {
        if (lower is null && higher is null)
            return null;

        return new UiWorkspaceToml
        {
            Chrome = MergeWorkspaceChrome(lower?.Chrome, higher?.Chrome),
            Routing = MergeRouting(lower?.Routing, higher?.Routing),
            WorkspaceNavigation = MergeWorkspaceNavigation(lower?.WorkspaceNavigation, higher?.WorkspaceNavigation),
        };
    }

    private static UiWorkspaceChromeToml? MergeWorkspaceChrome(
        UiWorkspaceChromeToml? lower,
        UiWorkspaceChromeToml? higher)
    {
        if (lower is null && higher is null)
            return null;

        return new UiWorkspaceChromeToml
        {
            PfdRegionDefaultWidthPixels = higher?.PfdRegionDefaultWidthPixels ?? lower?.PfdRegionDefaultWidthPixels,
            MainGridColumnSplitterWidthPixels = higher?.MainGridColumnSplitterWidthPixels ?? lower?.MainGridColumnSplitterWidthPixels,
            BottomPanelMinRowPixels = higher?.BottomPanelMinRowPixels ?? lower?.BottomPanelMinRowPixels,
            MfdRegionCollapsedWidthPixels = higher?.MfdRegionCollapsedWidthPixels ?? lower?.MfdRegionCollapsedWidthPixels,
            MfdRegionExpandedDefaultWidthPixels = higher?.MfdRegionExpandedDefaultWidthPixels ?? lower?.MfdRegionExpandedDefaultWidthPixels,
            MfdRegionExpandedPowerWidthPixels = higher?.MfdRegionExpandedPowerWidthPixels ?? lower?.MfdRegionExpandedPowerWidthPixels,
            MfdRegionExpandedAgentChatWidthPixels = higher?.MfdRegionExpandedAgentChatWidthPixels ?? lower?.MfdRegionExpandedAgentChatWidthPixels,
            MarkdownPreviewPlacement = higher?.MarkdownPreviewPlacement ?? lower?.MarkdownPreviewPlacement
        };
    }

    private static UiWorkspaceRoutingToml? MergeRouting(
        UiWorkspaceRoutingToml? lower,
        UiWorkspaceRoutingToml? higher)
    {
        var attention = MergeStringDictionary(lower?.Attention, higher?.Attention);
        var instruments = MergeStringDictionary(lower?.Instruments, higher?.Instruments);
        if (attention is null && instruments is null)
            return null;

        return new UiWorkspaceRoutingToml
        {
            Attention = attention,
            Instruments = instruments
        };
    }

    private static Dictionary<string, string>? MergeStringDictionary(
        Dictionary<string, string>? lower,
        Dictionary<string, string>? higher)
    {
        if (lower is not { Count: > 0 } && higher is not { Count: > 0 })
            return null;

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (lower is { Count: > 0 })
        {
            foreach (var kv in lower)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;
                merged[kv.Key.Trim()] = kv.Value ?? "";
            }
        }

        if (higher is { Count: > 0 })
        {
            foreach (var kv in higher)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;
                var v = kv.Value?.Trim();
                if (string.IsNullOrEmpty(v))
                    continue;
                merged[kv.Key.Trim()] = v;
            }
        }

        return merged.Count > 0 ? merged : null;
    }

    private static NavigationSettings? MergeWorkspaceNavigation(NavigationSettings? lower, NavigationSettings? higher)
    {
        if (lower is null && higher is null)
            return null;

        var merged = WorkspaceNavigationPresetsLoader.MergeBundledWithUser(
            lower?.Presets ?? [],
            higher?.Presets ?? []);
        return new NavigationSettings { Presets = merged };
    }
}
