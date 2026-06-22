using Avalonia.Controls;
using Avalonia.Threading;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Features.Editor.Presentation;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class DockDocumentView
{
    private MonacoEditorHostControl? _monacoHost;
    private bool _monacoSuppress;
    private Action? _monacoDiagHandler;
    private DispatcherTimer? _monacoHighlightTimer;
    private CancellationTokenSource? _monacoIntelCts;

    private bool UseMonacoForwardHost =>
        _vm?.GetCascadeSettingsForExecutor().Editor.ResolveForwardHost()
        == EditorForwardHostKind.MonacoWebView2;

    private void TrySetupMonacoForward()
    {
        _monacoHost = this.FindControl<MonacoEditorHostControl>("MonacoHost");
        if (_editor is not null)
            _editor.IsVisible = false;
        if (_monacoHost is not null)
            _monacoHost.IsVisible = true;

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

        _vmHandler = (_, args) =>
        {
            if (args.PropertyName is nameof(MainWindowViewModel.EditorText)
                or nameof(MainWindowViewModel.CurrentFilePath))
            {
                SyncMonacoFromVmIfActive();
                UpdateMonacoMcpProvidersIfActive();
            }

            if (args.PropertyName == nameof(MainWindowViewModel.CurrentFilePath))
                UpdateStabilizedHudRegistration();
        };
        _vm.PropertyChanged += _vmHandler;

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
            _ = PushMonacoDiagnosticsAsync();
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

        if (_monacoHost is not null)
        {
            _monacoHost.Ready -= OnMonacoHostReady;
            _monacoHost.Inbound -= OnMonacoInbound;
        }

        if (_vm?.WorkspaceDiagnostics is not null && _monacoDiagHandler is not null)
            _vm.WorkspaceDiagnostics.DiagnosticsChanged -= _monacoDiagHandler;
        _monacoDiagHandler = null;

        if (_monacoHost is not null)
            _monacoHost.IsVisible = false;
        if (_editor is not null)
            _editor.IsVisible = true;

        _monacoHost = null;
    }

    private void OnMonacoHostReady(object? sender, EventArgs e) =>
        UiScheduler.Default.Post(() => _ = InitializeMonacoDocumentAsync());

    private async Task InitializeMonacoDocumentAsync()
    {
        if (_monacoHost is null || _docVm is null || _vm is null || !_monacoHost.IsReady)
            return;

        var filePath = _docVm.Doc.FilePath ?? "untitled";
        var text = IsActive() ? _vm.EditorText ?? "" : _docVm.Doc.Content ?? "";
        var intel = CideEditorLanguageIds.SupportsRoslynIntelligence(filePath);
        try
        {
            await _monacoHost.PushSetModelAsync(filePath, text).ConfigureAwait(true);
            await _monacoHost.PushIntelligenceEnabledAsync(intel).ConfigureAwait(true);
            await PushMonacoDiagnosticsAsync().ConfigureAwait(true);
            await PushMonacoControlFlowGlyphsAsync().ConfigureAwait(true);
            await PushMonacoHighlightsAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco init: " + ex.Message);
        }
    }

    private void OnMonacoInbound(object? sender, CideEditorInboundMessage msg)
    {
        if (_monacoSuppress || _vm is null || _docVm is null || !IsActive())
            return;

        if (string.Equals(msg.Type, CideEditorBridgeTypes.DidChange, StringComparison.Ordinal)
            && msg.Text is not null
            && !string.Equals(_vm.EditorText, msg.Text, StringComparison.Ordinal))
        {
            _monacoSuppress = true;
            try
            {
                _vm.EditorText = msg.Text;
            }
            finally
            {
                _monacoSuppress = false;
            }

            PostStabilizedEditorInputIfActive(EditorInputDeltaKind.DocumentText);
            _ = PushMonacoControlFlowGlyphsAsync();
            return;
        }

        if (string.Equals(msg.Type, CideEditorBridgeTypes.DidChangeCursorSelection, StringComparison.Ordinal))
        {
            PostStabilizedEditorInputIfActive(EditorInputDeltaKind.CaretOrSelection);
            _vm.UpdateCodeNavigationMapCaretOffset(_editorSurface?.CaretOffset ?? 0);
            ScheduleMonacoHighlight();
            _ = PushMonacoControlFlowGlyphsAsync();
            return;
        }

        if (string.Equals(msg.Type, CideEditorBridgeTypes.DidScroll, StringComparison.Ordinal)
            && msg.TopLine is int topLine)
        {
            UpdateMonacoStickyScroll(topLine);
            return;
        }

        if (string.Equals(msg.Type, CideEditorBridgeTypes.RequestCompletion, StringComparison.Ordinal)
            && msg.RequestId is int completionId
            && msg.Line is int cLine
            && msg.Column is int cCol)
        {
            _ = HandleMonacoCompletionRequestAsync(completionId, cLine, cCol);
            return;
        }

        if (string.Equals(msg.Type, CideEditorBridgeTypes.RequestHover, StringComparison.Ordinal)
            && msg.RequestId is int hoverId
            && msg.Line is int hLine
            && msg.Column is int hCol)
        {
            _ = HandleMonacoHoverRequestAsync(hoverId, hLine, hCol);
            return;
        }

        if (string.Equals(msg.Type, CideEditorBridgeTypes.RequestSignature, StringComparison.Ordinal)
            && msg.RequestId is int sigId
            && msg.Line is int sLine
            && msg.Column is int sCol)
        {
            _ = HandleMonacoSignatureRequestAsync(sigId, sLine, sCol);
        }
    }

    private void ScheduleMonacoHighlight()
    {
        _monacoHighlightTimer?.Stop();
        _monacoHighlightTimer?.Start();
    }

    private async Task HandleMonacoCompletionRequestAsync(int requestId, int line, int column)
    {
        if (_monacoHost is null || _docVm is null || _vm is null)
            return;

        var filePath = _docVm.Doc.FilePath ?? "";
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(filePath))
        {
            await _monacoHost.PushCompletionResultAsync(requestId, []).ConfigureAwait(true);
            return;
        }

        _monacoHost.Session.ReadSnapshot(out _, out var text, out _, out _, out _);
        var items = await Task.Run(() =>
            _vm.CSharpLanguage.GetCompletionItems(filePath, text, line, column)).ConfigureAwait(true);
        var mapped = items.Select(i => new CideEditorCompletionItem(i.DisplayText, i.InsertText, i.Description)).ToList();
        try
        {
            await _monacoHost.PushCompletionResultAsync(requestId, mapped).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco completion: " + ex.Message);
        }
    }

    private async Task HandleMonacoHoverRequestAsync(int requestId, int line, int column)
    {
        if (_monacoHost is null || _docVm is null || _vm is null)
            return;

        var filePath = _docVm.Doc.FilePath ?? "";
        _monacoHost.Session.ReadSnapshot(out _, out var text, out _, out _, out _);
        var markdown = await ResolveMonacoHoverMarkdownAsync(filePath, text, line, column).ConfigureAwait(true);
        try
        {
            await _monacoHost.PushHoverResultAsync(requestId, markdown).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco hover: " + ex.Message);
        }
    }

    private async Task<string?> ResolveMonacoHoverMarkdownAsync(string filePath, string text, int line, int column)
    {
        if (_vm is null)
            return null;

        var offset = TryOffsetFromLineColumn(text, line, column);
        if (offset >= 0)
        {
            var strips = _vm.WorkspaceDiagnostics.GetStripsForFile(filePath);
            var hit = WorkspaceDiagnosticsCoordinator.HitTestForToolTip(
                strips, offset, line, column, text);
            if (hit is not null)
                return $"**{hit.Id}**: {hit.Message}";
        }

        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(filePath))
            return null;

        return await _vm.GetEditorQuickInfoAsync(filePath, text, line, column, CancellationToken.None)
            .ConfigureAwait(true)
            ?? _vm.CSharpLanguage.GetQuickInfo(filePath, text, line, column);
    }

    private async Task HandleMonacoSignatureRequestAsync(int requestId, int line, int column)
    {
        if (_monacoHost is null || _docVm is null || _vm is null)
            return;

        var filePath = _docVm.Doc.FilePath ?? "";
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(filePath))
        {
            await _monacoHost.PushSignatureResultAsync(requestId, null).ConfigureAwait(true);
            return;
        }

        _monacoHost.Session.ReadSnapshot(out _, out var text, out _, out _, out _);
        if (!text.Contains('('))
        {
            await _monacoHost.PushSignatureResultAsync(requestId, null).ConfigureAwait(true);
            return;
        }

        var sig = await Task.Run(() =>
            _vm.CSharpLanguage.GetSignatureHelp(filePath, text, line, column)).ConfigureAwait(true);
        try
        {
            await _monacoHost.PushSignatureResultAsync(requestId, sig).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco signature: " + ex.Message);
        }
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

    private async Task PushMonacoDiagnosticsAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
            return;

        var strips = _vm.WorkspaceDiagnostics.GetStripsForFile(_docVm.Doc.FilePath);
        var decos = MonacoEditorDiagnosticsMapper.ToDecorations(strips);
        try
        {
            await _monacoHost.PushDecorationsAsync("diagnostics", decos).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco diagnostics: " + ex.Message);
        }
    }

    private async Task PushMonacoHighlightsAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
            return;

        var filePath = _docVm.Doc.FilePath ?? "";
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(filePath))
        {
            await _monacoHost.PushDecorationsAsync("highlights", []).ConfigureAwait(true);
            return;
        }

        _monacoHost.Session.ReadSnapshot(out _, out var text, out var caret, out _, out _);
        var (line, column) = LineColumnFromOffset(text, caret);
        _monacoIntelCts?.Cancel();
        var cts = new CancellationTokenSource();
        _monacoIntelCts = cts;
        var spans = await Task.Run(() =>
            _vm.CSharpLanguage.GetHighlightSpans(filePath, text, line, column, cts.Token)).ConfigureAwait(true);
        if (cts.IsCancellationRequested)
            return;

        try
        {
            await _monacoHost.PushDecorationsAsync(
                "highlights",
                MonacoEditorHighlightMapper.ToDecorations(spans)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco highlights: " + ex.Message);
        }
    }

    private async Task PushMonacoControlFlowGlyphsAsync()
    {
        if (_monacoHost is null || !_monacoHost.IsReady || _docVm is null || _vm is null || !IsActive())
            return;

        var filePath = _docVm.Doc.FilePath ?? "";
        if (!_vm.NavigationMap.IsControlFlowEditorVirtualSpacingActiveForFile(filePath))
        {
            await _monacoHost.PushGutterGlyphsAsync([]).ConfigureAwait(true);
            return;
        }

        var visuals = _vm.NavigationMap.GetControlFlowGutterLineVisualsForFile(filePath);
        try
        {
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
}
