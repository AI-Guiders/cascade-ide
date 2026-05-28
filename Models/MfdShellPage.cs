namespace CascadeIDE.Models;

/// <summary>
/// Какая страница показана в <strong>оболочке Mfd</strong> (<c>MfdShellPageStack</c> / <c>MfdShellView</c>: одна активная поверхность без TabControl).
/// Семантика — пресетно-независимая; <strong>где</strong> на экране рисуется хост, задаётся пресетом/якорем (ADR 0021).
/// В текущей разметке v1 хост по умолчанию — колонка зоны Mfd.
/// Числовые значения — исторические (бывшие индексы вкладок нижнего региона).
/// </summary>
public enum MfdShellPage
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
    /// <summary>Параметры IDE (AI, MCP, Intercom, сочетания в чате) — по умолчанию в зоне Mfd; см. <c>[ai.chat].settings_presentation</c> в <c>settings.toml</c> (ADR 0083).</summary>
    AiChatSettings = 10,
    /// <summary>Готовность окружения (LSP, dotnet…) — ADR 0023; страница оболочки Mfd, не оверлей.</summary>
    EnvironmentReadiness = 11,

    /// <summary>Обозреватель решения (дерево проектов/файлов) — контент Mfd shell, не отдельная колонка вне shell.</summary>
    SolutionExplorer = 12,
    /// <summary>Markdown preview как MFD-first tool surface.</summary>
    MarkdownPreview = 13,
    /// <summary>Связанные файлы (related) по якорю — <b>WorkspaceNavigation</b> списком; не дублирует колонку PFD с картой намерений (граф/CF — <b>CodeNavigation</b>, ADR 0088).</summary>
    RelatedFiles = 14,

    /// <summary>
    /// Hybrid Codebase Index status / control surface (HCI): ECAM-like glance for docs count, freshness, scope, errors.
    /// </summary>
    HybridIndex = 15,

    /// <summary>Веб-портал (NativeWebView) + мост <c>executeIdeCommand</c> / <c>invokeCSharpAction</c>, ADR 0108.</summary>
    WebAiPortal = 16,

    /// <summary>Редактор документов в Mfd при <c>primary_work_surface = intercom</c> (ADR 0120).</summary>
    Editor = 17,

    /// <summary>Correspondence Surface (CRS): слои L0–L4, forward/reverse doc↔code (ADR 0156).</summary>
    Correspondence = 18,
}
