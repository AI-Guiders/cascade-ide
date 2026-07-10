using Avalonia.Controls;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Presentation;
using CascadeIDE.Views;

namespace CascadeIDE.ViewModels;

/// <summary>Presentation tier compact vs cockpit (ADR 0171).</summary>
public partial class MainWindowViewModel
{
    private PresentationMonitorSnapshot _presentationMonitorSnapshot = PresentationMonitorSnapshot.SingleFallback;
    private PresentationTierKind _effectivePresentationTier = PresentationTierKind.Compact;

    PresentationTierKind IMainWindowHostSurfaceInput.EffectivePresentationTier => _effectivePresentationTier;

    public PresentationTierKind EffectivePresentationTier => _effectivePresentationTier;

    public bool IsCompactPresentationTier => _effectivePresentationTier == PresentationTierKind.Compact;

    public bool IsCockpitPresentationTier => _effectivePresentationTier == PresentationTierKind.Cockpit;

    public bool UsesUltrawideCockpitLayout =>
        IsCockpitPresentationTier
        && _presentationMonitorSnapshot.PhysicalScreenCount == 1
        && PresentationTierResolver.IsUltrawideCockpitCapable(
            _settings.Display.Presentation,
            _presentationMonitorSnapshot);

    public bool ShouldShowPresentationTierFirstRunWizard =>
        !_settings.Display.Presentation.TierFirstRunCompleted
        && string.Equals(
            _settings.Display.Presentation.Tier?.Trim(),
            PresentationTierKindExtensions.AutoValue,
            StringComparison.OrdinalIgnoreCase);

    private void InitializePresentationTier()
    {
        _presentationMonitorSnapshot = PresentationMonitorProbe.Capture();
        _effectivePresentationTier = PresentationTierResolver.Resolve(
            _settings.Display.Presentation,
            _presentationParse,
            _presentationMonitorSnapshot);

        ApplyPresentationTierLayoutDefaults();
        OnPropertyChanged(nameof(EffectivePresentationTier));
        OnPropertyChanged(nameof(IsCompactPresentationTier));
        OnPropertyChanged(nameof(IsCockpitPresentationTier));
        OnPropertyChanged(nameof(UsesUltrawideCockpitLayout));
        OnPropertyChanged(nameof(OpenMfdHostWindowOnStartup));
        OnPropertyChanged(nameof(OpenPfdHostWindowOnStartup));
        OnPropertyChanged(nameof(MainGridColumnDefinitions));
        OnPropertyChanged(nameof(IsPfdColumnVisible));
        OnPropertyChanged(nameof(IsMfdColumnVisible));
    }

    internal void ApplyPresentationTierLayoutDefaults()
    {
        if (!IsCompactPresentationTier)
            return;

        ApplyPfdRegionExpanded(false);
        if (!IsMfdRegionExpanded)
            ApplyMfdRegionExpanded(true);

        if (PrimaryWorkSurface == PrimaryWorkSurfaceKind.Intercom)
        {
            _settings.Workspace.PrimaryWorkSurface = PrimaryWorkSurfaceKind.Editor.ToTomlValue();
            OnPropertyChanged(nameof(PrimaryWorkSurface));
            OnPropertyChanged(nameof(IsForwardEditorHostVisible));
            OnPropertyChanged(nameof(IsForwardIntercomHostVisible));
        }

        ChatPanel.IsForwardIntercomLayout = false;
        TryNavigateToMfdShellPage(MfdShellPage.Chat);
    }

    public async Task TryCompletePresentationTierFirstRunAsync(Window? owner)
    {
        if (!ShouldShowPresentationTierFirstRunWizard || owner is null)
            return;

        var recommended = PresentationTierResolver.RecommendForFirstRun(
            _settings.Display.Presentation,
            _presentationParse,
            _presentationMonitorSnapshot);

        var choice = await PresentationTierFirstRunDialog.ShowAsync(
            owner,
            _presentationMonitorSnapshot,
            recommended).ConfigureAwait(true);

        _settings.Display.Presentation.TierFirstRunCompleted = true;
        if (choice is PresentationTierKind tier)
            _settings.Display.Presentation.Tier = tier.ToTomlValue();

        SaveSettingsIfChanged();
    }
}
