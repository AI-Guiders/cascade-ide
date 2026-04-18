using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace CascadeIDE.Tests.VisualVerification;

/// <summary>
/// Рендер Avalonia-контрола в PNG (software <see cref="RenderTargetBitmap"/>).
/// Для headless-тестов (<see cref="Avalonia.Headless.XUnit.AvaloniaFact"/>) не требуется видимое окно, если выполнены Measure/Arrange.
/// </summary>
public static class ControlVisualCapture
{
    /// <summary>Снимает контрол в память PNG. Размер задаётся явно (DIP).</summary>
    public static byte[] CaptureControlToPng(Control control, Size size, Vector dpi)
    {
        control.Width = size.Width;
        control.Height = size.Height;
        control.Measure(size);
        control.Arrange(new Rect(size));

        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(size.Width)),
            Math.Max(1, (int)Math.Ceiling(size.Height)));

        using var rtb = new RenderTargetBitmap(pixelSize, dpi);
        rtb.Render(control);

        using var ms = new MemoryStream();
        rtb.Save(ms);
        return ms.ToArray();
    }
}
