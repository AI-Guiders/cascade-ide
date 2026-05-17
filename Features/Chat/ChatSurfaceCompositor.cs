#nullable enable
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

public interface IChatSurfaceIntentStage
{
    ChatSurfaceState Resolve(in ChatSurfaceIntent intent);
}

public interface IChatSurfaceDeclutterStage
{
    ChatSurfaceState Apply(in ChatSurfaceState state);
}

public interface IChatSurfaceLayoutStage
{
    ChatSurfaceLayout Layout(in ChatSurfaceState state);
}

/// <summary>Intent stage: строит first-class thread/message/confirmation graph из канонического chat intent.</summary>
public sealed class ChatSurfaceIntentStage : IChatSurfaceIntentStage
{
    public ChatSurfaceState Resolve(in ChatSurfaceIntent intent)
    {
        var currentIntent = intent;
        var messages = currentIntent.Messages
            .Select(message => new ChatMessageNode(
                message.MessageId,
                NodeIdForMessage(message.MessageId),
                message.ThreadId,
                message.ParentMessageId,
                message.MessageIndex,
                NormalizeRole(message.Role),
                message.Content ?? "",
                message.MessageIndex == currentIntent.SelectedMessageIndex,
                StartsBranch: false))
            .OrderBy(message => message.MessageIndex)
            .ToList();

        if (messages.Count == 0)
            return BuildEmptyState(currentIntent);

        var messageById = messages.ToDictionary(message => message.MessageId, message => message);
        var parentThreadByThread = new Dictionary<Guid, Guid?>();
        var parentMessageByThread = new Dictionary<Guid, Guid?>();
        foreach (var message in messages)
        {
            if (message.ParentMessageId is not { } parentMessageId)
                continue;
            if (!messageById.TryGetValue(parentMessageId, out var parent))
                continue;
            if (parent.ThreadId == message.ThreadId)
                continue;
            parentThreadByThread[message.ThreadId] = parent.ThreadId;
            parentMessageByThread[message.ThreadId] = parent.MessageId;
        }

        var firstMessageByThread = messages
            .GroupBy(message => message.ThreadId)
            .ToDictionary(group => group.Key, group => group.OrderBy(message => message.MessageIndex).First());

        var threads = firstMessageByThread
            .OrderBy(pair => pair.Value.MessageIndex)
            .Select(pair =>
            {
                var threadId = pair.Key;
                parentThreadByThread.TryGetValue(threadId, out var parentThreadId);
                parentMessageByThread.TryGetValue(threadId, out var parentMessageId);
                return new ChatThreadNode(
                    threadId,
                    NodeIdForThread(threadId),
                    BuildThreadTitle(threadId, pair.Value, currentIntent.MainThreadId, currentIntent.ThreadBranchHint),
                    IsMainThread: threadId == currentIntent.MainThreadId,
                    IsActive: threadId == currentIntent.ActiveThreadId,
                    ParentThreadId: parentThreadId,
                    ForkedFromMessageId: parentMessageId,
                    Depth: 0,
                    Order: pair.Value.MessageIndex);
            })
            .ToDictionary(thread => thread.ThreadId);

        foreach (var threadId in threads.Keys.ToList())
        {
            threads[threadId] = threads[threadId] with
            {
                Depth = ComputeDepth(threadId, threads)
            };
        }

        messages = messages
            .Select(message => message with
            {
                StartsBranch = message.ParentMessageId is { } parentId
                    && messageById.TryGetValue(parentId, out var parent)
                    && parent.ThreadId != message.ThreadId
            })
            .ToList();

        var confirmations = new List<ChatConfirmationNode>();
        var edges = new List<ChatDecisionEdge>();

        foreach (var message in messages)
        {
            if (message.ParentMessageId is { } parentMessageId && messageById.ContainsKey(parentMessageId))
            {
                edges.Add(new ChatDecisionEdge(
                    NodeIdForMessage(parentMessageId),
                    message.NodeId,
                    message.StartsBranch ? "fork" : "reply"));
            }
        }

        if (currentIntent.ActiveClarificationBatch is { } clarification)
        {
            var activeThreadId = currentIntent.ActiveThreadId != Guid.Empty ? currentIntent.ActiveThreadId : currentIntent.MainThreadId;
            var confirmation = new ChatConfirmationNode(
                NodeIdForClarification(clarification.Id),
                activeThreadId,
                clarification.Id,
                string.IsNullOrWhiteSpace(clarification.Title) ? "Уточнения к текущему шагу" : clarification.Title.Trim(),
                BuildClarificationBody(clarification),
                clarification.Items.Count,
                IsActive: true,
                IsResolved: false);
            confirmations.Add(confirmation);

            var lastMessageInThread = messages
                .Where(message => message.ThreadId == activeThreadId)
                .OrderByDescending(message => message.MessageIndex)
                .FirstOrDefault();
            if (lastMessageInThread is not null)
            {
                edges.Add(new ChatDecisionEdge(
                    lastMessageInThread.NodeId,
                    confirmation.NodeId,
                    "ask"));
            }
        }

        var orderedThreads = threads.Values.OrderBy(thread => thread.Order).ToList();
        var activeThreadLabel = orderedThreads.FirstOrDefault(thread => thread.IsActive)?.Title
            ?? orderedThreads.FirstOrDefault(thread => thread.IsMainThread)?.Title
            ?? "Chat";

        return new ChatSurfaceState(
            orderedThreads,
            messages,
            confirmations,
            edges.OrderBy(edge => edge.FromNodeId, StringComparer.Ordinal).ThenBy(edge => edge.ToNodeId, StringComparer.Ordinal).ToList(),
            currentIntent.ActiveThreadId,
            activeThreadLabel);
    }

