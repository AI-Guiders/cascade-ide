namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Слияние слоёв <see cref="UiWorkspaceToml"/> (ADR 0021 §2.1): верхний слой переопределяет нижний по полям и по ключам <c>attention_zone_panels</c>.
/// </summary>
public static class UiWorkspaceTomlMerger
{
    /// <summary>
    /// Объединяет два фрагмента; <paramref name="higher"/> имеет приоритет. Оба могут быть <see langword="null"/>.</summary>
    public static UiWorkspaceToml? Merge(UiWorkspaceToml? lower, UiWorkspaceToml? higher)
    {
        if (lower is null && higher is null)
            return null;

        return new UiWorkspaceToml
        {
            PfdRegionDefaultWidthPixels = higher?.PfdRegionDefaultWidthPixels ?? lower?.PfdRegionDefaultWidthPixels,
            MainGridColumnSplitterWidthPixels = higher?.MainGridColumnSplitterWidthPixels ?? lower?.MainGridColumnSplitterWidthPixels,
            BottomPanelMinRowPixels = higher?.BottomPanelMinRowPixels ?? lower?.BottomPanelMinRowPixels,
            MfdRegionCollapsedWidthPixels = higher?.MfdRegionCollapsedWidthPixels ?? lower?.MfdRegionCollapsedWidthPixels,
            MfdRegionExpandedDefaultWidthPixels = higher?.MfdRegionExpandedDefaultWidthPixels ?? lower?.MfdRegionExpandedDefaultWidthPixels,
            MfdRegionExpandedPowerWidthPixels = higher?.MfdRegionExpandedPowerWidthPixels ?? lower?.MfdRegionExpandedPowerWidthPixels,
            MfdRegionExpandedAgentChatWidthPixels = higher?.MfdRegionExpandedAgentChatWidthPixels ?? lower?.MfdRegionExpandedAgentChatWidthPixels,
            MarkdownPreviewPlacement = higher?.MarkdownPreviewPlacement ?? lower?.MarkdownPreviewPlacement,
            AttentionZonePanels = MergeAttentionPanels(lower?.AttentionZonePanels, higher?.AttentionZonePanels),
            InstrumentPlacementRules = MergeInstrumentPlacementRules(lower?.InstrumentPlacementRules, higher?.InstrumentPlacementRules)
        };
    }

    private static Dictionary<string, string>? MergeAttentionPanels(
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

    private static List<Models.InstrumentPlacementRuleSettings>? MergeInstrumentPlacementRules(
        List<Models.InstrumentPlacementRuleSettings>? lower,
        List<Models.InstrumentPlacementRuleSettings>? higher)
    {
        if (lower is not { Count: > 0 } && higher is not { Count: > 0 })
            return null;

        var merged = new List<Models.InstrumentPlacementRuleSettings>();
        var byKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        static string BuildKey(Models.InstrumentPlacementRuleSettings r) =>
            $"{r.SurfaceId.Trim().ToLowerInvariant()}::{r.SlotId.Trim().ToLowerInvariant()}";

        static bool IsValid(Models.InstrumentPlacementRuleSettings r) =>
            !string.IsNullOrWhiteSpace(r.SurfaceId)
            && !string.IsNullOrWhiteSpace(r.SlotId)
            && !string.IsNullOrWhiteSpace(r.InstrumentId);

        void AddRange(List<Models.InstrumentPlacementRuleSettings>? source)
        {
            if (source is not { Count: > 0 })
                return;

            foreach (var row in source)
            {
                if (row is null || !IsValid(row))
                    continue;

                var normalized = new Models.InstrumentPlacementRuleSettings
                {
                    SurfaceId = row.SurfaceId.Trim(),
                    SlotId = row.SlotId.Trim(),
                    InstrumentId = row.InstrumentId.Trim()
                };
                var key = BuildKey(normalized);
                if (byKey.TryGetValue(key, out var existingIndex))
                    merged[existingIndex] = normalized;
                else
                {
                    byKey[key] = merged.Count;
                    merged.Add(normalized);
                }
            }
        }

        AddRange(lower);
        AddRange(higher);
        return merged.Count > 0 ? merged : null;
    }
}
