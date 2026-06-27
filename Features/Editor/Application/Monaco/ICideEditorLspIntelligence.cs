namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>LSP intelligence surface for Monaco capability router (ADR 0163 M8).</summary>
public interface ICideEditorLspIntelligence
{
    bool IsActive { get; }

    bool SupportsSemanticTokens { get; }

    Task<IReadOnlyList<CideEditorCompletionItem>> RequestCompletionAsync(
        string filePath,
        string text,
        int line1,
        int col1,
        CancellationToken ct);

    Task<string?> RequestSignatureHelpAsync(
        string filePath,
        string text,
        int line1,
        int col1,
        CancellationToken ct);

    Task<CideEditorDefinitionLocation?> RequestDefinitionAsync(
        string filePath,
        string text,
        int line1,
        int col1,
        CancellationToken ct);

    Task<IReadOnlyList<CideEditorReferenceLocation>> RequestReferencesAsync(
        string filePath,
        string text,
        int line1,
        int col1,
        CancellationToken ct);

    Task<CideEditorSemanticTokensData?> RequestSemanticTokensFullAsync(
        string filePath,
        string text,
        CancellationToken ct);
}