    private static ChatSurfaceState BuildEmptyState(in ChatSurfaceIntent intent)
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

        return new ChatSurfaceState([], [], confirmations, [], intent.ActiveThreadId, "Chat");
    }

    private static int ComputeDepth(Guid threadId, IReadOnlyDictionary<Guid, ChatThreadNode> threads)
    {
        var seen = new HashSet<Guid>();
        var depth = 0;
        var current = threadId;
        while (threads.TryGetValue(current, out var thread) && thread.ParentThreadId is { } parent)
        {
            if (!seen.Add(parent))
                break;
            depth++;
            current = parent;
        }

        return depth;
    }

    private static string BuildThreadTitle(Guid threadId, ChatMessageNode firstMessage, Guid mainThreadId, string? branchHint)
    {
        if (threadId == mainThreadId)
            return "Основная тема";

        var basis = string.IsNullOrWhiteSpace(firstMessage.Content)
            ? branchHint
            : firstMessage.Content.Trim();
        if (string.IsNullOrWhiteSpace(basis))
            basis = $"Ветка {threadId:N}"[..12];

        return TrimForTitle(basis);
    }

    private static string BuildClarificationBody(ClarificationBatch batch)
    {
        if (batch.Items.Count == 0)
            return "Пакет уточнений пуст.";

        return string.Join(Environment.NewLine, batch.Items.Select(item => $"- {item.Prompt.Trim()}"));
    }

    private static string TrimForTitle(string text)
    {
        text = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return text.Length <= 48 ? text : text[..48].TrimEnd() + "...";
    }

    private static string NormalizeRole(string role) =>
        string.IsNullOrWhiteSpace(role) ? "assistant" : role.Trim().ToLowerInvariant();

    public static string NodeIdForThread(Guid threadId) => $"thread:{threadId:N}";
    public static string NodeIdForMessage(Guid messageId) => $"message:{messageId:N}";
    public static string NodeIdForClarification(Guid batchId) => $"clarification:{batchId:N}";
}

/// <summary>Declutter v1: keep all semantic nodes, but normalize thread ordering to active-first focus in overview.</summary>
public sealed class ChatSurfaceDeclutterStage : IChatSurfaceDeclutterStage
{
    public ChatSurfaceState Apply(in ChatSurfaceState state)
    {
        if (state.Threads.Count == 0)
            return state;

        var threads = state.Threads
            .OrderByDescending(thread => thread.IsActive)
            .ThenBy(thread => thread.Depth)
            .ThenBy(thread => thread.Order)
            .ToList();

        return state with { Threads = threads };
    }
}

