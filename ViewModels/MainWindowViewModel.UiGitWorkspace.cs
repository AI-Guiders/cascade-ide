using Avalonia.Threading;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Git + workspace UI.</summary>
public partial class MainWindowViewModel
{
    internal static string NormalizeUiMode(string? mode) => UiChromeViewModel.NormalizeUiMode(mode);

    private Task RefreshGitSummaryAsync() => Chrome.RefreshGitSummaryAsync(RunGitCommandAsync);

    /// <summary>Git для полоски Workspace Health (<see cref="RefreshGitSummaryAsync"/>); MCP git — <see cref="IdeMcpGitWorkspaceSession"/>.</summary>
    private async Task<(bool Success, int ExitCode, string Output)> RunGitCommandAsync(IReadOnlyList<string> args)
    {
        var workspace = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
            return (false, -1, IdeMcpGitOrchestrator.WorkspaceUnavailableMessage());
        return await _gitRunner.RunAsync(args, workspace).ConfigureAwait(false);
    }

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

        IsPfdRegionExpanded = spec.PfdRegionExpanded;
        IsBuildOutputVisible = spec.BuildOutputVisible;
        IsTerminalVisible = spec.TerminalVisible;
        IsMfdRegionExpanded = spec.MfdRegionExpanded;
        EditorGroupCount = spec.EditorGroupCount;
        IsInstrumentationDockVisible = spec.InstrumentationDockVisible;

        CoerceMfdShellPageToAllowed();
        if (spec.SelectTerminalTabWhenTerminalShown && IsTerminalVisible)
            CurrentMfdShellPage = MfdShellPage.Terminal;

        _ = spec.ThemeSlot switch
        {
            UiModeThemeSlot.PowerCockpit => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetPowerCockpitConceptThemeJson()),
            UiModeThemeSlot.Dark => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetDarkThemeJson()),
            UiModeThemeSlot.CursorLike => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetCursorLikeThemeJson()),
            _ => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetCursorLikeThemeJson())
        };

        if (!persist)
            return;

        _settings.Workspace.Mode = normalized;
        _settings.Workspace.PfdExpanded = IsPfdRegionExpanded;
        _settings.Workspace.ShowTerminal = IsTerminalVisible;
        SaveSettingsIfChanged();
    }

    private string GetWorkspacePath() => WorkspaceDirectoryFromSolutionPath.Resolve(Workspace.SolutionPath);

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
            _settings.Ai.Local.Ollama.Model = value;
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
            var preferred = _settings.Ai.Local.Ollama.Model?.Trim();
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
