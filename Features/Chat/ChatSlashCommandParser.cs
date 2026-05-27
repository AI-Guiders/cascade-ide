#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Слэш-строка в ChatInput: резолв только через каталог (<see cref="ChatSlashCommandCatalog.TryResolveInput"/>).</summary>
public static class ChatSlashCommandParser
{
    public static bool IsSlashLine(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return raw.TrimStart()[0] == '/';
    }

    /// <summary>
    /// После выбора подсказки с путём: сразу выполнить без второго Enter (<c>auto_run_on_commit</c> в intent-catalog).
    /// </summary>
    public static bool ShouldAutoExecuteAfterAutocompleteCommit(string? chatInput)
    {
        if (!IsSlashLine(chatInput))
            return false;

        return ChatSlashCommandCatalog.TryResolveInput(chatInput, out var descriptor, out var resolvedArgTail)
               && descriptor.ShouldAutoExecuteAfterAutocompleteCommit(resolvedArgTail);
    }
}