/// <summary>Layout stage: строит overview тредов и ленты карточек по каждой линии работы.</summary>
public sealed class ChatSurfaceLayoutStage : IChatSurfaceLayoutStage
{
    public ChatSurfaceLayout Layout(in ChatSurfaceState state)
    {
        var confirmationsByThread = state.Confirmations
            .GroupBy(confirmation => confirmation.ThreadId)
            .ToDictionary(group => group.Key, group => group.OrderBy(confirmation => confirmation.Title, StringComparer.Ordinal).ToList());

        var messagesByThread = state.Messages
            .GroupBy(message => message.ThreadId)
            .ToDictionary(group => group.Key, group => group.OrderBy(message => message.MessageIndex).ToList());

        var lanes = new List<ChatSurfaceLane>();
        foreach (var thread in state.Threads
                     .OrderByDescending(thread => thread.IsActive)
                     .ThenBy(thread => thread.Depth)
                     .ThenBy(thread => thread.Order))
        {
            var entries = new List<ChatSurfaceEntry>();
            if (messagesByThread.TryGetValue(thread.ThreadId, out var messages))
            {
                foreach (var message in messages)
                {
                    entries.Add(new ChatSurfaceEntry(
                        ChatSurfaceEntryKind.Message,
                        message.NodeId,
                        BuildMessageTitle(message, thread),
                        message.Content,
                        ChatMessageVisualRoleMapping.FromMessageRole(message.Role),
                        message.MessageIndex,
                        MessageIndex: message.MessageIndex,
                        IsSelected: message.IsSelected,
                        StartsBranch: message.StartsBranch));
                }
            }

            if (confirmationsByThread.TryGetValue(thread.ThreadId, out var confirmations))
            {
                var orderBase = entries.Count == 0 ? thread.Order : entries.Max(entry => entry.Order) + 1;
                foreach (var confirmation in confirmations)
                {
                    entries.Add(new ChatSurfaceEntry(
                        ChatSurfaceEntryKind.Confirmation,
                        confirmation.NodeId,
                        confirmation.Title,
                        confirmation.Body,
                        confirmation.IsResolved
                            ? ChatMessageVisualRole.ClarificationResolved
                            : ChatMessageVisualRole.ClarificationPending,
                        orderBase++,
                        IsPending: confirmation.IsActive && !confirmation.IsResolved));
                }
            }

            lanes.Add(new ChatSurfaceLane(thread, entries.OrderBy(entry => entry.Order).ToList()));
        }

        var overview = lanes
            .Select(lane => new ChatThreadOverviewItem(
                lane.Thread.ThreadId,
                lane.Thread.Title,
                BuildThreadSummary(lane),
                lane.Thread.IsActive,
                lane.Thread.IsMainThread,
                lane.Thread.Depth,
                lane.Entries.Count))
            .ToList();

        return new ChatSurfaceLayout(overview, lanes);
    }

    private static string BuildThreadSummary(ChatSurfaceLane lane)
    {
        var lastMeaningful = lane.Entries
            .Where(entry => entry.Kind == ChatSurfaceEntryKind.Message)
            .Where(entry => entry.VisualRole is ChatMessageVisualRole.User or ChatMessageVisualRole.Assistant)
            .OrderByDescending(entry => entry.Order)
            .FirstOrDefault();

        if (lastMeaningful is null)
        {
            var pending = lane.Entries.FirstOrDefault(entry => entry.Kind == ChatSurfaceEntryKind.Confirmation && entry.IsPending);
            if (pending is not null)
                return TruncateSummary(pending.Body);

            var any = lane.Entries
                .Where(entry => entry.Kind == ChatSurfaceEntryKind.Message)
                .OrderByDescending(entry => entry.Order)
                .FirstOrDefault();
            if (any is null)
                return "Пока без сообщений";

            return TruncateSummary(any.Body);
        }

        return TruncateSummary(lastMeaningful.Body);
    }

    private static string TruncateSummary(string text)
    {
        var normalized = string.Join(' ', (text ?? "").Replace('\r', ' ').Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
            return "Пока без текста";
        const int maxLen = 160;
        return normalized.Length <= maxLen ? normalized : normalized[..maxLen] + "…";
    }

    private static string BuildMessageTitle(ChatMessageNode message, ChatThreadNode thread)
    {
        if (message.StartsBranch)
            return message.Role == "user" ? "Новая ветка" : "Ответвление";
        if (thread.IsMainThread && message.MessageIndex == 0)
            return message.Role == "user" ? "Старт темы" : "Первый ответ";

        return message.Role switch
        {
            "user" => "Ты",
            "assistant" => "Агент",
            "thinking" => "Размышление",
            "tool" => "Инструмент",
            _ => message.Role
        };
    }
}

public sealed class ChatSurfaceCompositor(
    IChatSurfaceIntentStage? intentStage = null,
    IChatSurfaceDeclutterStage? declutterStage = null,
    IChatSurfaceLayoutStage? layoutStage = null)
{
    private readonly IChatSurfaceIntentStage _intentStage = intentStage ?? new ChatSurfaceIntentStage();
    private readonly IChatSurfaceDeclutterStage _declutterStage = declutterStage ?? new ChatSurfaceDeclutterStage();
    private readonly IChatSurfaceLayoutStage _layoutStage = layoutStage ?? new ChatSurfaceLayoutStage();

    public ChatSurfaceSnapshot Compose(ChatSurfaceIntent intent)
    {
        var resolved = _intentStage.Resolve(intent);
        var decluttered = _declutterStage.Apply(resolved);
        var layout = _layoutStage.Layout(decluttered);
        var spine = intent.ProductSpine ?? ChatProductSpine.Empty;
        return new ChatSurfaceSnapshot(decluttered, layout, spine);
    }
}
