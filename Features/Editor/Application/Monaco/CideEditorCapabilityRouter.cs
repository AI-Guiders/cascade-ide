using CascadeIDE.Features.Editor.Presentation;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Routes Monaco capability requests to Roslyn (LSP hook point for M8).</summary>
public sealed class CideEditorCapabilityRouter : ICideEditorCapabilityRouter
{
    public bool CanHandle(CideEditorInboundMessage message) =>
        CideEditorBusManifest.IsCapabilityRequest(message.Type);

    public async Task HandleAsync(
        CideEditorInboundMessage message,
        MonacoEditorCapabilityContext context,
        CancellationToken cancellationToken = default)
    {
        var type = CideEditorBusManifest.NormalizeInboundType(message.Type);
        if (message.RequestId is not int requestId
            || message.Line is not int line
            || message.Column is not int column)
        {
            return;
        }

        switch (type)
        {
            case CideEditorBusManifest.Capabilities.Completion:
                await HandleCompletionAsync(context, requestId, line, column, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.Hover:
                await HandleHoverAsync(context, requestId, line, column, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.SignatureHelp:
                await HandleSignatureAsync(context, requestId, line, column, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.Definition:
                await HandleDefinitionAsync(context, requestId, line, column, cancellationToken).ConfigureAwait(true);
                break;
        }
    }

    private static async Task HandleCompletionAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        int line,
        int column,
        CancellationToken cancellationToken)
    {
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath) || context.LspReady)
        {
            await PushCompletionAsync(context.Host, requestId, [], cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        var items = await Task.Run(
            () => context.CSharpLanguage.GetCompletionItems(context.FilePath, text, line, column),
            cancellationToken).ConfigureAwait(true);
        var mapped = items.Select(i => new CideEditorCompletionItem(i.DisplayText, i.InsertText, i.Description)).ToList();
        await PushCompletionAsync(context.Host, requestId, mapped, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleHoverAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        int line,
        int column,
        CancellationToken cancellationToken)
    {
        var text = context.GetEditorText();
        var markdown = await ResolveHoverMarkdownAsync(context, text, line, column, cancellationToken).ConfigureAwait(true);
        await PushHoverAsync(context.Host, requestId, markdown, cancellationToken).ConfigureAwait(true);
    }

    private static async Task<string?> ResolveHoverMarkdownAsync(
        MonacoEditorCapabilityContext context,
        string text,
        int line,
        int column,
        CancellationToken cancellationToken)
    {
        var offset = TryOffsetFromLineColumn(text, line, column);
        if (offset >= 0)
        {
            var strips = context.WorkspaceDiagnostics.GetStripsForFile(context.FilePath);
            var hit = WorkspaceDiagnosticsCoordinator.HitTestForToolTip(
                strips, offset, line, column, text);
            if (hit is not null)
                return $"**{hit.Id}**: {hit.Message}";
        }

        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
            return null;

        return await context.ResolveQuickInfoAsync(context.FilePath, text, line, column, cancellationToken)
            .ConfigureAwait(false)
            ?? await Task.Run(
                () => context.CSharpLanguage.GetQuickInfo(context.FilePath, text, line, column, cancellationToken),
                cancellationToken).ConfigureAwait(false);
    }

    private static async Task HandleSignatureAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        int line,
        int column,
        CancellationToken cancellationToken)
    {
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushSignatureAsync(context.Host, requestId, null, cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        if (!text.Contains('('))
        {
            await PushSignatureAsync(context.Host, requestId, null, cancellationToken).ConfigureAwait(true);
            return;
        }

        var sig = await Task.Run(
            () => context.CSharpLanguage.GetSignatureHelp(context.FilePath, text, line, column),
            cancellationToken).ConfigureAwait(true);
        await PushSignatureAsync(context.Host, requestId, sig, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleDefinitionAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        int line,
        int column,
        CancellationToken cancellationToken)
    {
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushDefinitionAsync(context.Host, requestId, null, cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        var location = await Task.Run(
            () => context.CSharpLanguage.TryGetDefinitionLocation(context.FilePath, text, line, column, cancellationToken),
            cancellationToken).ConfigureAwait(true);
        CideEditorDefinitionLocation? mapped = location is null
            ? null
            : new CideEditorDefinitionLocation(location.FilePath, location.Line, location.Column);
        await PushDefinitionAsync(context.Host, requestId, mapped, cancellationToken).ConfigureAwait(true);
    }

    private static Task PushCompletionAsync(
        MonacoEditorHostControl host,
        int requestId,
        IReadOnlyList<CideEditorCompletionItem> items,
        CancellationToken cancellationToken) =>
        host.PushCapabilityCompletionResultAsync(requestId, items, cancellationToken);

    private static Task PushHoverAsync(
        MonacoEditorHostControl host,
        int requestId,
        string? markdown,
        CancellationToken cancellationToken) =>
        host.PushCapabilityHoverResultAsync(requestId, markdown, cancellationToken);

    private static Task PushSignatureAsync(
        MonacoEditorHostControl host,
        int requestId,
        string? signature,
        CancellationToken cancellationToken) =>
        host.PushCapabilitySignatureResultAsync(requestId, signature, cancellationToken);

    private static Task PushDefinitionAsync(
        MonacoEditorHostControl host,
        int requestId,
        CideEditorDefinitionLocation? location,
        CancellationToken cancellationToken) =>
        host.PushCapabilityDefinitionResultAsync(requestId, location, cancellationToken);

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
}
