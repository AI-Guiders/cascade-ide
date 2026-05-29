#nullable enable

using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.MagicLink;

/// <summary>Выполнение <c>cide://</c> на UI-потоке (ADR 0157).</summary>
public static class CideMagicLinkExecutor
{
    public static async Task<string> TryExecuteAsync(
        MainWindowViewModel vm,
        CideMagicLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var workspaceRoot = ResolveWorkspaceRoot(vm, request.WorkspaceRoot);
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return "workspace_not_loaded: укажите root= или откройте solution в IDE.";

        if (!CideMagicLinkWorkspaceGuard.TryValidateRoot(workspaceRoot, out var root, out var rootError))
            return $"workspace_rejected: {rootError}";

        return request.Action switch
        {
            CideMagicLinkAction.Open => await ExecuteOpenAsync(vm, root, request, cancellationToken).ConfigureAwait(false),
            CideMagicLinkAction.Reveal => await ExecuteRevealAsync(vm, root, request, cancellationToken).ConfigureAwait(false),
            CideMagicLinkAction.Markdown => await ExecuteMarkdownAsync(vm, root, request, cancellationToken).ConfigureAwait(false),
            _ => "unsupported_action",
        };
    }

    private static string? ResolveWorkspaceRoot(MainWindowViewModel vm, string? requestedRoot)
    {
        if (!string.IsNullOrWhiteSpace(requestedRoot)
            && CideMagicLinkWorkspaceGuard.TryValidateRoot(requestedRoot, out var normalized, out _))
        {
            return normalized;
        }

        return vm.GetWorkspacePath();
    }

    private static async Task<string> ExecuteOpenAsync(
        MainWindowViewModel vm,
        string root,
        CideMagicLinkRequest request,
        CancellationToken cancellationToken)
    {
        var slnPath = request.SolutionPath;
        if (string.IsNullOrWhiteSpace(slnPath))
        {
            slnPath = Directory.EnumerateFiles(root, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault()
                      ?? Directory.EnumerateFiles(root, "*.slnx", SearchOption.TopDirectoryOnly).FirstOrDefault();
        }
        else if (!Path.IsPathRooted(slnPath))
        {
            if (!CideMagicLinkWorkspaceGuard.TryResolveUnderRoot(root, slnPath, out var abs, out var err))
                return $"sln_resolve_failed: {err}";
            slnPath = abs;
        }

        if (string.IsNullOrWhiteSpace(slnPath) || !File.Exists(slnPath))
            return "sln_not_found";

        cancellationToken.ThrowIfCancellationRequested();
        await vm.LoadSolutionAsync(slnPath).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.File) || !string.IsNullOrWhiteSpace(request.BracketInner))
            return await ExecuteRevealAsync(vm, root, request, cancellationToken).ConfigureAwait(false);

        return "OK";
    }

    private static async Task<string> ExecuteRevealAsync(
        MainWindowViewModel vm,
        string root,
        CideMagicLinkRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(request.BracketInner))
        {
            if (!BracketCodeReferenceParser.TryParse(request.BracketInner, out var reference, out var parseError))
                return $"bracket_parse_failed: {parseError}";

            if (!BracketCodeReferenceParser.TryToAttachmentAnchor(
                    reference,
                    vm.CurrentFilePath,
                    root,
                    vm.Workspace.SolutionPath,
                    indexDirectoryRelative: null,
                    out var anchor,
                    out var anchorError))
            {
                return $"anchor_resolve_failed: {anchorError}";
            }

            return await vm.RevealIntercomAttachmentInIdeAsync(anchor, select: true, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(request.File))
            return "reveal: file missing";

        if (!CideMagicLinkWorkspaceGuard.TryResolveUnderRoot(root, request.File, out var absolute, out var pathError))
            return $"file_resolve_failed: {pathError}";

        if (!File.Exists(absolute))
            return "file_not_found";

        var sln = SolutionFileLocator.TryFindSolutionForSourceFile(absolute);
        if (SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(sln, vm.Workspace.SolutionPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await vm.LoadSolutionAsync(sln!).ConfigureAwait(false);
        }

        var line = request.LineStart is > 0 ? request.LineStart.Value : 1;
        var endLine = request.LineEnd is > 0 ? request.LineEnd.Value : line;

        await UiScheduler.Default.InvokeAsync(() =>
        {
            vm.IdeMcp.GoToPosition(absolute, line, 1, endLine, null);
        }).ConfigureAwait(false);

        return "OK";
    }

    private static async Task<string> ExecuteMarkdownAsync(
        MainWindowViewModel vm,
        string root,
        CideMagicLinkRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.DocPath))
            return "md: doc missing";

        var sln = Directory.EnumerateFiles(root, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault()
                  ?? Directory.EnumerateFiles(root, "*.slnx", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(sln)
            && SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(sln, vm.Workspace.SolutionPath))
        {
            await vm.LoadSolutionAsync(sln).ConfigureAwait(false);
        }

        var docRel = request.DocPath!;
        var opened = false;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            opened = WorkspaceMarkdownPreviewOpener.TryOpenRepoDocument(
                root,
                docRel,
                (title, content, source) => vm.MarkdownPreviewTool.SetContent(title, content, source, request.DocLine),
                out _);
            if (opened)
            {
                vm.ApplyMfdRegionExpanded(true);
                vm.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
            }
        }).ConfigureAwait(false);

        return opened ? "OK" : "md_open_failed";
    }
}
