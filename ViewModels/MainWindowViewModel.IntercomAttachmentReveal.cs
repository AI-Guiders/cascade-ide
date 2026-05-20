#nullable enable

using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.ViewModels;

/// <summary>Reveal вложения Intercom из чата: загрузка решения при необходимости, затем навигация в редактор (ADR 0128).</summary>
public partial class MainWindowViewModel
{
    internal async Task<string> RevealIntercomAttachmentInIdeAsync(
        AttachmentAnchor anchor,
        bool select,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(anchor.File))
            return "excerpt_only: нет file в anchor.";

        var workspaceRoot = GetWorkspacePath();
        if (_settings.Intercom.Attachments.Code.ShouldLoadSolutionBeforeReveal()
            && AttachmentAnchorPaths.TryResolveAbsolute(anchor.File, workspaceRoot, out var absolute, out _)
            && File.Exists(absolute))
        {
            var sln = SolutionFileLocator.TryFindSolutionForSourceFile(absolute);
            if (SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(sln, Workspace.SolutionPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await LoadSolutionAsync(sln!).ConfigureAwait(false);
                workspaceRoot = GetWorkspacePath();
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await UiScheduler.Default.InvokeAsync(() =>
                IntercomAttachmentNavigator.Apply(
                    (IIdeMcpActions)this,
                    _settings.Intercom,
                    GetWorkspacePath(),
                    anchor,
                    selectExplicit: select,
                    shiftSelect: false,
                    durationMs: null,
                    solutionPath: ChatPanel.ResolveAttachSolutionPath()))
            .ConfigureAwait(false);
    }
}
