#nullable enable

using System.Collections.ObjectModel;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private const int BracketAutocompleteDebounceMs = 80;

    private CancellationTokenSource? _bracketAutocompleteDebounceCts;
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
