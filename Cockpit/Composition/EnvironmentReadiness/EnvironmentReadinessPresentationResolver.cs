namespace CascadeIDE.Cockpit.Composition.EnvironmentReadiness;

/// <summary>
/// Выбирает <see cref="EnvironmentReadinessPresentationKind"/> по ширине контейнера (ADR 0068: проекция ≠ payload).
/// Стабильные id строк и порядок — в <see cref="EnvironmentReadinessInstrumentDeck.OrderedCellIds"/> / снимке канала.
/// </summary>
public static class EnvironmentReadinessPresentationResolver
{
    /// <summary>Порог ширины (px) для режима таблицы; страница вторичного контура использует то же значение.</summary>
    public const double DefaultWideLayoutMinWidthPx = 420;

    public static EnvironmentReadinessPresentationKind Resolve(double containerWidth, double wideLayoutMinWidthPx = DefaultWideLayoutMinWidthPx)
    {
        if (containerWidth <= 0)
            return EnvironmentReadinessPresentationKind.CompactCards;

        return containerWidth >= wideLayoutMinWidthPx
            ? EnvironmentReadinessPresentationKind.WideTable
            : EnvironmentReadinessPresentationKind.CompactCards;
    }
}
