using System.Text.Json;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP-хендлеры Power / фокус-шагов, автономного агента, чата и установки модели Ollama.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterFocusPowerAndAgentActions(Action<string, Handler> add)
    {
        add(Services.IdeCommands.FocusCheckpoint, async (_, _) =>
        {
            if (_vm.FocusCheckpointCommand.CanExecute(null))
                _vm.FocusCheckpointCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.FocusRollback, async (_, _) =>
        {
            if (_vm.FocusRollbackCommand.CanExecute(null))
                _vm.FocusRollbackCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ConfirmFocusStep, async (_, _) =>
        {
            if (_vm.ConfirmFocusStepCommand.CanExecute(null))
                _vm.ConfirmFocusStepCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.CancelFocusStep, async (_, _) =>
        {
            if (_vm.CancelFocusStepCommand.CanExecute(null))
                _vm.CancelFocusStepCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ExplainCurrentStep, async (_, _) =>
        {
            if (_vm.ExplainCurrentStepCommand.CanExecute(null))
                _vm.ExplainCurrentStepCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.EmergencyStop, async (_, _) =>
        {
            if (_vm.EmergencyStopCommand.CanExecute(null))
                _vm.EmergencyStopCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.RefreshWorkspaceSnapshot, async (_, _) =>
        {
            if (_vm.RefreshWorkspaceSnapshotCommand.CanExecute(null))
                _vm.RefreshWorkspaceSnapshotCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.ExplainTraceStep, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("step_index", out var exIdx) || exIdx.ValueKind != JsonValueKind.Number || !exIdx.TryGetInt32(out var explainStepIndex) || explainStepIndex < 0)
                return "Missing or invalid step_index (non-negative int; 0 = oldest in AgentTraceSteps)";
            var list = _vm.InstrumentationPanel.AgentTraceSteps;
            if (explainStepIndex >= list.Count)
                return $"Invalid step_index (count={list.Count})";
            _vm.ExplainTraceStepCommand.Execute(list[explainStepIndex]);
            return "OK";
        });
        add(Services.IdeCommands.RollbackTraceStep, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("step_index", out var rbIdx) || rbIdx.ValueKind != JsonValueKind.Number || !rbIdx.TryGetInt32(out var rollbackStepIndex) || rollbackStepIndex < 0)
                return "Missing or invalid step_index (non-negative int; 0 = oldest in AgentTraceSteps)";
            var listRb = _vm.InstrumentationPanel.AgentTraceSteps;
            if (rollbackStepIndex >= listRb.Count)
                return $"Invalid step_index (count={listRb.Count})";
            _vm.RollbackTraceStepCommand.Execute(listRb[rollbackStepIndex]);
            return "OK";
        });

        add(Services.IdeCommands.SetSafetyL1, async (_, _) =>
        {
            if (_vm.SetSafetyL1Command.CanExecute(null))
                _vm.SetSafetyL1Command.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.SetSafetyL2, async (_, _) =>
        {
            if (_vm.SetSafetyL2Command.CanExecute(null))
                _vm.SetSafetyL2Command.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.SetSafetyL3, async (_, _) =>
        {
            if (_vm.SetSafetyL3Command.CanExecute(null))
                _vm.SetSafetyL3Command.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.StartAutonomous, async (_, _) =>
        {
            if (_vm.Autonomous.StartAutonomousCommand.CanExecute(null))
                _vm.Autonomous.StartAutonomousCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.PauseAutonomous, async (_, _) =>
        {
            if (_vm.Autonomous.PauseAutonomousCommand.CanExecute(null))
                _vm.Autonomous.PauseAutonomousCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ResumeAutonomous, async (_, _) =>
        {
            if (_vm.Autonomous.ResumeAutonomousCommand.CanExecute(null))
                _vm.Autonomous.ResumeAutonomousCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.FixFailingTests, async (_, _) =>
        {
            if (_vm.Autonomous.FixFailingTestsCommand.CanExecute(null))
                _vm.Autonomous.FixFailingTestsCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.InvestigateNullref, async (_, _) =>
        {
            if (_vm.Autonomous.InvestigateNullrefCommand.CanExecute(null))
                _vm.Autonomous.InvestigateNullrefCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.PrepareCommit, async (_, _) =>
        {
            if (_vm.Autonomous.PrepareCommitCommand.CanExecute(null))
                _vm.Autonomous.PrepareCommitCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.SendChat, async (args, ct) =>
        {
            var role = McpCommandJsonArgs.String(args, "role")?.Trim();
            var msg = McpCommandJsonArgs.String(args, "message");
            if (string.IsNullOrWhiteSpace(msg))
                return "Missing message";

            var useFastAttachPath = IntercomMcpSendChatRoute.ShouldAppendPreparedFeedMessage(role, msg);
            if (useFastAttachPath)
            {
                var feedRole = string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)
                    ? "assistant"
                    : "user";
                return await _vm.ChatPanel.AppendMessageFromMcpAsync(feedRole, msg!, ct).ConfigureAwait(false);
            }

            _vm.ChatPanel.ChatInput = msg!;
            if (_vm.ChatPanel.SendChatCommand.CanExecute(null))
                await _vm.ChatPanel.SendChatCommand.ExecuteAsync(null);
            return "OK";
        });

        add(Services.IdeCommands.ForkChatThread, async (args, _) =>
        {
            Guid? parent = null;
            var raw = McpCommandJsonArgs.String(args, "parent_message_id")?.Trim();
            if (!string.IsNullOrEmpty(raw) && Guid.TryParse(raw, out var pid))
                parent = pid;
            var title = McpCommandJsonArgs.String(args, "display_title")?.Trim();
            if (string.IsNullOrEmpty(title))
                title = McpCommandJsonArgs.String(args, "title")?.Trim();
            return string.IsNullOrEmpty(title)
                ? _vm.ChatPanel.ForkThread(parent)
                : _vm.ChatPanel.ForkThread(parent, title);
        });
        add(Services.IdeCommands.OpenChatClarificationBatch, async (args, _) =>
        {
            var batchJson = McpCommandJsonArgs.String(args, "batch_json");
            return _vm.ChatPanel.OpenClarificationBatchFromJson(batchJson ?? "");
        });
        add(Services.IdeCommands.SubmitChatClarificationResponse, async (args, _) =>
        {
            var responseJson = McpCommandJsonArgs.String(args, "response_json");
            return _vm.ChatPanel.SubmitClarificationResponseFromJson(responseJson ?? "");
        });
        add(Services.IdeCommands.ChatSelectPrevMessage, async (_, _) =>
        {
            return _vm.ChatPanel.SelectMessageByOffset(-1);
        });
        add(Services.IdeCommands.ChatSelectNextMessage, async (_, _) =>
        {
            return _vm.ChatPanel.SelectMessageByOffset(+1);
        });
        add(Services.IdeCommands.ChatToggleSelectedThinking, async (_, _) =>
        {
            return _vm.ChatPanel.ToggleSelectedThinkingDetails();
        });
        add(Services.IdeCommands.ChatToggleShowThinkingInHistory, async (_, _) =>
        {
            _vm.ShowThinkingInHistory = !_vm.ShowThinkingInHistory;
            return _vm.ShowThinkingInHistory ? "ShowThinkingInHistory=on" : "ShowThinkingInHistory=off";
        });
        add(Services.IdeCommands.ChatSelectPrevThread, async (_, _) =>
        {
            return _vm.ChatPanel.NavigateThreadSelection(-1);
        });
        add(Services.IdeCommands.ChatSelectNextThread, async (_, _) =>
        {
            return _vm.ChatPanel.NavigateThreadSelection(+1);
        });
        add(Services.IdeCommands.ChatOpenSelectedThread, async (_, _) =>
        {
            return _vm.ChatPanel.OpenSelectedThreadDetail();
        });
        add(Services.IdeCommands.ChatShowThreadOverview, async (_, _) =>
        {
            return _vm.ChatPanel.ShowThreadOverview();
        });
        add(Services.IdeCommands.CockpitOpenCommandLine, async (args, _) =>
        {
            var initial = McpCommandJsonArgs.String(args, "initial_text");
            await UiScheduler.Default.InvokeAsync(() =>
                _vm.ChatPanel.OpenCockpitCommandLine(string.IsNullOrWhiteSpace(initial) ? "/" : initial.Trim()));
            return "CockpitCommandLine=open";
        });
        add(Services.IdeCommands.TogglePrimaryWorkSurface, async (_, _) =>
        {
            if (_vm.TogglePrimaryWorkSurfaceCommand.CanExecute(null))
                _vm.TogglePrimaryWorkSurfaceCommand.Execute(null);
            return _vm.PrimaryWorkSurface.ToTomlValue();
        });
        add(Services.IdeCommands.SetPrimaryWorkSurface, async (args, _) =>
        {
            var surface = McpCommandJsonArgs.String(args, "surface") ?? "intercom";
            _vm.PrimaryWorkSurface = PrimaryWorkSurfaceKindExtensions.ParseTomlValue(surface);
            return _vm.PrimaryWorkSurface.ToTomlValue();
        });
        add(Services.IdeCommands.ChatToggleProductSpineInAgentContext, async (_, _) =>
        {
            return _vm.ChatPanel.ToggleProductSpineInAgentContext();
        });
        add(Services.IdeCommands.ChatGetProductSpine, async (_, _) =>
        {
            return _vm.ChatPanel.GetProductSpineJson();
        });
        add(Services.IdeCommands.ChatSetProductSpine, async (args, _) =>
        {
            return _vm.ChatPanel.SetProductSpineFromMcp(args);
        });

        add(Services.IdeCommands.InstallOllamaModel, async (args, _) =>
        {
            var model = McpCommandJsonArgs.String(args, "model");
            if (string.IsNullOrWhiteSpace(model))
                return "Missing model";
            var m = model.Trim();
            _vm.ModelToInstall = m;
            if (_vm.InstallModelCommand.CanExecute(null))
                await _vm.InstallModelCommand.ExecuteAsync(null);
            return "OK";
        });
    }

}
