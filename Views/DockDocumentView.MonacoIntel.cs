using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Features.Editor.Presentation;
using CascadeIDE.Features.WorkspaceNavigation.Presentation;
using CascadeIDE.Services;

namespace CascadeIDE.Views;

public partial class DockDocumentView
{
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
            GetSolutionPath = () => _vm.Workspace.SolutionPath,
            GetWorkspaceRoot = () => _vm.GetWorkspacePath(),
            ApplyWorkspaceChangesAsync = changes =>
            {
                _vm.Documents.ApplyRoslynWorkspaceChanges(changes);
                SyncMonacoFromVmIfActive();
                return Task.CompletedTask;
            },
        };
    }

    private void SyncMonacoFromVmIfActive()
    {
        if (!IsActive() || _vm is null || _docVm is null || _monacoHost is null || !_monacoHost.IsReady)
            return;

        var filePath = _docVm.Doc.FilePath ?? "untitled";
        var text = ResolveMonacoTextForThisTab();
        _monacoHost.Session.ReadSnapshot(out _, out var current, out _, out _, out _);
        if (string.Equals(current, text, StringComparison.Ordinal)
            && string.Equals(_monacoBoundFilePath, filePath, StringComparison.OrdinalIgnoreCase))
            return;

        _ = PushMonacoModelAsync(filePath, text);
    }

    private string ResolveMonacoTextForThisTab() =>
        DockDocumentMonacoTextResolver.Resolve(
            IsActive(),
            _vm?.CurrentFilePath,
            _vm?.EditorText,
            _docVm?.Doc.FilePath,
            _docVm?.Doc.Content);

    private async Task PushMonacoModelAsync(string filePath, string text)
    {
        if (_monacoHost is null)
            return;

        await _monacoHost.PushSetModelAsync(filePath, text).ConfigureAwait(true);
        _monacoBoundFilePath = filePath;
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

}
