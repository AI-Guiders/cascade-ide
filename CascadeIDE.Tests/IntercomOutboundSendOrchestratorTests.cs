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

    [Fact]
    public async Task RunAsync_pf_voice_commits_without_provider()
    {
        var host = new RecordingHost
        {
            TrimmedInput = "@PF hello PF",
            BuildResult = (true, new IntercomAttachmentMessageBuilder.Outbound("@PF hello PF", [], null), ""),
            ActiveProvider = "CursorACP",
            PfVoice = true,
        };
        await IntercomOutboundSendOrchestrator.RunAsync(host.ToHost());
        Assert.Equal(1, host.CommitCount);
        Assert.False(host.LastCommitStartProviderLoading);
        Assert.Equal(0, host.ProviderDispatchCount);
        Assert.Equal(0, host.EndProviderTurnCount);
    }

    [Fact]
    public async Task RunAsync_follow_up_defers_provider_and_enqueues()
    {
        var host = new RecordingHost
        {
            TrimmedInput = "wait",
            BuildResult = (true, new IntercomAttachmentMessageBuilder.Outbound("wait", [], null), ""),
            DeferProvider = true,
        };
        await IntercomOutboundSendOrchestrator.RunAsync(host.ToHost());
        Assert.Equal(1, host.CommitCount);
        Assert.False(host.LastCommitStartProviderLoading);
        Assert.Equal(0, host.ProviderDispatchCount);
        Assert.Equal(1, host.EnqueueCount);
        Assert.Equal("wait|agent", host.LastAgentInput);
    }

    private sealed class RecordingHost
    {
        public string TrimmedInput { get; init; } = "";
        public string? WorkspaceRoot { get; init; } = "C:\\ws";
        public int PendingAttachCount { get; init; }
        public bool SlashHandled { get; init; }
        public (bool Ok, IntercomAttachmentMessageBuilder.Outbound Outbound, string Error) BuildResult { get; init; }
        public bool McpOnly { get; init; }
        public bool PfVoice { get; init; }
        public string ActiveProvider { get; init; } = "CursorACP";

        public string DeliveryMode { get; init; } = "normal";
        public bool DeferProvider { get; init; }

        public bool BuildAttempted { get; private set; }
        public int EnqueueCount { get; private set; }
        public string? LastDeliveryMode { get; private set; }
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
                CommitUserMessageAsync = (_, _, startLoading, deliveryMode) =>
                {
                    CommitCount++;
                    LastCommitStartProviderLoading = startLoading;
                    LastDeliveryMode = deliveryMode;
                    return Task.CompletedTask;
                },
                ConsumeDeliveryMode = () => DeliveryMode,
                ShouldDeferProviderDispatch = _ => DeferProvider,
                CancelActiveTurnIfSteer = _ => { },
                EnqueueFollowUpAgentInputAsync = input =>
                {
                    EnqueueCount++;
                    LastAgentInput = input;
                    return Task.CompletedTask;
                },
                ProcessFollowUpQueueAsync = () => Task.CompletedTask,
                GetChatMcpOnly = () => McpOnly,
                IsPfDualCockpitVoice = _ => PfVoice,
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
