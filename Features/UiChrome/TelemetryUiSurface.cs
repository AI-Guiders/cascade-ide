namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Слой представления для телеметрии контура работы (build/tests/debug/git): нижняя полоса или отдельная страница зоны.
/// Не шина событий и не маршрутизация сообщений — только выбор разметки; данные (<see cref="WorkspaceTelemetryInputSnapshot"/>) от выбора не зависят.
/// </summary>
public enum TelemetryUiSurface
{
    /// <summary>Полоса под workspace (<c>TelemetryStripView</c>).</summary>
    BottomStrip = 0,

    /// <summary>Отдельная страница в MFD/PFD — полноразметка позже; нижняя полоса скрыта.</summary>
    DedicatedPage = 1,
}
