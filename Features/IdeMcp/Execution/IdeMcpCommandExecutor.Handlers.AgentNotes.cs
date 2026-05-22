using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>Хендлеры agent-notes.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterAgentNotes(Action<string, Handler> add)
    {
        add(Services.IdeCommands.WriteAgentNotes, async (args, ct) => await _actions.WriteAgentNotesAsync(McpCommandJsonArgs.String(args, "content") ?? "", ct));
        add(Services.IdeCommands.ReadAgentNotes, async (_, ct) => await _actions.ReadAgentNotesAsync(ct));
        add(Services.IdeCommands.AppendAgentNotes, async (args, ct) => await _actions.AppendAgentNotesAsync(McpCommandJsonArgs.String(args, "content") ?? "", ct));
        add(Services.IdeCommands.ListAgentNotesRevisions, async (args, ct) =>
            await _actions.ListAgentNotesRevisionsAsync(args is not null && args.TryGetValue("limit", out var l) && l.TryGetInt32(out var v) ? v : null, ct));
        add(Services.IdeCommands.RollbackAgentNotes, async (args, ct) =>
            await _actions.RollbackAgentNotesAsync(McpCommandJsonArgs.String(args, "revision_file"), ct));
        add(Services.IdeCommands.ReadHotContext, async (args, ct) =>
            await _actions.ReadHotContextAsync(McpCommandJsonArgs.String(args, "active_scope"), ct));
        add(Services.IdeCommands.RouteContext, async (args, ct) =>
            await _actions.RouteContextAsync(
                McpCommandJsonArgs.String(args, "query") ?? "",
                McpCommandJsonArgs.String(args, "active_scope"),
                args is not null && args.TryGetValue("max_sections", out var ms) && ms.TryGetInt32(out var msv) ? msv : null,
                args is not null && args.TryGetValue("max_chars", out var mc) && mc.TryGetInt32(out var mcv) ? mcv : null,
                ct));
        add(Services.IdeCommands.MemoryHealth, async (args, ct) =>
            await _actions.MemoryHealthAsync(McpCommandJsonArgs.String(args, "active_scope"), ct));
        add(Services.IdeCommands.CompactHotContext, async (args, ct) =>
            await _actions.CompactHotContextAsync(McpCommandJsonArgs.Bool(args, "apply"), ct));
        add(Services.IdeCommands.ExtractFromArchive, async (args, ct) =>
            await _actions.ExtractFromArchiveAsync(
                McpCommandJsonArgs.String(args, "query") ?? "",
                McpCommandJsonArgs.String(args, "revision_file"),
                args is not null && args.TryGetValue("head_limit", out var hl) && hl.TryGetInt32(out var hlv) ? hlv : null,
                args is not null && args.TryGetValue("context_lines", out var cl) && cl.TryGetInt32(out var clv) ? clv : null,
                ct));
        add(Services.IdeCommands.UpsertAgentNotesSection, async (args, ct) =>
            await _actions.UpsertAgentNotesSectionAsync(McpCommandJsonArgs.String(args, "section_id") ?? "", McpCommandJsonArgs.String(args, "content") ?? "", ct));
        add(Services.IdeCommands.SearchAgentNotes, async (args, ct) =>
            await _actions.SearchAgentNotesAsync(McpCommandJsonArgs.String(args, "query") ?? "", args is not null && args.TryGetValue("head_limit", out var hl) && hl.TryGetInt32(out var v) ? v : null, ct));
        add(Services.IdeCommands.ReadKnowledgeFile, async (args, ct) =>
            await _actions.ReadKnowledgeFileAsync(
                McpCommandJsonArgs.String(args, "file_path") ?? "",
                McpCommandJsonArgs.OptionalInt32(args, "offset"),
                McpCommandJsonArgs.OptionalInt32(args, "limit"),
                McpCommandJsonArgs.KnowledgeRootId(args),
                ct));
        add(Services.IdeCommands.ListKnowledgeFiles, async (args, ct) =>
            await _actions.ListKnowledgeFilesAsync(
                McpCommandJsonArgs.String(args, "subdir"),
                McpCommandJsonArgs.KnowledgeRootId(args),
                ct));
        add(Services.IdeCommands.WriteKnowledgeFile, async (args, ct) =>
            await _actions.WriteKnowledgeFileAsync(
                McpCommandJsonArgs.String(args, "file_path") ?? "",
                McpCommandJsonArgs.String(args, "content") ?? "",
                McpCommandJsonArgs.KnowledgePath(args),
                McpCommandJsonArgs.Bool(args, "save_revision", true),
                McpCommandJsonArgs.KnowledgeRootId(args),
                ct));
        add(Services.IdeCommands.AppendKnowledgeFile, async (args, ct) =>
            await _actions.AppendKnowledgeFileAsync(
                McpCommandJsonArgs.String(args, "file_path") ?? "",
                McpCommandJsonArgs.String(args, "content") ?? "",
                McpCommandJsonArgs.KnowledgePath(args),
                McpCommandJsonArgs.Bool(args, "save_revision", true),
                McpCommandJsonArgs.KnowledgeRootId(args),
                ct));
        add(Services.IdeCommands.UpsertKnowledgeSection, async (args, ct) =>
            await _actions.UpsertKnowledgeSectionAsync(
                McpCommandJsonArgs.String(args, "file_path") ?? "",
                McpCommandJsonArgs.String(args, "section_id") ?? "",
                McpCommandJsonArgs.String(args, "content") ?? "",
                McpCommandJsonArgs.KnowledgePath(args),
                McpCommandJsonArgs.Bool(args, "save_revision", true),
                McpCommandJsonArgs.KnowledgeRootId(args),
                ct));
        add(Services.IdeCommands.DeleteKnowledgeFile, async (args, ct) =>
            await _actions.DeleteKnowledgeFileAsync(
                McpCommandJsonArgs.String(args, "file_path") ?? "",
                McpCommandJsonArgs.KnowledgePath(args),
                McpCommandJsonArgs.KnowledgeRootId(args),
                ct));
        add(Services.IdeCommands.DeleteKnowledgeSection, async (args, ct) =>
            await _actions.DeleteKnowledgeSectionAsync(
                McpCommandJsonArgs.String(args, "file_path") ?? "",
                McpCommandJsonArgs.String(args, "section_id") ?? "",
                McpCommandJsonArgs.KnowledgePath(args),
                McpCommandJsonArgs.KnowledgeRootId(args),
                ct));
    }
}
