using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.ViewModels;

/// <summary>Live glass/topology patch from agent desk latch → settings.toml display fields.</summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Live apply operator presentation topology (agent desk wire / settings).
    /// Persists <c>display.screens.topology</c>, reparses layout flags, notifies MainGrid.
    /// Host TopLevel open/close may need a follow-up if screen count changes.
    /// </summary>
    public bool ApplyPresentationTopology(string topology) =>
        ApplyPresentationGlassPatch(topology: topology);

    /// <summary>
    /// Live apply operator glass patch from agent desk latch (topology / tier / instruments / mfd page).
    /// Persists user <c>settings.toml</c> display fields; does not mutate repo <c>workspace.toml</c>.
    /// </summary>
    public bool ApplyPresentationGlassPatch(
        string? topology = null,
        string? tier = null,
        IReadOnlyDictionary<string, string>? instruments = null,
        string? mfdPage = null)
    {
        var dirty = false;

        if (!string.IsNullOrWhiteSpace(topology))
        {
            var next = topology.Trim();
            if (!string.Equals(_settings.Display.Screens.Topology?.Trim(), next, StringComparison.Ordinal))
            {
                _settings.Display.Screens.Topology = next;
                dirty = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(tier))
        {
            var nextTier = tier.Trim().ToLowerInvariant();
            if (!string.Equals(_settings.Display.Presentation.Tier?.Trim(), nextTier, StringComparison.OrdinalIgnoreCase))
            {
                _settings.Display.Presentation.Tier = nextTier;
                dirty = true;
            }
        }

        if (instruments is { Count: > 0 })
        {
            _settings.Display.Instruments ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in instruments)
            {
                if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v))
                    continue;
                var key = k.Trim();
                var val = v.Trim();
                if (_settings.Display.Instruments.TryGetValue(key, out var cur)
                    && string.Equals(cur, val, StringComparison.OrdinalIgnoreCase))
                    continue;
                _settings.Display.Instruments[key] = val;
                dirty = true;
            }
        }

        if (dirty)
            SettingsService.Save(_settings);

        ReparsePresentationFromSettings();
        NotifyPresentationLayoutChanged();

        if (!string.IsNullOrWhiteSpace(mfdPage)
            && Enum.TryParse<MfdShellPage>(mfdPage.Trim(), ignoreCase: true, out var page))
        {
            TryNavigateToMfdShellPage(page);
        }

        return !string.IsNullOrWhiteSpace(topology)
            ? _presentationParse.IsSuccess
            : true;
    }

    void ReparsePresentationFromSettings()
    {
        var pg = _settings.GetEffectivePresentationGrammar();
        var grammar = PresentationGrammarTokens.FromSettings(
            pg.Brackets,
            pg.BetweenScreens,
            pg.BetweenZones,
            pg.Pfd,
            pg.Forward,
            pg.Mfd);
        _presentationParse = PresentationParser.Parse(_settings.GetEffectivePresentationLine(), grammar);
        var topologyFlags = PresentationTopologyResolver.ResolveFlags(_presentationParse);
        _presentationDedicatedMfdSecondScreen = topologyFlags.DedicatedMfdSecondScreen;
        _presentationTripleOneAnchorPerZone = topologyFlags.TripleOneAnchorPerZone;
        _presentationMfdHostTopology = topologyFlags.MfdHostTopology;
        _presentationPmForwardTwoScreen = topologyFlags.PmForwardTwoScreen;
        _presentationPmHostTopology = topologyFlags.PmHostTopology;
        InitializePresentationTier();
    }

    void NotifyPresentationLayoutChanged()
    {
        OnPropertyChanged(nameof(EffectivePresentationLine));
        OnPropertyChanged(nameof(PresentationParse));
        OnPropertyChanged(nameof(MainGridColumnDefinitions));
        OnPropertyChanged(nameof(MainGridLayoutFrame));
        OnPropertyChanged(nameof(PresentationRequestsMainWindowMaximized));
        OnPropertyChanged(nameof(PresentationRequestsDedicatedMfdSecondScreen));
        OnPropertyChanged(nameof(PresentationRequestsTriplePfdForwardMfd));
        OnPropertyChanged(nameof(PresentationRequestsPfdHostWindow));
        OnPropertyChanged(nameof(PresentationRequestsMfdHostWindow));
        OnPropertyChanged(nameof(PresentationRequestsPmSplitHostWindow));
        OnPropertyChanged(nameof(PresentationRequestsPmSplitMainWindowScreenPlacement));
        OnPropertyChanged(nameof(MfdHostPresentationScreenIndex));
        OnPropertyChanged(nameof(PfdHostPresentationScreenIndex));
        OnPropertyChanged(nameof(PmSplitHostPresentationScreenIndex));
        OnPropertyChanged(nameof(PmSplitHostColumnDefinitions));
        OnPropertyChanged(nameof(MainWindowPresentationScreenIndex));
        NotifyDockedInstrumentSlotBindings();
    }
}
