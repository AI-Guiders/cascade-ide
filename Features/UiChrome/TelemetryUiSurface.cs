namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Где показывается телеметрия build/tests/debug/git: нижняя полоса или отдельная страница зоны.
/// Данные (<see cref="AttentionStripInputSnapshot"/>) от выбора не зависят — только разметка.
/// </summary>
public enum TelemetryUiSurface
{
    /// <summary>Полоса под workspace (<c>TelemetryStripView</c>).</summary>
    BottomStrip = 0,

    /// <summary>Отдельная страница в MFD/PFD — полноразметка позже; нижняя полоса скрыта.</summary>
    DedicatedPage = 1,
}
