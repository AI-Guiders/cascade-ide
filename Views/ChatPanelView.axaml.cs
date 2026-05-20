using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

        surface.AttachmentRevealRequested += (_, e) =>
        {
            if (DataContext is not ChatPanelViewModel vm)
                return;
            _ = vm.RevealAttachmentFromFeedAsync(e.Anchor, e.Select, messageIndex: e.MessageIndex);
        };

        surface.MessageSelectContextRequested += (_, messageIndex) =>
        {
            if (DataContext is not ChatPanelViewModel vm)
                return;
            showMessageSelectContextMenu(surface, vm, messageIndex);
        };

        surface.ComposerKeyDown += (_, e) =>
        {
            if (DataContext is not ChatPanelViewModel vm)
                return;

            switch (e.Kind)
            {
                case IntercomComposerKeyKind.Tab:
                    if (vm.TryCommitSelectedComposerSuggestion(out bool _))
                        surface.ComposerCaretIndex = vm.ChatComposerCaretIndex;
                    break;
                case IntercomComposerKeyKind.SlashUp:
                    vm.MoveComposerAutocompleteSelection(-1);
                    break;
                case IntercomComposerKeyKind.SlashDown:
                    vm.MoveComposerAutocompleteSelection(1);
                    break;
                case IntercomComposerKeyKind.Escape:
                    vm.DismissChatSlashAutocomplete();
                    vm.DismissChatBracketAutocomplete();
                    break;
                case IntercomComposerKeyKind.Enter:
                    if (vm.IsComposerAutocompleteVisible
                        && vm.TryCommitSelectedComposerSuggestion(out var autoExecute))
                    {
                        surface.ComposerCaretIndex = vm.ChatComposerCaretIndex;
                        if (autoExecute)
                            TryExecuteSendChat(vm);
                    }
                    else if (e.KeyEvent is { } enterKey && ChatSendKeyMatcher.Matches(enterKey, vm.GetSendMessageKey()))
                    {
                        TryExecuteSendChat(vm);
                    }
                    else
                    {
                        var text = surface.ComposerText ?? vm.ChatInput;
                        var caret = Math.Clamp(surface.ComposerCaretIndex, 0, text.Length);
                        var next = text.Insert(caret, "\n");
                        vm.ChatInput = next;
                        surface.ComposerCaretIndex = caret + 1;
                        vm.ChatComposerCaretIndex = caret + 1;
                    }
                    break;
                case IntercomComposerKeyKind.CommitSlashSuggestion:
                    if (vm.TryCommitSelectedComposerSuggestion(out var execute))
                    {
                        surface.ComposerCaretIndex = vm.ChatComposerCaretIndex;
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

    private static void showMessageSelectContextMenu(
        SkiaChatSurfaceControl surface,
        ChatPanelViewModel vm,
        int messageIndex)
    {
        var label = vm.TryGetFeedOrdinalForMessageIndex(messageIndex, out var ordinal)
            ? $"Выбрать сообщение #{ordinal}"
            : "Выбрать сообщение";

        var menu = new ContextMenu();
        var item = new MenuItem { Header = label };
        item.Click += (_, _) =>
        {
            if (ordinal > 0)
            {
                _ = vm.SelectMessageByOrdinalInDetailLane(ordinal);
                surface.SelectedMessageIndex = vm.SelectedMessageIndex;
                return;
            }

            _ = vm.SelectMessageByIndex(messageIndex);
            surface.SelectedMessageIndex = vm.SelectedMessageIndex;
        };
        menu.Items.Add(item);
        menu.Open(surface);
    }
}
