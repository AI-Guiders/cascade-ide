using CascadeIDE.Features.IdeMcp.Application;

namespace CascadeIDE.ViewModels;

/// <summary>Реализация <see cref="IIdeMcpActions"/>: agent-notes.</summary>
public partial class MainWindowViewModel
{
    Task<string> Services.IIdeMcpActions.WriteAgentNotesAsync(string content, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.WriteAgentNotes(_mcpAgentNotes, Workspace.SolutionPath, content));

    Task<string> Services.IIdeMcpActions.ReadAgentNotesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.ReadAgentNotes(_mcpAgentNotes, Workspace.SolutionPath));

    Task<string> Services.IIdeMcpActions.AppendAgentNotesAsync(string content, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.AppendAgentNotes(_mcpAgentNotes, Workspace.SolutionPath, content));

    Task<string> Services.IIdeMcpActions.ListAgentNotesRevisionsAsync(int? limit, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.ListAgentNotesRevisions(_mcpAgentNotes, Workspace.SolutionPath, limit));

    Task<string> Services.IIdeMcpActions.RollbackAgentNotesAsync(string? revisionFile, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.RollbackAgentNotes(_mcpAgentNotes, Workspace.SolutionPath, revisionFile));

    Task<string> Services.IIdeMcpActions.ReadHotContextAsync(string? activeScope, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.ReadHotContext(_mcpAgentNotes, Workspace.SolutionPath, activeScope));

    Task<string> Services.IIdeMcpActions.RouteContextAsync(string query, string? activeScope, int? maxSections, int? maxChars, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.RouteContext(_mcpAgentNotes, Workspace.SolutionPath, query, activeScope, maxSections, maxChars));

    Task<string> Services.IIdeMcpActions.MemoryHealthAsync(string? activeScope, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.MemoryHealth(_mcpAgentNotes, Workspace.SolutionPath, activeScope));

    Task<string> Services.IIdeMcpActions.CompactHotContextAsync(bool apply, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.CompactHotContext(_mcpAgentNotes, Workspace.SolutionPath, apply));

    Task<string> Services.IIdeMcpActions.ExtractFromArchiveAsync(string query, string? revisionFile, int? headLimit, int? contextLines, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.ExtractFromArchive(_mcpAgentNotes, Workspace.SolutionPath, query, revisionFile, headLimit, contextLines));

    Task<string> Services.IIdeMcpActions.UpsertAgentNotesSectionAsync(string sectionId, string content, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.UpsertAgentNotesSection(_mcpAgentNotes, Workspace.SolutionPath, sectionId, content));

    Task<string> Services.IIdeMcpActions.SearchAgentNotesAsync(string query, int? headLimit, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.SearchAgentNotes(_mcpAgentNotes, Workspace.SolutionPath, query, headLimit));

    Task<string> Services.IIdeMcpActions.ReadKnowledgeFileAsync(string filePath, int? offset, int? limit, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.ReadKnowledgeFile(filePath, offset, limit, knowledgeRootId));

    Task<string> Services.IIdeMcpActions.ListKnowledgeFilesAsync(string? subdir, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.ListKnowledgeFiles(subdir, knowledgeRootId));

    Task<string> Services.IIdeMcpActions.WriteKnowledgeFileAsync(string filePath, string content, string? knowledgePath, bool saveRevision, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.WriteKnowledgeFile(filePath, content, knowledgePath, saveRevision, knowledgeRootId));

    Task<string> Services.IIdeMcpActions.AppendKnowledgeFileAsync(string filePath, string content, string? knowledgePath, bool saveRevision, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.AppendKnowledgeFile(filePath, content, knowledgePath, saveRevision, knowledgeRootId));

    Task<string> Services.IIdeMcpActions.UpsertKnowledgeSectionAsync(string filePath, string sectionId, string content, string? knowledgePath, bool saveRevision, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.UpsertKnowledgeSection(filePath, sectionId, content, knowledgePath, saveRevision, knowledgeRootId));

    Task<string> Services.IIdeMcpActions.DeleteKnowledgeFileAsync(string filePath, string? knowledgePath, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.DeleteKnowledgeFile(filePath, knowledgePath, knowledgeRootId));

    Task<string> Services.IIdeMcpActions.DeleteKnowledgeSectionAsync(string filePath, string sectionId, string? knowledgePath, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_mcpAgentNotes.DeleteKnowledgeSection(filePath, sectionId, knowledgePath, knowledgeRootId));
}
