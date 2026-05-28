#nullable enable

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.Features.Cockpit;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Models.Shell;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>
/// Оверлей Cockpit Command Line для <c>primary=editor</c> (как <see cref="CommandPaletteView"/> / Ctrl+Q).
/// </summary>
public partial class CockpitCommandLineOverlayView : UserControl
{
    private INotifyPropertyChanged? _boundVm;
    private INotifyPropertyChanged? _boundChatPanel;
    private Window? _hostWindow;
    private IInputElement? _focusBeforeOpen;
    private bool _wasVisible;
    private bool _commandLineHooksAttached;

    public CockpitCommandLineOverlayView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
        DataContextChanged += OnDataContextChanged;
        AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        AttachHostWindow();
        BindVm();
        EnsureCommandLineHooks();
        UpdateOpenState();
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        DetachVm();
        DetachHostWindow();
        _focusBeforeOpen = null;
        _wasVisible = false;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        BindVm();
        UpdateOpenState();
    }

    private void BindVm()
    {
        if (ReferenceEquals(_boundVm, DataContext))
            return;

        DetachVm();

        _boundVm = DataContext as INotifyPropertyChanged;
        if (_boundVm is not null)
            _boundVm.PropertyChanged += OnVmPropertyChanged;

        _boundChatPanel = ViewModel?.ChatPanel;
        if (_boundChatPanel is not null)
            _boundChatPanel.PropertyChanged += OnChatPanelPropertyChanged;
    }

    private void DetachVm()
    {
        if (_boundVm is not null)
            _boundVm.PropertyChanged -= OnVmPropertyChanged;
        _boundVm = null;

        if (_boundChatPanel is not null)
            _boundChatPanel.PropertyChanged -= OnChatPanelPropertyChanged;
        _boundChatPanel = null;
    }

    private void EnsureCommandLineHooks()
    {
        if (_commandLineHooksAttached || CommandLineTextBox is null)
            return;

        CommandLineTextBox.PropertyChanged += OnCommandLineTextBoxPropertyChanged;
        _commandLineHooksAttached = true;
    }

    private void OnCommandLineTextBoxPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.TextProperty && e.Property.Name != "CaretIndex")
            return;

        SyncCommandLineDraftToViewModel();
    }

    private void SyncCommandLineDraftToViewModel()
    {
        var vm = ViewModel;
        if (vm is null || CommandLineTextBox is null || !IsVisible)
            return;

        var text = string.IsNullOrWhiteSpace(CommandLineTextBox.Text) ? "/" : CommandLineTextBox.Text;
        var caret = Math.Clamp(CommandLineTextBox.CaretIndex, 0, text.Length);
        vm.ChatPanel.CockpitCommandLineCaretIndex = caret;
        vm.ChatPanel.RefreshCockpitCommandLineAutocomplete(text, caret);
    }

    private void SyncCommandLineFromViewModel()
    {
        var vm = ViewModel;
        if (vm is null || CommandLineTextBox is null)
            return;

        var cp = vm.ChatPanel;
        var text = cp.CockpitCommandLineText;
        if (!string.Equals(CommandLineTextBox.Text ?? "", text, StringComparison.Ordinal))
            CommandLineTextBox.Text = text;
        CommandLineTextBox.CaretIndex = Math.Clamp(cp.CockpitCommandLineCaretIndex, 0, text.Length);
    }

    private void AttachHostWindow()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (ReferenceEquals(_hostWindow, window))
            return;

        DetachHostWindow();
        _hostWindow = window;

        if (_hostWindow is null)
            return;

        _hostWindow.Activated += OnHostWindowActivationChanged;
        _hostWindow.Deactivated += OnHostWindowActivationChanged;
    }

    private void DetachHostWindow()
    {
        if (_hostWindow is null)
            return;

        _hostWindow.Activated -= OnHostWindowActivationChanged;
        _hostWindow.Deactivated -= OnHostWindowActivationChanged;
        _hostWindow = null;
    }

    private void OnHostWindowActivationChanged(object? sender, EventArgs e) => UpdateOpenState();

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.PrimaryWorkSurface)
            or nameof(MainWindowViewModel.CommandPaletteHost))
            UpdateOpenState();
    }

    private void OnChatPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatPanelViewModel.IsCockpitCommandLineOpen)
            or nameof(ChatPanelViewModel.CockpitCommandLineText)
            or nameof(ChatPanelViewModel.CockpitCommandLineCaretIndex)
            or nameof(ChatPanelViewModel.CommandLineSlashPreview)
            or nameof(ChatPanelViewModel.IsChatSlashAutocompleteVisible)
            or nameof(ChatPanelViewModel.SelectedChatSlashSuggestionIndex)
            or nameof(ChatPanelViewModel.ChatSlashBreadcrumb))
        {
            if (e.PropertyName is nameof(ChatPanelViewModel.CockpitCommandLineText)
                or nameof(ChatPanelViewModel.CockpitCommandLineCaretIndex))
                SyncCommandLineFromViewModel();

            UpdateOpenState();
        }
    }

    private void UpdateOpenState()
    {
        var vm = ViewModel;
        var session = vm?.ChatPanel.CommandLineSession;
        var visible = vm?.PrimaryWorkSurface == PrimaryWorkSurfaceKind.Editor
                      && vm.ChatPanel.IsCockpitCommandLineOpen
                      && session?.ActiveHost == CockpitCommandLineHostKind.Editor
                      && vm.CommandPaletteHost == ResolveHost();
        IsVisible = visible;

        if (visible == _wasVisible)
            return;

        _wasVisible = visible;
        if (visible)
        {
            _focusBeforeOpen = _hostWindow?.FocusManager?.GetFocusedElement();
            Dispatcher.UIThread.Post(FocusCommandLineBox, DispatcherPriority.Input);
            return;
        }

        var prev = _focusBeforeOpen;
        _focusBeforeOpen = null;
        Dispatcher.UIThread.Post(() =>
        {
            if (prev is Control control)
                control.Focus();
        }, DispatcherPriority.Background);
    }

    private void FocusCommandLineBox()
    {
        if (!IsVisible)
            return;

        if (CommandLineTextBox is not { } box)
            return;

        InputMethod.SetIsInputMethodEnabled(box, false);
        SyncCommandLineFromViewModel();
        box.Focus();
        SyncCommandLineDraftToViewModel();
    }

    private CommandPaletteHost ResolveHost() =>
        _hostWindow switch
        {
            MfdHostWindow => CommandPaletteHost.MfdHost,
            PfdHostWindow => CommandPaletteHost.PfdHost,
            PmSplitHostWindow => CommandPaletteHost.PmSplitHost,
            _ => CommandPaletteHost.MainWindow,
        };

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null || !IsVisible)
            return;

        var kind = e.Key switch
        {
            Key.Escape => IntercomComposerKeyKind.Escape,
            Key.Enter => IntercomComposerKeyKind.Enter,
            Key.Up => IntercomComposerKeyKind.SlashUp,
            Key.Down => IntercomComposerKeyKind.SlashDown,
            Key.Tab => IntercomComposerKeyKind.Tab,
            _ => (IntercomComposerKeyKind?)null,
        };

        if (kind is null)
            return;

        var result = vm.ChatPanel.TryHandleIntercomComposerKey(kind.Value, e);
        if (!result.Handled)
            return;

        e.Handled = true;

        if (result.SyncCommandLineFromViewModel)
            SyncCommandLineFromViewModel();

        if (result.RunCockpitCommit)
            _ = vm.ChatPanel.TryCommitCockpitCommandLineAsync();
    }

    private void OnDimmerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.ChatPanel.CommandLineSession.Close();
        e.Handled = true;
    }

    private void OnPanelPressed(object? sender, PointerPressedEventArgs e)
    {
        FocusCommandLineBox();
        e.Handled = true;
    }
}
