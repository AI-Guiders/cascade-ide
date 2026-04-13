using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Power / документы.</summary>
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

        add(Services.IdeCommands.SendChat, async (args, _) =>
        {
            var msg = McpCommandJsonArgs.String(args, "message");
            if (!string.IsNullOrWhiteSpace(msg))
                _vm.ChatPanel.ChatInput = msg!;
            if (_vm.ChatPanel.SendChatCommand.CanExecute(null))
                await _vm.ChatPanel.SendChatCommand.ExecuteAsync(null);
            return "OK";
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

    private void RegisterDocuments(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ReopenClosedDocument, async (_, _) =>
        {
            if (_vm.ReopenClosedDocumentCommand.CanExecute(null))
                _vm.ReopenClosedDocumentCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ActivateDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var pathAct = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.ActivateDocumentCommand.CanExecute(pathAct))
                _vm.ActivateDocumentCommand.Execute(pathAct);
            return "OK";
        });
        add(Services.IdeCommands.CloseDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var pathClose = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.CloseDocumentCommand.CanExecute(pathClose))
                _vm.CloseDocumentCommand.Execute(pathClose);
            return "OK";
        });
        add(Services.IdeCommands.TogglePinDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var pathPin = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.TogglePinDocumentCommand.CanExecute(pathPin))
                _vm.TogglePinDocumentCommand.Execute(pathPin);
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup1, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var p1 = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.MoveDocumentToGroup1Command.CanExecute(p1))
                _vm.MoveDocumentToGroup1Command.Execute(p1);
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup2, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var p2 = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.MoveDocumentToGroup2Command.CanExecute(p2))
                _vm.MoveDocumentToGroup2Command.Execute(p2);
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup3, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "file_path")))
                return "Missing file_path";
            var p3 = McpCommandJsonArgs.String(args, "file_path")!;
            if (_vm.MoveDocumentToGroup3Command.CanExecute(p3))
                _vm.MoveDocumentToGroup3Command.Execute(p3);
            return "OK";
        });
    }
}
