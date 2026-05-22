#nullable enable
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    public ObservableCollection<ChatSlashSuggestionItem> ChatSlashSuggestions { get; } = [];

    [ObservableProperty]
    private bool _isChatSlashAutocompleteVisible;

    [ObservableProperty]
    private int _selectedChatSlashSuggestionIndex = -1;

    partial void OnChatInputChanged(string value) => RefreshComposerAutocomplete();

    /// <param name="inputOverride">Текст из TextBox при <c>TextChanged</c> (биндинг может отставать на один тик).</param>
    public void RefreshChatSlashAutocomplete(string? inputOverride = null)
    {
        var text = inputOverride ?? ChatInput;
        var caret = Math.Clamp(ChatComposerCaretIndex, 0, text.Length);
        var suggestions = ChatSlashAutocomplete.GetSuggestions(
            text,
            _workspaceFileSlashCompletion,
            _sessionTopicSlashCompletion,
            caretIndex: caret);
        ChatSlashSuggestions.Clear();
        foreach (var s in suggestions)
            ChatSlashSuggestions.Add(new ChatSlashSuggestionItem(s));

        var visible = ChatSlashSuggestions.Count > 0;
        if (IsChatSlashAutocompleteVisible != visible)
            IsChatSlashAutocompleteVisible = visible;
        else
            OnPropertyChanged(nameof(IsChatSlashAutocompleteVisible));

        SelectedChatSlashSuggestionIndex = visible ? 0 : -1;
        rebuildComposerPopup();
    }

    public void MoveChatSlashSuggestionSelection(int delta)
    {
        if (ChatSlashSuggestions.Count == 0)
            return;

        if (SelectedChatSlashSuggestionIndex < 0)
            SelectedChatSlashSuggestionIndex = 0;
        else
        {
            var next = SelectedChatSlashSuggestionIndex + delta;
            if (next < 0)
                next = ChatSlashSuggestions.Count - 1;
            else if (next >= ChatSlashSuggestions.Count)
                next = 0;
            SelectedChatSlashSuggestionIndex = next;
        }
    }

    public bool TryApplySelectedChatSlashSuggestion() =>
        TryCommitSelectedChatSlashSuggestion(out _);

    /// <summary>Подставить выбранную подсказку; <paramref name="shouldAutoExecute"/> — сразу отправить open/load.</summary>
    public bool TryCommitSelectedChatSlashSuggestion(out bool shouldAutoExecute)
    {
        shouldAutoExecute = false;
        if (ChatSlashSuggestions.Count == 0)
            return false;

        var idx = SelectedChatSlashSuggestionIndex < 0 ? 0 : SelectedChatSlashSuggestionIndex;
        if (idx >= ChatSlashSuggestions.Count)
            idx = 0;

        var insertText = ChatSlashSuggestions[idx].InsertText;
        if (IsCockpitCommandLineOpen)
        {
            CockpitCommandLineText = insertText;
            CockpitCommandLineCaretIndex = insertText.Length;
        }
        else if (ChatSlashAutocomplete.TryReplaceSlashLineAtCaret(
                     ChatInput,
                     ChatComposerCaretIndex,
                     insertText,
                     out var newText,
                     out var newCaret))
        {
            ChatInput = newText;
            ChatComposerCaretIndex = newCaret;
        }
        else
        {
            ChatInput = insertText;
            ChatComposerCaretIndex = insertText.Length;
        }

        IsChatSlashAutocompleteVisible = false;
        shouldAutoExecute = ChatSlashCommandParser.ShouldAutoExecuteAfterAutocompleteCommit(insertText);
        return true;
    }

    public void DismissChatSlashAutocomplete() => IsChatSlashAutocompleteVisible = false;
}
