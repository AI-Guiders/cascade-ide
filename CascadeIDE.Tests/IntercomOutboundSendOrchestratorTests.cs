#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Chat.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomOutboundSendOrchestratorTests
{
    [Fact]
    public async Task RunAsync_empty_input_is_noop()
    {
        var host = new RecordingHost { TrimmedInput = "   " };
        await IntercomOutboundSendOrchestrator.RunAsync(host.ToHost());
        Assert.False(host.BuildAttempted);
    }

    [Fact]
    public async Task RunAsync_slash_handled_skips_build()
    {
        var host = new RecordingHost
        {
            TrimmedInput = "/help",
            SlashHandled = true,
        };
        await IntercomOutboundSendOrchestrator.RunAsync(host.ToHost());
        Assert.False(host.BuildAttempted);
    }

    [Fact]
    public async Task RunAsync_build_fail_sets_clarification()
    {
        var host = new RecordingHost
        {
            TrimmedInput = "hello",
            BuildResult = (false, new IntercomAttachmentMessageBuilder.Outbound("", [], null), "bad attach"),
        };
        await IntercomOutboundSendOrchestrator.RunAsync(host.ToHost());
        Assert.Equal("bad attach", host.LastClarification);
        Assert.Equal(0, host.CommitCount);
    }

    [Fact]
    public async Task RunAsync_mcp_only_commits_without_provider()
    {
        var host = new RecordingHost
        {
            TrimmedInput = "hi",
            BuildResult = (true, new IntercomAttachmentMessageBuilder.Outbound("hi", [], null), ""),
            McpOnly = true,
        };
        await IntercomOutboundSendOrchestrator.RunAsync(host.ToHost());
        Assert.Equal(1, host.CommitCount);
        Assert.False(host.LastCommitStartProviderLoading);
        Assert.Equal(0, host.ProviderDispatchCount);
        Assert.Equal(0, host.EndProviderTurnCount);
    }

    [Fact]
    public async Task RunAsync_happy_path_dispatches_streaming()
    {
        var host = new RecordingHost
        {
            TrimmedInput = "hi",
            BuildResult = (true, new IntercomAttachmentMessageBuilder.Outbound("hi", [], null), ""),
            ActiveProvider = "Ollama",
        };
        await IntercomOutboundSendOrchestrator.RunAsync(host.ToHost());
        Assert.Equal(1, host.CommitCount);
        Assert.True(host.LastCommitStartProviderLoading);
        Assert.Equal(1, host.ProviderDispatchCount);
        Assert.Equal(1, host.EndProviderTurnCount);
        Assert.Equal("hi|agent", host.LastAgentInput);
    }

    private sealed class RecordingHost
    {
        public string TrimmedInput { get; init; } = "";
        public string? WorkspaceRoot { get; init; } = "C:\\ws";
        public int PendingAttachCount { get; init; }
        public bool SlashHandled { get; init; }
        public (bool Ok, IntercomAttachmentMessageBuilder.Outbound Outbound, string Error) BuildResult { get; init; }
        public bool McpOnly { get; init; }
        public string ActiveProvider { get; init; } = "CursorACP";

        public bool BuildAttempted { get; private set; }
        public string? LastClarification { get; private set; }
        public int CommitCount { get; private set; }
        public bool LastCommitStartProviderLoading { get; private set; }
        public int ProviderDispatchCount { get; private set; }
        public int EndProviderTurnCount { get; private set; }
        public string? LastAgentInput { get; private set; }
        public string? LastDisplayInput { get; private set; }

        public IntercomOutboundSendHost ToHost() =>
            new()
            {
                GetTrimmedInput = () => TrimmedInput.Trim(),
                GetWorkspaceRoot = () => WorkspaceRoot,
                GetPendingAttachCount = () => PendingAttachCount,
                TryHandleSlashLineAsync = _ => Task.FromResult(SlashHandled),
                TryBuildOutboundAsync = (_, _) =>
                {
                    BuildAttempted = true;
                    return Task.FromResult(BuildResult);
                },
                BeginPrepareOutboundAsync = () => Task.CompletedTask,
                EndPrepareOutboundAsync = () => Task.CompletedTask,
                ApplyProductSpine = s => s,
                FormatAgentInput = (display, _) => display + "|agent",
                CommitUserMessageAsync = (_, _, startLoading) =>
                {
                    CommitCount++;
                    LastCommitStartProviderLoading = startLoading;
                    return Task.CompletedTask;
                },
                GetChatMcpOnly = () => McpOnly,
                GetActiveAiProvider = () => ActiveProvider,
                SendCursorAcpAsync = input =>
                {
                    LastAgentInput = input;
                    ProviderDispatchCount++;
                    return Task.CompletedTask;
                },
                SendStreamingAsync = (agent, display) =>
                {
                    LastAgentInput = agent;
                    LastDisplayInput = display;
                    ProviderDispatchCount++;
                    return Task.CompletedTask;
                },
                SetClarificationStatusAsync = text =>
                {
                    LastClarification = text;
                    return Task.CompletedTask;
                },
                EndProviderTurnAsync = () =>
                {
                    EndProviderTurnCount++;
                    return Task.CompletedTask;
                },
            };
    }
}
