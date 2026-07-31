#nullable enable
using System.Text.Json;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Chat;

/// <summary>Clarification batch: show/submit/dismiss + MCP JSON entry points.</summary>
public partial class ChatPanelViewModel
{
    public void ShowClarificationBatch(ClarificationBatch batch)
    {
        _activeClarificationBatch = batch;
        ClarificationDraftItems.Clear();
        foreach (var item in batch.Items)
            ClarificationDraftItems.Add(new ClarificationDraftItemViewModel(item));

        ClarificationStatusText = "";
        OnPropertyChanged(nameof(HasActiveClarificationBatch));
        OnPropertyChanged(nameof(ActiveClarificationTitle));
        SubmitClarificationResponseCommand.NotifyCanExecuteChanged();
        DismissClarificationBatchCommand.NotifyCanExecuteChanged();
        RefreshChatSurfaceSnapshot();
        _ = PersistEventAsync(ChatHistoryEventKind.ClarificationBatchOpened, batch);
    }

    public string OpenClarificationBatchFromJson(string batchJson)
    {
        if (string.IsNullOrWhiteSpace(batchJson))
            return "Missing batch_json";

        try
        {
            var batch = JsonSerializer.Deserialize<ClarificationBatch>(batchJson, ChatPanelJson);
            if (batch is null)
                return "Invalid clarification batch";
            ShowClarificationBatch(batch);
            return "OK";
        }
        catch (JsonException ex)
        {
            return $"Invalid clarification batch JSON: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmitClarificationResponse))]
    private void SubmitClarificationResponse()
    {
        if (_activeClarificationBatch is null)
            return;

        var answers = ClarificationDraftItems.ToDictionary(x => x.Id, x => x.Answer?.Trim() ?? "", StringComparer.Ordinal);
        var response = new ClarificationResponse(_activeClarificationBatch.Id, answers);
        if (!ClarificationBatchValidation.TryValidate(_activeClarificationBatch, response, out var error))
        {
            ClarificationStatusText = error ?? "Проверь ответы по пунктам.";
            return;
        }

        ApplyClarificationResponse(response, answers);
    }

    [RelayCommand(CanExecute = nameof(CanDismissClarificationBatch))]
    private void DismissClarificationBatch()
    {
        _activeClarificationBatch = null;
        ClarificationDraftItems.Clear();
        ClarificationStatusText = "";
        OnPropertyChanged(nameof(HasActiveClarificationBatch));
        OnPropertyChanged(nameof(ActiveClarificationTitle));
        SubmitClarificationResponseCommand.NotifyCanExecuteChanged();
        DismissClarificationBatchCommand.NotifyCanExecuteChanged();
        RefreshChatSurfaceSnapshot();
    }

    private bool CanSubmitClarificationResponse() =>
        _activeClarificationBatch is not null && ClarificationDraftItems.Count > 0 && !IsChatLoading;

    private bool CanDismissClarificationBatch() => _activeClarificationBatch is not null;

    public string SubmitClarificationResponseFromJson(string responseJson)
    {
        if (_activeClarificationBatch is null)
            return "No active clarification batch";
        if (string.IsNullOrWhiteSpace(responseJson))
            return "Missing response_json";

        try
        {
            var response = JsonSerializer.Deserialize<ClarificationResponse>(responseJson, ChatPanelJson);
            if (response is null)
                return "Invalid clarification response";
            if (response.BatchId != _activeClarificationBatch.Id)
                return "Batch mismatch";
            if (!ClarificationBatchValidation.TryValidate(_activeClarificationBatch, response, out var error))
                return error ?? "Invalid clarification response";

            ApplyClarificationResponse(response, response.AnswersByItemId);
            return "OK";
        }
        catch (JsonException ex)
        {
            return $"Invalid clarification response JSON: {ex.Message}";
        }
    }

    private void ApplyClarificationResponse(ClarificationResponse response, IReadOnlyDictionary<string, string> answers)
    {
        var clarifyMsg = new ChatMessageViewModel(
            "user",
            BuildClarificationTranscriptMessage(_activeClarificationBatch, answers),
            threadId: _activeThreadId);
        ChatMessages.Add(clarifyMsg);
        _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(clarifyMsg));
        _ = PersistEventAsync(
            ChatHistoryEventKind.ClarificationAnswerSubmitted,
            ChatHistoryPayloadMapping.ToClarificationAnswerPayload(response, answers));

        _activeClarificationBatch = null;
        ClarificationDraftItems.Clear();
        ClarificationStatusText = "Пакет уточнений сохранен в диалог.";
        OnPropertyChanged(nameof(HasActiveClarificationBatch));
        OnPropertyChanged(nameof(ActiveClarificationTitle));
        SubmitClarificationResponseCommand.NotifyCanExecuteChanged();
        DismissClarificationBatchCommand.NotifyCanExecuteChanged();
        RefreshChatSurfaceSnapshot();
    }

    private static string BuildClarificationTranscriptMessage(
        ClarificationBatch? batch,
        IReadOnlyDictionary<string, string> answers)
    {
        var title = string.IsNullOrWhiteSpace(batch?.Title) ? "Уточнения" : batch!.Title.Trim();
        var lines = new List<string> { $"{title}:", "" };
        foreach (var pair in answers)
            lines.Add($"- {pair.Key}: {pair.Value}");
        return string.Join(Environment.NewLine, lines);
    }
}
