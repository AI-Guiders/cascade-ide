#nullable enable

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Какие слои correspondence L0–L4 активны для текущего якоря (ADR 0155 §3).</summary>
public readonly record struct CorrespondenceLayerSignals(
    bool SemanticCanonL0,
    bool FeatureRegistryL1p,
    bool AdrPathMapL1,
    bool CodeIntentGraphL2,
    bool ChangeIntentL3,
    bool DiscourseCodeL4);

/// <summary>Строки PFD: бейдж слоёв и tooltip из каталога.</summary>
public static class CorrespondenceLayersProjection
{
    public static string BuildLayersBadge(CorrespondenceLayerSignals signals)
    {
        var parts = new List<string>(6);
        if (signals.SemanticCanonL0)
            parts.Add(Label("L0"));
        if (signals.FeatureRegistryL1p)
            parts.Add(Label("L1p"));
        if (signals.AdrPathMapL1)
            parts.Add(Label("L1"));
        if (signals.CodeIntentGraphL2)
            parts.Add(Label("L2"));
        if (signals.ChangeIntentL3)
            parts.Add(Label("L3"));
        if (signals.DiscourseCodeL4)
            parts.Add(Label("L4"));

        return parts.Count == 0 ? "" : $"Correspondence: {string.Join(" · ", parts)}";
    }

    public static string BuildLayersTooltip(CorrespondenceLayerSignals signals)
    {
        var lines = new List<string>();
        AppendTooltipLine(lines, signals.SemanticCanonL0, "L0");
        AppendTooltipLine(lines, signals.FeatureRegistryL1p, "L1p");
        AppendTooltipLine(lines, signals.AdrPathMapL1, "L1");
        AppendTooltipLine(lines, signals.CodeIntentGraphL2, "L2");
        AppendTooltipLine(lines, signals.ChangeIntentL3, "L3");
        AppendTooltipLine(lines, signals.DiscourseCodeL4, "L4");
        return lines.Count == 0
            ? "Нет активных слоёв correspondence для этого якоря."
            : string.Join(Environment.NewLine, lines);
    }

    public static CorrespondenceLayerSignals FromCorrespondence(
        bool hasHciOrientation,
        bool hasFeature,
        bool hasAdrDocs,
        bool hasCodeGraph,
        bool hasChangeIntent = false,
        bool hasDiscourse = false) =>
        new(
            SemanticCanonL0: hasHciOrientation,
            FeatureRegistryL1p: hasFeature,
            AdrPathMapL1: hasAdrDocs,
            CodeIntentGraphL2: hasCodeGraph,
            ChangeIntentL3: hasChangeIntent,
            DiscourseCodeL4: hasDiscourse);

    private static void AppendTooltipLine(List<string> lines, bool active, string layerId)
    {
        if (!active)
            return;

        if (CorrespondenceKindsCatalog.TryGetLayer(layerId, out var layer))
            lines.Add($"{layer.Label} — {layer.Title}");
        else
            lines.Add(layerId);
    }

    private static string Label(string layerId) =>
        CorrespondenceKindsCatalog.TryGetLayer(layerId, out var layer) ? layer.Label : layerId;
}
