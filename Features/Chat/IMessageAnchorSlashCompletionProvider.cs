#nullable enable

namespace CascadeIDE.Features.Chat;

/// <param name="InsertArg">Аргумент для <c>/anchor peek</c> (№ якоря 1…N или префикс hex).</param>
/// <param name="Label">Строка в popup: № + подпись.</param>
/// <param name="Help">Доп. метаданные (a:id, resolve status).</param>
public sealed record MessageAnchorSlashMatch(string InsertArg, string Label, string Help);

/// <summary>Якоря выбранного сообщения для <see cref="SlashCompletionKind.MessageAnchors"/>.</summary>
public interface IMessageAnchorSlashCompletionProvider
{
    IReadOnlyList<MessageAnchorSlashMatch> GetMatches(string ordinalOrIdPrefix, int limit);
}
