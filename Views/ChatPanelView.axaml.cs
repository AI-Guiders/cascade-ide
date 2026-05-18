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
    }

    private void WireChatInput(TextBox box)
    {
        box.AddHandler(InputElement.KeyDownEvent, OnChatInputKeyDown, RoutingStrategies.Tunnel);
        box.TextChanged += OnChatInputTextChanged;
    }

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
                    if (vm.TryApplySelectedChatSlashSuggestion())
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
                    if (vm.TryApplySelectedChatSlashSuggestion())
                    {
                        e.Handled = true;
                        input.CaretIndex = vm.ChatInput.Length;
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
