namespace CascadeIDE.Cockpit.Composition.EnvironmentReadiness;

/// <summary>
/// Проекция представления страницы готовности окружения: тот же <see cref="Models.AnnunciatorLampItem"/> payload,
/// разная раскладка (ADR 0068 — presentation projection vs row payload).
/// </summary>
public enum EnvironmentReadinessPresentationKind
{
    /// <summary>Узкая колонка: карточки с текстовым глифом уровня (не полоса ламп сверху).</summary>
    CompactCards,

    /// <summary>Широкая зона: таблица; примитив лампы Korry в первой колонке строки.</summary>
    WideTable,
}
