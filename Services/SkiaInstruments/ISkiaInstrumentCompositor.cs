#nullable enable
namespace CascadeIDE.Services.SkiaInstruments;

/// <summary>
/// Базовый контракт композиции Skia-инструмента: доменный intent + viewport -> результат композиции.
/// Это общий уровень для инструментов поверх host surface compositor.
/// </summary>
public interface ISkiaInstrumentCompositor<in TIntent, out TResult>
{
    TResult Compose(TIntent intent, in SkiaInstrumentViewport viewport);
}

/// <summary>Параметры viewport для композиции Skia-инструмента.</summary>
public readonly record struct SkiaInstrumentViewport(double Width, double Height);
