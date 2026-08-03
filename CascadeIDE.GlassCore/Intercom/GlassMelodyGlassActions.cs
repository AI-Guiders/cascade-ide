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
    public const string RunWebAiPortal = "run_webai_portal";

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
            ["show_web_ai_portal_page"] = RunWebAiPortal,
            ["show_problems_page"] = "mfd_problems",
            ["show_related_files_page"] = "mfd_related_files",
            ["show_semantic_map_page"] = "mfd_semantic_map",
            ["show_markdown_preview_page"] = "mfd_markdown",
            ["show_correspondence_page"] = "mfd_correspondence",
            ["show_debug_page"] = "mfd_debug_stack",
            ["show_git_page"] = "mfd_git",
            ["show_build_page"] = "mfd_build",
            ["show_tests_page"] = "mfd_tests",
            ["codebase_index_status"] = "mfd_hybrid_index",
            ["git_diff"] = RunGitStatus,
            ["git_log"] = RunGitStatus,
            ["git_preflight"] = RunGitStatus,
            ["open_docs_template"] = "mfd_markdown",
            ["debug_launch"] = "mfd_debug_stack",
            ["debug_attach"] = "mfd_debug_stack",
            ["debug_stack_trace"] = "mfd_debug_stack",
        };

    public static bool TryMapCommandId(string? commandId, out string glassActionId)
    {
        glassActionId = "";
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        var id = commandId.Trim();
        if (CommandIdToAction.TryGetValue(id, out glassActionId!))
            return true;

        return TryMapCommandIdPrefix(id, out glassActionId);
    }

    static bool TryMapCommandIdPrefix(string commandId, out string glassActionId)
    {
        glassActionId = "";
        if (commandId.StartsWith("git_", StringComparison.Ordinal))
        {
            glassActionId = RunGitStatus;
            return true;
        }

        if (commandId.StartsWith("build", StringComparison.Ordinal))
        {
            glassActionId = RunBuild;
            return true;
        }

        if (commandId.StartsWith("run_test", StringComparison.Ordinal))
        {
            glassActionId = RunTests;
            return true;
        }

        if (commandId.StartsWith("debug_", StringComparison.Ordinal))
        {
            glassActionId = "mfd_debug_stack";
            return true;
        }

        if (commandId.StartsWith("open_", StringComparison.Ordinal))
        {
            glassActionId = "open_file";
            return true;
        }

        if (!commandId.StartsWith("show_", StringComparison.Ordinal))
            return false;

        glassActionId = commandId switch
        {
            var s when s.Contains("terminal", StringComparison.Ordinal) => "mfd_terminal",
            var s when s.Contains("editor", StringComparison.Ordinal) => "mfd_editor",
            var s when s.Contains("chat", StringComparison.Ordinal) => "mfd_chat",
            var s when s.Contains("hybrid", StringComparison.Ordinal) => "mfd_hybrid_index",
            var s when s.Contains("solution", StringComparison.Ordinal) => "mfd_solution_explorer",
            var s when s.Contains("workspace_health", StringComparison.Ordinal) => "mfd_workspace_health",
            var s when s.Contains("environment", StringComparison.Ordinal) => "mfd_env_ready",
            var s when s.Contains("event", StringComparison.Ordinal) => "mfd_events",
            var s when s.Contains("hypothes", StringComparison.Ordinal) => "mfd_hypotheses",
            var s when s.Contains("web", StringComparison.Ordinal) => RunWebAiPortal,
            var s when s.Contains("problem", StringComparison.Ordinal) => "mfd_problems",
            var s when s.Contains("related", StringComparison.Ordinal) => "mfd_related_files",
            var s when s.Contains("semantic", StringComparison.Ordinal) => "mfd_semantic_map",
            var s when s.Contains("markdown", StringComparison.Ordinal) => "mfd_markdown",
            var s when s.Contains("correspondence", StringComparison.Ordinal) => "mfd_correspondence",
            var s when s.Contains("debug", StringComparison.Ordinal) => "mfd_debug_stack",
            var s when s.Contains("git", StringComparison.Ordinal) => "mfd_git",
            var s when s.Contains("build", StringComparison.Ordinal) => "mfd_build",
            var s when s.Contains("test", StringComparison.Ordinal) => "mfd_tests",
            _ => "",
        };
        return glassActionId.Length > 0;
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
