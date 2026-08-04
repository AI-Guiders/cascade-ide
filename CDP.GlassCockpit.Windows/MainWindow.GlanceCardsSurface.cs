#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// SoftOrgan glance pages — FDS uses card-deck instrument (Shared-SSOT), other pages keep chip wrap.
/// </summary>
public partial class MainWindow
{
    void RefreshMfdGlanceCardsVisibility()
    {
        if (MfdGlanceCardsHost is null)
            return;

        var show = IsGlancePage(CurrentMfdPage());
        MfdGlanceCardsHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
            RefreshGlanceCardsBody();
    }

    bool IsGlanceCardsHostActive() =>
        MfdGlanceCardsHost?.Visibility == Visibility.Visible && IsGlancePage(CurrentMfdPage());

    internal void GlanceCardsRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshGlanceCardsBody();

    void RefreshGlanceCardsBody()
    {
        if (GlanceCardsPanel is null)
            return;

        var page = CurrentMfdPage();
        var chips = page switch
        {
            "Events" => GlassGlanceCards.BuildEvents(GlassEventsGlance.ProbeCurrentHabitat()),
            "WorkspaceHealth" when GlassWorkspaceHealthGlance.TryProbe(_session.WorkspaceRoot) is { } status => GlassGlanceCards.BuildWorkspaceHealth(status),
            "EnvironmentReadiness" => GlassGlanceCards.BuildEnvironment(GlassEnvironmentReadinessGlance.ProbeCurrentProcess()),
            "Hypotheses" when GlassHypothesesGlance.TryProbe(_session.WorkspaceRoot) is { } status => GlassGlanceCards.BuildHypotheses(status),
            "FlightDataStorage" or "Fds" => GlassGlanceCards.BuildFds(GlassFdsGlance.Probe(_session.WorkspaceRoot)),
            "DomainBoard" or "Domain" => GlassGlanceCards.BuildDomain(
                GlassDomainBoardGlance.TryProbe(_session.WorkspaceRoot)
                ?? new GlassDomainBoardGlance.Snapshot(0, false, null, 0, false, null, [], "domain · unavailable")),
            "Chat" => GlassGlanceCards.BuildChat(GlassIntercomPresence.ProbeChatMfd()),
            _ => [],
        };

        var deck = page is "FlightDataStorage" or "Fds" or "DomainBoard" or "Domain";
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
            GlanceCardsPanel.Items.Add(deck ? CreateDeckCard(chip) : CreateGlanceChip(chip));

        if (GlanceCardsStatusLabel is not null)
        {
            GlanceCardsStatusLabel.Text = chips.Count == 0
                ? "glance · unavailable"
                : page is "DomainBoard" or "Domain"
                    ? $"domain · card deck · {chips.Count} · {chips[0].Value}"
                    : deck
                        ? $"fds · card deck · {chips.Count} · {chips[0].Value}"
                        : $"{page} · {chips[0].Value}";
        }
    }

    static bool IsGlancePage(string page) =>
        page is "Events" or "WorkspaceHealth" or "EnvironmentReadiness" or "Hypotheses"
            or "FlightDataStorage" or "Fds" or "DomainBoard" or "Domain" or "Chat";

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

    /// <summary>FDS card deck — larger instrument cards (Problems/Plan parity), not wrap chips.</summary>
    static Border CreateDeckCard(GlassGlanceChip chip)
    {
        var (background, foreground) = ToneColors(chip.Tone);
        var accent = chip.Tone switch
        {
            "ok" => "#4A8A4A",
            "warn" => "#D7A33C",
            "bad" => "#E05858",
            _ => "#3A3A3A",
        };

        return new Border
        {
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(12, 10, 12, 10),
            MinHeight = 72,
            CornerRadius = new CornerRadius(4),
            Background = (Brush)new BrushConverter().ConvertFromString(background)!,
            BorderBrush = (Brush)new BrushConverter().ConvertFromString(accent)!,
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
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        FontFamily = new FontFamily("Consolas"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 0),
                        Foreground = (Brush)new BrushConverter().ConvertFromString(foreground)!,
                    },
                },
            },
        };
    }

    static (string Background, string Foreground) ToneColors(string tone) => tone switch
    {
        "ok" => ("#1A2E1A", "#A8E0A8"),
        "warn" => ("#2A2618", "#E0C878"),
        "bad" => ("#2E1A1A", "#E0A8A8"),
        "idle" => ("#1A1A1A", "#888888"),
        _ => ("#121212", "#7A7A7A"),
    };
}
