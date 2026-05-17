using Avalonia.Controls;
using Avalonia.Input;
using CascadeIDE.Features.Chat;

namespace CascadeIDE.Views;

public partial class ChatPanelView : UserControl
{
    public ChatPanelView()
    {
        InitializeComponent();
        ChatInputBox.KeyDown += OnChatInputKeyDown;
    }

    private void OnChatInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ChatPanelViewModel vm || !vm.IsChatSlashAutocompleteVisible)
            return;

        switch (e.Key)
        {
            case Key.Tab:
                if (vm.TryApplySelectedChatSlashSuggestion())
                {
                    e.Handled = true;
                    ChatInputBox.CaretIndex = vm.ChatInput.Length;
                }
                break;
            case Key.Up:
                vm.MoveChatSlashSuggestionSelection(-1);
                e.Handled = true;
                break;
            case Key.Down:
                vm.MoveChatSlashSuggestionSelection(1);
                e.Handled = true;
                break;
            case Key.Escape:
                vm.DismissChatSlashAutocomplete();
                e.Handled = true;
                break;
        }
    }
}
