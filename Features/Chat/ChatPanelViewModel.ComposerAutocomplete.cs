#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Строки popup (slash или bracket) — один список для Skia.</summary>
    public ObservableCollection<ChatSlashSuggestionItem> ComposerPopupSuggestions { get; } = [];

    public ObservableCollection<ChatBracketSuggestionItem> ChatBracketSuggestions { get; } = [];

    [ObservableProperty]
    private bool _isChatBracketAutocompleteVisible;

    [ObservableProperty]
    private int _selectedChatBracketSuggestionIndex = -1;

    partial void OnSelectedChatSlashSuggestionIndexChanged(int value)
    {
        _ = value;
        NotifyComposerAutocompleteSelectionChanged();
    }

    partial void OnSelectedChatBracketSuggestionIndexChanged(int value)
    {
        _ = value;
        NotifyComposerAutocompleteSelectionChanged();
    }

    private void NotifyComposerAutocompleteSelectionChanged()
    {
        OnPropertyChanged(nameof(ComposerAutocompleteSelectionIndex));
        OnPropertyChanged(nameof(SelectedComposerAutocompleteIndex));
    }

    /// <summary>Slash или bracket — для UI popup и клавиш.</summary>
    public bool IsComposerAutocompleteVisible =>
        IsChatSlashAutocompleteVisible || IsChatBracketAutocompleteVisible;

    public int ComposerAutocompleteSelectionIndex =>
        IsChatBracketAutocompleteVisible
            ? SelectedChatBracketSuggestionIndex
            : SelectedChatSlashSuggestionIndex;

    public int SelectedComposerAutocompleteIndex
    {
        get => ComposerAutocompleteSelectionIndex;
        set
        {
            if (IsChatBracketAutocompleteVisible)
                SelectedChatBracketSuggestionIndex = value;
            else
                SelectedChatSlashSuggestionIndex = value;
        }
    }

    partial void OnChatComposerCaretIndexChanged(int value) => RefreshComposerAutocomplete();

    public void RefreshComposerAutocomplete(string? inputOverride = null)
    {
        var text = inputOverride ?? ChatInput;
        var caret = Math.Clamp(ChatComposerCaretIndex, 0, text.Length);

        if (ChatBracketAutocomplete.TryGetEditState(text, caret, out _))
        {
            RefreshChatBracketAutocomplete(text, caret);
            if (IsChatBracketAutocompleteVisible)
            {
                IsChatSlashAutocompleteVisible = false;
                ChatSlashSuggestions.Clear();
                rebuildComposerPopup();
                return;
            }
        }
        else
        {
            IsChatBracketAutocompleteVisible = false;
            ChatBracketSuggestions.Clear();
            SelectedChatBracketSuggestionIndex = -1;
        }

        RefreshChatSlashAutocomplete(text);
        rebuildComposerPopup();
    }

    private void rebuildComposerPopup()
    {
        ComposerPopupSuggestions.Clear();
        if (IsChatBracketAutocompleteVisible)
        {
            foreach (var b in ChatBracketSuggestions)
            {
                ComposerPopupSuggestions.Add(new ChatSlashSuggestionItem(
                    new ChatSlashSuggestion(b.Display, b.Display, b.Help, b.Group)));
            }
        }
        else if (IsChatSlashAutocompleteVisible)
        {
            foreach (var s in ChatSlashSuggestions)
                ComposerPopupSuggestions.Add(s);
        }

        OnPropertyChanged(nameof(IsComposerAutocompleteVisible));
        NotifyComposerAutocompleteSelectionChanged();
    }

    private void RefreshChatBracketAutocomplete(string text, int caret)
    {
        var suggestions = ChatBracketAutocomplete.GetSuggestions(
            text,
            caret,
            _getCurrentFilePath?.Invoke(),
            _getWorkspaceRoot(),
            _workspaceFileSlashCompletion);

        ChatBracketSuggestions.Clear();
        if (!ChatBracketAutocomplete.TryGetEditState(text, caret, out var state))
        {
            IsChatBracketAutocompleteVisible = false;
            SelectedChatBracketSuggestionIndex = -1;
            return;
        }

        foreach (var s in suggestions)
            ChatBracketSuggestions.Add(new ChatBracketSuggestionItem(s, state.BracketStart, state.CaretIndex));

        var visible = ChatBracketSuggestions.Count > 0;
        if (IsChatBracketAutocompleteVisible != visible)
            IsChatBracketAutocompleteVisible = visible;
        else
            OnPropertyChanged(nameof(IsChatBracketAutocompleteVisible));

        SelectedChatBracketSuggestionIndex = visible ? 0 : -1;
    }

    public void MoveChatBracketSuggestionSelection(int delta)
    {
        if (ChatBracketSuggestions.Count == 0)
            return;

        if (SelectedChatBracketSuggestionIndex < 0)
            SelectedChatBracketSuggestionIndex = 0;
        else
        {
            var next = SelectedChatBracketSuggestionIndex + delta;
            if (next < 0)
                next = ChatBracketSuggestions.Count - 1;
            else if (next >= ChatBracketSuggestions.Count)
                next = 0;
            SelectedChatBracketSuggestionIndex = next;
        }
    }

    public bool TryCommitSelectedBracketSuggestion()
    {
        if (ChatBracketSuggestions.Count == 0)
            return false;

        var idx = SelectedChatBracketSuggestionIndex < 0 ? 0 : SelectedChatBracketSuggestionIndex;
        if (idx >= ChatBracketSuggestions.Count)
            idx = 0;

        var item = ChatBracketSuggestions[idx];
        var tail = item.AddClosingBracket ? "]" : "";
        var newText = ChatInput[..item.BracketStart]
            + "["
            + item.NewBracketInner
            + tail
            + ChatInput[item.ReplaceEnd..];

        ChatComposerCaretIndex = item.BracketStart + 1 + item.NewBracketInner.Length + tail.Length;
        ChatInput = newText;
        IsChatBracketAutocompleteVisible = false;
        ChatBracketSuggestions.Clear();
        OnPropertyChanged(nameof(IsComposerAutocompleteVisible));
        RefreshComposerAutocomplete();
        return true;
    }

    public void DismissChatBracketAutocomplete()
    {
        IsChatBracketAutocompleteVisible = false;
        ChatBracketSuggestions.Clear();
        OnPropertyChanged(nameof(IsComposerAutocompleteVisible));
    }

    public void MoveComposerAutocompleteSelection(int delta)
    {
        if (!IsComposerAutocompleteVisible)
            return;

        if (IsChatBracketAutocompleteVisible)
            MoveChatBracketSuggestionSelection(delta);
        else
            MoveChatSlashSuggestionSelection(delta);

        NotifyComposerAutocompleteSelectionChanged();
    }

    public bool TryCommitSelectedComposerSuggestion(out bool shouldAutoExecute)
    {
        shouldAutoExecute = false;
        if (IsChatBracketAutocompleteVisible)
            return TryCommitSelectedBracketSuggestion();

        return TryCommitSelectedChatSlashSuggestion(out shouldAutoExecute);
    }
}
