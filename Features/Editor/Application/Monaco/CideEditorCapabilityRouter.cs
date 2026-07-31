using CascadeIDE.Services;
using CascadeIDE.Services.Roslyn;
using RoslynMcp.ServiceLayer;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Routes Monaco capability requests: LSP-first when ready, Roslyn fallback (ADR 0163 M8).</summary>
public sealed partial class CideEditorCapabilityRouter : ICideEditorCapabilityRouter
{
    private static readonly TimeSpan CompletionLspBudget = TimeSpan.FromMilliseconds(1800);

    public bool CanHandle(CideEditorInboundMessage message) =>
        CideEditorBusManifest.IsCapabilityRequest(message.Type)
        || CideEditorBusManifest.IsCapabilitySideChannel(message.Type);

    public async Task HandleAsync(
        CideEditorInboundMessage message,
        MonacoEditorCapabilityContext context,
        CancellationToken cancellationToken = default)
    {
        var type = CideEditorBusManifest.NormalizeInboundType(message.Type);
        if (string.Equals(type, CideEditorBusManifest.Capabilities.CodeLensClick, StringComparison.Ordinal))
        {
            if (message.LensId is not null && context.TryNavigateCodeLens?.Invoke(message.LensId) == true)
                return;
            return;
        }

        if (message.RequestId is not int requestId)
            return;

        switch (type)
        {
            case CideEditorBusManifest.Capabilities.Completion:
                if (message.Line is int cl && message.Column is int cc)
                    await HandleCompletionAsync(context, requestId, cl, cc, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.Hover:
                if (message.Line is int hl && message.Column is int hc)
                    await HandleHoverAsync(context, requestId, hl, hc, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.SignatureHelp:
                if (message.Line is int sl && message.Column is int sc)
                    await HandleSignatureAsync(context, requestId, sl, sc, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.Definition:
                if (message.Line is int dl && message.Column is int dc)
                    await HandleDefinitionAsync(context, requestId, dl, dc, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.References:
                if (message.Line is int rl && message.Column is int rc)
                    await HandleReferencesAsync(context, requestId, rl, rc, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.Format:
                await HandleFormatAsync(context, requestId, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.CodeAction:
                if (message.Line is int al && message.Column is int ac)
                    await HandleCodeActionAsync(context, requestId, al, ac, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.CodeActionApply:
                if (message.Line is int apl && message.Column is int apc && message.ActionIndex is int actionIndex)
                    await HandleCodeActionApplyAsync(context, requestId, apl, apc, actionIndex, cancellationToken)
                        .ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.Rename:
                if (message.Line is int rnl && message.Column is int rnc && !string.IsNullOrWhiteSpace(message.NewName))
                    await HandleRenameAsync(context, requestId, rnl, rnc, message.NewName!, cancellationToken)
                        .ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.InlayHints:
                await HandleInlayHintsAsync(context, requestId, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.CodeLens:
                await HandleCodeLensAsync(context, requestId, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.SemanticTokens:
                await HandleSemanticTokensAsync(context, requestId, cancellationToken).ConfigureAwait(true);
                break;
        }
    }


    private static Task PushCompletionAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        IReadOnlyList<CideEditorCompletionItem> items,
        CancellationToken cancellationToken) =>
        host.PushCapabilityCompletionResultAsync(requestId, items, cancellationToken);

    private static Task PushHoverAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        string? markdown,
        CancellationToken cancellationToken) =>
        host.PushCapabilityHoverResultAsync(requestId, markdown, cancellationToken);

    private static Task PushSignatureAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        string? signature,
        CancellationToken cancellationToken) =>
        host.PushCapabilitySignatureResultAsync(requestId, signature, cancellationToken);

    private static Task PushDefinitionAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        CideEditorDefinitionLocation? location,
        CancellationToken cancellationToken) =>
        host.PushCapabilityDefinitionResultAsync(requestId, location, cancellationToken);

    private static Task PushReferencesAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        IReadOnlyList<CideEditorReferenceLocation> locations,
        CancellationToken cancellationToken) =>
        host.PushCapabilityReferencesResultAsync(requestId, locations, cancellationToken);

    private static Task PushFormatAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        string? text,
        CancellationToken cancellationToken) =>
        host.PushCapabilityFormatResultAsync(requestId, text, cancellationToken);

    private static Task PushCodeActionsAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        IReadOnlyList<CideEditorCodeActionItem> actions,
        CancellationToken cancellationToken) =>
        host.PushCapabilityCodeActionResultAsync(requestId, actions, cancellationToken);

    private static Task PushWorkspaceEditAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        bool ok,
        string? error,
        IReadOnlyList<CideEditorDocumentTextChange> changes,
        CancellationToken cancellationToken) =>
        host.PushCapabilityWorkspaceEditResultAsync(requestId, ok, error, changes, cancellationToken);

    private static Task PushInlayHintsAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        IReadOnlyList<CideEditorInlayHint> hints,
        CancellationToken cancellationToken) =>
        host.PushCapabilityInlayHintsResultAsync(requestId, hints, cancellationToken);

    private static Task PushCodeLensAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        IReadOnlyList<CideEditorCodeLensItem> lenses,
        CancellationToken cancellationToken) =>
        host.PushCapabilityCodeLensResultAsync(requestId, lenses, cancellationToken);

    private static Task PushSemanticTokensAsync(
        ICideEditorCapabilityHost host,
        int requestId,
        IReadOnlyList<uint> data,
        string? resultId,
        CancellationToken cancellationToken) =>
        host.PushCapabilitySemanticTokensResultAsync(requestId, data, resultId, cancellationToken);

    private static int TryOffsetFromLineColumn(string text, int lineOneBased, int columnOneBased)
    {
        if (string.IsNullOrEmpty(text) || lineOneBased < 1 || columnOneBased < 1)
            return -1;
        var lineStart = 0;
        var line = 1;
        for (var i = 0; i < text.Length && line < lineOneBased; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        if (line != lineOneBased)
            return -1;
        var offset = lineStart + columnOneBased - 1;
        return offset <= text.Length ? offset : text.Length;
    }

    private static string? ResolveRoslynWorkspacePath(MonacoEditorCapabilityContext context) =>
        RoslynEditorWorkspacePath.Resolve(
            context.GetSolutionPath?.Invoke(),
            context.FilePath,
            context.GetWorkspaceRoot?.Invoke());
}
