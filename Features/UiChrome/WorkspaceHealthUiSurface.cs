using CascadeIDE.Cockpit;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Слой представления для контура Workspace Health (build/tests/debug/git): нижняя полоса или отдельная страница зоны.
/// Канал-специфичные имена TOML <c>workspace_health_surface</c>; абстрактная ось формы — <see cref="ContentRepresentation"/> (ADR 0063).
/// Не шина событий и не маршрутизация сообщений — только выбор разметки; данные (<see cref="WorkspaceHealthInputSnapshot"/>) от выбора не зависят.
/// </summary>
public enum WorkspaceHealthUiSurface
{
    /// <summary>Полоса под workspace (<c>WorkspaceHealthStripView</c>); <see cref="ContentRepresentation.Strip"/>.</summary>
    BottomStrip = 0,

    /// <summary>Отдельная страница в MFD/PFD — полноразметка позже; нижняя полоса скрыта; <see cref="ContentRepresentation.Page"/>.</summary>
    DedicatedPage = 1,
}

/// <summary>ADR 0063: соответствие поверхности WH общей оси <see cref="ContentRepresentation"/>.</summary>
public static class WorkspaceHealthUiSurfaceExtensions
{
    /// <summary>BottomStrip → Strip, DedicatedPage → Page.</summary>
    public static ContentRepresentation ToContentRepresentation(this WorkspaceHealthUiSurface surface) =>
        surface switch
        {
            WorkspaceHealthUiSurface.BottomStrip => ContentRepresentation.Strip,
            WorkspaceHealthUiSurface.DedicatedPage => ContentRepresentation.Page,
            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, null),
        };
}
