namespace CascadeIDE.ViewModels;

/// <summary>Реализация <see cref="IIdeMcpActions"/>: agent-notes.</summary>
public partial class MainWindowViewModel
{
    private string? TryGetNotesWorkspacePath() =>
        Services.McpAgentNotesService.ResolveNotesWorkspacePath(Workspace.SolutionPath);

    Task<string> Services.IIdeMcpActions.WriteAgentNotesAsync(string content, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.WriteAgentNotes(TryGetNotesWorkspacePath(), content));

    Task<string> Services.IIdeMcpActions.ReadAgentNotesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.ReadAgentNotes(TryGetNotesWorkspacePath()));

    Task<string> Services.IIdeMcpActions.AppendAgentNotesAsync(string content, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.AppendAgentNotes(TryGetNotesWorkspacePath(), content));

    Task<string> Services.IIdeMcpActions.ListAgentNotesRevisionsAsync(int? limit, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.ListAgentNotesRevisions(TryGetNotesWorkspacePath(), limit));

    Task<string> Services.IIdeMcpActions.RollbackAgentNotesAsync(string? revisionFile, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.RollbackAgentNotes(TryGetNotesWorkspacePath(), revisionFile));

    Task<string> Services.IIdeMcpActions.ReadHotContextAsync(string? activeScope, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.ReadHotContext(TryGetNotesWorkspacePath(), activeScope));

    Task<string> Services.IIdeMcpActions.RouteContextAsync(string query, string? activeScope, int? maxSections, int? maxChars, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.RouteContext(TryGetNotesWorkspacePath(), query, activeScope, maxSections, maxChars));

    Task<string> Services.IIdeMcpActions.MemoryHealthAsync(string? activeScope, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.MemoryHealth(TryGetNotesWorkspacePath(), activeScope));

    Task<string> Services.IIdeMcpActions.CompactHotContextAsync(bool apply, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.CompactHotContext(TryGetNotesWorkspacePath(), apply));

    Task<string> Services.IIdeMcpActions.ExtractFromArchiveAsync(string query, string? revisionFile, int? headLimit, int? contextLines, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.ExtractFromArchive(TryGetNotesWorkspacePath(), query, revisionFile, headLimit, contextLines));

    Task<string> Services.IIdeMcpActions.UpsertAgentNotesSectionAsync(string sectionId, string content, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.UpsertAgentNotesSection(TryGetNotesWorkspacePath(), sectionId, content));

    Task<string> Services.IIdeMcpActions.SearchAgentNotesAsync(string query, int? headLimit, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.SearchAgentNotes(TryGetNotesWorkspacePath(), query, headLimit));

    Task<string> Services.IIdeMcpActions.ReadKnowledgeFileAsync(string filePath, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.ReadKnowledgeFile(filePath));

    Task<string> Services.IIdeMcpActions.ListKnowledgeFilesAsync(string? subdir, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.ListKnowledgeFiles(subdir));

    Task<string> Services.IIdeMcpActions.WriteKnowledgeFileAsync(string filePath, string content, string? canonPath, bool saveRevision, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.WriteKnowledgeFile(filePath, content, canonPath, saveRevision));

    Task<string> Services.IIdeMcpActions.AppendKnowledgeFileAsync(string filePath, string content, string? canonPath, bool saveRevision, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.AppendKnowledgeFile(filePath, content, canonPath, saveRevision));

    Task<string> Services.IIdeMcpActions.UpsertKnowledgeSectionAsync(string filePath, string sectionId, string content, string? canonPath, bool saveRevision, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.UpsertKnowledgeSection(filePath, sectionId, content, canonPath, saveRevision));

    Task<string> Services.IIdeMcpActions.DeleteKnowledgeFileAsync(string filePath, string? canonPath, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.DeleteKnowledgeFile(filePath, canonPath));

    Task<string> Services.IIdeMcpActions.DeleteKnowledgeSectionAsync(string filePath, string sectionId, string? canonPath, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.DeleteKnowledgeSection(filePath, sectionId, canonPath));
}
