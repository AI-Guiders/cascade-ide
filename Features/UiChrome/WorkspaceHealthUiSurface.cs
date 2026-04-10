namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Слой представления для контура Workspace Health (build/tests/debug/git): нижняя полоса или отдельная страница зоны.
/// Не шина событий и не маршрутизация сообщений — только выбор разметки; данные (<see cref="WorkspaceHealthInputSnapshot"/>) от выбора не зависят.
/// </summary>
public enum WorkspaceHealthUiSurface
{
    /// <summary>Полоса под workspace (<c>WorkspaceHealthStripView</c>).</summary>
    BottomStrip = 0,

    /// <summary>Отдельная страница в MFD/PFD — полноразметка позже; нижняя полоса скрыта.</summary>
    DedicatedPage = 1,
}
