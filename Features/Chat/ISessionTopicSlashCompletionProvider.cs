#nullable enable

namespace CascadeIDE.Features.Chat;

public sealed record SessionTopicSlashMatch(string InsertArg, string Help);

/// <summary>Подсказки тем сессии для slash с <see cref="SlashCompletionKind.SessionTopics"/>.</summary>
public interface ISessionTopicSlashCompletionProvider
{
    IReadOnlyList<SessionTopicSlashMatch> GetMatches(string titleOrIdPrefix, int limit);
}
