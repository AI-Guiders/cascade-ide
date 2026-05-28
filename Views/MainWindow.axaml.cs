using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using CascadeIDE.Cockpit.Surface;

namespace CascadeIDE.Views;

public partial class MainWindow : PointerTrackingWindow
{
    private RegistryOptions? _registryOptions;
    private readonly ConditionalWeakTable<TextEditor, TextMate.Installation> _textMateByEditor = new();
    private bool _suppressEditorSync;
    private Services.CSharpLanguageService? _languageService;
    private Services.EditorIntelligence? _editorIntelligence;
    private MarkdownPreviewWindow? _previewWindow;
    private ViewModels.MarkdownPreviewWindowViewModel? _previewVm;
    private TextEditor? _marginPointerEditor;
    private ViewModels.MainWindowViewModel? _boundMainVm;
    private PropertyChangedEventHandler? _navigationMapHandler;
    private bool _workspaceEventsAttached;
    private static readonly object HighlightLogLock = new();

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnMainWindowClosing;
        // Глобальные хоткеи: tunnel + KeyGestureChordMatching + KeyBindings (см. MainWindowHotkeyService).
        AddHandler(InputElement.KeyDownEvent, OnDebugShortcutKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        DataContextChanged += OnDataContextChanged;
        Loaded += OnMainWindowLoaded;
        Activated += OnMainWindowFocusChanged;
        Deactivated += OnMainWindowFocusChanged;
        // Хосты PFD/MFD/PM: после первого показа главного окна — иначе Show(child, owner) падает, пока owner ещё не visible (headless/тесты).
        Opened += OnMainWindowOpenedPresentationHosts;
    }

    private void OnMainWindowFocusChanged(object? sender, EventArgs e)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        vm.NotifyCideWindowFocusChanged(IsActive);
    }

    private void OnMainWindowOpenedPresentationHosts(object? sender, EventArgs e)
    {
        Opened -= OnMainWindowOpenedPresentationHosts;
        TryOpenPfdHostWindowOnStartup();
        TryOpenMfdHostWindowOnStartup();
        TryOpenPmSplitHostWindowOnStartup();
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.ShutdownEditorStabilizedInput();
            vm.ReleaseWorkspaceHealthChannel();
        }
    }

    private void OnMainWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            _boundMainVm = vm;
            if (vm.PresentationRequestsMainWindowMaximized)
                WindowState = WindowState.Maximized;
        }

        TryApplyHotkeys();
    }

    private void TryApplyHotkeys()
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
            Features.Shell.Application.MainWindowViewServices.ApplyHotkeys(this, vm);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            if (_boundMainVm is not null)
                _boundMainVm.GotoActiveEditorLineColumnRequested -= OnGotoEditorLineColumn;
            _boundMainVm = vm;
            vm.GotoActiveEditorLineColumnRequested += OnGotoEditorLineColumn;
            WireAgentVerifyEpochDim(vm);

            vm.LoadSendMessageKeyFromStorage();
            vm.LoadComposerNewLineKeyFromStorage();
            vm.PropertyChanged += OnViewModelPropertyChanged;
            _navigationMapHandler ??= OnNavigationMapPropertyChanged;
            vm.NavigationMap.PropertyChanged += _navigationMapHandler;
            vm.RequestOpenSolution = () => _ = ShowOpenSolutionDialogAsync();
            vm.RequestCreateNewSolution = () => _ = ShowCreateNewSolutionDialogAsync();
            vm.RequestOpenFolder = () => _ = ShowOpenFolderDialogAsync();
            vm.RequestOpenFile = () => _ = ShowOpenFileDialogAsync();
            vm.RequestClose = Close;
            vm.RequestShowAbout = ShowAbout;
            vm.RequestOpenSettings = ShowSettingsWindow;
            vm.RequestToggleMfdHostWindow = ToggleMfdHostWindow;
            vm.RequestTogglePfdHostWindow = TogglePfdHostWindow;
            vm.RequestTogglePmSplitHostWindow = TogglePmSplitHostWindow;
            vm.RequestOpenThemeFile = ShowOpenThemeFileDialogAsync;
            vm.RequestShowMarkdownPreviewWindow = ShowMarkdownPreviewWindow;
            vm.RequestShowMarkdownPreviewForEditor = ShowMarkdownPreviewForEditor;
            vm.RequestConfirmation = ShowConfirmationDialogAsync;
            vm.RequestPickDebugTarget = ShowPickDebugTargetAsync;
            vm.RequestAttachProcessId = ShowAttachProcessIdAsync;
            vm.RequestShowInfoAsync = ShowInfoDialogAsync;
            vm.RequestPickFeatureDocAsync = (title, docs) => FeatureDocsPickDialog.ShowAsync(this, title, docs);
            vm.RequestSaveMarkdownFile = ShowSaveExpandedMarkdownDialogAsync;
            vm.CaptureWindowForMcpAsync = (ws, rel, scope) => CaptureWindowForMcpCoreAsync(ws, rel, scope);
            vm.GetUiLayoutProvider = () => UiLayoutSnapshot.BuildJsonAllWindows(this);
            vm.GetColorsUnderCursorProvider = () => Features.Shell.Application.MainWindowViewServices.GetColorsUnderCursorJson(this);
            vm.GetControlAppearanceProvider = name =>
                Features.Shell.Application.MainWindowViewServices.GetControlAppearanceJson(this, name ?? "");
            vm.SetControlLayoutProvider = (name, json) =>
                Features.Shell.Application.MainWindowViewServices.ApplyControlLayout(this, name, json);
            vm.AddControlProvider = (parentName, controlType, content, controlName) => Services.UiControlAdd.AddControl(this, parentName, controlType, content, controlName);
            vm.SetControlTextProvider = (name, text) =>
            {
                var result = Services.UiControlSetText.SetText(this, name, text);
                if (name is "ChatInputBox" or "IntercomSkiaSurface")
                    vm.ChatPanel.ChatInput = text ?? "";
                return result;
            };
            vm.ClickControlProvider = (name) => Services.UiControlClick.Click(this, this, name);
            vm.SendKeysProvider = (name, keys) => Services.UiControlSendKeys.SendKeys(this, this, name, keys);
            vm.SetFocusProvider = (name) => Services.UiControlSetFocus.SetFocus(this, this, name);
            vm.HighlightControlProvider = ShowHighlightForControl;
            vm.SetPanelSizeProvider = (panel, width, height) => Services.UiPanelResize.Resize(this, panel, width, height);
            UpdateChatColumnWidth(vm);
            UpdateSolutionColumnWidth(vm.IsPfdColumnVisible);
            SetupTerminalKeyHandler();
            SetupEditorAndTextMate();
            TryApplyHotkeys();
            ApplyMainGridColumnDefinitions(vm);
            AttachSkiaHostRenderers();
            InvalidateSkiaHosts();
        }
        else
        {
            if (_boundMainVm is not null)
            {
                _boundMainVm.GotoActiveEditorLineColumnRequested -= OnGotoEditorLineColumn;
                _boundMainVm.PropertyChanged -= OnViewModelPropertyChanged;
                if (_navigationMapHandler is not null)
                    _boundMainVm.NavigationMap.PropertyChanged -= _navigationMapHandler;
                _boundMainVm.CaptureWindowForMcpAsync = null;
                _boundMainVm = null;
            }

            ClosePfdHostWindowIfOpen();
            CloseMfdHostWindowIfOpen();
            ClosePmSplitHostWindowIfOpen();
        }
    }

    private void OnNavigationMapPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsSkiaHostRelatedProperty(e.PropertyName))
            InvalidateSkiaHosts();
    }
}
