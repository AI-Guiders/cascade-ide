using Avalonia.Controls;
using Avalonia.Threading;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Features.Editor.Presentation;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Views;

public partial class DockDocumentView
{
    private MonacoEditorHostControl? _monacoHost;
    private bool _monacoSuppress;
    private Action? _monacoDiagHandler;

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

        _monacoDiagHandler = () => _ = PushMonacoDiagnosticsAsync();
        _vm.WorkspaceDiagnostics.DiagnosticsChanged += _monacoDiagHandler;

        if (_monacoHost.IsReady)
            _ = InitializeMonacoDocumentAsync();
    }

    private void TeardownMonacoForward()
    {
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

        var text = IsActive() ? _vm.EditorText ?? "" : _docVm.Doc.Content ?? "";
        try
        {
            await _monacoHost.PushSetModelAsync(_docVm.Doc.FilePath ?? "untitled", text).ConfigureAwait(true);
            await PushMonacoDiagnosticsAsync().ConfigureAwait(true);
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
            return;
        }

        if (string.Equals(msg.Type, CideEditorBridgeTypes.DidChangeCursorSelection, StringComparison.Ordinal))
        {
            PostStabilizedEditorInputIfActive(EditorInputDeltaKind.CaretOrSelection);
            _vm.UpdateCodeNavigationMapCaretOffset(_editorSurface?.CaretOffset ?? 0);
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

    private void UpdateMonacoMcpProvidersIfActive()
    {
        if (!IsActive() || _vm is null || _monacoHost is null)
            return;

        _vm.SetEditorStateProvider(maxPreview =>
        {
            _monacoHost.Session.ReadSnapshot(out _, out var text, out var caret, out var selStart, out var selLen);
            var filePath = _vm.CurrentFilePath;
            string? preview = null;
            if (maxPreview is > 0)
                preview = text.Length <= maxPreview.Value ? text : text[..maxPreview.Value];

            return new EditorStateDto
            {
                FilePath = filePath,
                CaretLine = 1,
                CaretColumn = caret + 1,
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
