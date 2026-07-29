using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.WorkspaceNavigation.Presentation;
using CascadeIDE.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.Views;

public partial class DockDocumentView : UserControl
{
    private MainWindowViewModel? _vm;
    private DockDocumentViewModel? _docVm;
    private PropertyChangedEventHandler? _vmHandler;
    private PropertyChangedEventHandler? _navigationMapHandler;
    private PropertyChangedEventHandler? _documentsHandler;

    private Border? _stickyScrollHost;
    private TextBlock? _stickyScrollText;

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

        _stickyScrollHost = this.FindControl<Border>("StickyScrollHost");
        _stickyScrollText = this.FindControl<TextBlock>("StickyScrollText");
        TrySetupMonacoForward();
    }

    private Action<EditorInputDelta> StabilizedHudAction =>
        _stabilizedHudAction ??= OnStabilizedHud;

    private void OnStabilizedHud(EditorInputDelta d) =>
        _vm?.SetStabilizedEditorHudContext(_documentHudLayer.BuildStabilizedContext(d));

    internal void UpdateStabilizedHudRegistration()
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
        TeardownMonacoForward();

        if (_vm is not null)
        {
            _vm.ClearActiveEditorStabilizedHudHandlerIfEquals(StabilizedHudAction);
            _vm.SetStabilizedEditorHudContext(null);
            if (_vmHandler is not null)
                _vm.PropertyChanged -= _vmHandler;
            if (_navigationMapHandler is not null)
            {
                _vm.NavigationMap.PropertyChanged -= _navigationMapHandler;
                _navigationMapHandler = null;
            }

            if (_documentsHandler is not null)
                _vm.Documents.PropertyChanged -= _documentsHandler;
        }

        _documentHudLayer.ConfigureDiagnostics(null);
        _editorSurface = null;

        _vm = null;
        _docVm = null;
        _vmHandler = null;
        _documentsHandler = null;
        _stickyScrollHost = null;
        _stickyScrollText = null;
    }

    internal bool IsActive()
    {
        if (_vm is null || _docVm is null)
            return false;

        return ReferenceEquals(_vm.Documents.DockActiveDocument, _docVm)
               || string.Equals(_vm.CurrentFilePath, _docVm.Doc.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    internal void PostStabilizedEditorInputIfActive(EditorInputDeltaKind kind)
    {
        if (!IsActive() || _vm is null || _editorSurface is null)
            return;
        _editorSurface.GetSelection(out var selStart, out var selLen);
        var d = new EditorInputDelta(_docVm?.Doc.FilePath, _editorSurface.CaretOffset, selStart, selLen, kind);
        _vm.TryPostEditorStabilizedInput(d);
    }

    internal static string? BuildStickyScrollLabel(string sourceText, int topLineOneBased)
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

    /// <summary>Reveal строк из карты намерений / MCP (ADR 0130).</summary>
    public bool TryRevealEditorRange(int startLine, int endLine, int? durationMs)
    {
        if (_docVm is null || _vm is null || _monacoHost is null)
            return false;

        // Omitted/non-positive duration still paints transient frame (not scroll-only).
        var ms = durationMs is > 0 ? durationMs : EditorRevealDuration.DefaultMs;
        _ = _monacoHost.PushAgentRevealAsync(startLine, endLine, persistent: false, ms);
        return true;
    }

    public void FocusMonacoEditor() => _monacoHost?.Focus();

    public Task GotoLineColumnAsync(int line, int column, bool select = true) =>
        _monacoHost is { IsReady: true } host
            ? host.PushRevealRangeAsync(line, line, column, select)
            : Task.CompletedTask;

    public Task SetSelectionAsync(int start, int length) =>
        _monacoHost is { IsReady: true } host
            ? host.PushSetSelectionAsync(start, length)
            : Task.CompletedTask;

    public Task SetEpochDimAsync(bool dimmed) =>
        _monacoHost is { IsReady: true } host
            ? host.PushEpochDimAsync(dimmed)
            : Task.CompletedTask;

    public Task RevealAgentRangeAsync(int startLine, int endLine, bool persistent, int? durationMs = null) =>
        _monacoHost is { IsReady: true } host
            ? host.PushAgentRevealAsync(startLine, endLine, persistent, durationMs: persistent ? null : (durationMs ?? 3000))
            : Task.CompletedTask;

    public void ClearAgentReveal() => _ = ClearAgentRevealAsync();

    internal Task PushMonacoDebugVisualsAsync() => PushMonacoDebugOverlayAsync();

    internal string GetEditorTextSnapshot()
    {
        if (_monacoHost is null)
            return _vm?.EditorText ?? _docVm?.Doc.Content ?? "";
        _monacoHost.Session.ReadSnapshot(out _, out var text, out _, out _, out _);
        return text;
    }
}
