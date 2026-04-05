namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Глобальные метрики хрома, подгружаемые из <c>UiModes/workspace.toml</c> (ADR 0010); по умолчанию — <see cref="UiWorkspaceLayoutDimensions"/>.
/// </summary>
public static class UiWorkspaceLayoutRuntimeMetrics
{
    public static int SolutionExplorerDefaultWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.SolutionExplorerDefaultWidthPixels;

    public static double MainGridColumnSplitterWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.MainGridColumnSplitterWidthPixels;

    public static int BottomPanelMinRowPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.BottomPanelMinRowPixels;

    public static int ChatPanelCollapsedWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.ChatPanelCollapsedWidthPixels;

    public static int ChatPanelExpandedDefaultWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.ChatPanelExpandedDefaultWidthPixels;

    public static int ChatPanelExpandedPowerWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.ChatPanelExpandedPowerWidthPixels;

    public static int ChatPanelExpandedAgentChatWidthPixels { get; private set; } =
        UiWorkspaceLayoutDimensions.ChatPanelExpandedAgentChatWidthPixels;

    internal static void ResetToCodeDefaults()
    {
        SolutionExplorerDefaultWidthPixels = UiWorkspaceLayoutDimensions.SolutionExplorerDefaultWidthPixels;
        MainGridColumnSplitterWidthPixels = UiWorkspaceLayoutDimensions.MainGridColumnSplitterWidthPixels;
        BottomPanelMinRowPixels = UiWorkspaceLayoutDimensions.BottomPanelMinRowPixels;
        ChatPanelCollapsedWidthPixels = UiWorkspaceLayoutDimensions.ChatPanelCollapsedWidthPixels;
        ChatPanelExpandedDefaultWidthPixels = UiWorkspaceLayoutDimensions.ChatPanelExpandedDefaultWidthPixels;
        ChatPanelExpandedPowerWidthPixels = UiWorkspaceLayoutDimensions.ChatPanelExpandedPowerWidthPixels;
        ChatPanelExpandedAgentChatWidthPixels = UiWorkspaceLayoutDimensions.ChatPanelExpandedAgentChatWidthPixels;
    }

    internal static void ApplyWorkspaceToml(UiWorkspaceToml? w)
    {
        ResetToCodeDefaults();
        if (w is null)
            return;
        if (w.SolutionExplorerDefaultWidthPixels is { } se)
            SolutionExplorerDefaultWidthPixels = se;
        if (w.MainGridColumnSplitterWidthPixels is { } sp)
            MainGridColumnSplitterWidthPixels = sp;
        if (w.BottomPanelMinRowPixels is { } bp)
            BottomPanelMinRowPixels = bp;
        if (w.ChatPanelCollapsedWidthPixels is { } cc)
            ChatPanelCollapsedWidthPixels = cc;
        if (w.ChatPanelExpandedDefaultWidthPixels is { } cd)
            ChatPanelExpandedDefaultWidthPixels = cd;
        if (w.ChatPanelExpandedPowerWidthPixels is { } cp)
            ChatPanelExpandedPowerWidthPixels = cp;
        if (w.ChatPanelExpandedAgentChatWidthPixels is { } ca)
            ChatPanelExpandedAgentChatWidthPixels = ca;
    }
}
