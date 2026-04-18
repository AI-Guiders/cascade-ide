using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using CascadeIDE.Tests.VisualVerification;

namespace CascadeIDE.Tests;

/// <summary>
/// Дымовой тест пайплайна «контрол → PNG → сравнение с эталоном» в headless Avalonia.
/// Расширяй новыми сценариями (мини-карта, примитивы) по мере стабилизации визуала.
/// </summary>
public sealed class HeadlessVisualRegressionSmokeTests
{
    private static readonly Vector s_dpi = new(96, 96);

    [AvaloniaFact]
    public void Solid_red_border_matches_approved_png()
    {
        var border = new Border
        {
            Background = Brushes.Crimson,
        };

        var png = ControlVisualCapture.CaptureControlToPng(border, new Size(64, 64), s_dpi);
        ApprovedPngAssert.EqualToApproved(png, "headless_solid_red_64.approved.png");
    }
}
