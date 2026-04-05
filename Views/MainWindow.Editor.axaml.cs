using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private void SetupEditorAndTextMate()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vmSetup)
            return;

        // Use a dark TextMate theme to keep syntax readable in Focus/Balanced/Power dark palettes.
        _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        Services.TextMateTomlGrammar.TryLoadInto(_registryOptions);
        _languageService = vmSetup.CSharpLanguage;

        // Providers must always read/write the *active* dock document editor.
        vmSetup.SetEditorStateProvider(maxPreview =>
        {
            var active = TryGetActiveDockEditor();
            return active is null ? null : GetEditorState(active, vmSetup.CurrentFilePath, maxPreview);
        });
        vmSetup.SetEditorContentRangeProvider((startLine, endLine) =>
        {
            var active = TryGetActiveDockEditor();
            return active is null ? "" : GetEditorContentRange(active, startLine, endLine);
        });
        vmSetup.SetApplyEdit((path, sl, sc, el, ec, newText) =>
            ApplyEditInActiveDockEditor(vmSetup, path, sl, sc, el, ec, newText));
        vmSetup.SetFocusEditor(() =>
        {
            var active = TryGetActiveDockEditor();
            active?.Focus();
        });

        if (!_workspaceEventsAttached)
        {
            _workspaceEventsAttached = true;
            vmSetup.Workspace.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ViewModels.SolutionWorkspaceViewModel.SolutionPath))
                    _languageService?.InvalidateCache();
            };
        }

        // Initial attachment (if a dock editor already exists).
        TryAttachTextMateAndRenderers();
        SyncFromViewModel();
    }

    /// <summary>Один раз на экземпляр <see cref="TextEditor"/> (вкладка). Вызывается из <see cref="DockDocumentView"/>.</summary>
    internal void EnsureTextMateOnEditor(TextEditor editor)
    {
        if (_registryOptions is null)
            return;
        if (_textMateByEditor.TryGetValue(editor, out _))
            return;
        var inst = editor.InstallTextMate(_registryOptions);
        _textMateByEditor.Add(editor, inst);
    }

    private void TryAttachTextMateAndRenderers()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vmSetup)
            return;

        var editor = TryGetActiveDockEditor();
        if (editor is null)
        {
            LogHighlight("TryAttachTextMateAndRenderers: active editor not found.");
            return;
        }

        try
        {
            EnsureTextMateOnEditor(editor);
            LogHighlight($"TextMate: ensured for active editor, file='{vmSetup.CurrentFilePath ?? "<null>"}'.");
        }
        catch (Exception ex)
        {
            LogHighlight($"InstallTextMate: FAILED: {ex}");
            throw;
        }

        ApplyGrammarByFilePath(editor, vmSetup.CurrentFilePath);

        _editorIntelligence?.Detach();
        _editorIntelligence = new Services.EditorIntelligence(editor, _languageService!, () =>
            (vmSetup.CurrentFilePath, editor.Document.Text));
        _editorIntelligence.Attach();

        if (_marginPointerEditor is { } prev && !ReferenceEquals(prev, editor))
            prev.TextArea.PointerPressed -= OnDockMarginPointerPressed;
        _marginPointerEditor = editor;
        editor.TextArea.PointerPressed -= OnDockMarginPointerPressed;
        editor.TextArea.PointerPressed += OnDockMarginPointerPressed;
    }

    private void OnDockMarginPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_marginPointerEditor is null || DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        OnEditorMarginPointerPressed(e, _marginPointerEditor, vm);
    }

    /// <summary>
    /// Called by DockDocumentView when its editor is fully created in visual tree.
    /// Ensures TextMate/grammar attach happens after the editor is actually available.
    /// </summary>
    internal void AttachTextMateWhenEditorReady()
    {
        TryAttachTextMateAndRenderers();
        SyncFromViewModel();
    }

    private TextEditor? TryGetActiveDockEditor()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return null;

        var targetPath = vm.CurrentFilePath;
        if (string.IsNullOrWhiteSpace(targetPath))
            return null;

        foreach (var v in EnumerateVisualDescendants(this))
        {
            if (v is not DockDocumentView dockView)
                continue;

            if (dockView.DataContext is not ViewModels.DockDocumentViewModel dv)
                continue;

            if (!string.Equals(dv.Doc.FilePath, targetPath, StringComparison.OrdinalIgnoreCase))
                continue;

            return dockView.FindControl<TextEditor>("Editor");
        }

        // Fallback: if we can't match by path, try any dock editor named "Editor".
        foreach (var v in EnumerateVisualDescendants(this))
        {
            if (v is DockDocumentView dockView && dockView.FindControl<TextEditor>("Editor") is { } ed)
                return ed;
        }

        return null;
    }

    private static IEnumerable<Visual> EnumerateVisualDescendants(Visual root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            yield return child;
            foreach (var grandChild in EnumerateVisualDescendants(child))
                yield return grandChild;
        }
    }

    private void ApplyEditInActiveDockEditor(
        ViewModels.MainWindowViewModel vmSetup,
        string filePath,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        string newText)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        if (vmSetup.CurrentFilePath is null ||
            !string.Equals(vmSetup.CurrentFilePath, filePath, StringComparison.OrdinalIgnoreCase))
            return;

        var editor = TryGetActiveDockEditor();
        if (editor is null)
            return;

        var text = editor.Document.Text;
        int start = EditorTextCoordinateUtilities.LineColumnToOffset(text, startLine, startColumn);
        int end = EditorTextCoordinateUtilities.LineColumnToOffset(text, endLine, endColumn);
        if (start < 0 || end < 0)
            return;

        editor.Document.Replace(start, end - start, newText);
    }

    private static Services.EditorStateDto GetEditorState(TextEditor editor, string? currentFilePath, int? maxPreviewChars)
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
        return new Services.EditorStateDto
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

    private static string? GetEditorContentRange(TextEditor editor, int startLine, int endLine)
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

    private static void ApplyEditInEditor(TextEditor editor, ViewModels.MainWindowViewModel vm, string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText)
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.CurrentFilePath))
        {
            SyncFromViewModel();
            _languageService?.InvalidateCache();
            TryAttachTextMateAndRenderers();
        }
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.EditorSelectionStart) or nameof(ViewModels.MainWindowViewModel.EditorSelectionLength)
            && DataContext is ViewModels.MainWindowViewModel vm && vm.EditorSelectionStart is { } start && vm.EditorSelectionLength is { } length)
        {
            ApplyEditorSelection(start, length);
            vm.EditorSelectionStart = null;
            vm.EditorSelectionLength = null;
        }
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsSolutionExplorerVisible) && DataContext is ViewModels.MainWindowViewModel vmSol)
            UpdateSolutionColumnWidth(vmSol.IsSolutionExplorerVisible);
        if ((e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsChatPanelExpanded) or nameof(ViewModels.MainWindowViewModel.UiMode)
            or nameof(ViewModels.MainWindowViewModel.ChatPanelColumnPixelWidth) or nameof(ViewModels.MainWindowViewModel.IsChatPanelColumnVisible))
            && DataContext is ViewModels.MainWindowViewModel vmChat)
            UpdateChatColumnWidth(vmChat);
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsMarkdownPreviewVisible) && DataContext is ViewModels.MainWindowViewModel vmMd)
        {
            UpdateMarkdownPreviewColumn(vmMd.IsMarkdownPreviewVisible);
            UpdateInlineMarkdownPreview();
        }
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsMarkdownFile))
            UpdateInlineMarkdownPreview();
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.EditorText))
            UpdateInlineMarkdownPreview();
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.SelectedOllamaModel) && DataContext is ViewModels.MainWindowViewModel vm2
            && vm2.SelectedOllamaModel == ViewModels.MainWindowViewModel.InstallNewSentinel)
            _ = ShowInstallModelDialogAsync(vm2);
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.BreakpointLinesInCurrentFile)
            or nameof(ViewModels.MainWindowViewModel.DebuggerBreakpointLinesInCurrentFile)
            or nameof(ViewModels.MainWindowViewModel.McpFileBreakpointLinesInCurrentFile)
            or nameof(ViewModels.MainWindowViewModel.AllBreakpointLinesInCurrentFile)
            or nameof(ViewModels.MainWindowViewModel.CurrentFilePath)
            or nameof(ViewModels.MainWindowViewModel.DebugCurrentLineInCurrentFile)
            or nameof(ViewModels.MainWindowViewModel.DebugPositionFile)
            or nameof(ViewModels.MainWindowViewModel.DebugPositionLine))
        {
            var ed = TryGetActiveDockEditor();
            if (ed is not null)
            {
                ed.TextArea.TextView.Redraw();
                if (DataContext is ViewModels.MainWindowViewModel mainVm
                    && mainVm.DebugCurrentLineInCurrentFile is var debugLine && debugLine > 0
                    && debugLine <= ed.Document.LineCount)
                    ScrollEditorToLine(ed, debugLine);
            }
        }
    }

    /// <summary>Клик по полю слева от текста (номера строк / брейкпоинты) — переключить брейкпоинт в .dotnet-debug-mcp-breakpoints.json.</summary>
    private static void OnEditorMarginPointerPressed(PointerPressedEventArgs e, TextEditor editor, ViewModels.MainWindowViewModel vm)
    {
        var textView = editor.TextArea.TextView;
        var pt = e.GetCurrentPoint(textView).Position;
        const double gutterWidth = 50;
        if (pt.X > gutterWidth || pt.X < 0)
            return;
        var scrollOffset = textView.ScrollOffset;
        var docY = pt.Y + scrollOffset.Y;
        var vl = textView.VisualLines?.FirstOrDefault(v => docY >= v.VisualTop && docY < v.VisualTop + v.Height);
        if (vl is null)
            return;
        var line = vl.FirstDocumentLine.LineNumber;
        vm.ToggleBreakpointInFile(line);
    }

    private static void ScrollEditorToLine(TextEditor editor, int oneBasedLine)
    {
        if (oneBasedLine < 1 || oneBasedLine > editor.Document.LineCount)
            return;
        var line = editor.Document.GetLineByNumber(oneBasedLine);
        editor.TextArea.Caret.Offset = line.Offset;
        editor.TextArea.Caret.BringCaretToView();
    }

    private void OnGotoEditorLineColumn(int line1, int column1)
    {
        var editor = TryGetActiveDockEditor();
        if (editor is null)
            return;
        var text = editor.Document.Text ?? "";
        var offset = EditorTextCoordinateUtilities.LineColumnToOffset(text, line1, column1);
        if (offset < 0)
            return;
        editor.TextArea.Caret.Offset = offset;
        editor.TextArea.Caret.BringCaretToView();
    }

    private void ApplyEditorSelection(int start, int length)
    {
        var editor = TryGetActiveDockEditor();
        if (editor is null)
            return;
        var docLen = editor.Document.TextLength;
        start = Math.Clamp(start, 0, docLen);
        length = Math.Clamp(length, 0, docLen - start);
        editor.Select(start, length);
    }
}
