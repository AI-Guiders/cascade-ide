#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using CascadeConversationMessage = CascadeIDE.Services.ChatMessage;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

/// <summary>Streaming provider send + Microsoft Agent Framework (Ollama/cloud) IDE chat path.</summary>
public partial class ChatPanelViewModel
{
    private async Task SendChatWithStreamingProviderAsync(string agentInput, string displayInput)
    {
        if (TryResolveMafIdeChat(out var exec, out var chatClient))
        {
            try
            {
                await SendChatWithMafIdeAgentAsync(agentInput, exec!, chatClient!).ConfigureAwait(false);
            }
            finally
            {
                chatClient?.Dispose();
            }

            return;
        }

        var messages = ChatMessages.Take(ChatMessages.Count - 1)
            .Where(m => !m.IsLocalSelfOnly)
            .Select(m => new Services.ChatMessage(m.Role, m.Content))
            .Append(new Services.ChatMessage("user", agentInput))
            .ToList();
        var assistantMsg = new ChatMessageViewModel("assistant", "", threadId: _activeThreadId);
        ChatMessages.Add(assistantMsg);

        var usageCollector = new ChatTurnUsageCollector();
        await foreach (var token in _aiProviderManager.StreamChatAsync(
            _getActiveAiProvider(),
            messages,
            _getCurrentFilePath(),
            _getEditorText(),
            _getUseMinimizedContext(),
            BeginAgentTurnCancellation(),
            usageCollector))
        {
            var t = token;
            UiScheduler.Default.Post(() => assistantMsg.Content += t);
        }

        await RecordFmTurnUsageAsync(usageCollector.LastTurn).ConfigureAwait(false);
        _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(assistantMsg));
    }

    private bool TryResolveMafIdeChat(
        [NotNullWhen(true)] out Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? exec,
        [NotNullWhen(true)] out IChatClient? chatClient)
    {
        exec = null;
        chatClient = null;

        var handler = _executeIdeCommandForMafAgent;
        if (handler is null)
            return false;

        var key = _getActiveAiProvider();
        if (string.Equals(key, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = _getLocalOllamaEndpoint?.Invoke();
            var modelId = _getEffectiveOllamaModelId?.Invoke()?.Trim();
            if (endpoint is null || string.IsNullOrWhiteSpace(modelId))
                return false;

            exec = handler;
            chatClient = new OllamaChatClient(endpoint, modelId);
            return true;
        }

        if (string.Equals(key, "Anthropic", StringComparison.Ordinal)
            || string.Equals(key, "OpenAI", StringComparison.Ordinal)
            || string.Equals(key, "DeepSeek", StringComparison.Ordinal))
        {
            var cloud = _tryCreateCloudMafIChatClient?.Invoke();
            if (cloud is null)
                return false;

            exec = handler;
            chatClient = cloud;
            return true;
        }

        return false;
    }

    /// <summary>Microsoft Agent Framework + <see cref="IChatClient"/> (Ollama / облако) и вызовы IDE через <c>ExecuteCommandAsync</c> (как MCP).</summary>
    private async Task SendChatWithMafIdeAgentAsync(
        string input,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> executeIdeCommandAsync,
        IChatClient chatClient)
    {
        var dialogMessages = ChatMessages.Take(ChatMessages.Count - 1)
            .Where(m => !m.IsLocalSelfOnly)
            .Where(m => IsUserOrAssistantOrToolForMafHistory(m.Role))
            .Select(m => new CascadeConversationMessage(m.Role, m.Content))
            .Append(new CascadeConversationMessage("user", input))
            .ToList();

        var minimized = _getChatMinimizedContextBlock?.Invoke();
        minimized = string.IsNullOrWhiteSpace(minimized) ? null : minimized.Trim();

        var pendingHarness = Harness.TryConsumePendingAgentContext();
        if (!string.IsNullOrWhiteSpace(pendingHarness))
        {
            minimized = string.IsNullOrWhiteSpace(minimized)
                ? pendingHarness.Trim()
                : pendingHarness.Trim() + "\n\n---\n\n" + minimized;
        }

        var projectRules = CascadeIdeMafProjectAgentRules.TryLoadMerged(_getWorkspaceRoot());

        ChatMessageViewModel? assistantMsg = null;

        try
        {
            var (text, toolUiBubbles, fmUsage) = await CascadeIdeMafIdeAgentChat.RunAsync(
                chatClient,
                dialogMessages,
                minimized,
                projectRules,
                executeIdeCommandAsync,
                BeginAgentTurnCancellation()).ConfigureAwait(false);

            await RecordFmTurnUsageAsync(fmUsage).ConfigureAwait(false);

            await UiScheduler.Default.InvokeAsync(() =>
            {
                foreach (var bubble in toolUiBubbles)
                {
                    ChatMessages.Add(new ChatMessageViewModel(
                        "tool",
                        bubble,
                        threadId: _activeThreadId));
                }

                assistantMsg = new ChatMessageViewModel("assistant", text, threadId: _activeThreadId);
                ChatMessages.Add(assistantMsg);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                assistantMsg = new ChatMessageViewModel(
                    "assistant",
                    $"Ошибка агента (MAF): {ex.Message}",
                    threadId: _activeThreadId);
                ChatMessages.Add(assistantMsg);
            }).ConfigureAwait(false);
        }

        if (assistantMsg is not null)
            _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(assistantMsg));
    }

    private static bool IsUserOrAssistantRole(string role)
        => string.Equals(role, "user", StringComparison.OrdinalIgnoreCase)
           || string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase);

    /// <summary>Роли, которые уходят в <see cref="CascadeIdeMafIdeAgentChat.RunAsync"/> как история (в т.ч. <c>tool</c> → <see cref="Microsoft.Extensions.AI.ChatRole.Tool"/> с усечением в сборщике сообщений).</summary>
    private static bool IsUserOrAssistantOrToolForMafHistory(string role)
        => IsUserOrAssistantRole(role)
           || string.Equals(role, "tool", StringComparison.OrdinalIgnoreCase);
}
