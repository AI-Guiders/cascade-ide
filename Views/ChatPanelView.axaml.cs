using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Views.Chat;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Views;

public partial class ChatPanelView : UserControl
{
    private ChatPanelViewModel? _subscribedVm;

    public ChatPanelView()
    {
        InitializeComponent();
        WireIntercomSurface(IntercomSkiaSurface);
        IntercomSkiaSurface.ComposerDraftChanged += OnIntercomComposerDraftChanged;
        IntercomSkiaSurface.NavigatorSearchDraftChanged += OnIntercomNavigatorSearchDraftChanged;
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) =>
        TryApplyPanelFonts();

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedVm is not null)
        {
            _subscribedVm.IntercomPanelFontsChanged -= OnIntercomPanelFontsChanged;
            _subscribedVm.ComposerPopupSuggestions.CollectionChanged -= OnComposerPopupSuggestionsChanged;
        }

        _subscribedVm = DataContext as ChatPanelViewModel;
        if (_subscribedVm is not null)
        {
            _subscribedVm.IntercomPanelFontsChanged += OnIntercomPanelFontsChanged;
            _subscribedVm.ComposerPopupSuggestions.CollectionChanged += OnComposerPopupSuggestionsChanged;
        }

        TryApplyPanelFonts();
    }

    private void OnIntercomComposerDraftChanged(object? sender, EventArgs e)
    {
        if (DataContext is not ChatPanelViewModel vm)
            return;

        var text = IntercomSkiaSurface.ComposerText ?? "";
        var caret = Math.Clamp(IntercomSkiaSurface.ComposerCaretIndex, 0, text.Length);
        if (!string.Equals(vm.ChatInput, text, StringComparison.Ordinal))
            vm.ChatInput = text;
        if (vm.ChatComposerCaretIndex != caret)
            vm.ChatComposerCaretIndex = caret;
        vm.RefreshComposerAutocomplete(text);
    }

    private void OnIntercomNavigatorSearchDraftChanged(object? sender, EventArgs e)
    {
        if (DataContext is not ChatPanelViewModel vm)
            return;

        var query = IntercomSkiaSurface.TopicNavigatorSearchQuery ?? "";
        if (!string.Equals(vm.TopicNavigatorSearchQuery, query, StringComparison.Ordinal))
            vm.TopicNavigatorSearchQuery = query;
    }

    private void OnComposerPopupSuggestionsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        IntercomSkiaSurface.InvalidateVisual();

    private void OnIntercomPanelFontsChanged(object? sender, IntercomFontsSettings fonts) =>
        ChatPanelTypographyApplier.Apply(this, fonts);

    private void TryApplyPanelFonts()
    {
        if (DataContext is not ChatPanelViewModel vm)
            return;

        var fonts = vm.IntercomFonts;
        ChatPanelTypographyApplier.Apply(this, fonts);
        Dispatcher.UIThread.Post(
            () => ChatPanelTypographyApplier.Apply(this, fonts),
            DispatcherPriority.Loaded);
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

        surface.TopicCreateRequested += (_, _) =>
        {
            if (DataContext is not ChatPanelViewModel vm)
                return;
            var n = vm.ChatSurfaceSnapshot.Layout.Overview.Count + 1;
            _ = vm.CreateTopicWithTitle($"Тема {n}");
        };

        surface.TopicNavigatorToggleRequested += (_, _) =>
        {
            if (DataContext is ChatPanelViewModel vm)
                vm.ToggleIntercomTopicNavigator();
        };

        surface.TopicRenameRequested += (_, e) =>
            _ = HandleTopicRenameRequestAsync(surface, e);

        surface.ComposerKeyDown += (_, e) =>
        {
            if (DataContext is not ChatPanelViewModel vm)
                return;

            switch (e.Kind)
            {
                case IntercomComposerKeyKind.Tab:
                    if (vm.TryCommitSelectedComposerSuggestion(out var tabAutoExecute))
                    {
                        if (vm.IsCockpitCommandLineOpen)
                        {
                            surface.CommandLineText = vm.CockpitCommandLineText;
                            surface.CommandLineCaretIndex = vm.CockpitCommandLineCaretIndex;
                            if (tabAutoExecute)
                                _ = vm.TryCommitCockpitCommandLineAsync();
                        }
                        else
                        {
                            surface.ComposerCaretIndex = vm.ChatComposerCaretIndex;
                        }
                    }

                    break;
                case IntercomComposerKeyKind.SlashUp:
                    vm.MoveComposerAutocompleteSelection(-1);
                    break;
                case IntercomComposerKeyKind.SlashDown:
                    vm.MoveComposerAutocompleteSelection(1);
                    break;
                case IntercomComposerKeyKind.Escape:
                    if (vm.IsCockpitCommandLineOpen)
                    {
                        vm.CloseCockpitCommandLine();
                        surface.CommandLineText = "/";
                        break;
                    }

                    vm.DismissChatSlashAutocomplete();
                    vm.DismissChatBracketAutocomplete();
                    break;
                case IntercomComposerKeyKind.Enter:
                    if (vm.IsCockpitCommandLineOpen)
                    {
                        if (vm.IsComposerAutocompleteVisible
                            && vm.TryCommitSelectedComposerSuggestion(out var cclAutoExecute))
                        {
                            surface.CommandLineText = vm.CockpitCommandLineText;
                            surface.CommandLineCaretIndex = vm.CockpitCommandLineCaretIndex;
                            if (cclAutoExecute)
                                _ = vm.TryCommitCockpitCommandLineAsync();
                        }
                        else
                        {
                            surface.CommandLineText = vm.CockpitCommandLineText;
                            _ = vm.TryCommitCockpitCommandLineAsync();
                        }

                        break;
                    }

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
                        if (vm.IsCockpitCommandLineOpen)
                        {
                            surface.CommandLineText = vm.CockpitCommandLineText;
                            surface.CommandLineCaretIndex = vm.CockpitCommandLineCaretIndex;
                            if (execute)
                                _ = vm.TryCommitCockpitCommandLineAsync();
                        }
                        else
                        {
                            surface.ComposerCaretIndex = vm.ChatComposerCaretIndex;
                            if (execute)
                                TryExecuteSendChat(vm);
                        }
                    }

                    break;
            }
        };
    }

    private async Task HandleTopicRenameRequestAsync(SkiaChatSurfaceControl surface, TopicRenameRequestEventArgs e)
    {
        if (DataContext is not ChatPanelViewModel vm)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null)
            return;

        if (e.ShowContextMenu)
        {
            showTopicRenameContextMenu(surface, vm, owner, e.ThreadId);
            return;
        }

        await RunTopicRenameDialogAsync(surface, vm, owner, e.ThreadId).ConfigureAwait(true);
    }

    private void showTopicRenameContextMenu(
        SkiaChatSurfaceControl surface,
        ChatPanelViewModel vm,
        Window owner,
        Guid threadId)
    {
        var current = vm.TryGetThreadTitleForRename(threadId) ?? "";
        var menu = new ContextMenu();
        var renameItem = new MenuItem { Header = "Переименовать…" };
        renameItem.Click += async (_, _) =>
        {
            await RunTopicRenameDialogAsync(surface, vm, owner, threadId, current).ConfigureAwait(true);
        };
        menu.Items.Add(renameItem);
        menu.Open(surface);
    }

    private static async Task RunTopicRenameDialogAsync(
        SkiaChatSurfaceControl surface,
        ChatPanelViewModel vm,
        Window owner,
        Guid threadId,
        string? presetTitle = null)
    {
        var current = presetTitle ?? vm.TryGetThreadTitleForRename(threadId) ?? "";
        var newTitle = await TopicRenameDialog.ShowAsync(owner, current).ConfigureAwait(true);
        if (newTitle is null)
            return;

        var result = vm.RenameTopicWithTitle(newTitle, threadId);
        if (!result.Success)
        {
            await ShowTopicRenameErrorAsync(owner, result.Message).ConfigureAwait(true);
            return;
        }

        surface.DetailThreadId = vm.SelectedChatThreadId;
        surface.InvalidateVisual();
    }

    private static async Task ShowTopicRenameErrorAsync(Window owner, string message)
    {
        var ok = new Button { Content = "OK", MinWidth = 80, HorizontalAlignment = HorizontalAlignment.Right };
        var dlg = new Window
        {
            Title = "Переименование темы",
            Width = 360,
            Height = 140,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    ok,
                },
            },
        };
        ok.Click += (_, _) => dlg.Close();
        await dlg.ShowDialog(owner);
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
