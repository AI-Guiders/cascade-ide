using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace;

/// <summary>
/// Repository workspace overlay loaded from <c>.cascade/workspace.toml</c>.
/// This file intentionally holds non-UI-mode configuration (docs correspondence, feature registry, etc).
/// </summary>
public sealed class RepositoryWorkspaceToml
{
    /// <summary>Non-UI overlays under <c>[workspace.*]</c> (ADR 0061 / 0155).</summary>
    public RepositoryWorkspaceSectionToml? Workspace { get; set; }

    /// <summary>UI chrome metrics and markdown preview placement.</summary>
    public UiWorkspaceChromeToml? Chrome { get; set; }

    /// <summary>LOC thresholds for UI badges.</summary>
    public UiWorkspaceLocLimitsToml? LocLimits { get; set; }

    public UiWorkspaceRoutingToml? Routing { get; set; }

    /// <summary>Code navigation presets (ADR 0039, CNC).</summary>
    public CodeNavigationSettings? CodeNavigation { get; set; }

    /// <summary>Code navigation map settings (ADR 0053).</summary>
    public CodeNavigationMapSettings? CodeNavigationMap { get; set; }
}

public sealed class RepositoryWorkspaceSectionToml
{
    public RepositoryAdrToml? Adr { get; set; }
    public RepositoryFeaturesToml? Features { get; set; }
    public RepositoryDocsTemplatesToml? DocsTemplates { get; set; }
    public RepositoryCorrespondenceToml? Correspondence { get; set; }

    public RepositoryCasaFieldToml? CasaField { get; set; }
}

/// <summary>Explicit doc↔code anchors (<c>[[workspace.correspondence.code_anchors]]</c>, ADR 0156 §2.5).</summary>
public sealed class RepositoryCorrespondenceToml
{
    public List<RepositoryCorrespondenceCodeAnchorToml> CodeAnchors { get; set; } = [];
}

public sealed class RepositoryCorrespondenceCodeAnchorToml
{
    public string? Doc { get; set; }
    public string? File { get; set; }
    public string? Bracket { get; set; }
    public int? LineStart { get; set; }
    public int? LineEnd { get; set; }
    public string? Kind { get; set; }
    public string? MemberKey { get; set; }
}

public sealed class RepositoryAdrToml
{
    public string? AutoInclude { get; set; }
    public int? MaxRelated { get; set; }
    public Dictionary<string, object>? Map { get; set; }

    /// <summary>
    /// Repo-relative ADR root directory for correspondence and auto-include. Default: <c>docs/adr/</c>.
    /// TOML: <c>root_dir</c>.
    /// </summary>
    public string? RootDir { get; set; }

    /// <summary>
    /// Regex with named group <c>id</c> used to extract ADR id from a doc path.
    /// Default matches <c>docs/adr/####-*.md</c>.
    /// TOML: <c>id_regex</c>.
    /// </summary>
    public string? IdRegex { get; set; }
}

public sealed class RepositoryFeaturesToml
{
    public List<RepositoryFeatureToml> Feature { get; set; } = [];
}

public sealed class RepositoryFeatureToml
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public List<string> Paths { get; set; } = [];
    public List<string> Docs { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

public sealed class RepositoryDocsTemplatesToml
{
    public string? CatalogPath { get; set; }
    public List<DocsTemplateToml> Template { get; set; } = [];
}

public sealed class DocsTemplateToml
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Kind { get; set; }
    public string? Source { get; set; }
    public string? Path { get; set; }
    public string? KnowledgeRootId { get; set; }
    public string? FilePath { get; set; }
}

