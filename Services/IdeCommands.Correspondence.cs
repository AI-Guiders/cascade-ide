namespace CascadeIDE.Services;

/// <summary>Doc correspondence + feature registry actions (ADR 0061, 0155).</summary>
public static partial class IdeCommands
{
    /// <summary>Открыть correspondence ADR для текущего файла (как клик по строке ADR на PFD). returns: text.</summary>
    public const string OpenWorkspaceAdrCorrespondence = "open_workspace_adr_correspondence";

    /// <summary>Открыть документацию фичи для текущего файла (как клик по строке Feature на PFD); при множестве docs — pick. returns: text.</summary>
    public const string OpenWorkspaceFeatureDocs = "open_workspace_feature_docs";

    /// <summary>Открыть шаблон документации из <c>docs/templates</c> в Markdown Preview. args: path?:string; returns: text; example: {"path":"docs/templates/feature.md"}.</summary>
    public const string OpenDocsTemplate = "open_docs_template";
}

