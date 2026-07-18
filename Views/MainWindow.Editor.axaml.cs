using System.ComponentModel;
using Avalonia.Threading;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Services;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private void SetupEditorAndTextMate()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vmSetup)
            return;

        _languageService = vmSetup.CSharpLanguage;

        vmSetup.SetApplyEdit((path, sl, sc, el, ec, newText) =>
            _ = vmSetup.Documents.ApplyMcpEditToDocument(path, sl, sc, el, ec, newText));
        vmSetup.SetRevealEditorRange((path, start, end, duration) =>
            vmSetup.EditorNavigation.TryNavigateReveal(
                path,
                start,
                end,
                duration,
                EditorNavigationSource.Mcp));
        vmSetup.SetFocusEditor(() =>
        {
            if (DataContext is not ViewModels.MainWindowViewModel vm)
                return;
            EditorActiveDockResolver.TryGetDockDocumentView(vm, vm.CurrentFilePath)?.FocusMonacoEditor();
        });

        if (!_workspaceEventsAttached)
        {
            _workspaceEventsAttached = true;
            vmSetup.Workspace.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(CascadeIDE.Features.Workspace.SolutionWorkspaceViewModel.SolutionPath))
                    _languageService?.InvalidateCache();
            };
        }
    }

    internal void AttachTextMateWhenEditorReady()
    {
        // Monaco forward host: document setup is owned by DockDocumentView.
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.CurrentFilePath)
            && DataContext is ViewModels.MainWindowViewModel vmPath)
        {
            _languageService?.InvalidateCache();
            _ = RefreshActiveDockDebugVisualsAsync(vmPath);
        }

        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.EditorSelectionStart)
            or nameof(ViewModels.MainWindowViewModel.EditorSelectionLength)
            && DataContext is ViewModels.MainWindowViewModel vm
            && vm.EditorSelectionStart is { } start
            && vm.EditorSelectionLength is { } length)
        {
            ApplyEditorSelection(start, length);
            vm.EditorSelectionStart = null;
            vm.EditorSelectionLength = null;
        }

        if ((e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsPfdRegionExpanded) or nameof(ViewModels.MainWindowViewModel.IsPfdColumnVisible))
            && DataContext is ViewModels.MainWindowViewModel vmSol)
            UpdateSolutionColumnWidth(vmSol.IsPfdColumnVisible);
        if ((e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsMfdRegionExpanded)
            or nameof(ViewModels.MainWindowViewModel.UiMode)
            or nameof(ViewModels.MainWindowViewModel.CurrentMfdShellPage)
            or nameof(ViewModels.MainWindowViewModel.MfdRegionPixelWidth) or nameof(ViewModels.MainWindowViewModel.IsMfdRegionVisible)
            or nameof(ViewModels.MainWindowViewModel.IsMfdColumnVisible)
            or nameof(ViewModels.MainWindowViewModel.ChatPanelColumnPixelWidth))
            && DataContext is ViewModels.MainWindowViewModel vmChat)
            UpdateChatColumnWidth(vmChat);
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsCommandPaletteOpen) && DataContext is ViewModels.MainWindowViewModel vmPalette)
            HandleCommandPaletteOpenStateChanged(vmPalette.IsCommandPaletteOpen);
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.SelectedOllamaModel) && DataContext is ViewModels.MainWindowViewModel vm2
            && vm2.SelectedOllamaModel == ViewModels.MainWindowViewModel.InstallNewSentinel)
            _ = ShowInstallModelDialogAsync(vm2);
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.BreakpointLinesInCurrentFile)
            or nameof(ViewModels.MainWindowViewModel.AllBreakpointLinesInCurrentFile)
            or nameof(ViewModels.MainWindowViewModel.CurrentFilePath)
            or nameof(ViewModels.MainWindowViewModel.DebugCurrentLineInCurrentFile)
            or nameof(ViewModels.MainWindowViewModel.DebugPositionFile)
            or nameof(ViewModels.MainWindowViewModel.DebugPositionLine))
        {
            if (DataContext is ViewModels.MainWindowViewModel mainVm)
                _ = RefreshActiveDockDebugVisualsAsync(mainVm);
        }

        if (e.PropertyName == nameof(ViewModels.MainWindowViewModel.MainGridColumnDefinitions) && sender is ViewModels.MainWindowViewModel gridVm)
            ApplyMainGridColumnDefinitions(gridVm);
        if (e.PropertyName == nameof(ViewModels.MainWindowViewModel.MainGridRowDefinitions) && sender is ViewModels.MainWindowViewModel rowVm)
            ApplyMainGridRowDefinitions(rowVm);
        if (IsSkiaHostRelatedProperty(e.PropertyName))
            InvalidateSkiaHosts();
    }

    private static async Task RefreshActiveDockDebugVisualsAsync(ViewModels.MainWindowViewModel vm)
    {
        var dock = EditorActiveDockResolver.TryGetDockDocumentView(vm, vm.CurrentFilePath);
        if (dock is null)
            return;

        await dock.PushMonacoDebugVisualsAsync().ConfigureAwait(true);

        if (vm.DebugCurrentLineInCurrentFile is var debugLine && debugLine > 0)
            await dock.GotoLineColumnAsync(debugLine, 1).ConfigureAwait(true);
    }

    private void OnGotoEditorLineColumn(int line1, int column1)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        var dock = EditorActiveDockResolver.TryGetDockDocumentView(vm, vm.CurrentFilePath);
        if (dock is null)
            return;

        _ = dock.GotoLineColumnAsync(line1, column1);
    }

    private void ApplyEditorSelection(int start, int length)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        var dock = EditorActiveDockResolver.TryGetDockDocumentView(vm, vm.CurrentFilePath);
        if (dock is null)
            return;

        _ = dock.SetSelectionAsync(start, length);
    }

    private void OnRefreshActiveEditorEpochDim(bool dimmed)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        var dock = EditorActiveDockResolver.TryGetDockDocumentView(vm, vm.CurrentFilePath);
        if (dock is null)
            return;

        _ = dock.SetEpochDimAsync(dimmed);
    }

    internal void WireAgentVerifyEpochDim(ViewModels.MainWindowViewModel vm)
    {
        vm.RefreshActiveEditorEpochDimRequested -= OnRefreshActiveEditorEpochDim;
        vm.RefreshActiveEditorEpochDimRequested += OnRefreshActiveEditorEpochDim;
    }

    private const int RevealEditorRangeMaxAttempts = 10;

    private void RevealEditorRangeInDock(string? filePath, int startLine, int endLine, int? durationMs = null) =>
        RevealEditorRangeInDockWithRetry(filePath, startLine, endLine, durationMs, attempt: 0);

    private void RevealEditorRangeInDockWithRetry(string? filePath, int startLine, int endLine, int? durationMs, int attempt)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        if (EditorActiveDockResolver.TryGetDockDocumentView(vm, filePath)?.TryRevealEditorRange(startLine, endLine, durationMs) == true)
            return;

        if (attempt >= RevealEditorRangeMaxAttempts)
            return;

        Dispatcher.UIThread.Post(
            () => RevealEditorRangeInDockWithRetry(filePath, startLine, endLine, durationMs, attempt + 1),
            DispatcherPriority.Background);
    }
}
