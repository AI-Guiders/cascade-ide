using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.Editor.Application.Presentation;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    private EditorDocumentBackgroundVisualsHandle? _backgroundVisuals;
    private Action? _diagHubHandler;
    private bool _diagPointerHooked;
    private bool _inlineKeyHooked;
    private EditorInlineHoverToolTipController? _inlineHoverToolTip;
    private bool _editorThemeSubscribed;
    private Border? _stickyScrollHost;
    private TextBlock? _stickyScrollText;

    // ADR 0103: hi-freq → bounded + throttle на уровне MainWindowViewModel, не DataBus
    private IEditorSurfaceAdapter? _editorSurface;
    private readonly EditorDocumentHudLayer _documentHudLayer = new();
    private Action<EditorInputDelta>? _stabilizedHudAction;

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

        _editor = this.FindControl<TextEditor>("Editor");
        if (_editor is null)
            return;
        _stickyScrollHost = this.FindControl<Border>("StickyScrollHost");
        _stickyScrollText = this.FindControl<TextBlock>("StickyScrollText");

        if (!_editorThemeSubscribed)
        {
            UiThemeApply.ThemeApplied += OnThemeAppliedRefreshEditorSelection;
            _editorThemeSubscribed = true;
        }

        EditorSelectionChrome.Apply(_editor);
        EditorTextChrome.Apply(_editor);
        EditorHelpers.ApplyEditorFontFromSettings(_editor, _vm.GetCascadeSettingsForExecutor().Fonts.Editor);

        EditorInlineHoverChrome.ApplyToolTipServiceTo(_editor);

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
        _editor.TextArea.TextView.VisualLinesChanged += OnEditorViewportChanged;
        _editor.TextArea.TextView.ScrollOffsetChanged += OnEditorViewportChanged;

        _vmHandler = (_, args) =>
        {
            if (args.PropertyName is nameof(MainWindowViewModel.EditorText)
                or nameof(MainWindowViewModel.CurrentFilePath))
            {
                SyncFromVmIfActive();
                UpdateMcpProvidersIfActive();
            }

            if (args.PropertyName == nameof(MainWindowViewModel.CurrentFilePath))
                UpdateStabilizedHudRegistration();

            if (args.PropertyName is nameof(MainWindowViewModel.BreakpointLinesInCurrentFile)
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
                UpdateStabilizedHudRegistration();
            }
        };
        _vm.Documents.PropertyChanged += _documentsHandler;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow mainWindow })
        {
            mainWindow.EnsureDockEditorTextMate(_editor, _docVm.Doc.FilePath);
            if (IsActive())
                mainWindow.AttachTextMateWhenEditorReady();
        }

        SyncFromVmIfActive();
        InstallVisualAdornersOnce();
        UpdateMcpProvidersIfActive();
        UpdateStickyScroll();

        _editorSurface = new AvaloniaEditSurfaceAdapter(_editor, _docVm.Doc.FilePath);
        _documentHudLayer.ConfigureDiagnostics(p => _vm!.WorkspaceDiagnostics.GetStripsForFile(p));
        UpdateStabilizedHudRegistration();
        if (_vm is not null)
            _vm.UpdateCodeNavigationMapCaretOffset(_editorSurface.CaretOffset);
    }

    private Action<EditorInputDelta> StabilizedHudAction =>
        _stabilizedHudAction ??= OnStabilizedHud;

    private void OnStabilizedHud(EditorInputDelta d) =>
        _vm?.SetStabilizedEditorHudContext(_documentHudLayer.BuildStabilizedContext(d));

    private void UpdateStabilizedHudRegistration()
    {
        if (_vm is null)
            return;
        if (IsActive())
            _vm.SetActiveEditorStabilizedHudHandler(StabilizedHudAction);
        else
            _vm.ClearActiveEditorStabilizedHudHandlerIfEquals(StabilizedHudAction);
    }

    private void Teardown()
    {
        if (_editorThemeSubscribed)
        {
            UiThemeApply.ThemeApplied -= OnThemeAppliedRefreshEditorSelection;
            _editorThemeSubscribed = false;
        }

        if (_editor is not null)
        {
            if (_inlineKeyHooked)
            {
                _editor.TextArea.KeyDown -= OnTextAreaKeyDown;
                _inlineKeyHooked = false;
            }

            if (_diagPointerHooked && _inlineHoverToolTip is not null)
            {
                _inlineHoverToolTip.StopDebounce();
                _editor.TextArea.PointerMoved -= _inlineHoverToolTip.OnPointerMoved;
                _editor.TextArea.PointerExited -= _inlineHoverToolTip.OnPointerExited;
                _diagPointerHooked = false;
            }

            _inlineHoverToolTip?.Dispose();
            _inlineHoverToolTip = null;

            _backgroundVisuals?.Dispose();
            _backgroundVisuals = null;

            _editor.Document.Changed -= OnEditorDocumentChanged;
            _editor.TextArea.Caret.PositionChanged -= OnEditorCaretOrSelectionChanged;
            _editor.TextArea.SelectionChanged -= OnEditorCaretOrSelectionChanged;
            _editor.TextArea.TextView.VisualLinesChanged -= OnEditorViewportChanged;
            _editor.TextArea.TextView.ScrollOffsetChanged -= OnEditorViewportChanged;

            if (_vm?.WorkspaceDiagnostics is not null && _diagHubHandler is not null)
                _vm.WorkspaceDiagnostics.DiagnosticsChanged -= _diagHubHandler;
            _diagHubHandler = null;
        }

        if (_vm is not null)
        {
            _vm.ClearActiveEditorStabilizedHudHandlerIfEquals(StabilizedHudAction);
            _vm.SetStabilizedEditorHudContext(null);
            if (_vmHandler is not null)
                _vm.PropertyChanged -= _vmHandler;
            if (_documentsHandler is not null)
                _vm.Documents.PropertyChanged -= _documentsHandler;
        }

        _documentHudLayer.ConfigureDiagnostics(null);
        _editorSurface = null;

        _editor = null;
        _vm = null;
        _docVm = null;
        _vmHandler = null;
        _documentsHandler = null;
        _renderersInstalled = false;
        _stickyScrollHost = null;
        _stickyScrollText = null;
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

        _backgroundVisuals = EditorInlineHudLayer.InstallDocumentBackgroundVisuals(
            _editor,
            () => _vm.GetAllBreakpointLinesForFile(_docVm.Doc.FilePath),
            () => _vm.GetDebugCurrentLineForFile(_docVm.Doc.FilePath),
            () => _vm.WorkspaceDiagnostics.GetStripsForFile(_docVm.Doc.FilePath),
            () => _vm.GetEditorInlineHintsForFile(_docVm.Doc.FilePath, _editor.Document.Text ?? ""),
            () => _vm.GetEditorDebugHintsForFile(_docVm.Doc.FilePath, _editor.Document.Text ?? ""));

        _diagHubHandler = () =>
        {
            if (_editor is not null)
                _editor.TextArea.TextView.Redraw();
        };
        _vm.WorkspaceDiagnostics.DiagnosticsChanged += _diagHubHandler;

        _inlineHoverToolTip = new EditorInlineHoverToolTipController(
            _editor,
            EditorInlineHoverChrome.PointerPositionDebounce,
            () => _docVm.Doc.FilePath,
            () => _vm.WorkspaceDiagnostics.GetStripsForFile(_docVm.Doc.FilePath),
            (path, text, line, col, ct) => _vm.GetEditorQuickInfoAsync(path, text, line, col, ct),
            (path, text, line, col) => _vm.CSharpLanguage.GetQuickInfo(path, text, line, col),
            static p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

        if (!_diagPointerHooked)
        {
            _editor.TextArea.PointerMoved += _inlineHoverToolTip.OnPointerMoved;
            _editor.TextArea.PointerExited += _inlineHoverToolTip.OnPointerExited;
            _diagPointerHooked = true;
        }

        if (!_inlineKeyHooked)
        {
            _editor.TextArea.KeyDown += OnTextAreaKeyDown;
            _inlineKeyHooked = true;
        }

        _renderersInstalled = true;
    }

    private void OnTextAreaKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        _inlineHoverToolTip?.DismissToolTip();
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
        UpdateStickyScroll();
        PostStabilizedEditorInputIfActive(EditorInputDeltaKind.DocumentText);
    }

    private void OnEditorCaretOrSelectionChanged(object? sender, EventArgs e)
    {
        UpdateStickyScroll();
        PostStabilizedEditorInputIfActive(EditorInputDeltaKind.CaretOrSelection);
    }

    private void OnEditorViewportChanged(object? sender, EventArgs e) => UpdateStickyScroll();

    private void UpdateStickyScroll()
    {
        if (_stickyScrollHost is null || _stickyScrollText is null || _editor is null || _docVm is null)
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
            return;
        }

        var topLine = GetTopVisibleLineNumber(_editor);
        var text = _editor.Document.Text ?? "";
        var sticky = BuildStickyScrollLabel(text, topLine);
        if (string.IsNullOrWhiteSpace(sticky))
        {
            ToolTip.SetTip(_stickyScrollHost, null);
            _stickyScrollHost.IsVisible = false;
            return;
        }

        _stickyScrollText.Text = sticky;
        ToolTip.SetTip(_stickyScrollHost, sticky);
        _stickyScrollHost.IsVisible = true;
    }

    /// <summary>Первая видимая сверху строка документа по прокрутке (не по каретке): иначе при скролле без двига каретки
    /// мы оставались бы на строке каретки и sticky скрывался из-за BuildStickyScrollLabel(topLine &lt;= 1).</summary>
    private static int GetTopVisibleLineNumber(TextEditor editor)
    {
        var textView = editor.TextArea.TextView;
        if (textView.Document is null)
            return Math.Max(1, editor.TextArea.Caret.Line);

        try
        {
            var line = textView.GetDocumentLineByVisualTop(textView.ScrollOffset.Y);
            return line.LineNumber;
        }
        catch
        {
            if (textView.VisualLinesValid)
            {
                var first = textView.VisualLines.FirstOrDefault();
                if (first?.FirstDocumentLine is not null)
                    return first.FirstDocumentLine.LineNumber;
            }

            return Math.Max(1, editor.TextArea.Caret.Line);
        }
    }

    private static string? BuildStickyScrollLabel(string sourceText, int topLineOneBased)
    {
        if (topLineOneBased <= 1 || string.IsNullOrWhiteSpace(sourceText))
            return null;

        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceText);
            var root = tree.GetRoot();
            var line = tree.GetText().Lines[Math.Max(0, Math.Min(topLineOneBased - 1, tree.GetText().Lines.Count - 1))];
            var token = root.FindToken(line.Start);
            if (token.RawKind == 0)
                return null;

            var parts = token.Parent?
                .AncestorsAndSelf()
                .Reverse()
                .Select(n => ToStickyPart(n, tree, topLineOneBased))
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (parts is null || parts.Count == 0)
                return null;

            return string.Join(" > ", parts);
        }
        catch
        {
            return null;
        }
    }

    private static string? ToStickyPart(SyntaxNode node, SyntaxTree tree, int topLineOneBased)
    {
        var startLine = RoslynLinePositionMapper.ToEditorLineNumber(tree.GetLineSpan(node.Span).StartLinePosition).Value;
        if (startLine >= topLineOneBased)
            return null;

        return node switch
        {
            BaseNamespaceDeclarationSyntax n => $"namespace {n.Name}",
            ClassDeclarationSyntax c => $"class {c.Identifier.Text}",
            StructDeclarationSyntax s => $"struct {s.Identifier.Text}",
            InterfaceDeclarationSyntax i => $"interface {i.Identifier.Text}",
            RecordDeclarationSyntax r => $"record {r.Identifier.Text}",
            EnumDeclarationSyntax e => $"enum {e.Identifier.Text}",
            DelegateDeclarationSyntax d => $"delegate {d.Identifier.Text}",
            MethodDeclarationSyntax m => $"{m.Identifier.Text}()",
            ConstructorDeclarationSyntax c => $"{c.Identifier.Text}()",
            PropertyDeclarationSyntax p => p.Identifier.Text,
            IndexerDeclarationSyntax => "this[]",
            LocalFunctionStatementSyntax f => $"{f.Identifier.Text}()",
            _ => null
        };
    }


    private void PostStabilizedEditorInputIfActive(EditorInputDeltaKind kind)
    {
        if (!IsActive() || _vm is null || _editorSurface is null)
            return;
        _editorSurface.GetSelection(out var selStart, out var selLen);
        var d = new EditorInputDelta(_docVm?.Doc.FilePath, _editorSurface.CaretOffset, selStart, selLen, kind);
        _vm.TryPostEditorStabilizedInput(d);
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

    private void OnThemeAppliedRefreshEditorSelection(object? sender, EventArgs e)
    {
        if (_editor is not null)
        {
            EditorSelectionChrome.Apply(_editor);
            EditorTextChrome.Apply(_editor);
        }
    }
}

internal static class EditorHelpers
{
    public static void ApplyEditorFontFromSettings(TextEditor editor, EditorFontsSettings fonts)
    {
        editor.FontSize = fonts.ResolveSizePt();
        editor.FontFamily = new FontFamily(fonts.ResolveFamily());
    }

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
