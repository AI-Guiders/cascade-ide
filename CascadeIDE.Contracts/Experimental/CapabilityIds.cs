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
}

