using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using CommunityToolkit.Mvvm.Input;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace CascadeIDE.Views;

public partial class MainWindow : Window
{
    private RegistryOptions? _registryOptions;
    private TextMate.Installation? _textMateInstallation;
    private bool _suppressEditorSync;
    private Services.CSharpLanguageService? _languageService;
    private Services.EditorIntelligence? _editorIntelligence;
    private MarkdownPreviewWindow? _previewWindow;
    private ViewModels.MarkdownPreviewWindowViewModel? _previewVm;
    private IDisposable? _highlightHideTimer;
    private BreakpointLineRenderer? _breakpointRenderer;
    private DebugCurrentLineRenderer? _debugCurrentLineRenderer;
    private static readonly object HighlightLogLock = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.LoadSendMessageKeyFromStorage();
            vm.PropertyChanged += OnViewModelPropertyChanged;
            vm.RequestOpenSolution = () => _ = ShowOpenSolutionDialogAsync();
            vm.RequestClose = Close;
            vm.RequestShowAbout = ShowAbout;
            vm.RequestOpenSettings = ShowSettingsWindow;
            vm.RequestOpenThemeFile = ShowOpenThemeFileDialogAsync;
            vm.RequestShowMarkdownPreviewWindow = ShowMarkdownPreviewWindow;
            vm.RequestShowMarkdownPreviewForEditor = ShowMarkdownPreviewForEditor;
            vm.RequestConfirmation = ShowConfirmationDialogAsync;
            vm.GetUiLayoutProvider = () => Services.UiLayoutSnapshot.BuildJson(this);
            vm.GetColorsUnderCursorProvider = () => Services.UiColorsUnderCursor.GetJson(this);
            vm.GetControlAppearanceProvider = (name) => Services.UiControlAppearance.GetJson(this, name);
            vm.SetControlLayoutProvider = (name, json) => Services.UiControlLayoutApply.Apply(this, name, json);
            vm.AddControlProvider = (parentName, controlType, content, controlName) => Services.UiControlAdd.AddControl(this, parentName, controlType, content, controlName);
            vm.SetControlTextProvider = (name, text) =>
            {
                var result = Services.UiControlSetText.SetText(this, name, text);
                if (name == "ChatInputBox")
                    vm.ChatInput = text ?? "";
                return result;
            };
            vm.ClickControlProvider = (name) => Services.UiControlClick.Click(this, this, name);
            vm.SendKeysProvider = (name, keys) => Services.UiControlSendKeys.SendKeys(this, this, name, keys);
            vm.SetFocusProvider = (name) => Services.UiControlSetFocus.SetFocus(this, this, name);
            vm.HighlightControlProvider = ShowHighlightForControl;
            vm.SetPanelSizeProvider = (panel, width, height) => Services.UiPanelResize.Resize(this, panel, width, height);
            UpdateChatColumnWidth(vm);
            UpdateSolutionColumnWidth(vm.IsSolutionExplorerVisible);
            UpdateMarkdownPreviewColumn(vm.IsMarkdownFile);
            SetupChatInputKeyHandler();
            SetupTerminalKeyHandler();
            SetupEditorAndTextMate();
        }
    }

    private async Task<string?> ShowOpenThemeFileDialogAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть файл темы",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });
        if (files.Count == 0)
            return null;
        return files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
    }

    private async Task ShowOpenSolutionDialogAsync()
    {
        var storageProvider = StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть решение",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Решение") { Patterns = ["*.slnx", "*.sln"] }
            ]
        });
        if (files.Count > 0 && DataContext is ViewModels.MainWindowViewModel vm)
        {
            var path = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
            vm.LoadSolution(path);
        }
    }

    private Task<string> ShowConfirmationDialogAsync(string message, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration ctr = default;
        if (cancellationToken.CanBeCanceled)
        {
            ctr = cancellationToken.Register(() => tcs.TrySetResult(Services.ConfirmationResponses.Cancel));
        }

        Dispatcher.UIThread.Post(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetResult(Services.ConfirmationResponses.Cancel);
                return;
            }

            try
            {
                var result = await ShowConfirmationDialogOnUiThreadAsync(message).ConfigureAwait(true);
                tcs.TrySetResult(result);
            }
            catch
            {
                tcs.TrySetResult(Services.ConfirmationResponses.Cancel);
            }
            finally
            {
                ctr.Dispose();
            }
        });

        return tcs.Task;
    }

    private async Task<string> ShowConfirmationDialogOnUiThreadAsync(string message)
    {
        var dialog = new Window
        {
            Title = "Подтверждение",
            Width = 460,
            Height = 190,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var text = string.IsNullOrWhiteSpace(message) ? "Подтвердить действие?" : message;
        var result = Services.ConfirmationResponses.Cancel;

        var okButton = new Button { Content = "OK", MinWidth = 90 };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 90 };

        okButton.Click += (_, _) =>
        {
            result = Services.ConfirmationResponses.Ok;
            dialog.Close();
        };
        cancelButton.Click += (_, _) =>
        {
            result = Services.ConfirmationResponses.Cancel;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 14,
            Children =
            {
                new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { okButton, cancelButton }
                }
            }
        };

        await dialog.ShowDialog(this);

        return result;
    }

    private async void ShowAbout()
    {
        var w = new Window { Title = "О программе", Width = 400, Height = 180 };
        var okCommand = new RelayCommand(() => w.Close());
        w.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "CascadeIDE", FontWeight = FontWeight.SemiBold, FontSize = 16 },
                new TextBlock
                {
                    Text = "Тонкий клиент для управления агентом (MCP). Файл, вид, терминал, обозреватель решения.",
                    TextWrapping = TextWrapping.Wrap
                },
                new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right, Command = okCommand }
            }
        };
        await w.ShowDialog(this);
    }

    private void ShowSettingsWindow()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        var w = new SettingsWindow { DataContext = vm };
        w.Show(this);
    }

    private void UpdateSolutionColumnWidth(bool visible)
    {
        var grid = this.FindControl<Grid>("MainGrid");
        if (grid?.ColumnDefinitions.Count > 0 != true)
            return;
        grid.ColumnDefinitions[0] = new ColumnDefinition(visible ? new GridLength(220, GridUnitType.Pixel) : new GridLength(0, GridUnitType.Pixel));
    }

    private void SetupEditorAndTextMate()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vmSetup)
            return;

        // Use a dark TextMate theme to keep syntax readable in Focus/Balanced/Power dark palettes.
        _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        _languageService ??= new Services.CSharpLanguageService();

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

        // Initial attachment (if a dock editor already exists).
        TryAttachTextMateAndRenderers();
        SyncFromViewModel();
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
            _textMateInstallation = editor.InstallTextMate(_registryOptions!);
            LogHighlight($"InstallTextMate: ok, file='{vmSetup.CurrentFilePath ?? "<null>"}'.");
        }
        catch (Exception ex)
        {
            LogHighlight($"InstallTextMate: FAILED: {ex}");
            throw;
        }
        ApplyGrammarByFilePath(editor, vmSetup.CurrentFilePath);

        _editorIntelligence = new Services.EditorIntelligence(editor, _languageService!, () =>
            (vmSetup.CurrentFilePath, editor.Document.Text));
        _editorIntelligence.Attach();

        _breakpointRenderer = new BreakpointLineRenderer(() => vmSetup.AllBreakpointLinesInCurrentFile);
        editor.TextArea.TextView.BackgroundRenderers.Add(_breakpointRenderer);

        _debugCurrentLineRenderer = new DebugCurrentLineRenderer(() => vmSetup.DebugCurrentLineInCurrentFile);
        editor.TextArea.TextView.BackgroundRenderers.Add(_debugCurrentLineRenderer);

        editor.TextArea.PointerPressed -= OnDockEditorPointerPressed;
        editor.TextArea.PointerPressed += OnDockEditorPointerPressed;

        void OnDockEditorPointerPressed(object? s, PointerPressedEventArgs e)
        {
            // Use the editor instance that owns this pointer event.
            OnEditorMarginPointerPressed(e, editor, vmSetup);
        }
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
        int start = LineColumnToOffset(text, startLine, startColumn);
        int end = LineColumnToOffset(text, endLine, endColumn);
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
        int start = LineColumnToOffset(text, startLine, startColumn);
        int end = LineColumnToOffset(text, endLine, endColumn);
        if (start < 0 || end < 0)
            return;
        editor.Document.Replace(start, end - start, newText);
    }

    private static int LineColumnToOffset(string text, int line, int column)
    {
        if (line < 1 || column < 1) return -1;
        var lines = text.Split('\n');
        if (line > lines.Length) return -1;
        int offset = 0;
        for (int i = 0; i < line - 1; i++)
            offset += lines[i].Length + 1;
        int lineLen = lines[line - 1].Length;
        int col = Math.Min(column, lineLen + 1);
        return offset + (col - 1);
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
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.SolutionPath))
            _languageService?.InvalidateCache();
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsSolutionExplorerVisible) && DataContext is ViewModels.MainWindowViewModel vmSol)
            UpdateSolutionColumnWidth(vmSol.IsSolutionExplorerVisible);
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsChatPanelExpanded) or nameof(ViewModels.MainWindowViewModel.UiMode)
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

    private async Task ShowInstallModelDialogAsync(ViewModels.MainWindowViewModel mainVm)
    {
        mainVm.SelectedOllamaModel = mainVm.LastSelectedRealModel ?? mainVm.OllamaModels.FirstOrDefault();
        var dialog = new InstallModelDialog();
        dialog.DataContext = new ViewModels.InstallModelDialogViewModel(
            new Services.OllamaService(),
            () => dialog.Close());
        dialog.Closed += (_, _) => _ = mainVm.RefreshOllamaAsync();
        await dialog.ShowDialog(this);
    }

    private void SetupTerminalKeyHandler()
    {
        var box = this.FindControl<TextBox>("TerminalInputBox");
        if (box is null) return;
        box.KeyDown += (_, ev) =>
        {
            if (ev.Key != Key.Enter || DataContext is not ViewModels.MainWindowViewModel vm) return;
            var cmd = vm.RunTerminalCommandCommand;
            if (cmd.CanExecute(null))
            {
                cmd.Execute(null);
                ev.Handled = true;
            }
        };
    }

    private void SetupChatInputKeyHandler()
    {
        var box = this.FindControl<Avalonia.Controls.TextBox>("ChatInputBox");
        if (box is null) return;
        // Туннель: перехватываем Enter до того, как TextBox (AcceptsReturn=true) обработает его и вставит перевод строки.
        box.AddHandler(InputElement.KeyDownEvent, OnChatInputKeyDown, RoutingStrategies.Tunnel);

        void OnChatInputKeyDown(object? s, KeyEventArgs e)
        {
            if (DataContext is not ViewModels.MainWindowViewModel vm) return;
            var key = vm.SendMessageKey;
            var isEnter = e.Key == Key.Enter;
            var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            var match = key switch
            {
                "Enter" => isEnter && !ctrl && !shift,
                "Ctrl+Enter" => isEnter && ctrl && !shift,
                "Shift+Enter" => isEnter && !ctrl && shift,
                _ => false
            };
            if (match && (vm.SendChatCommand as IRelayCommand)?.CanExecute(null) == true)
            {
                _ = vm.SendChatCommand.ExecuteAsync(null);
                e.Handled = true;
            }
        }
    }

    private void UpdateChatColumnWidth(ViewModels.MainWindowViewModel vm)
    {
        var grid = this.FindControl<Grid>("MainGrid");
        if (grid?.ColumnDefinitions.Count > 4)
            grid.ColumnDefinitions[4].Width = new GridLength(vm.IsChatPanelExpanded ? (vm.IsPowerMode ? 420 : 340) : 88);
    }

    private void UpdateMarkdownPreviewColumn(bool showPreview)
    {
        var grid = this.FindControl<Grid>("EditorContentGrid");
        if (grid?.ColumnDefinitions.Count > 1)
            grid.ColumnDefinitions[1].Width = showPreview ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
    }

    /// <summary>Принудительно обновить контент панели превью справа от редактора (Markdown.Avalonia иногда не обновляет привязку при смене EditorText).</summary>
    private void UpdateInlineMarkdownPreview()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm || !vm.IsMarkdownFile)
            return;
        var viewer = this.FindControl<Markdown.Avalonia.MarkdownScrollViewer>("InlineMarkdownPreview");
        if (viewer is not null)
            viewer.Markdown = vm.EditorText ?? "";
    }

    private void EnsurePreviewWindow()
    {
        if (_previewWindow is not null)
            return;
        _previewVm = new ViewModels.MarkdownPreviewWindowViewModel();
        _previewWindow = new MarkdownPreviewWindow { DataContext = _previewVm };
        _previewWindow.Closed += (_, _) =>
        {
            _previewWindow = null;
            _previewVm?.DetachFromEditor();
        };
    }

    private void ShowMarkdownPreviewWindow(string title, string content)
    {
        EnsurePreviewWindow();
        _previewVm!.SetContent(title, content);
        _previewWindow!.Show(this);
        _previewWindow.Activate();
    }

    private void ShowMarkdownPreviewForEditor()
    {
        if (DataContext is not ViewModels.MainWindowViewModel mainVm)
            return;
        EnsurePreviewWindow();
        _previewVm!.AttachToEditor(mainVm);
        _previewWindow!.Show(this);
        _previewWindow.Activate();
    }

    private void SyncFromViewModel()
    {
        var editor = TryGetActiveDockEditor();
        if (editor is null || DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        _suppressEditorSync = true;
        try
        {
            var desired = vm.EditorText ?? "";
            if (!string.Equals(editor.Document.Text, desired, StringComparison.Ordinal))
                editor.Document.Text = desired;
            ApplyGrammarByFilePath(editor, vm.CurrentFilePath);
        }
        finally
        {
            _suppressEditorSync = false;
        }
    }

    private void ApplyGrammarByFilePath(TextEditor editor, string? filePath)
    {
        if (_textMateInstallation is null || _registryOptions is null)
        {
            LogHighlight("ApplyGrammarByFilePath: skipped (_textMateInstallation/_registryOptions is null).");
            return;
        }

        var ext = string.IsNullOrEmpty(filePath) ? "" : Path.GetExtension(filePath).ToLowerInvariant();
        if (!Services.EditorLanguageSupport.ExtensionToGrammarExtension.TryGetValue(ext, out var grammarExt))
        {
            LogHighlight($"ApplyGrammarByFilePath: no grammar mapping for ext='{ext}' file='{filePath ?? "<null>"}'.");
            return;
        }

        try
        {
            var lang = _registryOptions.GetLanguageByExtension(grammarExt);
            var scope = _registryOptions.GetScopeByLanguageId(lang.Id);
            _textMateInstallation.SetGrammar(scope);
            LogHighlight($"ApplyGrammarByFilePath: OK file='{filePath ?? "<null>"}' ext='{ext}' grammarExt='{grammarExt}' langId='{lang.Id}' scope='{scope}'.");
        }
        catch (Exception ex)
        {
            // грамматика не в бандле — не меняем подсветку
            LogHighlight($"ApplyGrammarByFilePath: FAILED file='{filePath ?? "<null>"}' ext='{ext}' grammarExt='{grammarExt}': {ex}");
        }
    }

    private static void LogHighlight(string message)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var logDir = Path.Combine(baseDir, ".cascade-ide");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "editor-highlight-log.txt");
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] {message}{Environment.NewLine}";
            lock (HighlightLogLock)
            {
                File.AppendAllText(logPath, line);
            }
        }
        catch
        {
            // Never crash UI because of debug logging.
        }
    }

    private void OnEditorDocumentChanged(object? sender, EventArgs e)
    {
        if (_suppressEditorSync || DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        var editor = sender as TextEditor ?? TryGetActiveDockEditor();
        if (editor is null)
            return;

        vm.EditorText = editor.Document.Text;
    }

    /// <summary>Подсветить эффективный контрол (по имени или под курсором). Оверлей скрывается через 3 с.</summary>
    private string ShowHighlightForControl(string? name)
    {
        Control? control;
        if (!string.IsNullOrWhiteSpace(name))
        {
            control = Services.UiControlAppearance.FindControlByName(this, name.Trim());
            if (control is null)
                return $"Control not found: {name}.";
        }
        else
        {
            var over = (this as IInputRoot)?.PointerOverElement;
            control = over as Control ?? FindAncestorControl(over as Visual);
            if (control is null)
                return "No control under cursor. Specify name from ide_get_ui_layout.";
        }

        var root = this as Visual;
        if (root is null)
            return "No visual root.";
        var topLeft = control.TranslatePoint(new Point(0, 0), root);
        if (topLeft is not { } pt)
            return "Could not get control position.";
        var w = control.Bounds.Width;
        var h = control.Bounds.Height;

        var overlay = this.FindControl<Border>("AgentHighlightOverlay");
        if (overlay is null)
            return "Highlight overlay not found.";
        Canvas.SetLeft(overlay, pt.X);
        Canvas.SetTop(overlay, pt.Y);
        overlay.Width = w;
        overlay.Height = h;
        overlay.IsVisible = true;

        _highlightHideTimer?.Dispose();
        _highlightHideTimer = DispatcherTimer.RunOnce(() =>
        {
            overlay.IsVisible = false;
            _highlightHideTimer = null;
        }, TimeSpan.FromSeconds(3));

        return "OK";
    }

    private static Control? FindAncestorControl(Visual? visual)
    {
        for (var v = visual?.GetVisualParent(); v is not null; v = v.GetVisualParent())
            if (v is Control c)
                return c;
        return null;
    }

    private async void OnDocumentTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ViewModels.OpenDocumentViewModel doc)
            return;
        if (!e.GetCurrentPoint(button).Properties.IsLeftButtonPressed)
            return;

        var data = new DataObject();
        data.Set(DataFormats.Text, doc.FilePath);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
    }

    private void OnGroupTabsDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Text) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnGroup1TabsDrop(object? sender, DragEventArgs e) => MoveDroppedDocumentToGroup(e, 1);
    private void OnGroup2TabsDrop(object? sender, DragEventArgs e) => MoveDroppedDocumentToGroup(e, 2);
    private void OnGroup3TabsDrop(object? sender, DragEventArgs e) => MoveDroppedDocumentToGroup(e, 3);

    private void MoveDroppedDocumentToGroup(DragEventArgs e, int group)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        var path = e.Data.GetText();
        if (string.IsNullOrWhiteSpace(path))
            return;

        switch (group)
        {
            case 2:
                if (vm.MoveDocumentToGroup2Command.CanExecute(path))
                    vm.MoveDocumentToGroup2Command.Execute(path);
                break;
            case 3:
                if (vm.MoveDocumentToGroup3Command.CanExecute(path))
                    vm.MoveDocumentToGroup3Command.Execute(path);
                break;
            default:
                if (vm.MoveDocumentToGroup1Command.CanExecute(path))
                    vm.MoveDocumentToGroup1Command.Execute(path);
                break;
        }
    }
}

