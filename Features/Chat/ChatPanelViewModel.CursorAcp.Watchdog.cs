#nullable enable
using AgentClientProtocol;
using CascadeIDE.Models;
using CascadeIDE.Services.CursorAcp;

namespace CascadeIDE.Features.Chat;

/// <summary>Cursor ACP loading stage, wait watchdog, session model list, error mapping.</summary>
public partial class ChatPanelViewModel
{
    private void SetChatLoadingStage(string stageText)
    {
        _chatLoadingStageBaseText = stageText;
        ChatLoadingStatusText = stageText;
    }

    private void MarkAcpActivity() => _lastAcpActivityUtc = DateTimeOffset.UtcNow;

    private void RestartAcpWaitWatchdog()
    {
        var generation = ++_acpWaitWatchdogGeneration;
        _ = Task.Run(async () =>
        {
            while (generation == _acpWaitWatchdogGeneration)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                var elapsed = DateTimeOffset.UtcNow - _lastAcpActivityUtc;
                if (elapsed < TimeSpan.FromSeconds(8))
                    continue;
                await UiScheduler.Default.InvokeAsync(() =>
                {
                    if (!IsChatLoading || generation != _acpWaitWatchdogGeneration)
                        return;
                    var seconds = Math.Max(8, (int)elapsed.TotalSeconds);
                    ChatLoadingStatusText = $"{_chatLoadingStageBaseText} Ждём ответ… {seconds}с";
                });
            }
        });
    }

    private void StopAcpWaitWatchdog() => _acpWaitWatchdogGeneration++;

    private void ApplyCursorAcpSessionModels(SessionModelState? state)
    {
        if (state?.AvailableModels is not { Length: > 0 } models)
        {
            _suppressCursorAcpModelPickChanged = true;
            try
            {
                CursorAcpModelPicks.Clear();
                SelectedCursorAcpModelPick = null;
            }
            finally
            {
                _suppressCursorAcpModelPickChanged = false;
            }

            return;
        }

        _suppressCursorAcpModelPickChanged = true;
        try
        {
            CursorAcpModelPicks.Clear();
            foreach (var m in models)
            {
                var label = string.IsNullOrWhiteSpace(m.Description)
                    ? m.Name
                    : $"{m.Name} — {m.Description}";
                CursorAcpModelPicks.Add(new CursorAcpModelPick(m.ModelId, label));
            }

            var currentId = state.CurrentModelId;
            SelectedCursorAcpModelPick = CursorAcpModelPicks.FirstOrDefault(p =>
                string.Equals(p.ModelId, currentId, StringComparison.Ordinal))
                ?? CursorAcpModelPicks[0];
        }
        finally
        {
            _suppressCursorAcpModelPickChanged = false;
        }
    }

    private async Task ApplyUserSelectedCursorAcpModelAsync(CursorAcpModelPick pick)
    {
        if (_cursorAcp is null)
            return;
        try
        {
            var ok = await _cursorAcp.TrySetSessionModelAsync(pick.ModelId, CancellationToken.None).ConfigureAwait(false);
            if (!ok)
                return;
            await UiScheduler.Default.InvokeAsync(() => _onUserSelectedCursorAcpModelId?.Invoke(pick.ModelId));
        }
        catch
        {
            // сессия может быть сброшена параллельно
        }
    }

    private static (string UserMessage, string StageText) MapCursorAcpError(Exception ex)
    {
        var message = ex.Message?.Trim() ?? "Неизвестная ошибка.";
        if (ContainsAny(message, "upgrade", "plan", "billing", "quota", "rate limit", "credits"))
        {
            return (
                "[Cursor ACP / provider-limit] Доступ к модели ограничен тарифом или квотой. Проверь план/биллинг в Cursor, либо выбери другую модель.",
                "Ошибка провайдера (план/квота)");
        }

        if (ContainsAny(message, "timeout", "timed out", "deadline", "network", "connection"))
        {
            return (
                "[Cursor ACP / network] Не удалось дождаться ответа от провайдера. Попробуй повторить запрос.",
                "Сетевая ошибка провайдера");
        }

        return ($"[Cursor ACP] {message}", "Ошибка ACP");
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
