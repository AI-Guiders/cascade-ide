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

    /// <summary>ADR 0063: компактный deck — ряд ламп (короткие подписи в <see cref="AnnunciatorLampItem.LampShortLabel"/>; примитив <see cref="DeckPrimitiveKind.Lamp"/>).</summary>
    public InstrumentDeckDescriptor EnvironmentReadinessCompactDeck => EnvironmentReadinessInstrumentDeck.CompactLampStrip;

    /// <summary>ADR 0063: текстовый deck — те же ячейки, развёрнутая детализация в списке ниже.</summary>
    public InstrumentDeckDescriptor EnvironmentReadinessTextualDeck => EnvironmentReadinessInstrumentDeck.TextualDetail;

    partial void OnCurrentSecondaryShellPageChanged(SecondaryShellPage value)
    {
        if (value == SecondaryShellPage.EnvironmentReadiness)
            _ = RefreshEnvironmentReadinessAsync();
    }

    /// <summary>Уйти со страницы готовности окружения на первую другую разрешённую страницу вторичного контура.</summary>
    [RelayCommand]
    private void CloseEnvironmentReadinessPage()
    {
        if (CurrentSecondaryShellPage != SecondaryShellPage.EnvironmentReadiness)
            return;
        foreach (var p in SecondaryShellPageOrder)
        {
            if (p == SecondaryShellPage.EnvironmentReadiness)
                continue;
            if (IsSecondaryShellPageAllowed(p))
            {
                CurrentSecondaryShellPage = p;
                return;
            }
        }

        CurrentSecondaryShellPage = SecondaryShellPage.Chat;
    }

    [RelayCommand]
    private async Task RefreshEnvironmentReadinessAsync()
    {
        var rows = await _environmentReadinessChannel.Build(new EnvironmentReadinessChannelContext(
                _settings,
                Workspace.SolutionPath,
                _csharpLspHost,
                _markdownLspHost))
            .ConfigureAwait(false);

        await UiScheduler.Default.InvokeAsync(() =>
        {
            _environmentReadinessSurfaceCompositor.Compose(
                EnvironmentReadinessItems,
                rows,
                new EnvironmentReadinessSurfaceDecision(Enabled: true));
        });
    }
}
