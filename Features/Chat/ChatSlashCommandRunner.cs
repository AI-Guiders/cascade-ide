#nullable enable
using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

public sealed record ChatSlashCommandRunResult(
    bool Handled,
    bool Success,
    string UserFacingText)
{
    public static ChatSlashCommandRunResult NotHandled() => new(false, false, "");
}

/// <summary>Локальное исполнение слэш-команд до отправки агенту (ADR 0119).</summary>
public sealed class ChatSlashCommandRunner
{
    private readonly Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? _executeIdeCommand;

    public ChatSlashCommandRunner(
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? executeIdeCommand)
    {
        _executeIdeCommand = executeIdeCommand;
    }

    public async Task<ChatSlashCommandRunResult> TryRunAsync(string rawInput, CancellationToken cancellationToken = default)
    {
        var parse = ChatSlashCommandParser.TryParse(rawInput);
        if (!parse.IsSlashLine)
            return ChatSlashCommandRunResult.NotHandled();

        if (parse.IsRejected)
            return new ChatSlashCommandRunResult(true, false, parse.RejectReason ?? "Неизвестная слэш-команда.");

        if (!ChatSlashCommandCatalog.TryResolve(parse, out var descriptor))
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                "Неизвестная команда. Введи /help — список доступных слэш-команд.");
        }

        if (descriptor.ExecutionKind == ChatSlashCommandExecutionKind.LocalHelp)
            return new ChatSlashCommandRunResult(true, true, string.Join(Environment.NewLine, ChatSlashCommandCatalog.ListHelpLines()));

        if (_executeIdeCommand is null)
            return new ChatSlashCommandRunResult(true, false, "IDE command bridge недоступен для слэш-команд.");

        var args = BuildArgs(descriptor, parse);
        try
        {
            var json = await _executeIdeCommand(descriptor.CommandId, args, cancellationToken).ConfigureAwait(false);
            return new ChatSlashCommandRunResult(true, true, FormatSuccess(descriptor, json));
        }
        catch (Exception ex)
        {
            return new ChatSlashCommandRunResult(true, false, $"{descriptor.SlashPath}: {ex.Message}");
        }
    }

    private static IReadOnlyDictionary<string, JsonElement>? BuildArgs(
        ChatSlashCommandDescriptor descriptor,
        ChatSlashCommandParseResult parse)
    {
        if (string.IsNullOrWhiteSpace(parse.ArgsTail))
            return null;

        return descriptor.CommandId switch
        {
            IdeCommands.ChatSetProductSpine => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["current_focus"] = JsonSerializer.SerializeToElement(parse.ArgsTail),
            },
            IdeCommands.ChatExportReadable => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["write_file"] = JsonSerializer.SerializeToElement(false),
            },
            _ => null,
        };
    }

    private static string FormatSuccess(ChatSlashCommandDescriptor descriptor, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return $"{descriptor.SlashPath}: выполнено.";

        var trimmed = json.Trim();
        if (trimmed.Length <= 400)
            return $"{descriptor.SlashPath}: {trimmed}";

        return $"{descriptor.SlashPath}: выполнено ({trimmed.Length} символов ответа).";
    }
}
