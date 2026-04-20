namespace CascadeIDE.Models;

/// <summary>
/// Какая страница показана в <strong>оболочке Mfd</strong> (<c>MfdShellView</c>: одна активная поверхность без TabControl).
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
    /// <summary>Параметры AI и чата (провайдеры, MCP, ключи) — по умолчанию в зоне Mfd; см. <c>ai_chat_settings_presentation</c> в <c>settings.toml</c>.</summary>
    AiChatSettings = 10,
    /// <summary>Готовность окружения (LSP, dotnet…) — ADR 0023; страница оболочки Mfd, не оверлей.</summary>
    EnvironmentReadiness = 11,

    /// <summary>Обозреватель решения (дерево проектов/файлов) — контент Mfd shell, не отдельная колонка вне shell.</summary>
    SolutionExplorer = 12,
    /// <summary>Markdown preview как MFD-first tool surface.</summary>
    MarkdownPreview = 13,
}
