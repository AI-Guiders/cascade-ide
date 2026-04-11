namespace CascadeIDE.Models;

/// <summary>
/// Какая страница показана во <strong>вторичном контуре оболочки</strong> (одна активная поверхность без TabControl).
/// Семантика — пресетно-независимая; <strong>где</strong> на экране рисуется этот контур, задаётся пресетом/якорем (ADR 0021).
/// В текущей разметке v1 хост по умолчанию — колонка зоны Mfd (тип представления <c>SecondaryShellView</c>).
/// Числовые значения — исторические (бывшие индексы вкладок нижнего региона).
/// </summary>
public enum SecondaryShellPage
{
    WorkspaceHealth = 0,
    Chat = 1,
    Terminal = 2,
    Build = 3,
    Problems = 4,
    Git = 5,
    Events = 6,
    Tests = 7,
    Hypotheses = 8,
    DebugStack = 9,
}
