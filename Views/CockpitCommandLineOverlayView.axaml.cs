#nullable enable

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Cockpit.Application;
using CascadeIDE.Models.Shell;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>
/// Оверлей Cockpit Command Line для <c>primary=editor</c> (как <see cref="CommandPaletteView"/> / Ctrl+Q).
/// </summary>
public partial class CockpitCommandLineOverlayView : UserControl
{
    private CockpitCommandLineOverlayViewModel? _overlayVm;
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

    private CockpitCommandLineOverlayViewModel? OverlayVm => DataContext as CockpitCommandLineOverlayViewModel;

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        AttachHostWindow();
        BindOverlayVm();
        EnsureCommandLineHooks();
        UpdateOpenState();
    }

    private void OnDetachedFromVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        DetachOverlayVm();
        DetachHostWindow();
        _focusBeforeOpen = null;
        _wasVisible = false;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        BindOverlayVm();
        UpdateOpenState();
    }

    private void BindOverlayVm()
    {
        if (ReferenceEquals(_overlayVm, OverlayVm))
            return;

        DetachOverlayVm();
        _overlayVm = OverlayVm;
        if (_overlayVm is not null)
            _overlayVm.PropertyChanged += OnOverlayVmPropertyChanged;
    }

    private void DetachOverlayVm()
    {
        if (_overlayVm is not null)
            _overlayVm.PropertyChanged -= OnOverlayVmPropertyChanged;
        _overlayVm = null;
    }

    private void OnOverlayVmPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        UpdateOpenState();

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
        var overlay = OverlayVm;
        if (overlay is null || CommandLineTextBox is null || !IsVisible)
            return;

        CockpitCommandLineDraftBridge.ApplyDraftFromView(
            overlay.ChatPanel,
            CommandLineTextBox.Text,
            CommandLineTextBox.CaretIndex);
    }

    private void SyncCommandLineFromViewModel()
    {
        var overlay = OverlayVm;
        if (overlay is null || CommandLineTextBox is null)
            return;

        CockpitCommandLineDraftBridge.ApplyDraftFromViewModel(
            overlay.ChatPanel,
            text => CommandLineTextBox.Text = text,
            caret => CommandLineTextBox.CaretIndex = caret);
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

    private void UpdateOpenState()
    {
        var overlay = OverlayVm;
        var visible = overlay?.IsOverlayVisible(ResolveHost()) == true;
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
        var overlay = OverlayVm;
        if (overlay is null || !IsVisible)
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

        var result = overlay.ChatPanel.TryHandleSlashComposerKey(kind.Value, e);
        if (!result.Handled)
            return;

        e.Handled = true;

        if (result.SyncCommandLineFromViewModel)
            SyncCommandLineFromViewModel();

        if (result.RunCockpitCommit)
            _ = overlay.ChatPanel.TryCommitCockpitCommandLineAsync();
    }

    private void OnDimmerPressed(object? sender, PointerPressedEventArgs e)
    {
        OverlayVm?.ChatPanel.CommandLineSession.Close();
        e.Handled = true;
    }

    private void OnPanelPressed(object? sender, PointerPressedEventArgs e)
    {
        FocusCommandLineBox();
        e.Handled = true;
    }
}
