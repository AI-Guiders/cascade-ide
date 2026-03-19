using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
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

    private bool _renderersInstalled;

    public DockDocumentView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => TrySetup();
        Dispatcher.UIThread.Post(TrySetup);
    }

    private void TrySetup()
    {
        Teardown();

        _docVm = DataContext as DockDocumentViewModel;
        if (_docVm is null)
            return;

        if (VisualRoot is not Window w)
            return;

        _vm = w.DataContext as MainWindowViewModel;
        if (_vm is null)
            return;

        _editor = this.FindControl<TextEditor>("Editor");
        if (_editor is null)
            return;

        _editor.Document.Changed += OnEditorDocumentChanged;

        _vmHandler = (_, args) =>
        {
            if (args.PropertyName is nameof(MainWindowViewModel.EditorText)
                or nameof(MainWindowViewModel.CurrentFilePath)
                or nameof(MainWindowViewModel.DockActiveDocument))
            {
                SyncFromVmIfActive();
                UpdateMcpProvidersIfActive();
            }
        };
        _vm.PropertyChanged += _vmHandler;

        if (w is MainWindow mainWindow)
            mainWindow.AttachTextMateWhenEditorReady();

        SyncFromVmIfActive();
        UpdateMcpProvidersIfActive();
    }

    private void Teardown()
    {
        if (_editor is not null)
            _editor.Document.Changed -= OnEditorDocumentChanged;
        if (_vm is not null && _vmHandler is not null)
            _vm.PropertyChanged -= _vmHandler;

        _editor = null;
        _vm = null;
        _docVm = null;
        _vmHandler = null;
        _renderersInstalled = false;
    }

    private bool IsActive()
    {
        if (_vm is null || _docVm is null)
            return false;

        return ReferenceEquals(_vm.DockActiveDocument, _docVm)
               || string.Equals(_vm.CurrentFilePath, _docVm.Doc.FilePath, StringComparison.OrdinalIgnoreCase);
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

    private void UpdateMcpProvidersIfActive()
    {
        if (!IsActive() || _vm is null || _editor is null)
            return;

        // MCP: editor state, selection ranges, and ApplyEdit all operate on *this* editor.
        _vm.SetEditorStateProvider(maxPreview => EditorHelpers.GetEditorState(_editor, _vm.CurrentFilePath, maxPreview));
        _vm.SetEditorContentRangeProvider((startLine, endLine) => EditorHelpers.GetEditorContentRange(_editor, startLine, endLine));
        _vm.SetApplyEdit((path, sl, sc, el, ec, newText) =>
            EditorHelpers.ApplyEditInEditor(_editor, _vm, path, sl, sc, el, ec, newText));
        _vm.SetFocusEditor(() => _editor.Focus());

        if (_renderersInstalled)
            return;

        // Best-effort: show breakpoints and current debug line in this editor instance.
        _editor.TextArea.TextView.BackgroundRenderers.Add(new BreakpointLineRenderer(() => _vm.AllBreakpointLinesInCurrentFile));
        _editor.TextArea.TextView.BackgroundRenderers.Add(new DebugCurrentLineRenderer(() => _vm.DebugCurrentLineInCurrentFile));
        _renderersInstalled = true;
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
        int start = LineColumnToOffset(text, startLine, startColumn);
        int end = LineColumnToOffset(text, endLine, endColumn);
        if (start < 0 || end < 0)
            return;

        editor.Document.Replace(start, end - start, newText);
    }

    private static int LineColumnToOffset(string text, int line, int column)
    {
        if (line < 1 || column < 1)
            return -1;

        var lines = text.Split('\n');
        if (line > lines.Length)
            return -1;

        int offset = 0;
        for (int i = 0; i < line - 1; i++)
            offset += lines[i].Length + 1;

        int lineLen = lines[line - 1].Length;
        int col = Math.Min(column, lineLen + 1);
        return offset + (col - 1);
    }
}

