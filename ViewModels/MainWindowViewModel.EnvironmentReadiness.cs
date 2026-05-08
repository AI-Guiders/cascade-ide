using System.Collections.ObjectModel;
using CascadeIDE.Cockpit;
using CascadeIDE.Cockpit.Channels.EnvironmentReadiness;
using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Снимок «готовность окружения» (ADR 0023), отдельно от Workspace Health.</summary>
public partial class MainWindowViewModel
{
    public ObservableCollection<AnnunciatorLampItem> EnvironmentReadinessItems { get; } = [];

    /// <summary>ADR 0063: семантический deck «компакт» (сетка ламп в метаданных); в UI узкая проекция — карточки с глифом (ADR 0068).</summary>
    public InstrumentDeckDescriptor EnvironmentReadinessCompactDeck => EnvironmentReadinessInstrumentDeck.CompactLampStrip;

    /// <summary>ADR 0063: тот же порядок ячеек; широкая проекция — таблица с лампой Korry в первой колонке (ADR 0068).</summary>
    public InstrumentDeckDescriptor EnvironmentReadinessTextualDeck => EnvironmentReadinessInstrumentDeck.TextualDetail;

    partial void OnCurrentMfdShellPageChanged(MfdShellPage value)
    {
        // Прямые присвоения CurrentMfdShellPage (сборка, отладка, …) обходят TryNavigateToMfdShellPage — не оставляем запрещённую страницу (в т.ч. SE в Mfd при дереве в PFD).
        if (!IsMfdShellPageAllowed(value))
        {
            CoerceMfdShellPageToAllowed();
            return;
        }

        if (value == MfdShellPage.EnvironmentReadiness)
            _ = RefreshEnvironmentReadinessAsync();

        if (value == MfdShellPage.HybridIndex)
        {
            EnsureHybridIndexSubscription();
            NotifyHybridIndexSnapshotChanged();
        }

        if (value == MfdShellPage.RelatedFiles)
            ScheduleWorkspaceNavigationMapRefresh();
    }

    /// <summary>Уйти со страницы готовности окружения на первую другую разрешённую страницу оболочки Mfd.</summary>
    [RelayCommand]
    private void CloseEnvironmentReadinessPage()
    {
        if (CurrentMfdShellPage != MfdShellPage.EnvironmentReadiness)
            return;
        foreach (var p in MfdShellPageOrder)
        {
            if (p == MfdShellPage.EnvironmentReadiness)
                continue;
            if (IsMfdShellPageAllowed(p))
            {
                CurrentMfdShellPage = p;
                return;
            }
        }

        CurrentMfdShellPage = MfdShellPage.Chat;
    }

    [RelayCommand]
    private async Task RefreshEnvironmentReadinessAsync()
    {
        await EnvironmentReadinessRefreshOrchestrator.RunAsync(
            _environmentReadinessChannel,
            _environmentReadinessSurfaceCompositor,
            EnvironmentReadinessItems,
            new EnvironmentReadinessChannelContext(
                _settings,
                Workspace.SolutionPath,
                Lsp: CaptureIdeHostLspState(),
                IsMcpStdioHost: IsMcpServerMode,
                ActiveAiProvider: ActiveAiProvider));
    }
}
