using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Debug;

/// <summary>Relay: DAP отладка (композитор — <see cref="MainWindowViewModel"/>).</summary>
public sealed partial class MainWindowDebugSessionViewModel : ViewModelBase
{
    private static readonly string[] DebugRelayUiPresentationNames =
    [
        nameof(MainWindowViewModel.HasDebugSession),
        nameof(MainWindowViewModel.IsDebugExecutionPaused),
        nameof(MainWindowViewModel.IsDebugExecutionRunning),
        nameof(MainWindowViewModel.IdeHealthDebugText),
        nameof(MainWindowViewModel.IdeHealthDebugCockpitShort),
        nameof(MainWindowViewModel.IdeHealthMountPayload),
        nameof(MainWindowViewModel.PfdIdeHealthMountContext),
        nameof(MainWindowViewModel.MfdIdeHealthMountContext),
    ];

    private readonly MainWindowViewModel _host;

    public MainWindowDebugSessionViewModel(MainWindowViewModel host) => _host = host;

    internal void NotifyRelayCommandsChanged()
    {
        DebugStartOrContinueCommand.NotifyCanExecuteChanged();
        DebugAttachCommand.NotifyCanExecuteChanged();
        DebugStopCommand.NotifyCanExecuteChanged();
        DebugStepOverCommand.NotifyCanExecuteChanged();
        DebugStepIntoCommand.NotifyCanExecuteChanged();
        DebugStepOutCommand.NotifyCanExecuteChanged();
        foreach (var name in DebugRelayUiPresentationNames)
            _host.McpNotifyPropertyChanged(name);
    }

    /// <summary><c>debug_launch</c> без JSON: тот же поток, что F5.</summary>
    internal async Task<string> DebugLaunchInteractiveAsync()
    {
        await DebugStartOrContinueAsync().ConfigureAwait(true);
        return "OK";
    }

    [RelayCommand(CanExecute = nameof(CanDebugStartOrContinue))]
    private async Task DebugStartOrContinueAsync()
    {
        var dap = _host.DapDebug;
        if (dap.HasActiveSession && dap.IsExecutionStopped)
        {
            try { await dap.ContinueAsync().ConfigureAwait(false); }
            catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
            return;
        }

        if (dap.HasActiveSession && !dap.IsExecutionStopped)
        {
            await ShowDebugInfoAsync("Отладка", "Выполнение не остановлено. Дождись брейкпоинта или останови отладку.").ConfigureAwait(false);
            return;
        }

        var ws = _host.GetWorkspacePath();
        if (string.IsNullOrEmpty(ws))
        {
            await ShowDebugInfoAsync("Отладка", "Сначала открой решение — нужен каталог workspace для брейкпоинтов.").ConfigureAwait(false);
            return;
        }

        var res = await _host.TryResolveDebugLaunchForF5Async().ConfigureAwait(false);
        if (res is not { } r)
        {
            var target = _host.RequestPickDebugTarget != null ? await _host.RequestPickDebugTarget().ConfigureAwait(false) : null;
            if (string.IsNullOrEmpty(target))
                return;
            r = new DebugLaunchResolution(target, null, null, null, OpenLaunchBrowser: false, LaunchUrl: null);
        }

        try
        {
            _host.IsInstrumentationDockVisible = true;
            _host.CurrentMfdShellPage = MfdShellPage.DebugStack;
            _ = await dap.LaunchAsync(
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
        if (_host.DapDebug.HasActiveSession)
            return _host.DapDebug.IsExecutionStopped;
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanDebugAttach))]
    private async Task DebugAttachAsync()
    {
        var ws = _host.GetWorkspacePath();
        if (string.IsNullOrEmpty(ws))
        {
            await ShowDebugInfoAsync("Отладка", "Сначала открой решение.").ConfigureAwait(false);
            return;
        }

        var pid = _host.RequestAttachProcessId != null ? await _host.RequestAttachProcessId().ConfigureAwait(false) : null;
        if (pid is null or <= 0)
            return;

        try
        {
            _host.IsInstrumentationDockVisible = true;
            _host.CurrentMfdShellPage = MfdShellPage.DebugStack;
            _ = await _host.DapDebug.AttachAsync(ws, pid.Value, targetPath: null, netcoredbgPath: null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowDebugInfoAsync("Ошибка присоединения", ex.Message).ConfigureAwait(false);
        }
    }

    private bool CanDebugAttach() => !_host.DapDebug.HasActiveSession;

    [RelayCommand(CanExecute = nameof(CanDebugStop))]
    private async Task DebugStopAsync()
    {
        try { await _host.DapDebug.StopAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
    }

    private bool CanDebugStop() => _host.DapDebug.HasActiveSession;

    [RelayCommand(CanExecute = nameof(CanDebugStep))]
    private async Task DebugStepOverAsync()
    {
        try { _ = await _host.DapDebug.StepOverAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
    }

    [RelayCommand(CanExecute = nameof(CanDebugStep))]
    private async Task DebugStepIntoAsync()
    {
        try { _ = await _host.DapDebug.StepIntoAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
    }

    [RelayCommand(CanExecute = nameof(CanDebugStep))]
    private async Task DebugStepOutAsync()
    {
        try { _ = await _host.DapDebug.StepOutAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowDebugInfoAsync("Отладка", ex.Message).ConfigureAwait(false); }
    }

    private bool CanDebugStep() => _host.DapDebug.HasActiveSession && _host.DapDebug.IsExecutionStopped;

    private Task ShowDebugInfoAsync(string title, string message) =>
        _host.ShowDebugInfoAsync(title, message);
}
