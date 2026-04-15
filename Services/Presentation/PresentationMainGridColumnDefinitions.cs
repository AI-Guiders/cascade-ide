namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Строка колонок Avalonia <c>Grid</c> для строки рабочей области главного окна:
/// пять колонок — контент PFD, сплиттер 4px, Forward, сплиттер 4px, MFD (см. <c>MainWindow.axaml</c>).
/// При заданных весах в <c>presentation</c> контент-колонки получают звёздочные доли (<c>*</c>).
/// </summary>
public static class PresentationMainGridColumnDefinitions
{
    /// <summary>Дефолт без весов в строке <c>presentation</c> (как в разметке до весов).</summary>
    public const string Default = PresentationMainGridLayoutFrameBuilder.DefaultColumnDefinitions;

    /// <param name="mfdColumnSuppressedForHost">Колонка MFD в главном окне свёрнута — узкий хвост для двухякорного пресета с весами.</param>
    public static string Get(
        PresentationParseResult parse,
        bool dedicatedMfdSecondScreen,
        bool mfdColumnSuppressedForHost)
        => PresentationMainGridLayoutFrameBuilder
            .Build(parse, dedicatedMfdSecondScreen, mfdColumnSuppressedForHost)
            .ColumnDefinitions;
}
