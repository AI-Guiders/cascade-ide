#nullable enable

using Avalonia.Input;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private string _pendingDeliveryMode = IntercomComposerDeliveryModes.Normal;
    private readonly Queue<string> _followUpAgentInputs = new();
    private CancellationTokenSource? _agentTurnCts;
    private int _followUpQueueCount;

    public int FollowUpQueueCount => _followUpQueueCount;

    partial void OnIsChatLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IntercomComposerPlaceholder));
        SendChatCommand.NotifyCanExecuteChanged();
    }

    public string IntercomComposerPlaceholder =>
        IsChatLoading
            ? FollowUpQueueCount > 0
                ? $"Агент отвечает… в очереди {FollowUpQueueCount}. Enter — перехват · Alt+Enter — очередь"
                : "Агент отвечает… Enter — перехват · Alt+Enter — в очередь"
            : "Сообщение, /команда или [M:Method]…";

    /// <summary>Вызывается перед отправкой из composer (ADR 0116).</summary>
    internal void PrepareDeliveryModeForComposerKey(IntercomComposerKeyKind kind, KeyEventArgs? keyEvent)
    {
        if (!IsChatLoading || kind != IntercomComposerKeyKind.Enter || keyEvent is null)
        {
            _pendingDeliveryMode = IntercomComposerDeliveryModes.Normal;
            return;
        }

        var alt = keyEvent.KeyModifiers.HasFlag(KeyModifiers.Alt);
        _pendingDeliveryMode = alt
            ? IntercomComposerDeliveryModes.FollowUp
            : IntercomComposerDeliveryModes.Steer;
    }

    internal string ConsumePendingDeliveryMode()
    {
        var mode = IntercomComposerDeliveryModes.Normalize(_pendingDeliveryMode);
        _pendingDeliveryMode = IntercomComposerDeliveryModes.Normal;
        return mode;
    }

    internal bool ShouldDeferProviderDispatch(string deliveryMode) =>
        IsChatLoading && IntercomComposerDeliveryModes.IsFollowUp(deliveryMode);

    internal void CancelActiveAgentTurnIfSteer(string deliveryMode)
    {
        if (!IsChatLoading || !IntercomComposerDeliveryModes.IsSteer(deliveryMode))
            return;

        try
        {
            _agentTurnCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    internal void EnqueueFollowUpAgentInput(string agentInput)
    {
        if (string.IsNullOrWhiteSpace(agentInput))
            return;

        _followUpAgentInputs.Enqueue(agentInput.Trim());
        _followUpQueueCount = _followUpAgentInputs.Count;
        OnPropertyChanged(nameof(FollowUpQueueCount));
        OnPropertyChanged(nameof(IntercomComposerPlaceholder));
        ChatLoadingStatusText = _followUpQueueCount > 0
            ? $"Агент отвечает… в очереди {_followUpQueueCount}"
            : ChatLoadingStatusText;
    }

    internal CancellationToken BeginAgentTurnCancellation()
    {
        _agentTurnCts?.Cancel();
        _agentTurnCts?.Dispose();
        _agentTurnCts = new CancellationTokenSource();
        return _agentTurnCts.Token;
    }

    private async Task TryDispatchFollowUpQueueAsync()
    {
        if (_followUpAgentInputs.Count == 0)
            return;

        var agentInput = _followUpAgentInputs.Dequeue();
        _followUpQueueCount = _followUpAgentInputs.Count;
        OnPropertyChanged(nameof(FollowUpQueueCount));
        OnPropertyChanged(nameof(IntercomComposerPlaceholder));

        IsChatLoading = true;
        ChatLoadingStatusText = "Модель отвечает (очередь)…";
        try
        {
            if (string.Equals(_getActiveAiProvider(), "CursorACP", StringComparison.Ordinal))
                await SendChatWithCursorAcpAsync(agentInput).ConfigureAwait(false);
            else
                await SendChatWithStreamingProviderAsync(agentInput, agentInput).ConfigureAwait(false);
        }
        finally
        {
            await endIntercomProviderTurnAsync().ConfigureAwait(false);
        }
    }
}
