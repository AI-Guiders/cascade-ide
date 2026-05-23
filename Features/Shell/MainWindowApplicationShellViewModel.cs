using System.Globalization;
using System.Threading.Tasks;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Lang;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Shell;

/// <summary>Relay: меню приложения, тема, язык, окна-хосты (композитор — <see cref="MainWindowViewModel"/>).</summary>
public sealed partial class MainWindowApplicationShellViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _host;

    public MainWindowApplicationShellViewModel(MainWindowViewModel host) => _host = host;

    [RelayCommand]
    private void OpenSolution() => _host.RequestOpenSolution?.Invoke();

    [RelayCommand]
    private void CreateNewSolution() => _host.RequestCreateNewSolution?.Invoke();

    [RelayCommand]
    private void OpenFolder() => _host.RequestOpenFolder?.Invoke();

    [RelayCommand]
    private void OpenFileFromDialog() => _host.RequestOpenFile?.Invoke();

    [RelayCommand]
    private void Exit() => _host.RequestClose?.Invoke();

    [RelayCommand]
    private void About() => _host.RequestShowAbout?.Invoke();

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsPresentation = _host.McpSettings.Ai.Chat.SettingsPresentation ?? "mfd";
        if (string.Equals(settingsPresentation.Trim(), "window", StringComparison.OrdinalIgnoreCase))
        {
            _host.RequestOpenSettings?.Invoke();
            return;
        }

        _host.ApplyMfdRegionExpanded(true);
        _host.TryNavigateToMfdShellPage(MfdShellPage.AiChatSettings);
    }

    [RelayCommand]
    private void OpenFullSettingsWindow() => _host.RequestOpenSettings?.Invoke();

    [RelayCommand(CanExecute = nameof(CanToggleMfdHostWindow))]
    private void ToggleMfdHostWindow() => _host.RequestToggleMfdHostWindow?.Invoke();

    private bool CanToggleMfdHostWindow() => _host.PresentationRequestsMfdHostWindow;

    [RelayCommand(CanExecute = nameof(CanTogglePmSplitHostWindow))]
    private void TogglePmSplitHostWindow() => _host.RequestTogglePmSplitHostWindow?.Invoke();

    private bool CanTogglePmSplitHostWindow() => _host.PresentationRequestsPmSplitHostWindow;

    [RelayCommand]
    private async Task ApplyDarkThemeAsync() =>
        await UiThemeApply.ApplyOnUiThreadAsync(UiThemeApply.GetDarkThemeJson());

    [RelayCommand]
    private async Task ApplyLightThemeAsync() =>
        await UiThemeApply.ApplyOnUiThreadAsync(UiThemeApply.GetLightThemeJson());

    [RelayCommand]
    private async Task ApplyCursorLikeThemeAsync() =>
        await UiThemeApply.ApplyOnUiThreadAsync(UiThemeApply.GetCursorLikeThemeJson());

    [RelayCommand]
    private async Task ApplyPowerClassicThemeAsync() =>
        await UiThemeApply.ApplyOnUiThreadAsync(UiThemeApply.GetPowerThemeJson());

    [RelayCommand]
    private void SetUiLanguage(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return;
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName.Trim());
            LocViewModel.Current?.SetCulture(culture);
            _host.McpNotifyPropertyChanged(nameof(MainWindowViewModel.SafetyLevelDescription));
            _host.McpSettings.Workspace.Culture = culture.Name;
            _host.HostSaveSettingsIfChanged();
        }
        catch (CultureNotFoundException)
        {
        }
    }

    [RelayCommand]
    private void ResetUiLanguageToSystem()
    {
        _host.McpSettings.Workspace.Culture = "";
        UiCulture.ApplyFromSystem();
        _host.McpNotifyPropertyChanged(nameof(MainWindowViewModel.SafetyLevelDescription));
        _host.HostSaveSettingsIfChanged();
    }

    [RelayCommand]
    private async Task OpenThemeFileAsync()
    {
        var path = _host.RequestOpenThemeFile != null ? await _host.RequestOpenThemeFile() : null;
        if (string.IsNullOrEmpty(path))
            return;
        await UiThemeApply.ApplyOnUiThreadAsync(UiThemeApply.GetThemeJsonFromFile(path));
    }
}
