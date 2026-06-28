using RoslynMcp.ServiceLayer;

namespace CascadeIDE.Services.Roslyn;

/// <summary>In-process Roslyn MCP ServiceLayer for Monaco refactorings (move type, extract interface, rename).</summary>
public static class RoslynMcpEditorIntelligence
{
    public static Task<RoslynEditorCodeActionsResult> ListCodeActionsAsync(
        string solutionPath,
        string filePath,
        string liveDocumentText,
        int line,
        int column,
        CancellationToken cancellationToken = default) =>
        CodeActions.ListForEditorAsync(
            solutionPath,
            filePath,
            line,
            column,
            liveDocumentText,
            cancellationToken: cancellationToken);

    public static Task<RoslynEditorApplyResult> ApplyCodeActionAsync(
        string solutionPath,
        string filePath,
        string liveDocumentText,
        int line,
        int column,
        int actionIndex,
        CancellationToken cancellationToken = default) =>
        CodeActions.ApplyForEditorAsync(
            solutionPath,
            filePath,
            line,
            column,
            actionIndex,
            liveDocumentText,
            cancellationToken: cancellationToken);

    public static Task<RoslynEditorApplyResult> RenameAsync(
        string solutionPath,
        string filePath,
        string liveDocumentText,
        int line,
        int column,
        string newName,
        CancellationToken cancellationToken = default) =>
        RenameSymbol.RenameForEditorAsync(
            solutionPath,
            filePath,
            line,
            column,
            newName,
            liveDocumentText,
            renameFile: true,
            renamePartialTypeFiles: true,
            cancellationToken: cancellationToken);
}
