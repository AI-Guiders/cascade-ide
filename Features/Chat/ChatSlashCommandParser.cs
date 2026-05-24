#nullable enable

namespace CascadeIDE.Features.Chat;

public enum ChatSlashCommandShape
{
    Flat,
    NamespaceAction,
}

public readonly record struct ChatSlashCommandParseResult(
    bool IsSlashLine,
    bool IsRejected,
    ChatSlashCommandShape Shape,
    string Head,
    string? Action,
    string? SubAction,
    string ArgsTail,
    string? RejectReason)
{
    public static ChatSlashCommandParseResult NotSlash() =>
        new(false, false, ChatSlashCommandShape.Flat, "", null, null, "", null);

    public static ChatSlashCommandParseResult Reject(string reason) =>
        new(true, true, ChatSlashCommandShape.Flat, "", null, null, "", reason);
}

/// <summary>Разбор строки <c>/verb</c> или <c>/namespace action …</c> (ADR 0119).</summary>
public static class ChatSlashCommandParser
{
    public static ChatSlashCommandParseResult TryParse(string? raw) =>
        SlashParse.ChatSlashParsePipeline.Parse(raw);

    /// <summary>
    /// После выбора подсказки с путём: сразу выполнить без второго Enter (<c>auto_run_on_commit</c> в intent-catalog).
    /// </summary>
    public static bool ShouldAutoExecuteAfterAutocompleteCommit(string? chatInput)
    {
        var parse = TryParse(chatInput);
        if (!parse.IsSlashLine || parse.IsRejected)
            return false;

        return ChatSlashCommandCatalog.TryResolve(parse, out var descriptor)
               && descriptor.ShouldAutoExecuteAfterAutocompleteCommit(parse.ArgsTail);
    }
}
