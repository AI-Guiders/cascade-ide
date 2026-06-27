namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Outbound capability results from C# router to Monaco host (ADR 0163).</summary>
public interface ICideEditorCapabilityHost
{
    Task PushCapabilityCompletionResultAsync(
        int requestId,
        IReadOnlyList<CideEditorCompletionItem> items,
        CancellationToken cancellationToken = default);

    Task PushCapabilityHoverResultAsync(
        int requestId,
        string? markdown,
        CancellationToken cancellationToken = default);

    Task PushCapabilitySignatureResultAsync(
        int requestId,
        string? signature,
        CancellationToken cancellationToken = default);

    Task PushCapabilityDefinitionResultAsync(
        int requestId,
        CideEditorDefinitionLocation? location,
        CancellationToken cancellationToken = default);

    Task PushCapabilityInlayHintsResultAsync(
        int requestId,
        IReadOnlyList<CideEditorInlayHint> hints,
        CancellationToken cancellationToken = default);

    Task PushCapabilityCodeLensResultAsync(
        int requestId,
        IReadOnlyList<CideEditorCodeLensItem> lenses,
        CancellationToken cancellationToken = default);

    Task PushCapabilitySemanticTokensResultAsync(
        int requestId,
        IReadOnlyList<uint> data,
        string? resultId,
        CancellationToken cancellationToken = default);
}
