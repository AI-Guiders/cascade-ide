namespace CascadeIDE.Contracts.Experimental;

/// <summary>
/// Каноничные идентификаторы capabilities (строковые ids), используемые registry/overlay’ями.
/// </summary>
[ApiStability(ApiStability.Experimental)]
public static class CapabilityIds
{
    public static class DocsMarkdown
    {
        public const string ModuleId = "docs.markdown";

        public const string DiagramExpansionService = "docs.markdown.diagram_expansion";

        public const string DumpCapabilitiesCommand = "docs.markdown.dump_capabilities";

        /// <summary>Экспортировать Markdown с развёрнутыми include-дерективами (portable для публикации).</summary>
        public const string ExportExpandedMarkdownCommand = "docs.markdown.export_expanded";
    }

    /// <summary>Shell / кокпит: поверхности, привязанные к панелям и зонам внимания (ADR 0025).</summary>
    public static class UiChrome
    {
        public const string ModuleId = "ui.chrome";

        /// <summary>Обозреватель решения — PFD, панель <c>solution_explorer</c>.</summary>
        public const string SolutionExplorerSurface = "ui.chrome.surface.solution_explorer";
    }
}

