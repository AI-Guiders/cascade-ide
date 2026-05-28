using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Слияние слоёв <see cref="UiWorkspaceToml"/> (ADR 0021 §2.1): <c>chrome</c>, <c>loc_limits</c>, <c>routing</c>, <c>code_navigation.presets</c>.
/// </summary>
public static class UiWorkspaceTomlMerger
{
    public static UiWorkspaceToml? Merge(UiWorkspaceToml? lower, UiWorkspaceToml? higher)
    {
        if (lower is null && higher is null)
            return null;

        return new UiWorkspaceToml
        {
            Workspace = MergeWorkspace(lower?.Workspace, higher?.Workspace),
            Chrome = MergeWorkspaceChrome(lower?.Chrome, higher?.Chrome),
            LocLimits = MergeLocLimits(lower?.LocLimits, higher?.LocLimits),
            Routing = MergeRouting(lower?.Routing, higher?.Routing),
            CodeNavigation = MergeCodeNavigation(lower?.CodeNavigation, higher?.CodeNavigation),
        };
    }

    private static UiWorkspaceWorkspaceToml? MergeWorkspace(
        UiWorkspaceWorkspaceToml? lower,
        UiWorkspaceWorkspaceToml? higher)
    {
        if (lower is null && higher is null)
            return null;

        return new UiWorkspaceWorkspaceToml
        {
            Adr = MergeWorkspaceAdr(lower?.Adr, higher?.Adr),
            Features = MergeWorkspaceFeatures(lower?.Features, higher?.Features),
        };
    }

    private static UiWorkspaceAdrToml? MergeWorkspaceAdr(
        UiWorkspaceAdrToml? lower,
        UiWorkspaceAdrToml? higher)
    {
        var map = MergeObjectDictionary(lower?.Map, higher?.Map);
        if (map is null)
            return null;
        return new UiWorkspaceAdrToml
        {
            AutoInclude = higher?.AutoInclude ?? lower?.AutoInclude,
            MaxRelated = higher?.MaxRelated ?? lower?.MaxRelated,
            Map = map
        };
    }

    private static UiWorkspaceFeaturesToml? MergeWorkspaceFeatures(
        UiWorkspaceFeaturesToml? lower,
        UiWorkspaceFeaturesToml? higher)
    {
        if (lower?.Feature is not { Count: > 0 } && higher?.Feature is not { Count: > 0 })
            return null;

        var merged = new Dictionary<string, UiWorkspaceFeatureToml>(StringComparer.OrdinalIgnoreCase);
        if (lower?.Feature is { Count: > 0 })
        {
            foreach (var f in lower.Feature)
            {
                var id = (f.Id ?? "").Trim();
                if (id.Length == 0)
                    continue;
                merged[id] = CloneFeature(f);
            }
        }

        if (higher?.Feature is { Count: > 0 })
        {
            foreach (var f in higher.Feature)
            {
                var id = (f.Id ?? "").Trim();
                if (id.Length == 0)
                    continue;
                merged[id] = CloneFeature(f); // higher overrides by id
            }
        }

        return merged.Count == 0 ? null : new UiWorkspaceFeaturesToml { Feature = merged.Values.ToList() };
    }

    private static UiWorkspaceFeatureToml CloneFeature(UiWorkspaceFeatureToml f) => new()
    {
        Id = f.Id?.Trim(),
        Title = f.Title?.Trim(),
        Paths = f.Paths?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [],
        Docs = f.Docs?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [],
        Tags = f.Tags?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [],
    };

    private static Dictionary<string, object>? MergeObjectDictionary(
        Dictionary<string, object>? lower,
        Dictionary<string, object>? higher)
    {
        if (lower is not { Count: > 0 } && higher is not { Count: > 0 })
            return null;

        var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (lower is { Count: > 0 })
        {
            foreach (var kv in lower)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;
                merged[kv.Key.Trim()] = kv.Value;
            }
        }

        if (higher is { Count: > 0 })
        {
            foreach (var kv in higher)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;
                merged[kv.Key.Trim()] = kv.Value;
            }
        }

        return merged.Count > 0 ? merged : null;
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

    private static UiWorkspaceLocLimitsToml? MergeLocLimits(
        UiWorkspaceLocLimitsToml? lower,
        UiWorkspaceLocLimitsToml? higher)
    {
        if (lower is null && higher is null)
            return null;

        return new UiWorkspaceLocLimitsToml
        {
            MediumMin = higher?.MediumMin ?? lower?.MediumMin,
            HighMin = higher?.HighMin ?? lower?.HighMin
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

    private static CodeNavigationSettings? MergeCodeNavigation(CodeNavigationSettings? lower, CodeNavigationSettings? higher)
    {
        if (lower is null && higher is null)
            return null;

        var merged = CodeNavigationPresetsLoader.MergeBundledWithUser(
            lower?.Presets ?? [],
            higher?.Presets ?? []);
        return new CodeNavigationSettings { Presets = merged };
    }
}
