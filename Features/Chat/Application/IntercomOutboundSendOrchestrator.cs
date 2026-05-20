#nullable enable

using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Chat.Application;

/// <summary>
/// Сценарий отправки сообщения Intercom: слэш → сборка outbound → spine → лента → провайдер (ADR 0119).
/// Трассировка только на границах фаз.
/// </summary>
[ApplicationOrchestrator("intercom-outbound-send")]
[UiThreadMarshal("host callbacks marshal UI where needed")]
public static class IntercomOutboundSendOrchestrator
{
    public static Task RunAsync(IntercomOutboundSendHost host, CancellationToken cancellationToken = default) =>
        IntercomSendTrace.RunAsync(host.GetWorkspaceRoot(), IntercomSendPhases.SendChat.Root, rootPhase =>
            runCoreAsync(host, rootPhase, cancellationToken));

    private static async Task runCoreAsync(
        IntercomOutboundSendHost host,
        IntercomSendPhase rootPhase,
        CancellationToken cancellationToken)
    {
        var rawInput = host.GetTrimmedInput();
        if (string.IsNullOrEmpty(rawInput))
            return;

        var workspaceRoot = host.GetWorkspaceRoot();
        rootPhase.Detail($"input_len={rawInput.Length} pending_attach={host.GetPendingAttachCount()}");

        var slashHandled = await IntercomSendTrace.RunAsync(
            workspaceRoot,
            IntercomSendPhases.SendChat.Slash,
            _ => host.TryHandleSlashLineAsync(rawInput)).ConfigureAwait(false);
        if (slashHandled)
            return;

        (bool Ok, IntercomAttachmentMessageBuilder.Outbound Outbound, string Error) build;
        await host.BeginPrepareOutboundAsync().ConfigureAwait(false);
        try
        {
            build = await IntercomSendTrace.RunAsync(
                workspaceRoot,
                IntercomSendPhases.SendChat.BuildOutbound,
                async buildPhase =>
                {
                    var result = await host.TryBuildOutboundAsync(rawInput, cancellationToken).ConfigureAwait(false);
                    if (!result.Ok)
                    {
                        buildPhase.Detail("failed: " + result.Error);
                        return result;
                    }

                    buildPhase.Detail(
                        $"ok attachments={result.Outbound.Attachments.Count} content_len={result.Outbound.Content.Length}");
                    return result;
                }).ConfigureAwait(false);
        }
        finally
        {
            await host.EndPrepareOutboundAsync().ConfigureAwait(false);
        }

        if (!build.Ok)
        {
            await host.SetClarificationStatusAsync(build.Error).ConfigureAwait(false);
            return;
        }

        var prepared = IntercomSendTrace.Run(
            workspaceRoot,
            IntercomSendPhases.SendChat.PrepareMessage,
            preparePhase =>
            {
                var display = host.ApplyProductSpine(build.Outbound.Content);
                if (string.IsNullOrEmpty(display))
                    return (Display: (string?)null, Agent: (string?)null);

                var agent = host.FormatAgentInput(display, build.Outbound);
                preparePhase.Detail($"display_len={display.Length} agent_len={agent.Length}");
                return (Display: display, Agent: agent);
            });

        if (prepared.Display is null)
        {
            await host.SetClarificationStatusAsync("Сообщение пустое после подготовки.").ConfigureAwait(false);
            return;
        }

        var displayInput = prepared.Display;
        var agentInput = prepared.Agent!;
        var mcpOnly = host.GetChatMcpOnly();
        var startProviderLoading = !mcpOnly;

        try
        {
            await IntercomSendTrace.RunAsync(
                workspaceRoot,
                IntercomSendPhases.SendChat.CommitFeed,
                _ => host.CommitUserMessageAsync(displayInput, build.Outbound, startProviderLoading)).ConfigureAwait(false);

            if (mcpOnly)
                return;

            await IntercomSendTrace.RunAsync(
                workspaceRoot,
                IntercomSendPhases.SendChat.DispatchProvider,
                async dispatchPhase =>
                {
                    var provider = host.GetActiveAiProvider();
                    dispatchPhase.Detail($"provider={provider} agent_input_len={agentInput.Length}");

                    if (string.Equals(provider, "CursorACP", StringComparison.Ordinal))
                        await host.SendCursorAcpAsync(agentInput).ConfigureAwait(false);
                    else
                        await host.SendStreamingAsync(agentInput, displayInput).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            rootPhase.Detail("error: " + ex);
            throw;
        }
        finally
        {
            if (startProviderLoading)
                await host.EndProviderTurnAsync().ConfigureAwait(false);
        }
    }
}
