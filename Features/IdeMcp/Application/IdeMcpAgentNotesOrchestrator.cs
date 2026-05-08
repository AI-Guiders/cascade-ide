using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>Тонкий фасад MCP agent-notes: workspace к заметкам через <see cref="McpAgentNotesService.ResolveNotesWorkspacePath"/>.</summary>
public static class IdeMcpAgentNotesOrchestrator
{
    public static string WriteAgentNotes(McpAgentNotesService svc, string? solutionPath, string content) =>
        svc.WriteAgentNotes(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), content);

    public static string ReadAgentNotes(McpAgentNotesService svc, string? solutionPath) =>
        svc.ReadAgentNotes(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath));

    public static string AppendAgentNotes(McpAgentNotesService svc, string? solutionPath, string content) =>
        svc.AppendAgentNotes(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), content);

    public static string ListAgentNotesRevisions(McpAgentNotesService svc, string? solutionPath, int? limit) =>
        svc.ListAgentNotesRevisions(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), limit);

    public static string RollbackAgentNotes(McpAgentNotesService svc, string? solutionPath, string? revisionFile) =>
        svc.RollbackAgentNotes(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), revisionFile);

    public static string ReadHotContext(McpAgentNotesService svc, string? solutionPath, string? activeScope) =>
        svc.ReadHotContext(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), activeScope);

    public static string RouteContext(
        McpAgentNotesService svc,
        string? solutionPath,
        string query,
        string? activeScope,
        int? maxSections,
        int? maxChars) =>
        svc.RouteContext(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), query, activeScope, maxSections, maxChars);

    public static string MemoryHealth(McpAgentNotesService svc, string? solutionPath, string? activeScope) =>
        svc.MemoryHealth(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), activeScope);

    public static string CompactHotContext(McpAgentNotesService svc, string? solutionPath, bool apply) =>
        svc.CompactHotContext(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), apply);

    public static string ExtractFromArchive(
        McpAgentNotesService svc,
        string? solutionPath,
        string query,
        string? revisionFile,
        int? headLimit,
        int? contextLines) =>
        svc.ExtractFromArchive(
            McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath),
            query,
            revisionFile,
            headLimit,
            contextLines);

    public static string UpsertAgentNotesSection(McpAgentNotesService svc, string? solutionPath, string sectionId, string content) =>
        svc.UpsertAgentNotesSection(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), sectionId, content);

    public static string SearchAgentNotes(McpAgentNotesService svc, string? solutionPath, string query, int? headLimit) =>
        svc.SearchAgentNotes(McpAgentNotesService.ResolveNotesWorkspacePath(solutionPath), query, headLimit);
}
