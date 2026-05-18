using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CascadeIDE.Features.Chat;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Views;

public partial class ChatPanelView : UserControl
{
    public ChatPanelView()
    {
        InitializeComponent();
        WireChatInput(ForwardChatInputBox);
        WireChatInput(ClassicChatInputBox);
        WireSlashSuggestionList(ForwardSlashSuggestionList);
        WireSlashSuggestionList(ClassicSlashSuggestionList);
    }

    private void WireChatInput(TextBox box)
    {
        box.AddHandler(InputElement.KeyDownEvent, OnChatInputKeyDown, RoutingStrategies.Tunnel);
        box.TextChanged += OnChatInputTextChanged;
    }

    private void WireSlashSuggestionList(ListBox list) =>
        list.PointerReleased += OnSlashSuggestionPointerReleased;

    private TextBox? SenderAsTextBox(object? sender) => sender as TextBox;

    private void OnChatInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is not ChatPanelViewModel vm)
            return;
        var text = SenderAsTextBox(sender)?.Text;
        vm.RefreshChatSlashAutocomplete(text);
    }

    private void OnChatInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ChatPanelViewModel vm)
            return;

        var input = SenderAsTextBox(sender);
        if (input is null)
            return;

        if (vm.IsChatSlashAutocompleteVisible)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    if (vm.TryCommitSelectedChatSlashSuggestion(out _))
                    {
                        e.Handled = true;
                        input.CaretIndex = vm.ChatInput.Length;
                    }
                    return;
                case Key.Up:
                    vm.MoveChatSlashSuggestionSelection(-1);
                    e.Handled = true;
                    return;
                case Key.Down:
                    vm.MoveChatSlashSuggestionSelection(1);
                    e.Handled = true;
                    return;
                case Key.Escape:
                    vm.DismissChatSlashAutocomplete();
                    e.Handled = true;
                    return;
                case Key.Enter when !e.KeyModifiers.HasFlag(KeyModifiers.Control)
                                    && !e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                    if (vm.TryCommitSelectedChatSlashSuggestion(out var autoExecute))
                    {
                        e.Handled = true;
                        if (autoExecute)
                            TryExecuteSendChat(vm);
                        else
                            input.CaretIndex = vm.ChatInput.Length;
                        return;
                    }
                    break;
            }
        }

        if (!ChatSendKeyMatcher.Matches(e, vm.GetSendMessageKey()))
            return;

        TryExecuteSendChat(vm);
        e.Handled = true;
    }

    private void OnSlashSuggestionPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;
        if (DataContext is not ChatPanelViewModel vm || !vm.IsChatSlashAutocompleteVisible)
            return;
        if (sender is not ListBox { SelectedIndex: >= 0 })
            return;

        if (!vm.TryCommitSelectedChatSlashSuggestion(out var autoExecute))
            return;

        e.Handled = true;
        if (autoExecute)
            TryExecuteSendChat(vm);
    }

    private static void TryExecuteSendChat(ChatPanelViewModel vm)
    {
        if (vm.SendChatCommand is IAsyncRelayCommand async && async.CanExecute(null))
            _ = async.ExecuteAsync(null);
        else if (vm.SendChatCommand is IRelayCommand relay && relay.CanExecute(null))
            relay.Execute(null);
    }
}
