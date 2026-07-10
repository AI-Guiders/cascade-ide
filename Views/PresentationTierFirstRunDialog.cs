using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Views;

/// <summary>First-run wizard: recommend compact vs cockpit (ADR 0171 P1).</summary>
internal static class PresentationTierFirstRunDialog
{
    public static async Task<PresentationTierKind?> ShowAsync(
        Window owner,
        PresentationMonitorSnapshot monitors,
        PresentationTierKind recommended)
    {
        var dialog = new Window
        {
            Title = "Раскладка CascadeIDE",
            Width = 520,
            Height = 280,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        PresentationTierKind? result = null;

        var summary = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
            Text =
                $"Обнаружено мониторов: {monitors.PhysicalScreenCount}, ширина основного: {monitors.PrimaryWorkingAreaWidthPx}px.\n\n"
                + (recommended == PresentationTierKind.Compact
                    ? "Рекомендуем обычную IDE (редактор в центре, Intercom справа) — без трёхзонной кабины на малом экране."
                    : "Рекомендуем cockpit (P/F/M) — достаточно экранов или ultrawide для spatial scan."),
        };

        var compactBtn = new Button
        {
            Content = "Обычная IDE (compact)",
            MinWidth = 160,
            IsDefault = recommended == PresentationTierKind.Compact,
        };
        var cockpitBtn = new Button
        {
            Content = "Cockpit (P/F/M)",
            MinWidth = 160,
            IsDefault = recommended == PresentationTierKind.Cockpit,
        };
        var laterBtn = new Button { Content = "Позже (auto)", MinWidth = 120, IsCancel = true };

        compactBtn.Click += (_, _) =>
        {
            result = PresentationTierKind.Compact;
            dialog.Close();
        };
        cockpitBtn.Click += (_, _) =>
        {
            result = PresentationTierKind.Cockpit;
            dialog.Close();
        };
        laterBtn.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Выберите режим раскладки",
                    FontWeight = FontWeight.SemiBold,
                },
                summary,
                new TextBlock
                {
                    Text = "Можно сменить в settings.toml → [display.presentation] tier.",
                    Opacity = 0.75,
                    FontSize = 12,
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { laterBtn, compactBtn, cockpitBtn },
                },
            },
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
