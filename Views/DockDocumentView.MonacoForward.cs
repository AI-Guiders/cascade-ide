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
        }

        if (_vm?.WorkspaceDiagnostics is not null && _monacoDiagHandler is not null)
            _vm.WorkspaceDiagnostics.DiagnosticsChanged -= _monacoDiagHandler;
        _monacoDiagHandler = null;

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
        var text = _vm.EditorText ?? _docVm.Doc.Content ?? "";
        var intel = CideEditorLanguageIds.SupportsRoslynIntelligence(filePath);
        try
        {
            await _monacoHost.PushSetModelAsync(filePath, text).ConfigureAwait(true);
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

    private void OnMonacoInbound(object? sender, CideEditorInboundMessage msg)
    {
        try
        {
            if (_monacoSuppress || _vm is null || _docVm is null || !IsActive())
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

    private void ScheduleMonacoHighlight()
    {
        _monacoHighlightTimer?.Stop();
        _monacoHighlightTimer?.Start();
    }

    private void ScheduleMonacoDiagnosticsRefresh()
    {
        _monacoDiagnosticsTimer?.Stop();
        _monacoDiagnosticsTimer?.Start();
    }

    private void ScheduleMonacoInlayRefresh(bool clearImmediately)
    {
        _monacoInlayGeneration += 1;
        if (clearImmediately)
            _ = ClearMonacoInlayHintsAsync();

        _monacoInlayTimer?.Stop();
        _monacoInlayTimer?.Start();
    }

    private void ScheduleMonacoControlFlowRefresh()
    {
        _monacoCfTimer?.Stop();
        _monacoCfTimer?.Start();
    }

    private async Task ClearMonacoInlayHintsAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady)
            return;

        try
        {
            await _monacoHost.PushInlayHintsAsync([], expectedModelVersion: null).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco inlay clear: " + ex.Message);
        }
    }

    private async Task ClearMonacoReferenceHighlightsAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady)
            return;

        try
        {
            await _monacoHost.PushDecorationsAsync(CideEditorBusManifest.SetIds.Highlights, []).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco highlight clear: " + ex.Message);
        }
    }

    private async Task PushMonacoDiagnosticsDecorationsAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
            return;

        var filePath = _docVm.Doc.FilePath ?? "";
        _monacoHost.Session.ReadSnapshot(out var version, out var text, out _, out _, out _);
        var strips = _vm.WorkspaceDiagnostics.GetStripsForFile(filePath);
        var push = MonacoEditorPresentationProjector.ProjectDiagnosticsOnly(version, text, strips);
        try
        {
            await _monacoHost.PushDecorationsAsync(
                CideEditorBusManifest.SetIds.Diagnostics,
                push.DiagnosticDecorations,
                push.ModelVersion).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco diagnostics: " + ex.Message);
        }
    }

    private async Task PushMonacoInlayHintsWhenIdleAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
            return;

        var generation = _monacoInlayGeneration;
        var filePath = _docVm.Doc.FilePath ?? "";
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(filePath))
            return;

        _monacoHost.Session.ReadSnapshot(out var version, out var text, out _, out _, out _);
        var strips = _vm.WorkspaceDiagnostics.GetStripsForFile(filePath);
        var parts = _vm.GetEditorInlineHintsForFile(filePath, text);
        var hints = MonacoEditorPresentationProjector.MergeInlayHints(text, strips, parts);
        try
        {
            await _monacoHost.PushInlayHintsAsync(hints, version).ConfigureAwait(true);
            if (generation != _monacoInlayGeneration)
                await ClearMonacoInlayHintsAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco inlay hints: " + ex.Message);
        }
    }

    private MonacoEditorCapabilityContext? BuildCapabilityContext()
    {
        if (_monacoHost is null || _docVm is null || _vm is null)
            return null;

        var filePath = _docVm.Doc.FilePath ?? "";
        return new MonacoEditorCapabilityContext
        {
            Host = _monacoHost,
            FilePath = filePath,
            GetEditorText = () =>
            {
                _monacoHost.Session.ReadSnapshot(out _, out var t, out _, out _, out _);
                return t;
            },
            CSharpLanguage = _vm.CSharpLanguage,
            WorkspaceDiagnostics = _vm.WorkspaceDiagnostics,
            ResolveQuickInfoAsync = (path, text, line, column, ct) =>
                _vm.GetEditorQuickInfoAsync(path, text, line, column, ct),
            CSharpLspHost = _vm.CSharpLspHost,
            GetInlineHintsForFile = _vm.GetEditorInlineHintsForFile,
            GetCodeLensesForFile = path =>
                MonacoEditorCodeLensComposer.FromNavigationScene(
                    path,
                    _vm.NavigationMap.CodeNavigationMapGraphScene),
            TryNavigateCodeLens = lensId => _vm.TryNavigateCodeLens(lensId),
            NavigateToLocationAsync = loc =>
            {
                _vm.EditorNavigation.TryNavigateGoTo(loc.FilePath, loc.Line, loc.Column);
                return Task.CompletedTask;
            },
        };
    }

    private void SyncMonacoFromVmIfActive()
    {
        if (!IsActive() || _vm is null || _docVm is null || _monacoHost is null || !_monacoHost.IsReady)
            return;

        var vmText = _vm.EditorText ?? "";
        _monacoHost.Session.ReadSnapshot(out _, out var current, out _, out _, out _);
        if (string.Equals(current, vmText, StringComparison.Ordinal))
            return;

        _ = _monacoHost.PushSetModelAsync(_docVm.Doc.FilePath ?? "untitled", vmText);
    }

    private async Task PushMonacoDiagnosticsAsync() =>
        await PushMonacoDiagnosticsDecorationsAsync().ConfigureAwait(true);

    private async Task PushMonacoInlayHintsAsync() =>
        await PushMonacoInlayHintsWhenIdleAsync().ConfigureAwait(true);

    private async Task PushMonacoSemanticTokensLegendAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
            return;

        var filePath = _docVm.Doc.FilePath ?? "";
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(filePath))
            return;

        var legend = _vm.CSharpLspHost?.SemanticLegend;
        if (legend is null)
            return;

        try
        {
            await _monacoHost.PushSemanticTokensLegendAsync(legend).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco semantic legend: " + ex.Message);
        }
    }

    private async Task PushMonacoHighlightsAsync()
    {
        try
        {
            if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
                return;

            var filePath = _docVm.Doc.FilePath ?? "";
            if (!CideEditorLanguageIds.SupportsRoslynIntelligence(filePath))
            {
                await _monacoHost.PushDecorationsAsync(CideEditorBusManifest.SetIds.Highlights, []).ConfigureAwait(true);
                return;
            }

            _monacoHost.Session.ReadSnapshot(out _, out var text, out var caret, out _, out _);
            var (line, column) = LineColumnFromOffset(text, caret);
            _monacoIntelCts?.Cancel();
            var cts = new CancellationTokenSource();
            _monacoIntelCts = cts;
            var spans = await Task.Run(() =>
                _vm.CSharpLanguage.GetHighlightSpans(filePath, text, line, column, cts.Token)).ConfigureAwait(true);
            if (cts.IsCancellationRequested || _monacoHost is null || !_monacoHost.IsReady || !IsActive())
                return;

            await _monacoHost.PushDecorationsAsync(
                CideEditorBusManifest.SetIds.Highlights,
                MonacoEditorHighlightMapper.ToDecorations(spans)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco highlights: " + ex.Message);
        }
    }

    private async Task PushMonacoControlFlowGlyphsAsync()
    {
        try
        {
            if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
                return;

            var filePath = _docVm.Doc.FilePath ?? "";
            var laneActive = _vm.NavigationMap.IsControlFlowEditorVirtualSpacingActiveForFile(filePath);
            if (!laneActive)
            {
                await _monacoHost.PushGutterGlyphsAsync([]).ConfigureAwait(true);
                await _monacoHost.PushCfContentLaneAsync(false, 0).ConfigureAwait(true);
                return;
            }

            var visuals = _vm.NavigationMap.GetControlFlowGutterLineVisualsForFile(filePath);
            await _monacoHost.PushCfContentLaneAsync(true, EditorControlFlowLanePolicy.LaneWidthPixels)
                .ConfigureAwait(true);
            await _monacoHost.PushGutterGlyphsAsync(MonacoEditorGutterMapper.ToGlyphs(visuals)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco CF gutter: " + ex.Message);
        }
    }

    private void UpdateMonacoStickyScroll(int topLineOneBased)
    {
        if (_stickyScrollHost is null || _stickyScrollText is null || _docVm is null || _vm is null)
            return;
        if (!IsActive())
        {
            ToolTip.SetTip(_stickyScrollHost, null);
            _stickyScrollHost.IsVisible = false;
            return;
        }

        var filePath = _docVm.Doc.FilePath ?? "";
        if (!filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            ToolTip.SetTip(_stickyScrollHost, null);
            _stickyScrollHost.IsVisible = false;
            _ = _monacoHost?.PushStickyScrollAsync(null);
            return;
        }

        var text = _vm.EditorText ?? "";
        var sticky = BuildStickyScrollLabel(text, topLineOneBased);
        if (string.IsNullOrWhiteSpace(sticky))
        {
            ToolTip.SetTip(_stickyScrollHost, null);
            _stickyScrollHost.IsVisible = false;
            _ = _monacoHost?.PushStickyScrollAsync(null);
            return;
        }

        _stickyScrollText.Text = sticky;
        ToolTip.SetTip(_stickyScrollHost, sticky);
        _stickyScrollHost.IsVisible = true;
        _ = _monacoHost?.PushStickyScrollAsync(sticky);
    }

    private static int TryOffsetFromLineColumn(string text, int lineOneBased, int columnOneBased)
    {
        if (string.IsNullOrEmpty(text) || lineOneBased < 1 || columnOneBased < 1)
            return -1;
        var lineStart = 0;
        var line = 1;
        for (var i = 0; i < text.Length && line < lineOneBased; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        if (line != lineOneBased)
            return -1;
        var offset = lineStart + columnOneBased - 1;
        return offset <= text.Length ? offset : text.Length;
    }

    private static (int line, int column) LineColumnFromOffset(string text, int offset)
    {
        if (string.IsNullOrEmpty(text) || offset < 0)
            return (1, 1);
        offset = Math.Min(offset, text.Length);
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < offset; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return (line, offset - lineStart + 1);
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
