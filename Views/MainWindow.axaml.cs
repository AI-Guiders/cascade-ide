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
using AvaloniaEdit.TextMate;
using CommunityToolkit.Mvvm.Input;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace CascadeIDE.Views;

public partial class MainWindow : Window
{
    private TextMate.Installation? _textMateInstallation;
    private bool _suppressEditorSync;
    private Services.CSharpLanguageService? _languageService;
    private Services.EditorIntelligence? _editorIntelligence;
    private MarkdownPreviewWindow? _previewWindow;
    private ViewModels.MarkdownPreviewWindowViewModel? _previewVm;
    private IDisposable? _highlightHideTimer;

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
            vm.RequestShowMarkdownPreviewWindow = ShowMarkdownPreviewWindow;
            vm.RequestShowMarkdownPreviewForEditor = ShowMarkdownPreviewForEditor;
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
            UpdateChatColumnWidth(vm.IsChatPanelExpanded);
            UpdateSolutionColumnWidth(vm.IsSolutionExplorerVisible);
            UpdateMarkdownPreviewColumn(vm.IsMarkdownFile);
            SetupChatInputKeyHandler();
            SetupTerminalKeyHandler();
            SetupEditorAndTextMate();
        }
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
        var editor = this.FindControl<TextEditor>("Editor");
        if (editor is null)
            return;

        var registryOptions = new RegistryOptions(ThemeName.LightPlus);
        _textMateInstallation = editor.InstallTextMate(registryOptions);

        editor.Document.Changed += OnEditorDocumentChanged;

        _languageService ??= new Services.CSharpLanguageService();
        _editorIntelligence ??= new Services.EditorIntelligence(editor, _languageService, () =>
        {
            if (DataContext is ViewModels.MainWindowViewModel vm)
                return (vm.CurrentFilePath, editor.Document.Text);
            return (null, editor.Document.Text);
        });
        _editorIntelligence.Attach();

        if (DataContext is ViewModels.MainWindowViewModel vmSetup)
        {
            vmSetup.SetEditorStateProvider(() => GetEditorState(editor, vmSetup.CurrentFilePath));
            vmSetup.SetApplyEdit((path, sl, sc, el, ec, newText) => ApplyEditInEditor(editor, vmSetup, path, sl, sc, el, ec, newText));
            vmSetup.SetFocusEditor(() => editor.Focus());
        }

        SyncFromViewModel();
    }

    private static Services.EditorStateDto GetEditorState(TextEditor editor, string? currentFilePath)
    {
        var doc = editor.Document;
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
        return new Services.EditorStateDto
        {
            FilePath = currentFilePath,
            CaretLine = line.LineNumber,
            CaretColumn = offset - line.Offset + 1,
            SelectionStart = selStart,
            SelectionLength = selLen,
            SelectionText = selectionText
        };
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
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.CurrentFilePath) or nameof(ViewModels.MainWindowViewModel.EditorText))
        {
            SyncFromViewModel();
            if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.CurrentFilePath))
                _languageService?.InvalidateCache();
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
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsChatPanelExpanded) && DataContext is ViewModels.MainWindowViewModel vmChat)
            UpdateChatColumnWidth(vmChat.IsChatPanelExpanded);
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.IsMarkdownFile) && DataContext is ViewModels.MainWindowViewModel vmMd)
            UpdateMarkdownPreviewColumn(vmMd.IsMarkdownFile);
        if (e.PropertyName is nameof(ViewModels.MainWindowViewModel.SelectedOllamaModel) && DataContext is ViewModels.MainWindowViewModel vm2
            && vm2.SelectedOllamaModel == ViewModels.MainWindowViewModel.InstallNewSentinel)
            _ = ShowInstallModelDialogAsync(vm2);
    }

    private void ApplyEditorSelection(int start, int length)
    {
        var editor = this.FindControl<TextEditor>("Editor");
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

    private void UpdateChatColumnWidth(bool isExpanded)
    {
        var grid = this.FindControl<Grid>("MainGrid");
        if (grid?.ColumnDefinitions.Count > 4)
            grid.ColumnDefinitions[4].Width = new GridLength(isExpanded ? 340 : 88);
    }

    private void UpdateMarkdownPreviewColumn(bool showPreview)
    {
        var grid = this.FindControl<Grid>("EditorContentGrid");
        if (grid?.ColumnDefinitions.Count > 1)
            grid.ColumnDefinitions[1].Width = showPreview ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
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
        var editor = this.FindControl<TextEditor>("Editor");
        if (editor is null || DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        _suppressEditorSync = true;
        try
        {
            editor.Document.Text = vm.EditorText ?? "";
            ApplyGrammarByFilePath(editor, vm.CurrentFilePath);
        }
        finally
        {
            _suppressEditorSync = false;
        }
    }

    private void ApplyGrammarByFilePath(TextEditor editor, string? filePath)
    {
        if (_textMateInstallation is null)
            return;

        var registryOptions = new RegistryOptions(ThemeName.LightPlus);
        var ext = string.IsNullOrEmpty(filePath) ? "" : Path.GetExtension(filePath).ToLowerInvariant();
        try
        {
            string scope = ext switch
            {
                ".cs" => registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".cs").Id),
                ".md" or ".markdown" => registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".md").Id),
                ".csproj" or ".xml" or ".axaml" or ".xaml" or ".config" or ".props" or ".targets" =>
                    registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".xml").Id),
                ".json" => registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".json").Id),
                _ => registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".txt").Id)
            };
            _textMateInstallation.SetGrammar(scope);
        }
        catch
        {
            // Неизвестное расширение — без подсветки
        }
    }

    private void OnEditorDocumentChanged(object? sender, EventArgs e)
    {
        if (_suppressEditorSync || DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        var editor = this.FindControl<TextEditor>("Editor");
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

}