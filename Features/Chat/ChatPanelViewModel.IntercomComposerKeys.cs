#nullable enable

using Avalonia.Input;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Обработка клавиш slash-composer / CCL; view синхронизирует surface по флагам результата.</summary>
    public IntercomComposerKeyHandleResult TryHandleSlashComposerKey(
        IntercomComposerKeyKind kind,
        KeyEventArgs? keyEvent = null)
    {
        switch (kind)
        {
            case IntercomComposerKeyKind.Tab:
                return handleTabCommitSuggestion();
            case IntercomComposerKeyKind.SlashUp:
                MoveComposerAutocompleteSelection(-1);
                return new(true, false, false, false, false);
            case IntercomComposerKeyKind.SlashDown:
                MoveComposerAutocompleteSelection(1);
                return new(true, false, false, false, false);
            case IntercomComposerKeyKind.Escape:
                if (IsCockpitCommandLineOpen)
                {
                    CloseCockpitCommandLine();
                    return new(true, true, false, false, false);
                }

                DismissChatSlashAutocomplete();
                DismissChatBracketAutocomplete();
                return new(true, false, false, false, false);
            case IntercomComposerKeyKind.Enter:
                return handleEnter(keyEvent);
            case IntercomComposerKeyKind.CommitSlashSuggestion:
                return handleTabCommitSuggestion();
            default:
                return default;
        }
    }

    private IntercomComposerKeyHandleResult handleTabCommitSuggestion()
    {
        if (!TryCommitSelectedComposerSuggestion(out var autoExecute))
            return default;

        if (IsCockpitCommandLineOpen)
            return new(true, true, false, autoExecute, false);

        return new(true, false, true, false, autoExecute);
    }

    private IntercomComposerKeyHandleResult handleEnter(KeyEventArgs? keyEvent)
    {
        if (IsCockpitCommandLineOpen)
        {
            if (trySendRunnableSlashLine(keyEvent, CockpitCommandLineText, CockpitCommandLineCaretIndex, out var cclSend))
                return new(true, true, false, cclSend, false);

            if (IsComposerAutocompleteVisible
                && TryCommitSelectedComposerSuggestion(out var cclAutoExecute))
            {
                return new(true, true, false, cclAutoExecute, false);
            }

            return new(true, true, false, true, false);
        }

        if (trySendRunnableSlashLine(keyEvent, ChatInput, ChatComposerCaretIndex, out var sendSlash))
            return new(true, false, true, false, true);

        if (IsComposerAutocompleteVisible
            && TryCommitSelectedComposerSuggestion(out var autoExecute))
        {
            if (!autoExecute
                && trySendRunnableSlashLine(keyEvent, ChatInput, ChatComposerCaretIndex, out _))
                return new(true, false, true, false, true);

            return new(true, false, true, false, autoExecute);
        }

        if (keyEvent is { } enterKey && ChatSendKeyMatcher.Matches(enterKey, GetSendMessageKey()))
            return new(true, false, false, false, true);

        return default;
    }

    private bool trySendRunnableSlashLine(KeyEventArgs? keyEvent, string text, int caret, out bool runCockpitCommit)
    {
        runCockpitCommit = false;
        if (keyEvent is not { } enterKey)
            return false;

        if (!ChatSlashAutocomplete.IsRunnableSlashLineAtCaret(text, caret))
            return false;

        if (!ChatSendKeyMatcher.Matches(enterKey, GetSendMessageKey())
            && !ChatSendKeyMatcher.IsBareEnterForSlashCommit(enterKey))
            return false;

        if (IsComposerAutocompleteVisible)
        {
            DismissChatSlashAutocomplete();
            DismissChatBracketAutocomplete();
        }

        runCockpitCommit = IsCockpitCommandLineOpen;
        return true;
    }

    /// <summary>Composer Enter без send: вставка новой строки (view обновляет текст/caret).</summary>
    public bool TryInsertComposerNewLineAtCaret(string currentText, int caretIndex, out string newText, out int newCaret)
    {
        if (IsCockpitCommandLineOpen)
        {
            newText = currentText;
            newCaret = caretIndex;
            return false;
        }

        var caret = Math.Clamp(caretIndex, 0, currentText.Length);
        newText = currentText.Insert(caret, "\n");
        newCaret = caret + 1;
        ChatInput = newText;
        ChatComposerCaretIndex = newCaret;
        return true;
    }

    [Obsolete("Use TryHandleSlashComposerKey.")]
    public IntercomComposerKeyHandleResult TryHandleIntercomComposerKey(
        IntercomComposerKeyKind kind,
        KeyEventArgs? keyEvent = null) =>
        TryHandleSlashComposerKey(kind, keyEvent);
}
