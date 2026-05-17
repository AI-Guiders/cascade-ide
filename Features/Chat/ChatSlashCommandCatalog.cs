#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

public enum ChatSlashCommandExecutionKind
{
    IdeCommand,
    LocalHelp,
}

public sealed record ChatSlashCommandDescriptor(
    string SlashPath,
    string CommandId,
    string Help,
    ChatSlashCommandExecutionKind ExecutionKind = ChatSlashCommandExecutionKind.IdeCommand);

/// <summary>Curated slash → <see cref="IdeCommands"/> (ADR 0119 §5).</summary>
public static class ChatSlashCommandCatalog
{
    private static readonly ChatSlashCommandDescriptor[] Entries =
    [
        new("/overview", IdeCommands.ChatShowThreadOverview, "Вернуться к overview тем."),
        new("/open", IdeCommands.ChatOpenSelectedThread, "Открыть detail выбранной темы."),
        new("/card", IdeCommands.ForkChatThread, "Новая тема (ветка). Хвост — заголовок/контекст."),
        new("/spine", IdeCommands.ChatSetProductSpine, "Обновить product spine (хвост — текст)."),
        new("/spine-toggle", IdeCommands.ChatToggleProductSpineInAgentContext, "Вкл/выкл spine в контексте агента."),
        new("/export", IdeCommands.ChatExportReadable, "Экспорт чата в читаемый Markdown."),
        new("/build run", IdeCommands.Build, "Собрать решение (structured MCP build)."),
        new("/build ui", IdeCommands.BuildSolutionUi, "Собрать решение (вывод в панель)."),
        new("/test run", IdeCommands.RunTests, "Запустить тесты."),
        new("/test affected", IdeCommands.RunAffectedTests, "Запустить затронутые тесты."),
        new("/debug launch", IdeCommands.DebugLaunch, "Запустить отладку (launch profile / target)."),
        new("/help", "", "Список слэш-команд.", ChatSlashCommandExecutionKind.LocalHelp),
    ];

    public static bool TryResolve(ChatSlashCommandParseResult parse, out ChatSlashCommandDescriptor descriptor)
    {
        descriptor = null!;
        if (!parse.IsSlashLine || parse.IsRejected)
            return false;

        if (parse.Shape == ChatSlashCommandShape.Flat)
        {
            var flat = "/" + parse.Head;
            foreach (var entry in Entries)
            {
                if (entry.ExecutionKind == ChatSlashCommandExecutionKind.LocalHelp
                    && string.Equals(entry.SlashPath, flat, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor = entry;
                    return true;
                }

                if (!entry.SlashPath.Contains(' ', StringComparison.Ordinal)
                    && string.Equals(entry.SlashPath, flat, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor = entry;
                    return true;
                }
            }

            return false;
        }

        var path = "/" + parse.Head + " " + parse.Action;
        foreach (var entry in Entries)
        {
            if (string.Equals(entry.SlashPath, path, StringComparison.OrdinalIgnoreCase))
            {
                descriptor = entry;
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<string> ListHelpLines()
    {
        var lines = new List<string> { "Слэш-команды Intercom / IDE (Tab — autocomplete, позже):" };
        foreach (var entry in Entries)
        {
            if (entry.ExecutionKind == ChatSlashCommandExecutionKind.LocalHelp)
                lines.Add($"  {entry.SlashPath} — {entry.Help}");
            else
                lines.Add($"  {entry.SlashPath} — {entry.Help} ({entry.CommandId})");
        }

        return lines;
    }
}
