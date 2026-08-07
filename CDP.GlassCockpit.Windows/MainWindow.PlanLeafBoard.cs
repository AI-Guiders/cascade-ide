#nullable enable

using System.Windows;
using System.Windows.Media;

namespace CDP.GlassCockpit.Windows;

/// <summary>P Plan leaf-board instrument — FLY/OPEN/DONE strip + Face cards (not TM text wall).</summary>
public partial class MainWindow
{
    IReadOnlyList<PlanBoardLeaf> _planLeaves = [];
    string? _planBoardFilter = "active";
    bool _planBoardWired;

    void EnsurePlanLeafBoardWired()
    {
        if (_planBoardWired)
            return;
        if (PlanFlyCard is not null)
            PlanFlyCard.MouseLeftButtonUp += (_, _) => SetPlanBoardFilter("fly");
        if (PlanOpenCard is not null)
            PlanOpenCard.MouseLeftButtonUp += (_, _) => SetPlanBoardFilter("open");
        if (PlanDoneCard is not null)
            PlanDoneCard.MouseLeftButtonUp += (_, _) => SetPlanBoardFilter("done");
        _planBoardWired = true;
    }

    void SetPlanBoardFilter(string filter)
    {
        // Second click on same chip → active Face default (hide DONE wall).
        _planBoardFilter = string.Equals(_planBoardFilter, filter, StringComparison.OrdinalIgnoreCase)
            ? "active"
            : filter;
        PaintPlanLeafBoard();
    }

    void ApplyPlanLeaves(IReadOnlyList<PlanBoardLeaf>? leaves)
    {
        EnsurePlanLeafBoardWired();
        _planLeaves = leaves ?? [];
        PaintPlanLeafBoard();
    }

    void PaintPlanLeafBoard()
    {
        var fly = 0;
        var open = 0;
        var done = 0;
        foreach (var leaf in _planLeaves)
        {
            if (leaf.IsFeature)
                continue;
            if (leaf.IsFly) fly++;
            else if (leaf.IsDone) done++;
            else if (leaf.IsOpen) open++;
        }

        if (PlanFlyCount is not null)
            PlanFlyCount.Text = fly.ToString();
        if (PlanOpenCount is not null)
            PlanOpenCount.Text = open.ToString();
        if (PlanDoneCount is not null)
            PlanDoneCount.Text = done.ToString();

        HighlightPlanBoardCard(PlanFlyCard, string.Equals(_planBoardFilter, "fly", StringComparison.OrdinalIgnoreCase), "#4A9EFF", "#1A2430");
        HighlightPlanBoardCard(PlanOpenCard, string.Equals(_planBoardFilter, "open", StringComparison.OrdinalIgnoreCase), "#D7A33C", "#2A2618");
        HighlightPlanBoardCard(PlanDoneCard, string.Equals(_planBoardFilter, "done", StringComparison.OrdinalIgnoreCase), "#5A8F5A", "#1A2E1A");

        if (PlanLeafBoardList is null)
            return;

        PlanLeafBoardList.ItemsSource = PlanBoardLeaf.FaceRows(_planLeaves, _planBoardFilter);
    }

    static void HighlightPlanBoardCard(System.Windows.Controls.Border? card, bool selected, string accentHex, string bgHex)
    {
        if (card is null)
            return;
        card.BorderBrush = (Brush)new BrushConverter().ConvertFromString(selected ? accentHex : "#3A3A3A")!;
        card.BorderThickness = new Thickness(selected ? 2 : 1);
        card.Background = (Brush)new BrushConverter().ConvertFromString(bgHex)!;
    }
}
