using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Relay: отладка.</summary>
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
        OnPropertyChanged(nameof(IdeHealthDebugText));
        OnPropertyChanged(nameof(IdeHealthDebugCockpitShort));
        OnPropertyChanged(nameof(IdeHealthMountPayload));
        OnPropertyChanged(nameof(PfdIdeHealthMountContext));
        OnPropertyChanged(nameof(MfdIdeHealthMountContext));
    }

    private Task ShowDebugInfoAsync(string title, string message) =>
        RequestShowInfoAsync != null ? RequestShowInfoAsync(title, message) : Task.CompletedTask;

    /// <summary>F5: продолжить при остановке; иначе старт — сохранённый/единственный в решении/по активному .cs, при полном провале — диалог .dll/.exe.</summary>
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

        var res = await TryResolveDebugLaunchForF5Async().ConfigureAwait(false);
        if (res is not { } r)
        {
            var target = RequestPickDebugTarget != null ? await RequestPickDebugTarget().ConfigureAwait(false) : null;
            if (string.IsNullOrEmpty(target))
                return;
            r = new DebugLaunchResolution(target, null, null, null, OpenLaunchBrowser: false, LaunchUrl: null);
        }

        try
        {
            IsInstrumentationDockVisible = true;
            CurrentMfdShellPage = MfdShellPage.DebugStack;
            _ = await _dapDebug.LaunchAsync(
                ws,
                r.TargetDllPath,
                netcoredbgPath: null,
                r.ProgramArgs,
                r.Environment,
                r.WorkingDirectoryRelativeToSolution).ConfigureAwait(false);
            if (r.OpenLaunchBrowser)
                KestrelLaunchBrowser.TryOpenAfterLaunch(r.Environment, r.LaunchUrl);
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
            CurrentMfdShellPage = MfdShellPage.DebugStack;
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

    /// <summary>
    /// <c>debug_launch</c> без JSON-аргументов (мелодия <c>dl</c>, CascadeChord, палитра): тот же поток, что F5 (старт / continue / диалог цели).
    /// </summary>
    internal async Task<string> DebugLaunchInteractiveAsync()
    {
        await DebugStartOrContinueAsync().ConfigureAwait(true);
        return "OK";
    }
}
