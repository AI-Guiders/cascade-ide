#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Строки popup (slash или bracket) — один список для Skia.</summary>
    public ObservableCollection<ChatSlashSuggestionItem> ComposerPopupSuggestions { get; } = [];

    partial void OnSelectedChatSlashSuggestionIndexChanged(int value)
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

    partial void OnChatComposerCaretIndexChanged(int value)
    {
        RefreshComposerAutocomplete();
        RefreshComposerSlashPreview();
    }

    /// <summary>Slash popup для Cockpit Command Line (тот же каталог, что у composer).</summary>
    public void RefreshCockpitCommandLineAutocomplete(string? inputOverride = null, int? caretOverride = null)
    {
        if (!IsCockpitCommandLineOpen)
            return;

        IsChatBracketAutocompleteVisible = false;
        ChatBracketSuggestions.Clear();
        SelectedChatBracketSuggestionIndex = -1;
        RefreshChatSlashAutocomplete(
            inputOverride ?? CockpitCommandLineText,
            caretOverride ?? CockpitCommandLineCaretIndex);
    }

    public void RefreshComposerAutocomplete(string? inputOverride = null, int? caretOverride = null)
    {
        if (IsCockpitCommandLineOpen)
        {
            RefreshCockpitCommandLineAutocomplete(inputOverride, caretOverride);
            return;
        }

        var text = inputOverride ?? ChatInput;
        var caret = Math.Clamp(caretOverride ?? ChatComposerCaretIndex, 0, text.Length);

        if (ChatBracketAutocomplete.TryGetEditState(text, caret, out var bracketState))
        {
            scheduleAnchorDraftPreview(text, caret);
            var syncBracketRefresh = bracketState.ActiveAxis == ChatBracketAutocomplete.Axis.Start
                && bracketState.AxisPrefix.Length == 0;
            if (syncBracketRefresh)
                RefreshChatBracketAutocomplete(text, caret);
            else
                scheduleBracketAutocompleteRefresh(text, caret);

            if (syncBracketRefresh && IsChatBracketAutocompleteVisible)
            {
                IsChatSlashAutocompleteVisible = false;
                ChatSlashSuggestions.Clear();
                rebuildComposerPopup();
                return;
            }

            if (!syncBracketRefresh)
            {
                IsChatSlashAutocompleteVisible = false;
                ChatSlashSuggestions.Clear();
                rebuildComposerPopup();
                return;
            }
        }
        else
        {
            _bracketAutocompleteDebounceCts?.Cancel();
            IsChatBracketAutocompleteVisible = false;
            ChatBracketSuggestions.Clear();
            SelectedChatBracketSuggestionIndex = -1;
            clearAnchorDraftPreview();
        }

        RefreshChatSlashAutocomplete(text, caretOverride: caret);
        rebuildComposerPopup();
        RefreshComposerSlashPreview(text, caret);
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
