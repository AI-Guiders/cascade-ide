using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    public Task<string> WriteAgentNotesAsync(string content, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.WriteAgentNotes(_host.McpAgentNotes, _host.Workspace.SolutionPath, content));

    public Task<string> ReadAgentNotesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.ReadAgentNotes(_host.McpAgentNotes, _host.Workspace.SolutionPath));

    public Task<string> AppendAgentNotesAsync(string content, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.AppendAgentNotes(_host.McpAgentNotes, _host.Workspace.SolutionPath, content));

    public Task<string> ListAgentNotesRevisionsAsync(int? limit, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.ListAgentNotesRevisions(_host.McpAgentNotes, _host.Workspace.SolutionPath, limit));

    public Task<string> RollbackAgentNotesAsync(string? revisionFile, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.RollbackAgentNotes(_host.McpAgentNotes, _host.Workspace.SolutionPath, revisionFile));

    public Task<string> ReadHotContextAsync(string? activeScope, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.ReadHotContext(_host.McpAgentNotes, _host.Workspace.SolutionPath, activeScope));

    public Task<string> RouteContextAsync(string query, string? activeScope, int? maxSections, int? maxChars, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.RouteContext(_host.McpAgentNotes, _host.Workspace.SolutionPath, query, activeScope, maxSections, maxChars));

    public Task<string> MemoryHealthAsync(string? activeScope, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.MemoryHealth(_host.McpAgentNotes, _host.Workspace.SolutionPath, activeScope));

    public Task<string> CompactHotContextAsync(bool apply, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.CompactHotContext(_host.McpAgentNotes, _host.Workspace.SolutionPath, apply));

    public Task<string> ExtractFromArchiveAsync(string query, string? revisionFile, int? headLimit, int? contextLines, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.ExtractFromArchive(_host.McpAgentNotes, _host.Workspace.SolutionPath, query, revisionFile, headLimit, contextLines));

    public Task<string> UpsertAgentNotesSectionAsync(string sectionId, string content, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.UpsertAgentNotesSection(_host.McpAgentNotes, _host.Workspace.SolutionPath, sectionId, content));

    public Task<string> SearchAgentNotesAsync(string query, int? headLimit, CancellationToken cancellationToken) =>
        Task.FromResult(IdeMcpAgentNotesOrchestrator.SearchAgentNotes(_host.McpAgentNotes, _host.Workspace.SolutionPath, query, headLimit));

    public Task<string> ReadKnowledgeFileAsync(string filePath, int? offset, int? limit, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_host.McpAgentNotes.ReadKnowledgeFile(filePath, offset, limit, knowledgeRootId));

    public Task<string> ListKnowledgeFilesAsync(string? subdir, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_host.McpAgentNotes.ListKnowledgeFiles(subdir, knowledgeRootId));

    public Task<string> WriteKnowledgeFileAsync(string filePath, string content, string? knowledgePath, bool saveRevision, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_host.McpAgentNotes.WriteKnowledgeFile(filePath, content, knowledgePath, saveRevision, knowledgeRootId));

    public Task<string> AppendKnowledgeFileAsync(string filePath, string content, string? knowledgePath, bool saveRevision, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_host.McpAgentNotes.AppendKnowledgeFile(filePath, content, knowledgePath, saveRevision, knowledgeRootId));

    public Task<string> UpsertKnowledgeSectionAsync(string filePath, string sectionId, string content, string? knowledgePath, bool saveRevision, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_host.McpAgentNotes.UpsertKnowledgeSection(filePath, sectionId, content, knowledgePath, saveRevision, knowledgeRootId));

    public Task<string> DeleteKnowledgeFileAsync(string filePath, string? knowledgePath, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_host.McpAgentNotes.DeleteKnowledgeFile(filePath, knowledgePath, knowledgeRootId));

    public Task<string> DeleteKnowledgeSectionAsync(string filePath, string sectionId, string? knowledgePath, string? knowledgeRootId, CancellationToken cancellationToken) =>
        Task.FromResult(_host.McpAgentNotes.DeleteKnowledgeSection(filePath, sectionId, knowledgePath, knowledgeRootId));

}