internal sealed class BreakpointLineRenderer(Func<IReadOnlyList<int>> getBreakpointLines) : IBackgroundRenderer
{
    private const double SymbolRadius = 5;
    private static readonly SolidColorBrush s_backBrush = new(Color.FromArgb(40, 200, 80, 80));
    private static readonly SolidColorBrush s_symbolBrush = new(Color.FromRgb(200, 80, 80));
    private static readonly Pen s_symbolPen = new(new SolidColorBrush(Color.FromRgb(160, 60, 60)), 1);

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var document = textView.Document;
        if (document is null) return;
        var lines = getBreakpointLines();
        if (lines.Count == 0) return;
        foreach (var lineNumber in lines)
        {
            if (lineNumber < 1 || lineNumber > document.LineCount) continue;
            var line = document.GetLineByNumber(lineNumber);
            var first = true;
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
            {
                drawingContext.DrawRectangle(s_backBrush, null, rect);
                if (first)
                {
                    var centerX = rect.Left + SymbolRadius + 2;
                    var centerY = rect.Top + rect.Height / 2;
                    drawingContext.DrawEllipse(s_symbolBrush, s_symbolPen, new Rect(centerX - SymbolRadius, centerY - SymbolRadius, SymbolRadius * 2, SymbolRadius * 2));
                    first = false;
                }
            }
        }
    }
}

internal sealed class DebugCurrentLineRenderer(Func<int> getCurrentLine) : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var lineNumber = getCurrentLine();
        if (lineNumber < 1) return;
        var document = textView.Document;
        if (document is null || lineNumber > document.LineCount) return;
        var line = document.GetLineByNumber(lineNumber);
        var brush = new SolidColorBrush(Color.FromArgb(60, 255, 200, 80));
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
            drawingContext.DrawRectangle(brush, null, rect);
    }
}