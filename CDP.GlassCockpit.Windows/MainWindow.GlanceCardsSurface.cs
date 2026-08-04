#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

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
            _ => [],
        };

        GlanceCardsPanel.Items.Clear();
        foreach (var chip in chips)
            GlanceCardsPanel.Items.Add(CreateGlanceChip(chip));

        if (GlanceCardsStatusLabel is not null)
            GlanceCardsStatusLabel.Text = chips.Count > 0
                ? $"{page} · {chips[0].Value}"
                : "glance · unavailable";
    }

    static bool IsGlancePage(string page) => page is "Events" or "WorkspaceHealth" or "EnvironmentReadiness" or "Hypotheses";

    static Border CreateGlanceChip(GlassGlanceChip chip)
    {
        var (background, foreground) = chip.Tone switch
        {
            "ok" => ("#1A2E1A", "#A8E0A8"),
            "warn" => ("#2A2618", "#E0C878"),
            "bad" => ("#2E1A1A", "#E0A8A8"),
            "idle" => ("#1A1A1A", "#888888"),
            _ => ("#121212", "#7A7A7A"),
        };

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
}
