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
            SolutionExplorerDefaultWidthPixels = higher?.SolutionExplorerDefaultWidthPixels ?? lower?.SolutionExplorerDefaultWidthPixels,
            MainGridColumnSplitterWidthPixels = higher?.MainGridColumnSplitterWidthPixels ?? lower?.MainGridColumnSplitterWidthPixels,
            BottomPanelMinRowPixels = higher?.BottomPanelMinRowPixels ?? lower?.BottomPanelMinRowPixels,
            ChatPanelCollapsedWidthPixels = higher?.ChatPanelCollapsedWidthPixels ?? lower?.ChatPanelCollapsedWidthPixels,
            ChatPanelExpandedDefaultWidthPixels = higher?.ChatPanelExpandedDefaultWidthPixels ?? lower?.ChatPanelExpandedDefaultWidthPixels,
            ChatPanelExpandedPowerWidthPixels = higher?.ChatPanelExpandedPowerWidthPixels ?? lower?.ChatPanelExpandedPowerWidthPixels,
            ChatPanelExpandedAgentChatWidthPixels = higher?.ChatPanelExpandedAgentChatWidthPixels ?? lower?.ChatPanelExpandedAgentChatWidthPixels,
            MarkdownPreviewPlacement = higher?.MarkdownPreviewPlacement ?? lower?.MarkdownPreviewPlacement,
            AttentionZonePanels = MergeAttentionPanels(lower?.AttentionZonePanels, higher?.AttentionZonePanels)
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
}
