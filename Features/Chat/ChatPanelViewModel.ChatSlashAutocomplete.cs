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

    [ObservableProperty]
    private string? _chatSlashPathPrefix;

    [ObservableProperty]
    private string? _chatSlashNextStepLabel;

    [ObservableProperty]
    private string? _chatSlashBreadcrumb;

    partial void OnChatInputChanged(string value)
    {
        RefreshComposerAutocomplete();
        RefreshComposerSlashPreview();
    }

    /// <param name="inputOverride">Текст из TextBox при <c>TextChanged</c> (биндинг может отставать на один тик).</param>
    /// <param name="caretOverride">Каретка из Skia composer (приоритет над VM, если биндинг отстаёт).</param>
    public void RefreshChatSlashAutocomplete(string? inputOverride = null, int? caretOverride = null)
    {
        var text = inputOverride ?? ChatInput;
        var caret = Math.Clamp(caretOverride ?? ChatComposerCaretIndex, 0, text.Length);
        var suggestions = ChatSlashAutocomplete.GetSuggestions(
            text,
            _workspaceFileSlashCompletion,
            _sessionTopicSlashCompletion,
            _messageAnchorSlashCompletion,
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

        var hierarchy = ChatSlashAutocomplete.GetHierarchyContext(text, caret);
        ChatSlashPathPrefix = hierarchy?.PathPrefix;
        ChatSlashNextStepLabel = hierarchy?.NextStepLabel;
        ChatSlashBreadcrumb = hierarchy?.Breadcrumb;

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

        shouldAutoExecute = ChatSlashCommandParser.ShouldAutoExecuteAfterAutocompleteCommit(insertText);
        if (shouldAutoExecute)
        {
            IsChatSlashAutocompleteVisible = false;
        }
        else
        {
            // Не гасить popup после сегмента: иначе следующий Enter уходит в Send вместо «start» (ADR 0119 §6).
            RefreshComposerAutocomplete(
                inputOverride: IsCockpitCommandLineOpen ? CockpitCommandLineText : ChatInput,
                caretOverride: IsCockpitCommandLineOpen ? CockpitCommandLineCaretIndex : ChatComposerCaretIndex);
        }

        return true;
    }

    public void DismissChatSlashAutocomplete()
    {
        IsChatSlashAutocompleteVisible = false;
        ChatSlashPathPrefix = null;
        ChatSlashNextStepLabel = null;
        ChatSlashBreadcrumb = null;
    }
}
