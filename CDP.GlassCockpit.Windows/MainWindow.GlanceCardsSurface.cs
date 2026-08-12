#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CascadeIDE.SoftInstrument;
using CDP.GlassCockpit.Windows.UiKit;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// SoftInstrument glance pages — FDS uses card-deck instrument (Shared-SSOT), other pages keep chip wrap.
/// </summary>
public partial class MainWindow
{
    void RefreshMfdGlanceCardsVisibility()
    {
        if (MfdGlanceCardsHost is null)
            return;

        var show = IsGlancePage(CurrentMfdPage());
        MfdGlanceCardsHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (SoftInstrumentFindBox is not null)
            SoftInstrumentFindBox.Visibility = SoftInstrumentFaceHandbook.IsSoftInstrumentGlancePage(CurrentMfdPage())
                ? Visibility.Visible
                : Visibility.Collapsed;
        if (show)
        {
            // Leaving SoftInstrument pages clears drill; entering HereNext auto-opens PickHere.
            if (!SoftInstrumentFaceHandbook.IsSoftInstrumentGlancePage(CurrentMfdPage()))
                ExitGuideStepsQuiet();
            RefreshGlanceCardsBody();
            if (string.Equals(CurrentMfdPage(), "HereNext", StringComparison.OrdinalIgnoreCase)
                && !_guideStepsMode)
                EnterGuideSituation(OperatorSituationCatalog.PickHere(ProbeHereLocus()).Id, autoHere: true);
            else
                RefreshOperatorGuideChrome();
        }
        else
        {
            ExitGuideStepsQuiet();
        }
    }

    void ExitGuideStepsQuiet()
    {
        _guideStepsMode = false;
        _guideSituation = null;
        _guideStepIndex = 0;
        if (OperatorGuideStepsHost is not null)
            OperatorGuideStepsHost.Visibility = Visibility.Collapsed;
        if (GlanceCardsScroll is not null)
            GlanceCardsScroll.Visibility = Visibility.Visible;
        if (OperatorHereLine is not null)
            OperatorHereLine.Visibility = Visibility.Collapsed;
    }

    bool IsGlanceCardsHostActive() =>
        MfdGlanceCardsHost?.Visibility == Visibility.Visible && IsGlancePage(CurrentMfdPage());

    internal void GlanceCardsRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshGlanceCardsBody();

