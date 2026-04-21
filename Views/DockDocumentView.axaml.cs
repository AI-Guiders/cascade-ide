using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using CascadeIDE.Features.Documents;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class DockDocumentView : UserControl
{
    private bool _suppress;
    private TextEditor? _editor;
    private MainWindowViewModel? _vm;
    private DockDocumentViewModel? _docVm;
    private PropertyChangedEventHandler? _vmHandler;
    private PropertyChangedEventHandler? _documentsHandler;

    private bool _renderersInstalled;
    private EditorDiagnosticBackgroundRenderer? _diagRenderer;
    private Action? _diagHubHandler;
    private bool _diagPointerHooked;
    private DispatcherTimer? _diagTipDebounce;
    private Point _lastPointerInTextView;
    private string? _lastTipText;
    private int _tooltipSeq;

    public DockDocumentView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => TrySetup();
        AttachedToVisualTree += (_, _) => TrySetup();
        UiScheduler.Default.Post(TrySetup);
    }

    private void TrySetup()
    {
        Teardown();

        _docVm = DataContext as DockDocumentViewModel;
        if (_docVm is null)
            return;

        // Avalonia 12: VisualRoot может быть не Window; DataContext главного окна всё равно нужен для документа и TextMate.
        var top = TopLevel.GetTopLevel(this);
        _vm = top?.DataContext as MainWindowViewModel;
        if (_vm is null)
        {
            for (Visual? v = this; v is not null; v = v.GetVisualParent())
            {
                if (v is MainWindow mw)
                {
                    _vm = mw.DataContext as MainWindowViewModel;
                    break;
                }
            }
        }

        if (_vm is null)
            return;

        MainWindow? mainWindow = null;
        for (Visual? v = this; v is not null; v = v.GetVisualParent())
        {
            if (v is MainWindow mw)
            {
                mainWindow = mw;
                break;
            }
        }

        _editor = this.FindControl<TextEditor>("Editor");
        if (_editor is null)
            return;

        // Иначе тултип якорится к огромному TextEditor и «прыгает» в углу; Pointer — у курсора (как в IDE).
        ToolTip.SetPlacement(_editor, PlacementMode.Pointer);
        ToolTip.SetShowDelay(_editor, 280);
        ToolTip.SetVerticalOffset(_editor, 10);
        ToolTip.SetHorizontalOffset(_editor, 10);

        _suppress = true;
        try
        {
            var fromModel = _docVm.Doc.Content ?? "";
            if (!string.Equals(_editor.Document.Text, fromModel, StringComparison.Ordinal))
                _editor.Document.Text = fromModel;
        }
        finally
        {
            _suppress = false;
        }

        _editor.Document.Changed += OnEditorDocumentChanged;
        _editor.TextArea.Caret.PositionChanged += OnEditorCaretOrSelectionChanged;
        _editor.TextArea.SelectionChanged += OnEditorCaretOrSelectionChanged;

        _vmHandler = (_, args) =>
        {
            if (args.PropertyName is nameof(MainWindowViewModel.EditorText)
                or nameof(MainWindowViewModel.CurrentFilePath))
            {
                SyncFromVmIfActive();
                UpdateMcpProvidersIfActive();
            }

            if (args.PropertyName is nameof(MainWindowViewModel.BreakpointLinesInCurrentFile)
                or nameof(MainWindowViewModel.DebuggerBreakpointLinesInCurrentFile)
                or nameof(MainWindowViewModel.McpFileBreakpointLinesInCurrentFile)
                or nameof(MainWindowViewModel.AllBreakpointLinesInCurrentFile)
                or nameof(MainWindowViewModel.DebugCurrentLineInCurrentFile)
                or nameof(MainWindowViewModel.DebugPositionFile)
                or nameof(MainWindowViewModel.DebugPositionLine))
            {
                if (_editor is not null)
                    _editor.TextArea.TextView.Redraw();
            }
        };
        _vm.PropertyChanged += _vmHandler;

        _documentsHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(DocumentsWorkspaceViewModel.DockActiveDocument))
            {
                SyncFromVmIfActive();
                UpdateMcpProvidersIfActive();
            }
        };
        _vm.Documents.PropertyChanged += _documentsHandler;

        if (mainWindow is not null)
        {
            mainWindow.EnsureTextMateOnEditor(_editor);
            mainWindow.AttachTextMateWhenEditorReady();
        }

        SyncFromVmIfActive();
        InstallVisualAdornersOnce();
        UpdateMcpProvidersIfActive();
        SyncCaretContextToVmIfActive();
    }

    private void Teardown()
    {
        _diagTipDebounce?.Stop();
        _diagTipDebounce = null;

        if (_editor is not null)
        {
            if (_diagPointerHooked)
            {
                _editor.TextArea.PointerMoved -= OnPointerMovedDiagnosticTip;
                _editor.TextArea.PointerExited -= OnPointerExitedDiagnosticTip;
                _diagPointerHooked = false;
            }

            _editor.Document.Changed -= OnEditorDocumentChanged;
            _editor.TextArea.Caret.PositionChanged -= OnEditorCaretOrSelectionChanged;
            _editor.TextArea.SelectionChanged -= OnEditorCaretOrSelectionChanged;

            if (_vm?.WorkspaceDiagnostics is not null && _diagHubHandler is not null)
                _vm.WorkspaceDiagnostics.DiagnosticsChanged -= _diagHubHandler;
            _diagHubHandler = null;

            if (_renderersInstalled && _editor.TextArea.TextView.BackgroundRenderers is { } br)
            {
                for (var i = br.Count - 1; i >= 0; i--)
                {
                    if (br[i] is BreakpointLineRenderer or DebugCurrentLineRenderer or DebugInstructionArrowRenderer
                        or EditorDiagnosticBackgroundRenderer)
                        br.RemoveAt(i);
                }
            }
        }

        if (_vm is not null)
        {
            if (_vmHandler is not null)
                _vm.PropertyChanged -= _vmHandler;
            if (_documentsHandler is not null)
                _vm.Documents.PropertyChanged -= _documentsHandler;
        }

        _diagRenderer = null;
        _editor = null;
        _vm = null;
        _docVm = null;
        _vmHandler = null;
        _documentsHandler = null;
        _renderersInstalled = false;
        _lastTipText = null;
    }

    private bool IsActive()
    {
        if (_vm is null || _docVm is null)
            return false;

        return ReferenceEquals(_vm.Documents.DockActiveDocument, _docVm)
               || string.Equals(_vm.CurrentFilePath, _docVm.Doc.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    private void InstallVisualAdornersOnce()
    {
        if (_renderersInstalled || _vm is null || _editor is null || _docVm is null)
            return;

        var textView = _editor.TextArea.TextView;
        textView.BackgroundRenderers.Add(new BreakpointLineRenderer(() =>
            _vm.GetAllBreakpointLinesForFile(_docVm.Doc.FilePath)));
        textView.BackgroundRenderers.Add(new DebugCurrentLineRenderer(() =>
            _vm.GetDebugCurrentLineForFile(_docVm.Doc.FilePath)));
        textView.BackgroundRenderers.Add(new DebugInstructionArrowRenderer(() =>
            _vm.GetDebugCurrentLineForFile(_docVm.Doc.FilePath)));

        _diagRenderer = new EditorDiagnosticBackgroundRenderer(() => _vm.WorkspaceDiagnostics.GetStripsForFile(_docVm.Doc.FilePath));
        textView.BackgroundRenderers.Add(_diagRenderer);

        _diagHubHandler = () =>
        {
            if (_editor is not null)
                _editor.TextArea.TextView.Redraw();
        };
        _vm.WorkspaceDiagnostics.DiagnosticsChanged += _diagHubHandler;

        if (!_diagPointerHooked)
        {
            _editor.TextArea.PointerMoved += OnPointerMovedDiagnosticTip;
            _editor.TextArea.PointerExited += OnPointerExitedDiagnosticTip;
            _diagPointerHooked = true;
        }

        _diagTipDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _diagTipDebounce.Tick += (_, _) =>
        {
            _diagTipDebounce?.Stop();
            UpdateDiagnosticToolTip();
        };

        _renderersInstalled = true;
    }

    private void OnPointerMovedDiagnosticTip(object? sender, PointerEventArgs e)
    {
        if (_editor is null)
            return;
        _lastPointerInTextView = e.GetPosition(_editor.TextArea.TextView);
        _diagTipDebounce?.Stop();
        _diagTipDebounce?.Start();
    }

    private void OnPointerExitedDiagnosticTip(object? sender, PointerEventArgs e)
    {
        if (_editor is not null)
            ToolTip.SetTip(_editor, null);
        _lastTipText = null;
        _tooltipSeq++;
    }

    private void UpdateDiagnosticToolTip()
    {
        if (_editor is null || _vm is null || _docVm is null)
            return;
        if (!_docVm.Doc.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            ToolTip.SetTip(_editor, null);
            return;
        }

        var seq = ++_tooltipSeq;

        var tv = _editor.TextArea.TextView;
        var pos = tv.GetPosition(_lastPointerInTextView);
        if (pos is null)
        {
            if (_lastTipText is not null)
            {
                ToolTip.SetTip(_editor, null);
                _lastTipText = null;
            }

            return;
        }

        int offset;
        try
        {
            offset = _editor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
        }
        catch
        {
            ToolTip.SetTip(_editor, null);
            return;
        }

        var strips = _vm.WorkspaceDiagnostics.GetStripsForFile(_docVm.Doc.FilePath);
        var hit = WorkspaceDiagnosticsCoordinator.HitTest(strips, offset);
        if (hit is not null)
        {
            var tip = $"{hit.Id}: {hit.Message}";
            if (seq != _tooltipSeq)
                return;
            if (string.Equals(tip, _lastTipText, StringComparison.Ordinal))
                return;
            _lastTipText = tip;
            ToolTip.SetTip(_editor, tip);
            return;
        }

        var path = _docVm.Doc.FilePath;
        var text = _editor.Document.Text ?? "";
        var line = pos.Value.Line;
        var col = pos.Value.Column;
        _ = Task.Run(async () =>
        {
            string? q;
            try
            {
                q = await _vm.GetEditorQuickInfoAsync(path, text, line, col, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                q = _vm.CSharpLanguage.GetQuickInfo(path, text, line, col);
            }

            UiScheduler.Default.Post(() =>
            {
                if (seq != _tooltipSeq)
                    return;
                if (string.IsNullOrEmpty(q))
                {
                    if (_lastTipText is not null)
                    {
                        ToolTip.SetTip(_editor, null);
                        _lastTipText = null;
                    }

                    return;
                }

                if (string.Equals(q, _lastTipText, StringComparison.Ordinal))
                    return;
                _lastTipText = q;
                ToolTip.SetTip(_editor, q);
            });
        });
    }

    private void SyncFromVmIfActive()
    {
        if (!IsActive() || _vm is null || _editor is null)
            return;

        var vmText = _vm.EditorText ?? "";
        if (string.Equals(_editor.Document.Text, vmText, StringComparison.Ordinal))
            return;

        _suppress = true;
        try
        {
            _editor.Document.Text = vmText;
        }
        finally
        {
            _suppress = false;
        }
    }

    private void OnEditorDocumentChanged(object? sender, EventArgs e)
    {
        if (_suppress || _vm is null || _docVm is null || _editor is null)
            return;
        if (!IsActive())
            return;

        var newText = _editor.Document.Text ?? "";
        if (!string.Equals(_vm.EditorText, newText, StringComparison.Ordinal))
            _vm.EditorText = newText;
    }

    private void OnEditorCaretOrSelectionChanged(object? sender, EventArgs e) => SyncCaretContextToVmIfActive();

    private void SyncCaretContextToVmIfActive()
    {
        if (!IsActive() || _vm is null || _editor is null)
            return;

        var doc = _editor.Document;
        var offset = _editor.TextArea.Caret.Offset;
        if (offset < 0 || offset > doc.TextLength)
            offset = 0;
        _vm.UpdateSemanticMapCaretOffset(offset);
    }

    private void UpdateMcpProvidersIfActive()
    {
        if (!IsActive() || _vm is null || _editor is null)
            return;

        _vm.SetEditorStateProvider(maxPreview => EditorHelpers.GetEditorState(_editor, _vm.CurrentFilePath, maxPreview));
        _vm.SetEditorContentRangeProvider((startLine, endLine) => EditorHelpers.GetEditorContentRange(_editor, startLine, endLine));
        _vm.SetApplyEdit((path, sl, sc, el, ec, newText) =>
            EditorHelpers.ApplyEditInEditor(_editor, _vm, path, sl, sc, el, ec, newText));
        _vm.SetFocusEditor(() => _editor.Focus());
    }
}

internal static class EditorHelpers
{
    public static EditorStateDto GetEditorState(TextEditor editor, string? currentFilePath, int? maxPreviewChars)
    {
        var doc = editor.Document;
        var text = doc.Text ?? "";
        var caret = editor.TextArea.Caret;
        var offset = caret.Offset;
        if (offset < 0 || offset > doc.TextLength)
            offset = 0;
        var line = doc.GetLineByOffset(offset);
        int selStart = 0, selLen = 0;
        var seg = editor.TextArea.Selection.Segments.FirstOrDefault();
        if (seg is not null)
        {
            selStart = seg.StartOffset;
            selLen = seg.EndOffset - seg.StartOffset;
        }

        var selectionText = selLen > 0 ? doc.GetText(selStart, selLen) : "";
        string? preview = null;
        if (maxPreviewChars is > 0)
            preview = text.Length <= maxPreviewChars.Value ? text : text[..maxPreviewChars.Value];

        return new EditorStateDto
        {
            FilePath = currentFilePath,
            CaretLine = line.LineNumber,
            CaretColumn = offset - line.Offset + 1,
            SelectionStart = selStart,
            SelectionLength = selLen,
            SelectionText = selectionText,
            ContentLength = text.Length,
            IsEmpty = text.Length == 0,
            ContentPreview = preview
        };
    }

    public static string? GetEditorContentRange(TextEditor editor, int startLine, int endLine)
    {
        var text = editor.Document.Text ?? "";
        if (text.Length == 0)
            return "";

        var lines = text.Split('\n');
        var oneBased = startLine >= 1 && endLine >= 1 && startLine <= endLine;
        if (!oneBased || lines.Length == 0)
            return "";

        var from = Math.Max(1, Math.Min(startLine, lines.Length));
        var to = Math.Max(from, Math.Min(endLine, lines.Length));
        return string.Join("\n", lines.Skip(from - 1).Take(to - from + 1));
    }

    public static void ApplyEditInEditor(
        TextEditor editor,
        MainWindowViewModel vm,
        string filePath,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        string newText)
    {
        if (vm.CurrentFilePath != filePath)
            return;

        var text = editor.Document.Text;
        int start = EditorTextCoordinateUtilities.LineColumnToOffset(text, startLine, startColumn);
        int end = EditorTextCoordinateUtilities.LineColumnToOffset(text, endLine, endColumn);
        if (start < 0 || end < 0)
            return;

        editor.Document.Replace(start, end - start, newText);
    }
}
