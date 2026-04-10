using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    private void NotifyDebugRelayCommandsChanged()
    {
        DebugStartOrContinueCommand.NotifyCanExecuteChanged();
        DebugAttachCommand.NotifyCanExecuteChanged();
        DebugStopCommand.NotifyCanExecuteChanged();
        DebugStepOverCommand.NotifyCanExecuteChanged();
        DebugStepIntoCommand.NotifyCanExecuteChanged();
        DebugStepOutCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasDebugSession));
        OnPropertyChanged(nameof(IsDebugExecutionPaused));
        OnPropertyChanged(nameof(IsDebugExecutionRunning));
        OnPropertyChanged(nameof(WorkspaceHealthDebugText));
        OnPropertyChanged(nameof(WorkspaceHealthDebugCockpitShort));
    }

    private Task ShowDebugInfoAsync(string title, string message) =>
        RequestShowInfoAsync != null ? RequestShowInfoAsync(title, message) : Task.CompletedTask;

    /// <summary>F5: продолжить при остановке; иначе запустить стартовый проект (если задан) или выбрать .dll/.exe в диалоге.</summary>
    [RelayCommand(CanExecute = nameof(CanDebugStartOrContinue))]
    private async Task DebugStartOrContinueAsync()
    {
        if (_dapDebug.HasActiveSession && _dapDebug.IsExecutionStopped)
        {
            try { await _dapDebug.ContinueAsync().ConfigureAwait(false); }
            catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
            return;
        }

        if (_dapDebug.HasActiveSession && !_dapDebug.IsExecutionStopped)
        {
            await ShowDebugInfoAsync("Отладка", "Выполнение не остановлено. Дождись брейкпоинта или останови отладку.").ConfigureAwait(false);
            return;
        }

        var ws = GetWorkspacePath();
        if (string.IsNullOrEmpty(ws))
        {
            await ShowDebugInfoAsync("Отладка", "Сначала открой решение — нужен каталог workspace для брейкпоинтов.").ConfigureAwait(false);
            return;
        }

        var target = await TryResolveStartupDebugTargetAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(target))
            target = RequestPickDebugTarget != null ? await RequestPickDebugTarget().ConfigureAwait(false) : null;
        if (string.IsNullOrEmpty(target))
            return;

        try
        {
            IsInstrumentationDockVisible = true;
            MfdShellTabIndex = MfdShellTabDebugStackIndex;
            _ = await _dapDebug.LaunchAsync(ws, target, netcoredbgPath: null, programArgs: null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowDebugInfoAsync("Ошибка запуска отладки", ex.Message).ConfigureAwait(false);
        }
    }

    private bool CanDebugStartOrContinue()
    {
        if (_dapDebug.HasActiveSession)
            return _dapDebug.IsExecutionStopped;
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanDebugAttach))]
    private async Task DebugAttachAsync()
    {
        var ws = GetWorkspacePath();
        if (string.IsNullOrEmpty(ws))
        {
            await ShowDebugInfoAsync("Отладка", "Сначала открой решение.").ConfigureAwait(false);
            return;
        }

        var pid = RequestAttachProcessId != null ? await RequestAttachProcessId().ConfigureAwait(false) : null;
        if (pid is null or <= 0)
            return;

        try
        {
            IsInstrumentationDockVisible = true;
            MfdShellTabIndex = MfdShellTabDebugStackIndex;
            _ = await _dapDebug.AttachAsync(ws, pid.Value, targetPath: null, netcoredbgPath: null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowDebugInfoAsync("Ошибка присоединения", ex.Message).ConfigureAwait(false);
        }
    }

    private bool CanDebugAttach() => !_dapDebug.HasActiveSession;

    [RelayCommand(CanExecute = nameof(CanDebugStop))]
    private async Task DebugStopAsync()
    {
        try { await _dapDebug.StopAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
    }

    private bool CanDebugStop() => _dapDebug.HasActiveSession;

    [RelayCommand(CanExecute = nameof(CanDebugStep))]
    private async Task DebugStepOverAsync()
    {
        try { _ = await _dapDebug.StepOverAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
    }

    [RelayCommand(CanExecute = nameof(CanDebugStep))]
    private async Task DebugStepIntoAsync()
    {
        try { _ = await _dapDebug.StepIntoAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
    }

    [RelayCommand(CanExecute = nameof(CanDebugStep))]
    private async Task DebugStepOutAsync()
    {
        try { _ = await _dapDebug.StepOutAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
    }

    private bool CanDebugStep() => _dapDebug.HasActiveSession && _dapDebug.IsExecutionStopped;
}