    internal void SoftInstrumentFindBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!SoftInstrumentFaceHandbook.IsSoftInstrumentGlancePage(CurrentMfdPage()))
            return;
        if (_guideStepsMode)
            ExitGuideStepsQuiet();
        RefreshGlanceCardsBody();
    }

    void RefreshGlanceCardsBody()
    {
        if (GlanceCardsPanel is null)
            return;

        var page = CurrentMfdPage();
        var soft = SoftInstrumentFaceHandbook.IsSoftInstrumentGlancePage(page);
        var filter = soft ? SoftInstrumentFindBox?.Text : null;
        var chips = page switch
        {
            "Events" => GlassGlanceCards.BuildEvents(GlassEventsGlance.ProbeCurrentHabitat()),
            // Climb when session root empty — never paint glance · unavailable (DomainBoard parity).
            "WorkspaceHealth" => GlassGlanceCards.BuildWorkspaceHealth(
                GlassWorkspaceHealthGlance.TryProbe(_session.WorkspaceRoot)
                ?? new GlassWorkspaceHealthGlance.WorkspaceFsStatus("—", false, false, null, false)),
            "EnvironmentReadiness" => GlassGlanceCards.BuildEnvironment(GlassEnvironmentReadinessGlance.ProbeCurrentProcess()),
            "Hypotheses" => GlassGlanceCards.BuildHypotheses(
                GlassHypothesesGlance.TryProbe(_session.WorkspaceRoot)
                ?? new GlassHypothesesGlance.HypothesesFsStatus(
                    GlassHypothesesGlance.RelativePath, false, 0, 0, 0, 0, null)),
            "FlightDataStorage" or "Fds" => GlassGlanceCards.BuildFds(GlassFdsGlance.Probe(_session.WorkspaceRoot)),
            "DomainBoard" or "Domain" => GlassGlanceCards.BuildDomain(
                GlassDomainBoardGlance.TryProbe(_session.WorkspaceRoot)
                ?? new GlassDomainBoardGlance.Snapshot(0, false, null, 0, false, null, [], "domain · unavailable")),
            "Chat" => GlassGlanceCards.BuildChat(GlassIntercomPresence.ProbeChatMfd()),
            "QRH" or "ECL" or "Alert" or "HereNext" => SoftInstrumentFaceHandbook.ChipsFor(
                SoftInstrumentFaceHandbook.OrganIdFromMfdPage(page), filter),
            _ => [],
        };

        var deck = page is "FlightDataStorage" or "Fds" or "DomainBoard" or "Domain"
            or "QRH" or "ECL" or "Alert" or "HereNext";
        if (deck)
        {
            var factory = new FrameworkElementFactory(typeof(UniformGrid));
            factory.SetValue(UniformGrid.ColumnsProperty, 3);
            GlanceCardsPanel.ItemsPanel = new ItemsPanelTemplate(factory);
        }
        else
        {
            GlanceCardsPanel.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)));
        }

        GlanceCardsPanel.Items.Clear();
        foreach (var chip in chips)
        {
            GlanceCardsPanel.Items.Add(soft
                ? CreateSoftInstrumentSituationCard(chip)
                : deck
                    ? CreateDeckCard(chip)
                    : CreateGlanceChip(chip));
        }

        if (GlanceCardsStatusLabel is not null && !_guideStepsMode)
        {
            GlanceCardsStatusLabel.Text = chips.Count == 0
                ? soft
                    ? $"soft · {page} · no match"
                    : "glance · unavailable"
                : soft
                    ? page is "HereNext"
                        ? $"here · situations · {chips.Count} · click → steps"
                        : $"soft · {page} · situations · {chips.Count} · click → steps"
                    : page is "DomainBoard" or "Domain"
                        ? $"domain · card deck · {chips.Count} · {chips[0].Value}"
                        : deck
                            ? $"fds · card deck · {chips.Count} · {chips[0].Value}"
                            : $"{page} · {chips[0].Value}";
        }

        RefreshOperatorGuideChrome();
    }

    static bool IsGlancePage(string page) =>
        page is "Events" or "WorkspaceHealth" or "EnvironmentReadiness" or "Hypotheses"
            or "FlightDataStorage" or "Fds" or "DomainBoard" or "Domain" or "Chat"
            or "QRH" or "ECL" or "Alert" or "HereNext";

    static Border CreateGlanceChip(GlassGlanceChip chip)
    {
        var (background, foreground) = ToneColors(chip.Tone);
        return new Border
        {
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(10, 7, 12, 8),
            CornerRadius = new CornerRadius(5),
            Background = (Brush)new BrushConverter().ConvertFromString(background)!,
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = chip.Label,
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")),
                    },
                    new TextBlock
                    {
                        Text = chip.Value,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (Brush)new BrushConverter().ConvertFromString(foreground)!,
                    },
                },
            },
        };
    }

    /// <summary>FDS card deck — UiKit GlassDeckCard (tone tokens), not inline Border factory.</summary>
    static FrameworkElement CreateDeckCard(GlassGlanceChip chip) =>
        GlassDeckCard.FromChip(chip);

    static (string Background, string Foreground) ToneColors(string tone) => tone switch
    {
        "ok" => ("#1A2E1A", "#A8E0A8"),
        "warn" => ("#2A2618", "#E0C878"),
        "bad" => ("#2E1A1A", "#E0A8A8"),
        "idle" => ("#1A1A1A", "#888888"),
        _ => ("#121212", "#7A7A7A"),
    };
}
