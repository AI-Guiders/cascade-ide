namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Слияние слоёв <see cref="UiWorkspaceToml"/> (ADR 0021 §2.1): верхний слой переопределяет нижний по полям <c>workspace_chrome</c>, по ключам <c>attention_routing</c> и <c>instrument_routing</c>.
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
            WorkspaceChrome = MergeWorkspaceChrome(lower?.WorkspaceChrome, higher?.WorkspaceChrome),
            AttentionRouting = MergeAttentionRouting(lower?.AttentionRouting, higher?.AttentionRouting),
            InstrumentRouting = MergeInstrumentRouting(lower?.InstrumentRouting, higher?.InstrumentRouting)
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

    private static Dictionary<string, string>? MergeAttentionRouting(
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

    private static Dictionary<string, string>? MergeInstrumentRouting(
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
}
