#nullable enable

using System.Collections.ObjectModel;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private const int BracketAutocompleteDebounceMs = 80;

    private CancellationTokenSource? _bracketAutocompleteDebounceCts;

    public ObservableCollection<ChatBracketSuggestionItem> ChatBracketSuggestions { get; } = [];

    [ObservableProperty]
    private bool _isChatBracketAutocompleteVisible;

    [ObservableProperty]
    private int _selectedChatBracketSuggestionIndex = -1;

    partial void OnSelectedChatBracketSuggestionIndexChanged(int value)
    {
        _ = value;
        NotifyComposerAutocompleteSelectionChanged();
    }

    private void scheduleBracketAutocompleteRefresh(string text, int caret)
    {
        _bracketAutocompleteDebounceCts?.Cancel();
        _bracketAutocompleteDebounceCts = new CancellationTokenSource();
        var cts = _bracketAutocompleteDebounceCts;
        var capturedText = text;
        var capturedCaret = caret;
        _ = refreshBracketAutocompleteDebouncedAsync(capturedText, capturedCaret, cts);
    }

    private async Task refreshBracketAutocompleteDebouncedAsync(
        string text,
        int caret,
        CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(BracketAutocompleteDebounceMs, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await UiScheduler.Default.InvokeAsync(() =>
        {
            if (cts.IsCancellationRequested)
                return;

            if (!ChatBracketAutocomplete.TryGetEditState(text, caret, out _))
            {
                IsChatBracketAutocompleteVisible = false;
                ChatBracketSuggestions.Clear();
                return;
            }

            RefreshChatBracketAutocomplete(text, caret);
            rebuildComposerPopup();
            scheduleAnchorDraftPreview(text, caret);
        }).ConfigureAwait(false);
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
            OnPropertyChanged(nameof(IsComposerAutocompleteVisible));

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
        scheduleAnchorDraftPreview(newText, ChatComposerCaretIndex);
        return true;
    }

    public void DismissChatBracketAutocomplete()
    {
        IsChatBracketAutocompleteVisible = false;
        ChatBracketSuggestions.Clear();
        clearAnchorDraftPreview();
        OnPropertyChanged(nameof(IsComposerAutocompleteVisible));
    }
}
