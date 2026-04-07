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
        var spec = UiModeCatalog.GetSpec(normalized);

        IsSolutionExplorerVisible = spec.SolutionExplorerVisible;
        IsBuildOutputVisible = spec.BuildOutputVisible;
        IsTerminalVisible = spec.TerminalVisible;
        IsChatPanelExpanded = spec.ChatPanelExpanded;
        EditorGroupCount = spec.EditorGroupCount;
        IsInstrumentationDockVisible = spec.InstrumentationDockVisible;

        CoerceMfdShellTabToVisible();
        if (spec.SelectTerminalTabWhenTerminalShown && IsTerminalVisible)
            MfdShellTabIndex = MfdShellTabTerminalIndex;

        _ = spec.ThemeSlot switch
        {
            UiModeThemeSlot.PowerCockpit => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetPowerCockpitConceptThemeJson()),
            UiModeThemeSlot.Dark => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetDarkThemeJson()),
            UiModeThemeSlot.CursorLike => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetCursorLikeThemeJson()),
            _ => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetCursorLikeThemeJson())
        };

        if (!persist)
            return;

        _settings.UiMode = normalized;
        _settings.SolutionExplorerVisible = IsSolutionExplorerVisible;
        _settings.TerminalVisible = IsTerminalVisible;
        SaveSettingsIfChanged();
    }

    /// <summary>Вкладки MFD: см. <see cref="MfdShellTabWorkspaceIndex"/> … <see cref="MfdShellTabDebugStackIndex"/>.</summary>
    private bool IsMfdShellTabVisible(int index) => index switch
    {
        MfdShellTabWorkspaceIndex => ShowTelemetryMfdPage,
        MfdShellTabChatIndex => true,
        MfdShellTabTerminalIndex => IsTerminalVisible,
        MfdShellTabBuildIndex => IsBuildOutputVisible,
        MfdShellTabProblemsIndex => IsProblemsPanelVisible,
        MfdShellTabGitIndex => IsGitPanelVisible,
        MfdShellTabEventsIndex or MfdShellTabTestsIndex or MfdShellTabDebugStackIndex => InstrumentationTabs,
        MfdShellTabHypothesesIndex => HypothesesTab,
        _ => false,
    };

    private int GetFirstVisibleMfdShellTabIndex()
    {
        for (var i = 0; i <= MfdShellTabDebugStackIndex; i++)
        {
            if (IsMfdShellTabVisible(i))
                return i;
        }

        return MfdShellTabChatIndex;
    }

    /// <summary>Если выбрана скрытая вкладка, TabControl в Avalonia показывает пустую область — переключаем на первую видимую.</summary>
    private void CoerceMfdShellTabToVisible()
    {
        if (IsMfdShellTabVisible(MfdShellTabIndex))
            return;
        MfdShellTabIndex = GetFirstVisibleMfdShellTabIndex();
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
            await UiScheduler.Default.InvokeAsync(() =>
            {
                if (SelectedOllamaModel == modelName)
                    SelectedModelDetails = details?.ToShortString() ?? "";
            });
        }
        catch
        {
            await UiScheduler.Default.InvokeAsync(() =>
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
            UiScheduler.Default.Post(RefreshWorkspaceSnapshotCore, DispatcherPriority.Background);

        Chrome.NotifyUiModeChangedForBloom(normalized);
        RefreshCommandPaletteIfOpen();
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
