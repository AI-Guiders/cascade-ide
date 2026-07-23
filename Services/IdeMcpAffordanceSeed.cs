#nullable enable

using Cdp.Core;

namespace CascadeIDE.Services;

/// <summary>
/// MLP affordance seed for CIDE MCP ListTools filter — same axes as Cursor CDP (<see cref="Cdp.Core"/>).
/// Starts from MAF-promoted <c>ide_*</c> set; full IdeCommands tagging is later.
/// </summary>
internal static class IdeMcpAffordanceSeed
{
    private static readonly CdpPhase[] ExploreClarify = [CdpPhase.Explore, CdpPhase.Clarify];
    private static readonly CdpPhase[] ExploreAct = [CdpPhase.Explore, CdpPhase.Act];
    private static readonly CdpPhase[] ActVerify = [CdpPhase.Act, CdpPhase.Verify];
    private static readonly CdpPhase[] AllPhases =
    [
        CdpPhase.Explore, CdpPhase.Clarify, CdpPhase.Act, CdpPhase.Verify, CdpPhase.Handoff
    ];

    public static IReadOnlyList<ToolAffordance> Build() =>
    [
        // Session / orientation
        A("ide_ping", AllPhases, [CdpObjectKind.Session], [CdpIntent.Verify], 1, 1),
        A("ide_get_ide_state", ExploreClarify.Concat([CdpPhase.Verify]).ToArray(), [CdpObjectKind.Session], [CdpIntent.Find], 1, 1),
        A("ide_get_ui_layout", ExploreClarify, [CdpObjectKind.Session], [CdpIntent.Find], 2, 1),

        // KB / notes (in-proc AN surface)
        A("ide_route_context", ExploreClarify, [CdpObjectKind.Kb, CdpObjectKind.Session], [CdpIntent.Find], 1, 1),
        A("ide_read_hot_context", ExploreClarify.Concat([CdpPhase.Handoff]).ToArray(), [CdpObjectKind.Kb, CdpObjectKind.Session], [CdpIntent.Find, CdpIntent.Cite], 1, 1),
        A("ide_read_agent_notes", AllPhases, [CdpObjectKind.Kb, CdpObjectKind.Session], [CdpIntent.Find, CdpIntent.Cite], 1, 1),
        A("ide_search_agent_notes", ExploreClarify, [CdpObjectKind.Kb, CdpObjectKind.Session], [CdpIntent.Find], 2, 1),
        A("ide_append_agent_notes", [CdpPhase.Act, CdpPhase.Handoff], [CdpObjectKind.Session], [CdpIntent.Change, CdpIntent.Record], 2, 3),

        // Code / editor
        A("ide_get_editor_state", ExploreAct.Concat([CdpPhase.Verify]).ToArray(), [CdpObjectKind.Code], [CdpIntent.Find], 1, 1),
        A("ide_get_editor_content_range", ExploreAct.Concat([CdpPhase.Verify]).ToArray(), [CdpObjectKind.Code], [CdpIntent.Find, CdpIntent.Cite], 1, 1),
        A("ide_open_file", ExploreAct, [CdpObjectKind.Code], [CdpIntent.Find, CdpIntent.Change], 1, 1),
        A("ide_load_solution", ExploreAct, [CdpObjectKind.Code, CdpObjectKind.Repo], [CdpIntent.Change], 2, 2),
        A("ide_get_solution_info", ExploreClarify, [CdpObjectKind.Code, CdpObjectKind.Repo], [CdpIntent.Find], 1, 1),
        A("ide_get_solution_files", ExploreClarify, [CdpObjectKind.Code, CdpObjectKind.Repo], [CdpIntent.Find], 2, 1),
        A("ide_go_to_position", ExploreAct, [CdpObjectKind.Code], [CdpIntent.Find, CdpIntent.Change], 1, 1),
        A("ide_select", ExploreAct, [CdpObjectKind.Code], [CdpIntent.Change], 1, 1),
        A("ide_apply_edit", [CdpPhase.Act], [CdpObjectKind.Code], [CdpIntent.Change], 3, 3),
        A("ide_get_current_file_diagnostics", ActVerify, [CdpObjectKind.Code], [CdpIntent.Verify, CdpIntent.Find], 1, 1),
        A("ide_search_workspace_text", ExploreClarify, [CdpObjectKind.Code, CdpObjectKind.Repo], [CdpIntent.Find], 2, 1),

        // Build / process
        A("ide_build", [CdpPhase.Act, CdpPhase.Verify], [CdpObjectKind.Code, CdpObjectKind.Process], [CdpIntent.Verify, CdpIntent.Change], 3, 3),
        A("ide_get_build_output", ActVerify, [CdpObjectKind.Process, CdpObjectKind.Code], [CdpIntent.Find, CdpIntent.Verify], 1, 1),
        A("ide_run_tests", ActVerify, [CdpObjectKind.Code, CdpObjectKind.Process], [CdpIntent.Verify], 3, 2),
        A("ide_run_affected_tests", ActVerify, [CdpObjectKind.Code, CdpObjectKind.Process], [CdpIntent.Verify], 3, 2),
        A("ide_set_breakpoint", [CdpPhase.Act], [CdpObjectKind.Code], [CdpIntent.Change], 2, 2),
        A("ide_remove_breakpoint", [CdpPhase.Act], [CdpObjectKind.Code], [CdpIntent.Change], 2, 2),

        // Web (session/research)
        A("ide_search_web_public_query", ExploreClarify, [CdpObjectKind.Session], [CdpIntent.Find], 2, 2),
        A("ide_fetch_web_public_url", ExploreClarify, [CdpObjectKind.Session], [CdpIntent.Find, CdpIntent.Cite], 2, 2),
    ];

    private static ToolAffordance A(
        string mcpToolName,
        CdpPhase[] phases,
        CdpObjectKind[] objects,
        CdpIntent[] intents,
        int cost,
        int risk) =>
        new(
            mcpToolName,
            "ide",
            mcpToolName,
            phases,
            objects,
            intents,
            cost,
            risk);
}
