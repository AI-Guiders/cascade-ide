using Avalonia.Threading;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    private static string GetWorkspacePath(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return "";
        var p = Path.GetFullPath(solutionPath.Trim());
        return File.Exists(p) ? Path.GetDirectoryName(p) ?? "" : p;
    }

    internal static string NormalizeUiMode(string? mode) => UiChromeViewModel.NormalizeUiMode(mode);

    private Task RefreshGitSummaryAsync() => Chrome.RefreshGitSummaryAsync(RunGitCommandAsync);

    private void InitializeAgentUiDefaults()
    {
        // Keep operation/trace feeds empty until real runtime events arrive.
        // This avoids demo-like placeholder content in production UI.
    }

    private void RegisterAgentFeedHandlers()
    {
        FocusPlanItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFocusPlanItems));
    }

    private void ApplyUiModeLayout(string mode, bool persist)
    {
        var normalized = NormalizeUiMode(mode);
        switch (normalized)
        {
            case "Focus":
                IsSolutionExplorerVisible = true;
                IsBuildOutputVisible = false;
                IsTerminalVisible = false;
                IsChatPanelExpanded = true;
                EditorGroupCount = 1;
                break;
            case "Power":
                IsSolutionExplorerVisible = true;
                IsBuildOutputVisible = true;
                IsTerminalVisible = true;
                IsChatPanelExpanded = true;
                EditorGroupCount = 3;
                break;
            default:
                IsSolutionExplorerVisible = true;
                // Balanced: терминал и журнал сборки видны по умолчанию (иначе TabControl часто остаётся на скрытой вкладке 0 — «пустая» панель).
                IsBuildOutputVisible = true;
                IsTerminalVisible = true;
                IsChatPanelExpanded = true;
                EditorGroupCount = 2;
                break;
        }

        CoerceBottomPanelTabToVisible();
        // Power cockpit: сразу вкладка «Терминал» (консоль + сборка рядом), а не «События».
        if (string.Equals(normalized, "Power", StringComparison.OrdinalIgnoreCase) && IsTerminalVisible)
            BottomPanelTabIndex = 0;

        // Mode-specific visual identity: Power gets cosmic palette; Focus/Balanced keep calmer dark themes.
        _ = normalized switch
        {
            "Power" => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetPowerCockpitConceptThemeJson()),
            "Focus" => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetDarkThemeJson()),
            _ => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetCursorLikeThemeJson())
        };

        if (!persist)
            return;

        _settings.UiMode = normalized;
        _settings.SolutionExplorerVisible = IsSolutionExplorerVisible;
        _settings.TerminalVisible = IsTerminalVisible;
        SaveSettingsIfChanged();
    }

    /// <summary>Вкладки 0–6: терминал, сборка, problems, Git, события, тесты, отладка.</summary>
    private bool IsBottomPanelTabVisible(int index) => index switch
    {
        0 => IsTerminalVisible,
        1 => IsBuildOutputVisible,
        2 => IsProblemsPanelVisible,
        3 => IsGitPanelVisible,
        4 or 5 or 6 => ShowInstrumentationTabs,
        _ => false,
    };

    private int GetFirstVisibleBottomPanelTabIndex()
    {
        if (IsTerminalVisible)
            return 0;
        if (IsBuildOutputVisible)
            return 1;
        if (IsProblemsPanelVisible)
            return 2;
        if (IsGitPanelVisible)
            return 3;
        if (ShowInstrumentationTabs)
            return 4;
        return 2;
    }

    /// <summary>Если выбрана скрытая вкладка, TabControl в Avalonia показывает пустую область — переключаем на первую видимую.</summary>
    private void CoerceBottomPanelTabToVisible()
    {
        if (IsBottomPanelTabVisible(BottomPanelTabIndex))
            return;
        BottomPanelTabIndex = GetFirstVisibleBottomPanelTabIndex();
    }

    private string GetWorkspacePath() => GetWorkspacePath(Workspace.SolutionPath);

    partial void OnSelectedOllamaModelChanged(string? value)
    {
        ChatPanel.RefreshSendChatCommandState();
        if (value == InstallNewSentinel)
        {
            SelectedModelDetails = "";
            return;
        }
        if (!string.IsNullOrEmpty(value))
        {
            LastSelectedRealModel = value;
            _settings.PreferredOllamaModel = value;
            SaveSettingsIfChanged();
            _ = LoadModelDetailsAsync(value);
        }
        else
            SelectedModelDetails = "";
    }

    private async Task LoadModelDetailsAsync(string modelName)
    {
        try
        {
            var details = await _ollama.GetModelDetailsAsync(modelName).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (SelectedOllamaModel == modelName)
                    SelectedModelDetails = details?.ToShortString() ?? "";
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (SelectedOllamaModel == modelName)
                    SelectedModelDetails = "";
            });
        }
    }

    partial void OnUiModeChanged(string value)
    {
        var normalized = NormalizeUiMode(value);
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            UiMode = normalized;
            return;
        }

        ApplyUiModeLayout(normalized, persist: true);
        Autonomous.NotifyHostPowerContextChanged();
        if (string.Equals(normalized, "Power", StringComparison.OrdinalIgnoreCase))
            Dispatcher.UIThread.Post(RefreshWorkspaceSnapshotCore, DispatcherPriority.Background);

        Chrome.NotifyUiModeChangedForBloom(normalized, IsPowerMode, IsFocusMode);
    }

    public async Task RefreshOllamaAsync()
    {
        OllamaStatus = "Проверка Ollama…";
        OllamaAvailable = await _ollama.IsAvailableAsync();
        if (OllamaAvailable)
        {
            var names = await _ollama.GetModelNamesAsync();
            OllamaModels.Clear();
            OllamaModelChoices.Clear();
            foreach (var n in names)
            {
                OllamaModels.Add(n);
                OllamaModelChoices.Add(n);
            }
            OllamaModelChoices.Add(InstallNewSentinel);
            var preferred = _settings.PreferredOllamaModel?.Trim();
            SelectedOllamaModel = !string.IsNullOrEmpty(preferred) && OllamaModels.Contains(preferred)
                ? preferred
                : OllamaModels.FirstOrDefault();
            if (LastSelectedRealModel is null && OllamaModels.Count > 0)
                LastSelectedRealModel = OllamaModels[0];
            OllamaStatus = names.Count > 0
                ? $"Ollama: {names.Count} моделей"
                : "Ollama запущен, моделей нет (ollama pull <model>)";
        }
        else
        {
            OllamaModels.Clear();
            OllamaStatus = "Ollama недоступен (localhost:11434). Установи и запусти Ollama.";
        }
    }
}
