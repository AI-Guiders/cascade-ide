#nullable enable



using CascadeIDE.Features.Agent.Harness;

using CascadeIDE.Models;

using CascadeIDE.Models.AgentChat;

using CascadeIDE.ViewModels;



namespace CascadeIDE.Features.Chat;



public partial class ChatPanelViewModel

{

    internal const string DefaultTopicForkBriefTemplate =

        "Цель (1 строка):\n" +

        "Граница / не делаем:\n" +

        "Готово когда:";



    private ChatHarnessCoordinator? _harness;



    public ChatHarnessCoordinator Harness =>

        _harness ??= new ChatHarnessCoordinator(

            () => _getCascadeSettings?.Invoke() ?? new CascadeIdeSettings(),

            _executeIdeCommandForMafAgent);



    private int CountMessagesInActiveThread()

    {

        var threadId = _activeThreadId != Guid.Empty ? _activeThreadId : _mainThreadId;

        if (threadId == Guid.Empty)

            return ChatMessages.Count;



        var count = 0;

        foreach (var msg in ChatMessages)

        {

            if (msg.ThreadId == threadId)

                count++;

        }



        return count;

    }



    internal void ApplyTopicForkBriefToComposer()

    {

        var h = _getCascadeSettings?.Invoke()?.Agent.Harness;

        if (h is null || !h.InjectTopicForkBrief)

            return;



        var template = string.IsNullOrWhiteSpace(h.TopicForkBriefTemplate)

            ? DefaultTopicForkBriefTemplate

            : h.TopicForkBriefTemplate.Trim();

        ChatInput = template;

    }



    private async Task OnHarnessAfterUserMessageCommittedAsync()

    {

        HarnessUserTurnResult turn = HarnessUserTurnResult.None;

        HarnessContextPressureResult pressure = HarnessContextPressureResult.None;



        await UiScheduler.Default.InvokeAsync(() =>

        {

            var threadCount = CountMessagesInActiveThread();

            turn = Harness.OnUserMessageCommitted();

            pressure = Harness.OnThreadMessageCommitted(threadCount);

        }).ConfigureAwait(false);



        if (pressure.InjectPreCompact && !string.IsNullOrWhiteSpace(pressure.PreCompactUserMessage))

            await InjectHarnessUserMessageAsync(pressure.PreCompactUserMessage).ConfigureAwait(false);



        if (turn.InjectCheckpoint && !string.IsNullOrWhiteSpace(turn.CheckpointUserMessage))

            await InjectHarnessUserMessageAsync(turn.CheckpointUserMessage).ConfigureAwait(false);

    }



    private async Task InjectHarnessUserMessageAsync(string prompt)

    {

        await UiScheduler.Default.InvokeAsync(() =>

        {

            var msg = new ChatMessageViewModel("user", prompt, threadId: _activeThreadId);

            ChatMessages.Add(msg);

            _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(msg));

            RefreshChatSurfaceSnapshot();

        }).ConfigureAwait(false);

    }

}


