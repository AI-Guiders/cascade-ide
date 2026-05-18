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
        WireIntercomSurface(ForwardIntercomSurface);
        WireIntercomSurface(ClassicIntercomSurface);
    }

    private void WireIntercomSurface(SkiaChatSurfaceControl surface)
    {
        surface.SendRequested += (_, _) =>
        {
            if (DataContext is ChatPanelViewModel vm)
                TryExecuteSendChat(vm);
        };

        surface.ThinkingToggleRequested += (_, messageIndex) =>
        {
            if (DataContext is not ChatPanelViewModel vm)
                return;
            vm.SelectedMessageIndex = messageIndex;
            _ = vm.ToggleSelectedThinkingDetails();
        };

        surface.ComposerKeyDown += (_, e) =>
        {
            if (DataContext is not ChatPanelViewModel vm)
                return;

            switch (e.Kind)
            {
                case IntercomComposerKeyKind.Tab:
                    if (vm.TryCommitSelectedChatSlashSuggestion(out bool _))
                        surface.ComposerCaretIndex = vm.ChatInput.Length;
                    break;
                case IntercomComposerKeyKind.SlashUp:
                    vm.MoveChatSlashSuggestionSelection(-1);
                    break;
                case IntercomComposerKeyKind.SlashDown:
                    vm.MoveChatSlashSuggestionSelection(1);
                    break;
                case IntercomComposerKeyKind.Escape:
                    vm.DismissChatSlashAutocomplete();
                    break;
                case IntercomComposerKeyKind.Enter:
                    if (vm.IsChatSlashAutocompleteVisible)
                    {
                        if (vm.TryCommitSelectedChatSlashSuggestion(out var autoExecute))
                        {
                            surface.ComposerCaretIndex = vm.ChatInput.Length;
                            if (autoExecute)
                                TryExecuteSendChat(vm);
                        }
                    }
                    else if (e.KeyEvent is { } enterKey && ChatSendKeyMatcher.Matches(enterKey, vm.GetSendMessageKey()))
                    {
                        TryExecuteSendChat(vm);
                    }
                    else
                    {
                        var caret = surface.ComposerCaretIndex;
                        vm.ChatInput = vm.ChatInput.Insert(Math.Clamp(caret, 0, vm.ChatInput.Length), "\n");
                        surface.ComposerCaretIndex = caret + 1;
                    }
                    break;
                case IntercomComposerKeyKind.CommitSlashSuggestion:
                    if (vm.TryCommitSelectedChatSlashSuggestion(out var execute))
                    {
                        surface.ComposerCaretIndex = vm.ChatInput.Length;
                        if (execute)
                            TryExecuteSendChat(vm);
                    }
                    break;
            }
        };
    }

    private static void TryExecuteSendChat(ChatPanelViewModel vm)
    {
        if (vm.SendChatCommand is IAsyncRelayCommand async && async.CanExecute(null))
            _ = async.ExecuteAsync(null);
        else if (vm.SendChatCommand is IRelayCommand relay && relay.CanExecute(null))
            relay.Execute(null);
    }
}
