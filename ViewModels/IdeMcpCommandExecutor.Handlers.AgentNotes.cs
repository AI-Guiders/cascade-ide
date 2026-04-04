using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterAgentNotes(Action<string, Handler> add)
    {
        add(Services.IdeCommands.WriteAgentNotes, async (args, ct) => await ((IIdeMcpActions)_vm).WriteAgentNotesAsync(JsonArgs.String(args, "content") ?? "", ct));
        add(Services.IdeCommands.ReadAgentNotes, async (_, ct) => await ((IIdeMcpActions)_vm).ReadAgentNotesAsync(ct));
        add(Services.IdeCommands.AppendAgentNotes, async (args, ct) => await ((IIdeMcpActions)_vm).AppendAgentNotesAsync(JsonArgs.String(args, "content") ?? "", ct));
        add(Services.IdeCommands.ListAgentNotesRevisions, async (args, ct) =>
            await ((IIdeMcpActions)_vm).ListAgentNotesRevisionsAsync(args is not null && args.TryGetValue("limit", out var l) && l.TryGetInt32(out var v) ? v : null, ct));
        add(Services.IdeCommands.RollbackAgentNotes, async (args, ct) =>
            await ((IIdeMcpActions)_vm).RollbackAgentNotesAsync(JsonArgs.String(args, "revision_file"), ct));
        add(Services.IdeCommands.ReadHotContext, async (args, ct) =>
            await ((IIdeMcpActions)_vm).ReadHotContextAsync(JsonArgs.String(args, "active_scope"), ct));
        add(Services.IdeCommands.RouteContext, async (args, ct) =>
            await ((IIdeMcpActions)_vm).RouteContextAsync(
                JsonArgs.String(args, "query") ?? "",
                JsonArgs.String(args, "active_scope"),
                args is not null && args.TryGetValue("max_sections", out var ms) && ms.TryGetInt32(out var msv) ? msv : null,
                args is not null && args.TryGetValue("max_chars", out var mc) && mc.TryGetInt32(out var mcv) ? mcv : null,
                ct));
        add(Services.IdeCommands.MemoryHealth, async (args, ct) =>
            await ((IIdeMcpActions)_vm).MemoryHealthAsync(JsonArgs.String(args, "active_scope"), ct));
        add(Services.IdeCommands.CompactHotContext, async (args, ct) =>
            await ((IIdeMcpActions)_vm).CompactHotContextAsync(JsonArgs.Bool(args, "apply"), ct));
        add(Services.IdeCommands.ExtractFromArchive, async (args, ct) =>
            await ((IIdeMcpActions)_vm).ExtractFromArchiveAsync(
                JsonArgs.String(args, "query") ?? "",
                JsonArgs.String(args, "revision_file"),
                args is not null && args.TryGetValue("head_limit", out var hl) && hl.TryGetInt32(out var hlv) ? hlv : null,
                args is not null && args.TryGetValue("context_lines", out var cl) && cl.TryGetInt32(out var clv) ? clv : null,
                ct));
        add(Services.IdeCommands.UpsertAgentNotesSection, async (args, ct) =>
            await ((IIdeMcpActions)_vm).UpsertAgentNotesSectionAsync(JsonArgs.String(args, "section_id") ?? "", JsonArgs.String(args, "content") ?? "", ct));
        add(Services.IdeCommands.SearchAgentNotes, async (args, ct) =>
            await ((IIdeMcpActions)_vm).SearchAgentNotesAsync(JsonArgs.String(args, "query") ?? "", args is not null && args.TryGetValue("head_limit", out var hl) && hl.TryGetInt32(out var v) ? v : null, ct));
        add(Services.IdeCommands.ReadKnowledgeFile, async (args, ct) =>
            await ((IIdeMcpActions)_vm).ReadKnowledgeFileAsync(JsonArgs.String(args, "file_path") ?? "", ct));
        add(Services.IdeCommands.ListKnowledgeFiles, async (args, ct) =>
            await ((IIdeMcpActions)_vm).ListKnowledgeFilesAsync(JsonArgs.String(args, "subdir"), ct));
        add(Services.IdeCommands.WriteKnowledgeFile, async (args, ct) =>
            await ((IIdeMcpActions)_vm).WriteKnowledgeFileAsync(
                JsonArgs.String(args, "file_path") ?? "",
                JsonArgs.String(args, "content") ?? "",
                JsonArgs.String(args, "canon_path"),
                JsonArgs.Bool(args, "save_revision", true),
                ct));
        add(Services.IdeCommands.AppendKnowledgeFile, async (args, ct) =>
            await ((IIdeMcpActions)_vm).AppendKnowledgeFileAsync(
                JsonArgs.String(args, "file_path") ?? "",
                JsonArgs.String(args, "content") ?? "",
                JsonArgs.String(args, "canon_path"),
                JsonArgs.Bool(args, "save_revision", true),
                ct));
        add(Services.IdeCommands.UpsertKnowledgeSection, async (args, ct) =>
            await ((IIdeMcpActions)_vm).UpsertKnowledgeSectionAsync(
                JsonArgs.String(args, "file_path") ?? "",
                JsonArgs.String(args, "section_id") ?? "",
                JsonArgs.String(args, "content") ?? "",
                JsonArgs.String(args, "canon_path"),
                JsonArgs.Bool(args, "save_revision", true),
                ct));
        add(Services.IdeCommands.DeleteKnowledgeFile, async (args, ct) =>
            await ((IIdeMcpActions)_vm).DeleteKnowledgeFileAsync(JsonArgs.String(args, "file_path") ?? "", JsonArgs.String(args, "canon_path"), ct));
        add(Services.IdeCommands.DeleteKnowledgeSection, async (args, ct) =>
            await ((IIdeMcpActions)_vm).DeleteKnowledgeSectionAsync(JsonArgs.String(args, "file_path") ?? "", JsonArgs.String(args, "section_id") ?? "", JsonArgs.String(args, "canon_path"), ct));
    }
}
