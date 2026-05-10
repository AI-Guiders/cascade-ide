using System.Globalization;
using System.Threading.Tasks;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Lang;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Relay: приложение, диалоги открытия, тема, язык, окна-хосты.</summary>
public partial class MainWindowViewModel
{
    [RelayCommand]
    private void OpenSolution()
    {
        RequestOpenSolution?.Invoke();
    }

    [RelayCommand]
    private void CreateNewSolution()
    {
        RequestCreateNewSolution?.Invoke();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        RequestOpenFolder?.Invoke();
    }

    [RelayCommand]
    private void OpenFileFromDialog()
    {
        RequestOpenFile?.Invoke();
    }

    [RelayCommand]
    private void Exit()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void About()
    {
        RequestShowAbout?.Invoke();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var placement = (_settings.Ai.Chat.SettingsPresentation ?? "mfd").Trim();
        if (string.Equals(placement, "window", StringComparison.OrdinalIgnoreCase))
        {
            RequestOpenSettings?.Invoke();
            return;
        }

        // Зона Mfd (или окно-хост Mfd на втором экране, ADR 0017): страница параметров AI.
        ApplyMfdRegionExpanded(true);
        TryNavigateToMfdShellPage(MfdShellPage.AiChatSettings);
    }

    /// <summary>Полное окно настроек (все секции), в т.ч. из страницы AI во вторичном контуре.</summary>
    [RelayCommand]
    private void OpenFullSettingsWindow() => RequestOpenSettings?.Invoke();

    /// <summary>MCP / автоматизация: сфокусировать или снова открыть хост только если пресет <c>presentation</c> требует второго окна (нет дубля с меню «вынести Mfd»).</summary>
    [RelayCommand(CanExecute = nameof(CanToggleMfdHostWindow))]
    private void ToggleMfdHostWindow()
    {
        RequestToggleMfdHostWindow?.Invoke();
    }

    private bool CanToggleMfdHostWindow() => PresentationRequestsMfdHostWindow;

    [RelayCommand(CanExecute = nameof(CanTogglePmSplitHostWindow))]
    private void TogglePmSplitHostWindow()
    {
        RequestTogglePmSplitHostWindow?.Invoke();
    }

    private bool CanTogglePmSplitHostWindow() => PresentationRequestsPmSplitHostWindow;

    [RelayCommand]
    private async Task ApplyDarkThemeAsync()
    {
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetDarkThemeJson());
    }

    [RelayCommand]
    private async Task ApplyLightThemeAsync()
    {
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetLightThemeJson());
    }

    [RelayCommand]
    private async Task ApplyCursorLikeThemeAsync()
    {
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetCursorLikeThemeJson());
    }

    /// <summary>Предыдущая Power-палитра (циан/неон без фиолетового кокпита концепта).</summary>
    [RelayCommand]
    private async Task ApplyPowerClassicThemeAsync()
    {
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetPowerThemeJson());
    }

    [RelayCommand]
    private void SetUiLanguage(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return;
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName.Trim());
            LocViewModel.Current?.SetCulture(culture);
            OnPropertyChanged(nameof(SafetyLevelDescription));
            _settings.Workspace.Culture = culture.Name;
            SaveSettingsIfChanged();
        }
        catch (CultureNotFoundException)
        {
            // игнорируем неверный параметр меню
        }
    }

    [RelayCommand]
    private void ResetUiLanguageToSystem()
    {
        _settings.Workspace.Culture = "";
        UiCulture.ApplyFromSystem();
        OnPropertyChanged(nameof(SafetyLevelDescription));
        SaveSettingsIfChanged();
    }

    [RelayCommand]
    private async Task OpenThemeFileAsync()
    {
        var path = RequestOpenThemeFile != null ? await RequestOpenThemeFile() : null;
        if (string.IsNullOrEmpty(path))
            return;
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetThemeJsonFromFile(path));
    }
}
