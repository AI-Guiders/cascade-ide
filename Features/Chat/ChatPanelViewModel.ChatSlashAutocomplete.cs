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

    partial void OnChatInputChanged(string value) => RefreshChatSlashAutocomplete();

    public void RefreshChatSlashAutocomplete()
    {
        var suggestions = ChatSlashAutocomplete.GetSuggestions(ChatInput);
        ChatSlashSuggestions.Clear();
        foreach (var s in suggestions)
            ChatSlashSuggestions.Add(new ChatSlashSuggestionItem(s));

        IsChatSlashAutocompleteVisible = ChatSlashSuggestions.Count > 0;
        SelectedChatSlashSuggestionIndex = ChatSlashSuggestions.Count > 0 ? 0 : -1;
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

    public bool TryApplySelectedChatSlashSuggestion()
    {
        if (ChatSlashSuggestions.Count == 0)
            return false;

        var idx = SelectedChatSlashSuggestionIndex < 0 ? 0 : SelectedChatSlashSuggestionIndex;
        if (idx >= ChatSlashSuggestions.Count)
            idx = 0;

        ChatInput = ChatSlashSuggestions[idx].InsertText;
        IsChatSlashAutocompleteVisible = false;
        return true;
    }

    public void DismissChatSlashAutocomplete() => IsChatSlashAutocompleteVisible = false;
}
