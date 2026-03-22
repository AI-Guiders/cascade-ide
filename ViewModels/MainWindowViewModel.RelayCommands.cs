using System.Globalization;
using System.Threading.Tasks;
using CascadeIDE.Lang;
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
            BottomPanelTabIndex = 1;
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
        if (IsTerminalVisible)
            BottomPanelTabIndex = 0;
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
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is null)
            OpenOrActivateDocument(filePath);
        else
            ActivateDocumentInternal(doc);
    }

    [RelayCommand]
    private void CloseDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is null)
            return;

        var index = OpenDocuments.IndexOf(doc);
        GetGroupCollection(doc.GroupIndex).Remove(doc);
        OpenDocuments.Remove(doc);
        var dockDoc = DockDocuments
            .OfType<DockDocumentViewModel>()
            .FirstOrDefault(d => string.Equals(d.Doc.FilePath, doc.FilePath, StringComparison.OrdinalIgnoreCase));
        if (dockDoc is not null)
            DockDocuments.Remove(dockDoc);
        _recentlyClosedDocumentPaths.Push(doc.FilePath);
        _recentlyClosedDocumentCount = _recentlyClosedDocumentPaths.Count;
        ReopenClosedDocumentCommand.NotifyCanExecuteChanged();

        if (OpenDocuments.Count == 0)
        {
            SelectedDocument = null;
            SelectedDocumentGroup2 = null;
            SelectedDocumentGroup3 = null;
            return;
        }

        SelectedDocument = Group1Documents.FirstOrDefault();
        SelectedDocumentGroup2 = Group2Documents.FirstOrDefault();
        SelectedDocumentGroup3 = Group3Documents.FirstOrDefault();

        // Ensure DockActiveDocument always points to a still-open dockable.
        var next =
            SelectedDocument ??
            SelectedDocumentGroup2 ??
            SelectedDocumentGroup3 ??
            OpenDocuments[Math.Clamp(index, 0, OpenDocuments.Count - 1)];

        ActivateDocumentInternal(next);
        RebuildAndReinitDockLayout();
    }

    [RelayCommand]
    private void TogglePinDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            doc.IsPinned = !doc.IsPinned;
    }

    [RelayCommand]
    private void MoveDocumentToGroup1(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            MoveDocumentToGroupInternal(doc, 1);
    }

    [RelayCommand]
    private void MoveDocumentToGroup2(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            MoveDocumentToGroupInternal(doc, 2);
    }

    [RelayCommand]
    private void MoveDocumentToGroup3(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            MoveDocumentToGroupInternal(doc, 3);
    }

    [RelayCommand(CanExecute = nameof(CanReopenClosedDocument))]
    private void ReopenClosedDocument()
    {
        if (_recentlyClosedDocumentPaths.Count == 0)
            return;
        var path = _recentlyClosedDocumentPaths.Pop();
        _recentlyClosedDocumentCount = _recentlyClosedDocumentPaths.Count;
        ReopenClosedDocumentCommand.NotifyCanExecuteChanged();
        OpenOrActivateDocument(path);
    }

    private bool CanReopenClosedDocument() => _recentlyClosedDocumentCount > 0;

    [RelayCommand]
    private void ShowSolutionExplorerPanel() => IsSolutionExplorerVisible = true;

    [RelayCommand]
    private void ShowBuildOutputPanel()
    {
        IsBuildOutputVisible = true;
        BottomPanelTabIndex = 1;
    }

    [RelayCommand]
    private void ShowChatPanel() => IsChatPanelExpanded = true;

    [RelayCommand]
    private void ShowTerminalPanel()
    {
        IsTerminalVisible = true;
        BottomPanelTabIndex = 0;
    }

    [RelayCommand]
    private void SetFocusMode()
    {
        UiMode = "Focus";
    }

    [RelayCommand]
    private void SetBalancedMode()
    {
        UiMode = "Balanced";
    }

    [RelayCommand]
    private void SetPowerMode()
    {
        UiMode = "Power";
    }

    [RelayCommand]
    private void CycleUiMode()
    {
        UiMode = UiMode switch
        {
            "Focus" => "Balanced",
            "Balanced" => "Power",
            _ => "Focus"
        };
    }

    [RelayCommand]
    private void SetSafetyL1() => SafetyLevel = "L1";

    [RelayCommand]
    private void SetSafetyL2() => SafetyLevel = "L2";

    [RelayCommand]
    private void SetSafetyL3() => SafetyLevel = "L3";
}