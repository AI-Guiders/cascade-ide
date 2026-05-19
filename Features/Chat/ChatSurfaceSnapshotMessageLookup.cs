#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Поиск тела сообщения в <see cref="ChatSurfaceSnapshot"/> по индексу ленты.</summary>
public static class ChatSurfaceSnapshotMessageLookup
{
    public static string? TryGetMessageBody(ChatSurfaceSnapshot? snapshot, int messageIndex)
    {
        if (snapshot is null || messageIndex < 0)
            return null;

        foreach (var lane in snapshot.Layout.Lanes)
        {
            foreach (var entry in lane.Entries)
            {
                if (entry.Kind != ChatSurfaceEntryKind.Message)
                    continue;
                if (entry.MessageIndex == messageIndex)
                    return entry.Body;
            }
        }

        return null;
    }
}
