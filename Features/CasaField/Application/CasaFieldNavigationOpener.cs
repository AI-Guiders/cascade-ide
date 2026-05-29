#nullable enable

using CascadeIDE.Features.CasaField.Application;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.CasaField.Application;

/// <summary>Open first CASA query target in MFD Markdown Preview.</summary>
public static class CasaFieldNavigationOpener
{
    public static bool TryOpenFirstTarget(MainWindowViewModel vm, CasaFieldQueryResult result)
    {
        var target = result.Targets.FirstOrDefault();
        if (target is null)
            return false;

        if (target.Kind == "code")
            return CasaFieldCodeNavigationOpener.TryOpenCodeTarget(vm, target);

        if (string.IsNullOrWhiteSpace(target.DocPath))
            return false;

        var ws = vm.GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return false;

        var notesWs = McpAgentNotesService.ResolveNotesWorkspacePath(ws);
        if (string.IsNullOrEmpty(notesWs))
            return false;

        try
        {
            var storage = new AgentNotes.Core.NotesStorage();
            var content = storage.ReadKnowledgeFile(knowledgePath: null, filePath: target.DocPath);
            var title = $"KB: {target.ConceptId}";
            vm.MarkdownPreviewTool.SetContent(title, content, target.DocPath, target.SectionLine);
            vm.ApplyMfdRegionExpanded(true);
            vm.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
            return true;
        }
        catch
        {
            if (WorkspaceMarkdownPreviewOpener.TryOpenRepoDocument(
                    ws,
                    target.DocPath,
                    (title, content, source) => vm.MarkdownPreviewTool.SetContent(title, content, source, target.SectionLine),
                    out _))
            {
                vm.ApplyMfdRegionExpanded(true);
                vm.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
                return true;
            }
        }

        return false;
    }
}
