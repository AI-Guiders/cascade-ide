#nullable enable

namespace CascadeIDE.Features.Chat;

/// <param name="InsertArg">Аргумент для <c>/topic open</c> (короткий id, 8 hex).</param>
/// <param name="Label">Строка в списке подсказок: id + заголовок.</param>
/// <param name="Help">Доп. метаданные (main/active, число сообщений).</param>
public sealed record SessionTopicSlashMatch(string InsertArg, string Label, string Help);

/// <summary>Подсказки тем сессии для slash с <see cref="SlashCompletionKind.SessionTopics"/>.</summary>
public interface ISessionTopicSlashCompletionProvider
{
    IReadOnlyList<SessionTopicSlashMatch> GetMatches(string titleOrIdPrefix, int limit);
}
