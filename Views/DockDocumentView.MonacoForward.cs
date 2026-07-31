using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Features.Editor.Presentation;
using CascadeIDE.Features.WorkspaceNavigation.Presentation;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class DockDocumentView
{
    private MonacoEditorHostControl? _monacoHost;
    private bool _monacoSuppress;
    private string? _monacoBoundFilePath;
    private Action? _monacoDiagHandler;
    private DispatcherTimer? _monacoHighlightTimer;
    private DispatcherTimer? _monacoDiagnosticsTimer;
    private DispatcherTimer? _monacoInlayTimer;
    private DispatcherTimer? _monacoCfTimer;
    private int _monacoInlayGeneration;
    private CancellationTokenSource? _monacoIntelCts;
    private readonly ICideEditorCapabilityRouter _capabilityRouter = new CideEditorCapabilityRouter();

    private void TrySetupMonacoForward()
    {
        _monacoHost = this.FindControl<MonacoEditorHostControl>("MonacoHost");
        if (_monacoHost is null || _docVm is null || _vm is null)
            return;

        _monacoHost.Ready -= OnMonacoHostReady;
        _monacoHost.Ready += OnMonacoHostReady;
        _monacoHost.Inbound -= OnMonacoInbound;
        _monacoHost.Inbound += OnMonacoInbound;
        _monacoHost.HostShortcutRequested -= OnMonacoHostShortcutRequested;
        _monacoHost.HostShortcutRequested += OnMonacoHostShortcutRequested;
        _monacoHost.HostAttachDragCompleted -= OnMonacoHostAttachDragCompleted;
        _monacoHost.HostAttachDragCompleted += OnMonacoHostAttachDragCompleted;

        _editorSurface = new MonacoWebViewSurfaceAdapter(_monacoHost.Session, _docVm.Doc.FilePath);
        _documentHudLayer.ConfigureDiagnostics(p => _vm!.WorkspaceDiagnostics.GetStripsForFile(p));
        UpdateStabilizedHudRegistration();

        _monacoHighlightTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(280) };
        _monacoHighlightTimer.Tick += (_, _) =>
        {
            _monacoHighlightTimer?.Stop();
            _ = PushMonacoHighlightsAsync();
        };

        _monacoDiagnosticsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _monacoDiagnosticsTimer.Tick += (_, _) =>
        {
            _monacoDiagnosticsTimer?.Stop();
            _ = PushMonacoDiagnosticsDecorationsAsync();
        };

        // Inlays only after typing idle (VS/Rider pattern: hide while editing, refresh after pause).
        _monacoInlayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        _monacoInlayTimer.Tick += (_, _) =>
        {
            _monacoInlayTimer?.Stop();
            _ = PushMonacoInlayHintsWhenIdleAsync();
        };

        _monacoCfTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _monacoCfTimer.Tick += (_, _) =>
        {
            _monacoCfTimer?.Stop();
            _ = PushMonacoControlFlowGlyphsAsync();
        };

        _vmHandler = (_, args) =>
        {
            if (args.PropertyName is nameof(MainWindowViewModel.EditorText)
                or nameof(MainWindowViewModel.CurrentFilePath))
            {
                SyncMonacoFromVmIfActive();
                UpdateMonacoMcpProvidersIfActive();
            }

            if (args.PropertyName is nameof(MainWindowViewModel.BreakpointLinesInCurrentFile)
                or nameof(MainWindowViewModel.AllBreakpointLinesInCurrentFile)
                or nameof(MainWindowViewModel.DebugCurrentLineInCurrentFile)
                or nameof(MainWindowViewModel.DebugPositionFile)
                or nameof(MainWindowViewModel.DebugPositionLine))
            {
                _ = PushMonacoDebugOverlayAsync();
            }

            if (args.PropertyName == nameof(MainWindowViewModel.CurrentFilePath))
                UpdateStabilizedHudRegistration();
        };
        _vm.PropertyChanged += _vmHandler;

        _navigationMapHandler = (_, args) =>
        {
            if (args.PropertyName is nameof(WorkspaceNavigationMapViewModel.CodeNavigationMapGraphScene)
                or nameof(WorkspaceNavigationMapViewModel.CodeNavigationMapLevel)
                or nameof(WorkspaceNavigationMapViewModel.WorkspaceNavigationMapCfAnchorFullPath))
            {
                _ = PushMonacoControlFlowGlyphsAsync();
            }
        };
        _vm.NavigationMap.PropertyChanged += _navigationMapHandler;

        _documentsHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(DocumentsWorkspaceViewModel.DockActiveDocument))
            {
                SyncMonacoFromVmIfActive();
                UpdateMonacoMcpProvidersIfActive();
                UpdateStabilizedHudRegistration();
            }
        };
        _vm.Documents.PropertyChanged += _documentsHandler;

        _monacoDiagHandler = () =>
        {
            _ = PushMonacoDiagnosticsDecorationsAsync();
            ScheduleMonacoInlayRefresh(clearImmediately: false);
            _ = PushMonacoControlFlowGlyphsAsync();
        };
        _vm.WorkspaceDiagnostics.DiagnosticsChanged += _monacoDiagHandler;

        if (_monacoHost.IsReady)
            _ = InitializeMonacoDocumentAsync();
    }

    private void TeardownMonacoForward()
    {
        _monacoIntelCts?.Cancel();
        _monacoHighlightTimer?.Stop();
        _monacoDiagnosticsTimer?.Stop();
        _monacoInlayTimer?.Stop();
        _monacoCfTimer?.Stop();

        if (_monacoHost is not null)
        {
            _monacoHost.Ready -= OnMonacoHostReady;
            _monacoHost.Inbound -= OnMonacoInbound;
            _monacoHost.HostShortcutRequested -= OnMonacoHostShortcutRequested;
            _monacoHost.HostAttachDragCompleted -= OnMonacoHostAttachDragCompleted;
        }

        if (_vm?.WorkspaceDiagnostics is not null && _monacoDiagHandler is not null)
            _vm.WorkspaceDiagnostics.DiagnosticsChanged -= _monacoDiagHandler;
        _monacoDiagHandler = null;

        _monacoBoundFilePath = null;
        _monacoHost = null;
    }

    internal bool IsMonacoReady => _monacoHost?.IsReady == true;

    private void OnMonacoHostReady(object? sender, EventArgs e) =>
        UiScheduler.Default.Post(() => _ = InitializeMonacoDocumentAsync());

    private async Task InitializeMonacoDocumentAsync()
    {
        if (_monacoHost is null || _docVm is null || _vm is null || !_monacoHost.IsReady)
            return;

        var filePath = _docVm.Doc.FilePath ?? "untitled";
        var text = ResolveMonacoTextForThisTab();
        var intel = CideEditorLanguageIds.SupportsRoslynIntelligence(filePath);
        try
        {
            await PushMonacoModelAsync(filePath, text).ConfigureAwait(true);
            await _monacoHost.PushIntelligenceEnabledAsync(intel).ConfigureAwait(true);
            var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            await _monacoHost.PushThemeAsync(isDark).ConfigureAwait(true);
            await PushMonacoDiagnosticsDecorationsAsync().ConfigureAwait(true);
            await PushMonacoControlFlowGlyphsAsync().ConfigureAwait(true);
            await PushMonacoHighlightsAsync().ConfigureAwait(true);
            await PushMonacoDebugOverlayAsync().ConfigureAwait(true);
            await PushMonacoInlayHintsWhenIdleAsync().ConfigureAwait(true);
            await PushMonacoSemanticTokensLegendAsync().ConfigureAwait(true);
            _vm.UpdateCodeNavigationMapCaretOffset(_editorSurface?.CaretOffset ?? 0);
            _vm.ScheduleWorkspaceNavigationMapRefresh();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco init: " + ex.Message);
        }
    }

    private void OnMonacoHostShortcutRequested(object? sender, string tomlKey)
    {
        if (_vm is null)
            return;

        MainWindowHotkeyService.TryExecuteEditorHostShortcut(tomlKey, _vm);
    }

    private void OnMonacoHostAttachDragCompleted(object? sender, HostAttachDragCompleteEventArgs e)
    {
        if (_vm is null)
            return;

        _ = _vm.TryCompleteIntercomAttachDragAtScreen(e.ScreenX, e.ScreenY, e.Kind);
    }

    private void OnMonacoInbound(object? sender, CideEditorInboundMessage msg)
    {
        try
        {
            if (_vm is null || _docVm is null)
                return;

            if (CideEditorBusManifest.IsCapabilityRequest(msg.Type)
                || CideEditorBusManifest.IsCapabilitySideChannel(msg.Type))
            {
                if (string.Equals(msg.Type, CideEditorBusManifest.Capabilities.Navigate, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(msg.FilePath)
                    && msg.Line is int navLine)
                {
                    _vm.EditorNavigation.TryNavigateGoTo(
                        msg.FilePath,
                        navLine,
                        msg.Column ?? 1,
                        source: EditorNavigationSource.Other);
                    return;
                }

                var ctx = BuildCapabilityContext();
                if (ctx is not null)
                    _ = _capabilityRouter.HandleAsync(msg, ctx, CancellationToken.None);
                return;
            }

            if (_monacoSuppress || !IsActive())
                return;

            if (string.Equals(msg.Type, CideEditorBridgeTypes.DidChange, StringComparison.Ordinal)
                && msg.Text is not null)
            {
                var monacoText = msg.Text;
                var openDoc = _docVm.Doc;

                if (!string.Equals(openDoc.Content, monacoText, StringComparison.Ordinal))
                {
                    _vm.Documents.ApplyEditorTextToOpenDocument(openDoc, monacoText);
                    _vm.WorkspaceDiagnostics.ScheduleDocumentText(openDoc.FilePath, monacoText);
                }

                if (!string.Equals(_vm.EditorText, monacoText, StringComparison.Ordinal))
                {
                    _monacoSuppress = true;
                    try
                    {
                        _vm.EditorText = monacoText;
                    }
                    finally
                    {
                        _monacoSuppress = false;
                    }
                }

                PostStabilizedEditorInputIfActive(EditorInputDeltaKind.DocumentText);
                ScheduleMonacoDiagnosticsRefresh();
                ScheduleMonacoInlayRefresh(clearImmediately: true);
                ScheduleMonacoControlFlowRefresh();
                ScheduleMonacoHighlight();
                _ = ClearMonacoReferenceHighlightsAsync();
                return;
            }

            if (string.Equals(msg.Type, CideEditorBridgeTypes.DidChangeCursorSelection, StringComparison.Ordinal))
            {
                PostStabilizedEditorInputIfActive(EditorInputDeltaKind.CaretOrSelection);
                var caret = msg.CaretOffset ?? _editorSurface?.CaretOffset ?? 0;
                _vm.UpdateCodeNavigationMapCaretOffset(caret);
                ScheduleMonacoHighlight();
                ScheduleMonacoControlFlowRefresh();
                return;
            }

            if (string.Equals(msg.Type, CideEditorBridgeTypes.DidScroll, StringComparison.Ordinal)
                && msg.TopLine is int topLine)
            {
                UpdateMonacoStickyScroll(topLine);
                return;
            }

            if (string.Equals(msg.Type, CideEditorBridgeTypes.DidGutterClick, StringComparison.Ordinal)
                && msg.Line is int gutterLine)
            {
                _vm.ToggleBreakpointInFile(gutterLine);
                _ = PushMonacoDebugOverlayAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco inbound: " + ex);
        }
    }

    private void UpdateMonacoMcpProvidersIfActive()
    {
        if (!IsActive() || _vm is null || _monacoHost is null)
            return;

        _vm.SetEditorStateProvider(maxPreview =>
        {
            _monacoHost.Session.ReadSnapshot(out _, out var text, out var caret, out var selStart, out var selLen);
            var filePath = _vm.CurrentFilePath;
            var (line, column) = LineColumnFromOffset(text, caret);
            string? preview = null;
            if (maxPreview is > 0)
                preview = text.Length <= maxPreview.Value ? text : text[..maxPreview.Value];

            return new EditorStateDto
            {
                FilePath = filePath,
                CaretLine = line,
                CaretColumn = column,
                SelectionStart = selStart,
                SelectionLength = selLen,
                SelectionText = selLen > 0 && selStart + selLen <= text.Length
                    ? text.Substring(selStart, selLen)
                    : "",
                ContentLength = text.Length,
                IsEmpty = text.Length == 0,
                ContentPreview = preview,
            };
        });

        _vm.SetEditorContentRangeProvider((startLine, endLine) =>
        {
            _monacoHost.Session.ReadSnapshot(out _, out var text, out _, out _, out _);
            if (text.Length == 0)
                return "";
            var lines = text.Split('\n');
            if (startLine < 1 || endLine < startLine)
                return "";
            var from = Math.Max(1, Math.Min(startLine, lines.Length));
            var to = Math.Max(from, Math.Min(endLine, lines.Length));
            return string.Join("\n", lines.Skip(from - 1).Take(to - from + 1));
        });

        _vm.SetFocusEditor(() => _monacoHost.Focus());
    }

    private async Task PushMonacoDebugOverlayAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
            return;

        var filePath = _docVm.Doc.FilePath ?? "";
        _monacoHost.Session.ReadSnapshot(out _, out var text, out _, out _, out _);
        var breakpoints = _vm.GetAllBreakpointLinesForFile(filePath);
        var debugLine = _vm.GetDebugCurrentLineForFile(filePath);

        try
        {
            await _monacoHost.PushDecorationsAsync(
                CideEditorBusManifest.SetIds.Breakpoints,
                MonacoEditorDebugMapper.ToBreakpointDecorations(text, breakpoints)).ConfigureAwait(true);
            await _monacoHost.PushDecorationsAsync(
                CideEditorBusManifest.SetIds.DebugLine,
                MonacoEditorDebugMapper.ToDebugLineDecoration(text, debugLine)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco debug overlay: " + ex.Message);
        }
    }

    internal async Task ClearAgentRevealAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady)
            return;

        try
        {
            await _monacoHost.PushClearAgentRevealAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco clear agent reveal: " + ex.Message);
        }
    }
}
