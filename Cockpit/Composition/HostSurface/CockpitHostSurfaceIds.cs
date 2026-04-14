namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>Стабильные идентификаторы слотов внимания (ADR 0021 / 0047); без Avalonia.</summary>
public static class CockpitSlotIds
{
    public const string Pfd = "pfd";
    public const string Mfd = "mfd";
    public const string Forward = "forward";
}

/// <summary>Стабильные <c>instrument_id</c> для контракта хоста (Skia/Avalonia); не смешивать с именами контролов.</summary>
public static class CockpitStandardInstrumentIds
{
    public const string SolutionExplorerTree = "solution_explorer_tree";
    public const string WorkspaceNavigationMap = "workspace_navigation_map";
}
