namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Строка колонок Avalonia <c>Grid</c> для строки рабочей области главного окна:
/// три колонки контента — PFD, Forward, MFD (см. <c>MainWindow.axaml</c>).
/// При заданных весах в <c>presentation</c> контент-колонки получают звёздочные доли (<c>*</c>).
/// </summary>
public static class PresentationMainGridColumnDefinitions
{
    /// <summary>Дефолт без весов в строке <c>presentation</c> (как в разметке до весов).</summary>
    public const string Default = PresentationMainGridLayoutFrameBuilder.DefaultColumnDefinitions;

    /// <param name="mfdColumnSuppressedForHost">Колонка MFD в главном окне свёрнута — узкий хвост для двухякорного пресета с весами.</param>
    /// <param name="tripleOneAnchorPerZone">Тройной пресет <c>(P)(F)(M)</c> (любой порядок) — отдельная ветка колонок под хосты.</param>
    /// <param name="suppressPfdColumnForPfdHostWindow">Колонка PFD в main скрыта — контент в <c>PfdHostWindow</c>.</param>
    public static string Get(
        PresentationParseResult parse,
        bool dedicatedMfdSecondScreen,
        bool mfdColumnSuppressedForHost,
        bool tripleOneAnchorPerZone,
        bool suppressPfdColumnForPfdHostWindow)
        => PresentationMainGridLayoutFrameBuilder
            .Build(parse, dedicatedMfdSecondScreen, mfdColumnSuppressedForHost, tripleOneAnchorPerZone, suppressPfdColumnForPfdHostWindow)
            .ColumnDefinitions;
}
