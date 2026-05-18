using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using CascadeIDE.Services;

namespace CascadeIDE.Views;

/// <summary>
/// Базовое окно: подписка на указатель для MCP (Avalonia 12 не даёт <c>PointerOverElement</c> на <see cref="TopLevel"/>).
/// </summary>
public class PointerTrackingWindow : Window
{
    private static readonly SolidColorBrush FallbackWindowBackground = new(Color.Parse("#FF1E1E1E"));

    protected PointerTrackingWindow()
    {
        Background = FallbackWindowBackground;
        UiPointerClientPosition.Attach(this);
        Opened += (_, _) => EnsureOpaqueClientBackground();
        UiThemeApply.ThemeApplied += (_, _) => EnsureOpaqueClientBackground();
    }

    /// <summary>Непрозрачный фон клиента: DynamicResource + Fluent PART_WindowBorder (см. App.axaml).</summary>
    protected void EnsureOpaqueClientBackground()
    {
        if (TryResolveMainWindowBackgroundBrush(out var brush))
            Background = EnsureOpaqueBrush(brush);
        else
            Background = FallbackWindowBackground;
    }

    private static IBrush EnsureOpaqueBrush(IBrush brush)
    {
        if (brush is SolidColorBrush solid)
        {
            var c = solid.Color;
            if (c.A < byte.MaxValue)
                return new SolidColorBrush(Color.FromArgb(byte.MaxValue, c.R, c.G, c.B));
        }

        return brush;
    }

    private static bool TryResolveMainWindowBackgroundBrush(out IBrush brush)
    {
        brush = FallbackWindowBackground;
        if (Application.Current?.Resources is not { } resources)
            return false;

        if (resources.TryGetResource(UiThemeApply.Keys.MainWindowBackground, ThemeVariant.Default, out var value)
            && value is IBrush resolved)
        {
            brush = resolved;
            return true;
        }

        return false;
    }
}
