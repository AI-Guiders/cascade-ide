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
        ChatInputBox.AddHandler(InputElement.KeyDownEvent, OnChatInputKeyDown, RoutingStrategies.Tunnel);
        ChatInputBox.TextChanged += OnChatInputTextChanged;
    }

    private void OnChatInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is ChatPanelViewModel vm)
            vm.RefreshChatSlashAutocomplete(ChatInputBox.Text);
    }

    private void OnChatInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ChatPanelViewModel vm)
            return;

        if (vm.IsChatSlashAutocompleteVisible)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    if (vm.TryApplySelectedChatSlashSuggestion())
                    {
                        e.Handled = true;
                        ChatInputBox.CaretIndex = vm.ChatInput.Length;
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
                    if (vm.TryApplySelectedChatSlashSuggestion())
                    {
                        e.Handled = true;
                        ChatInputBox.CaretIndex = vm.ChatInput.Length;
                        return;
                    }
                    break;
            }
        }

        if (!ChatSendKeyMatcher.Matches(e, vm.GetSendMessageKey()))
            return;

        if (vm.SendChatCommand is IRelayCommand relay && relay.CanExecute(null))
        {
            _ = vm.SendChatCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }
}
