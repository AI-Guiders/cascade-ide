using System.IO;
using System.Text.Json;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatClarificationIntegrationTests
{
    [Fact]
    public void SubmitClarificationResponseFromJson_StoresStructuredDraftWithoutLegacyPrefix()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "cascade-ide-chat-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);

        var vm = CreateViewModel(workspace);
        var batch = new ClarificationBatch(
            Guid.NewGuid(),
            [
                new ClarificationItem("scope", "Какой surface делаем первым?"),
                new ClarificationItem("fallback", "Нужен ли fallback?")
            ],
            "Уточнения по ADR 0031");

        var openResult = vm.OpenClarificationBatchFromJson(JsonSerializer.Serialize(batch));
        Assert.Equal("OK", openResult);
        Assert.True(vm.HasActiveClarificationBatch);

        var response = new ClarificationResponse(batch.Id, new Dictionary<string, string>
        {
            ["scope"] = "Skia-centered surface",
            ["fallback"] = "Avalonia baseline можно удалить"
        });

        var submitResult = vm.SubmitClarificationResponseFromJson(JsonSerializer.Serialize(response));
        Assert.Equal("OK", submitResult);

        var message = Assert.Single(vm.ChatMessages);
        Assert.Equal("user", message.Role);
        Assert.DoesNotContain("[clarification]", message.Content, StringComparison.Ordinal);
        Assert.Contains("Уточнения по ADR 0031", message.Content);
        Assert.Contains("scope: Skia-centered surface", message.Content);
        Assert.False(vm.HasActiveClarificationBatch);
        Assert.Empty(vm.ChatSurfaceSnapshot.State.Confirmations);
        Assert.Contains(vm.ChatSurfaceSnapshot.Layout.Lanes.SelectMany(lane => lane.Entries), entry => entry.MessageIndex == 0);
    }

    private static ChatPanelViewModel CreateViewModel(string workspace)
    {
        var minimizer = new ContextMinimizer(new CSharpLanguageService());
        var aiProviderManager = new AiProviderManager(minimizer, _ => (null, ""));
        return new ChatPanelViewModel(
            aiProviderManager,
            getActiveAiProvider: () => "CursorACP",
            getSelectedOllamaModel: () => null,
            getChatMcpOnly: () => true,
            getShowThinkingInHistory: () => true,
            getUseMinimizedContext: () => false,
            getCurrentFilePath: () => null,
            getEditorText: () => "",
            getWorkspaceRoot: () => workspace,
            getCursorAcpAgentPath: () => "",
            getExternalMcpServersJson: () => "",
            getAcpAutoInjectIdeMcp: () => false);
    }
}
