#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Allowlist: CIDE <c>command_id</c> → Glass local action (≠ full Avalonia IdeMcpCommandExecutor).
/// Unmapped melody rows stay discoverability-only.
/// </summary>
public static class GlassMelodyGlassActions
{
    /// <summary>Special ActionIds handled in WPF <c>RunPaletteEntry</c> beyond plain MFD/slash.</summary>
    public const string RunGitStatus = "run_git_status";
    public const string RunBuild = "run_build";
    public const string RunTests = "run_tests_glass";
    public const string RunSelectLines = "run_select_lines";

    static readonly Dictionary<string, string> CommandIdToAction =
        new(StringComparer.Ordinal)
        {
            ["git_status"] = RunGitStatus,
            ["build_solution_ui"] = RunBuild,
            ["build_structured"] = RunBuild,
            ["build"] = RunBuild,
            ["run_tests"] = RunTests,
            ["select"] = RunSelectLines,
            ["show_terminal_panel"] = "mfd_terminal",
            ["show_solution_explorer_page"] = "mfd_solution_explorer",
            ["show_hybrid_index_page"] = "mfd_hybrid_index",
            ["show_environment_readiness_page"] = "mfd_env_ready",
            ["show_chat_page"] = "mfd_chat",
            ["focus_editor"] = "mfd_editor",
            ["get_ide_state"] = "slash_status",
            // File/IOP peels already on Glass palette — DIG REJECT solution/portal/Markdig/correspondence hosts.
            ["open_file"] = "open_file",
            ["open_file_dialog"] = "open_file",
            ["intercom.attach_selection"] = "slash_attach",
            ["intercom.attach_scope"] = "slash_attach",
            // Glass peels already in RunPaletteEntry — cabin c: without inventing Avalonia hosts.
            ["save_document"] = "save_file",
            ["focus_composer"] = "focus_composer",
            ["glass.slash_help"] = "slash_help",
            ["glass.slash_fds"] = "slash_fds",
            ["glass.slash_topics"] = "slash_topics",
            ["glass.slash_letter"] = "slash_letter",
            ["glass.slash_citizen"] = "slash_citizen",
            ["glass.mfd_fds"] = "mfd_fds",
            ["glass.mfd_workspace_health"] = "mfd_workspace_health",
            ["glass.mfd_events"] = "mfd_events",
            ["glass.mfd_hypotheses"] = "mfd_hypotheses",
        };

    public static bool TryMapCommandId(string? commandId, out string glassActionId)
    {
        glassActionId = "";
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        return CommandIdToAction.TryGetValue(commandId.Trim(), out glassActionId!);
    }

    public static bool TryMapRowId(string? rowId, out string glassActionId)
    {
        glassActionId = "";
        if (!GlassIntentMelodyCatalog.TryParseRowId(rowId, out var commandId))
            return false;

        return TryMapCommandId(commandId, out glassActionId);
    }

    public static bool IsMappedCommandId(string? commandId) =>
        TryMapCommandId(commandId, out _);
}
