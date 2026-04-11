using System.Globalization;
using System.Threading.Tasks;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Lang;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private void OpenSolution()
    {
        RequestOpenSolution?.Invoke();
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
        RequestOpenSettings?.Invoke();
    }

    [RelayCommand]
    private void ToggleMfdHostWindow()
    {
        RequestToggleMfdHostWindow?.Invoke();
    }

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
            _settings.UiCultureName = culture.Name;
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
        _settings.UiCultureName = "";
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

    [RelayCommand]
    private void ToggleSolutionExplorer()
    {
        IsSolutionExplorerVisible = !IsSolutionExplorerVisible;
    }

    [RelayCommand]
    private void ToggleBuildOutput()
    {
        IsBuildOutputVisible = !IsBuildOutputVisible;
        if (IsBuildOutputVisible)
            CurrentSecondaryShellPage = SecondaryShellPage.Build;
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
        if (IsTerminalVisible)
            CurrentSecondaryShellPage = SecondaryShellPage.Terminal;
    }

    [RelayCommand]
    private void ToggleInstrumentationDock() => IsInstrumentationDockVisible = !IsInstrumentationDockVisible;

    [RelayCommand]
    private void SetSingleEditorGroup() => EditorGroupCount = 1;

    [RelayCommand]
    private void SetDualEditorGroup() => EditorGroupCount = 2;

    [RelayCommand]
    private void SetTripleEditorGroup() => EditorGroupCount = 3;

    [RelayCommand]
    private void ActivateDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is null)
            Documents.OpenOrActivateDocument(filePath);
        else
            Documents.ActivateDocumentInternal(doc);
    }

    [RelayCommand]
    private void CloseDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        Documents.CloseDocument(filePath);
    }

    [RelayCommand]
    private void TogglePinDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            doc.IsPinned = !doc.IsPinned;
    }

    [RelayCommand]
    private void MoveDocumentToGroup1(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            Documents.MoveDocumentToGroupInternal(doc, 1);
    }

    [RelayCommand]
    private void MoveDocumentToGroup2(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            Documents.MoveDocumentToGroupInternal(doc, 2);
    }

    [RelayCommand]
    private void MoveDocumentToGroup3(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            Documents.MoveDocumentToGroupInternal(doc, 3);
    }

    [RelayCommand(CanExecute = nameof(CanReopenClosedDocument))]
    private void ReopenClosedDocument() => Documents.ReopenLastClosedDocument();

    private bool CanReopenClosedDocument() => Documents.CanReopenClosedDocument();

    [RelayCommand]
    private void ShowSolutionExplorerPanel() => IsSolutionExplorerVisible = true;

    [RelayCommand]
    private void ShowBuildOutputPanel()
    {
        IsBuildOutputVisible = true;
        CurrentSecondaryShellPage = SecondaryShellPage.Build;
    }

    [RelayCommand]
    private void ShowChatPanel()
    {
        IsChatPanelExpanded = true;
        CurrentSecondaryShellPage = SecondaryShellPage.Chat;
    }

    [RelayCommand]
    private void ShowTerminalPanel()
    {
        IsTerminalVisible = true;
        CurrentSecondaryShellPage = SecondaryShellPage.Terminal;
    }

    /// <summary>Переключение режима по id из каталога (<see cref="UiModeCatalog.OrderedModeIds"/>), меню и MCP.</summary>
    [RelayCommand]
    private void SetUiModeById(string? modeId)
    {
        if (string.IsNullOrWhiteSpace(modeId))
            return;
        UiMode = NormalizeUiMode(modeId);
    }

    /// <summary>Alt+1…9: N-й режим в <see cref="UiModeCatalog.OrderedModeIds"/> (0-based).</summary>
    [RelayCommand]
    private void SetUiModeByIndex(object? parameter)
    {
        var idx = ParseUiModeIndex(parameter);
        if (idx < 0)
            return;
        var ids = UiModeCatalog.OrderedModeIds;
        if (idx >= ids.Count)
            return;
        UiMode = ids[idx];
    }

    private static int ParseUiModeIndex(object? parameter)
    {
        return parameter switch
        {
            int i => i,
            long l => l > int.MaxValue ? -1 : (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var j) => j,
            _ => -1,
        };
    }

    [RelayCommand]
    private void CycleUiMode()
    {
        var norm = NormalizeUiMode(UiMode);
        var ids = UiModeCatalog.OrderedModeIds;
        var idx = -1;
        for (var i = 0; i < ids.Count; i++)
        {
            if (string.Equals(ids[i], norm, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }

        if (idx < 0 || ids.Count == 0)
        {
            UiMode = "Focus";
            return;
        }

        UiMode = ids[(idx + 1) % ids.Count];
    }

    [RelayCommand]
    private void SetSafetyL1() => SafetyLevel = "L1";

    [RelayCommand]
    private void SetSafetyL2() => SafetyLevel = "L2";

    [RelayCommand]
    private void SetSafetyL3() => SafetyLevel = "L3";
}