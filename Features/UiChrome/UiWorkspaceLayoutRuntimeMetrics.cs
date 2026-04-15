namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Глобальные метрики хрома, подгружаемые из <c>UiModes/workspace.toml</c> (ADR 0010); по умолчанию — <see cref="UiWorkspaceLayoutDimensions"/>.
/// </summary>
public static class UiWorkspaceLayoutRuntimeMetrics
{
    public static int PfdRegionDefaultWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.PfdRegionDefaultWidthPixels;

    public static double MainGridColumnSplitterWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.MainGridColumnSplitterWidthPixels;

    public static int BottomPanelMinRowPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.BottomPanelMinRowPixels;

    public static int MfdRegionCollapsedWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.MfdRegionCollapsedWidthPixels;

    public static int MfdRegionExpandedDefaultWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.MfdRegionExpandedDefaultWidthPixels;

    public static int MfdRegionExpandedPowerWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.MfdRegionExpandedPowerWidthPixels;

    public static int MfdRegionExpandedAgentChatWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.MfdRegionExpandedAgentChatWidthPixels;

    internal static void ResetToCodeDefaults()
    {
        PfdRegionDefaultWidthPixels = UiWorkspaceLayoutDimensions.PfdRegionDefaultWidthPixels;
        MainGridColumnSplitterWidthPixels = UiWorkspaceLayoutDimensions.MainGridColumnSplitterWidthPixels;
        BottomPanelMinRowPixels = UiWorkspaceLayoutDimensions.BottomPanelMinRowPixels;
        MfdRegionCollapsedWidthPixels = UiWorkspaceLayoutDimensions.MfdRegionCollapsedWidthPixels;
        MfdRegionExpandedDefaultWidthPixels = UiWorkspaceLayoutDimensions.MfdRegionExpandedDefaultWidthPixels;
        MfdRegionExpandedPowerWidthPixels = UiWorkspaceLayoutDimensions.MfdRegionExpandedPowerWidthPixels;
        MfdRegionExpandedAgentChatWidthPixels = UiWorkspaceLayoutDimensions.MfdRegionExpandedAgentChatWidthPixels;
    }

    internal static void ApplyWorkspaceToml(UiWorkspaceToml? w)
    {
        ResetToCodeDefaults();
        if (w is null)
            return;
        if (w.PfdRegionDefaultWidthPixels is { } se)
            PfdRegionDefaultWidthPixels = se;
        if (w.MainGridColumnSplitterWidthPixels is { } sp)
            MainGridColumnSplitterWidthPixels = sp;
        if (w.BottomPanelMinRowPixels is { } bp)
            BottomPanelMinRowPixels = bp;
        if (w.MfdRegionCollapsedWidthPixels is { } cc)
            MfdRegionCollapsedWidthPixels = cc;
        if (w.MfdRegionExpandedDefaultWidthPixels is { } cd)
            MfdRegionExpandedDefaultWidthPixels = cd;
        if (w.MfdRegionExpandedPowerWidthPixels is { } cp)
            MfdRegionExpandedPowerWidthPixels = cp;
        if (w.MfdRegionExpandedAgentChatWidthPixels is { } ca)
            MfdRegionExpandedAgentChatWidthPixels = ca;
    }
}
