using Avalonia.Controls;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private void UpdateSolutionColumnWidth(bool visible)
    {
        if (this.FindControl<Grid>("MainGrid") is not { } grid)
            return;
        UiWorkspaceLayout.ApplyPfdRegionExpanded(grid, visible);
    }

    private void UpdateChatColumnWidth(ViewModels.MainWindowViewModel vm)
    {
        if (this.FindControl<Grid>("MainGrid") is not { } main)
            return;
        var inner = this.FindControl<Grid>("WorkspaceHealthColumnsGrid");
        // MainGrid: сплиттер и MFD — см. UiWorkspaceLayoutDimensions.MainWindowMainGridColumns (индексы 3 и 4). Пока зона скрыта (в т.ч. Mfd на отдельном TopLevel),
        // не оставляем ширину по «чату» — иначе серая полоса без контента при пресете «P+F на первом дисплее».
        var w = vm.IsCompactPresentationTier
            ? vm.CompactRightChromeColumnPixelWidth
            : vm.IsMfdColumnVisible
                ? vm.MfdRegionPixelWidth
                : 0;
        UiWorkspaceLayout.ApplyMfdRegionColumns(main, inner, w);
    }
}
