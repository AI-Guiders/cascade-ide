#nullable enable

namespace CascadeIDE.Features.Chat;

public sealed partial class ChatSurfaceIntentStage
{
    private static void ApplyForkHints(
        in ChatSurfaceIntent intent,
        Dictionary<Guid, Guid?> parentThreadByThread)
    {
        if (intent.ThreadForks is null)
            return;

        foreach (var fork in intent.ThreadForks)
        {
            if (fork.NewThreadId == Guid.Empty)
                continue;
            if (parentThreadByThread.ContainsKey(fork.NewThreadId))
                continue;

            parentThreadByThread[fork.NewThreadId] =
                fork.PreviousThreadId == Guid.Empty ? null : fork.PreviousThreadId;
        }
    }

    private static void MergeSyntheticThreads(
        Dictionary<Guid, ChatThreadNode> threads,
        in ChatSurfaceIntent intent,
        IReadOnlyDictionary<Guid, Guid?> parentThreadByThread,
        int orderBase)
    {
        var order = orderBase;
        foreach (var threadId in CollectSyntheticThreadIds(intent))
        {
            if (threads.ContainsKey(threadId))
                continue;

            parentThreadByThread.TryGetValue(threadId, out var parentThreadId);
            threads[threadId] = new ChatThreadNode(
                threadId,
                NodeIdForThread(threadId),
                BuildSyntheticThreadTitle(threadId, in intent),
                IsMainThread: threadId == intent.MainThreadId,
                IsActive: threadId == intent.ActiveThreadId,
                ParentThreadId: parentThreadId,
                ForkedFromMessageId: null,
                Depth: 0,
                Order: order++);
        }
    }

    private static IEnumerable<Guid> CollectSyntheticThreadIds(ChatSurfaceIntent intent)
    {
        if (intent.MainThreadId != Guid.Empty)
            yield return intent.MainThreadId;

        if (intent.ActiveThreadId != Guid.Empty)
            yield return intent.ActiveThreadId;

        if (intent.ThreadDisplayTitles is not null)
        {
            foreach (var threadId in intent.ThreadDisplayTitles.Keys)
                yield return threadId;
        }

        if (intent.ThreadForks is not null)
        {
            foreach (var fork in intent.ThreadForks)
            {
                if (fork.NewThreadId != Guid.Empty)
                    yield return fork.NewThreadId;
            }
        }
    }

    private static bool HasSyntheticThreadSources(in ChatSurfaceIntent intent) =>
        intent.MainThreadId != Guid.Empty
        || intent.ActiveThreadId != Guid.Empty
        || intent.ThreadDisplayTitles is { Count: > 0 }
        || intent.ThreadForks is { Count: > 0 };

    private static string BuildSyntheticThreadTitle(Guid threadId, in ChatSurfaceIntent intent)
    {
        if (intent.ThreadDisplayTitles is not null
            && intent.ThreadDisplayTitles.TryGetValue(threadId, out var custom)
            && !string.IsNullOrWhiteSpace(custom))
        {
            return TrimForTitle(custom);
        }

        if (threadId == intent.MainThreadId)
            return "Основная тема";

        if (!string.IsNullOrWhiteSpace(intent.ThreadBranchHint)
            && threadId == intent.ActiveThreadId)
        {
            return TrimForTitle(intent.ThreadBranchHint);
        }

        return $"Ветка {threadId:N}"[..12];
    }

    private static ChatSurfaceState BuildStateWithoutMessages(in ChatSurfaceIntent intent)
    {
        var confirmations = new List<ChatConfirmationNode>();
        if (intent.ActiveClarificationBatch is { } clarification)
        {
            var threadId = intent.ActiveThreadId != Guid.Empty ? intent.ActiveThreadId : intent.MainThreadId;
            confirmations.Add(new ChatConfirmationNode(
                NodeIdForClarification(clarification.Id),
                threadId,
                clarification.Id,
                string.IsNullOrWhiteSpace(clarification.Title) ? "Уточнения к текущему шагу" : clarification.Title.Trim(),
                BuildClarificationBody(clarification),
                clarification.Items.Count,
                IsActive: true,
                IsResolved: false));
        }

        if (!HasSyntheticThreadSources(in intent))
        {
            return new ChatSurfaceState(
                [],
                [],
                confirmations,
                [],
                intent.ActiveThreadId,
                "Chat");
        }

        var parentThreadByThread = new Dictionary<Guid, Guid?>();
        ApplyForkHints(in intent, parentThreadByThread);
        var threads = new Dictionary<Guid, ChatThreadNode>();
        MergeSyntheticThreads(threads, in intent, parentThreadByThread, orderBase: 0);

        foreach (var threadId in threads.Keys.ToList())
        {
            threads[threadId] = threads[threadId] with
            {
                Depth = ComputeDepth(threadId, threads)
            };
        }

        var orderedThreads = threads.Values.OrderBy(thread => thread.Order).ToList();
        var activeThreadLabel = orderedThreads.FirstOrDefault(thread => thread.IsActive)?.Title
            ?? orderedThreads.FirstOrDefault(thread => thread.IsMainThread)?.Title
            ?? "Chat";

        return new ChatSurfaceState(
            orderedThreads,
            [],
            confirmations,
            [],
            intent.ActiveThreadId,
            activeThreadLabel);
    }
}
