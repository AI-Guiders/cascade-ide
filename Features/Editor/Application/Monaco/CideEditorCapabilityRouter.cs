using CascadeIDE.Features.Editor.Presentation;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Routes Monaco capability requests: LSP-first when ready, Roslyn fallback (ADR 0163 M8).</summary>
public sealed class CideEditorCapabilityRouter : ICideEditorCapabilityRouter
{
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

            case CideEditorBusManifest.Capabilities.InlayHints:
                await HandleInlayHintsAsync(context, requestId, cancellationToken).ConfigureAwait(true);
                break;

            case CideEditorBusManifest.Capabilities.CodeLens:
                await HandleCodeLensAsync(context, requestId, cancellationToken).ConfigureAwait(true);
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
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushCompletionAsync(context.Host, requestId, [], cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        if (context.LspReady && context.CSharpLspHost is not null)
        {
            var lspItems = await context.CSharpLspHost
                .RequestCompletionAsync(context.FilePath, text, line, column, cancellationToken)
                .ConfigureAwait(true);
            if (lspItems.Count > 0)
            {
                await PushCompletionAsync(context.Host, requestId, lspItems, cancellationToken).ConfigureAwait(true);
                return;
            }
        }

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
            .ConfigureAwait(false);
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

        string? sig;
        if (context.LspReady && context.CSharpLspHost is not null)
        {
            sig = await context.CSharpLspHost
                .RequestSignatureHelpAsync(context.FilePath, text, line, column, cancellationToken)
                .ConfigureAwait(true);
            if (!string.IsNullOrWhiteSpace(sig))
            {
                await PushSignatureAsync(context.Host, requestId, sig, cancellationToken).ConfigureAwait(true);
                return;
            }
        }

        sig = await Task.Run(
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
        CideEditorDefinitionLocation? mapped;
        if (context.LspReady && context.CSharpLspHost is not null)
        {
            mapped = await context.CSharpLspHost
                .RequestDefinitionAsync(context.FilePath, text, line, column, cancellationToken)
                .ConfigureAwait(true);
            if (mapped is not null)
            {
                await PushDefinitionAsync(context.Host, requestId, mapped, cancellationToken).ConfigureAwait(true);
                return;
            }
        }

        var location = await Task.Run(
            () => context.CSharpLanguage.TryGetDefinitionLocation(context.FilePath, text, line, column, cancellationToken),
            cancellationToken).ConfigureAwait(true);
        mapped = location is null
            ? null
            : new CideEditorDefinitionLocation(location.FilePath, location.Line, location.Column);
        await PushDefinitionAsync(context.Host, requestId, mapped, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleInlayHintsAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        CancellationToken cancellationToken)
    {
        if (context.GetInlineHintsForFile is null
            || !CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushInlayHintsAsync(context.Host, requestId, [], cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        var parts = await Task.Run(
            () => context.GetInlineHintsForFile!(context.FilePath, text),
            cancellationToken).ConfigureAwait(true);
        var hints = MonacoEditorInlayMapper.ToHints(text, parts);
        await PushInlayHintsAsync(context.Host, requestId, hints, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleCodeLensAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        CancellationToken cancellationToken)
    {
        var lenses = context.GetCodeLensesForFile?.Invoke(context.FilePath) ?? [];
        await PushCodeLensAsync(context.Host, requestId, lenses, cancellationToken).ConfigureAwait(true);
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

    private static Task PushInlayHintsAsync(
        MonacoEditorHostControl host,
        int requestId,
        IReadOnlyList<CideEditorInlayHint> hints,
        CancellationToken cancellationToken) =>
        host.PushCapabilityInlayHintsResultAsync(requestId, hints, cancellationToken);

    private static Task PushCodeLensAsync(
        MonacoEditorHostControl host,
        int requestId,
        IReadOnlyList<CideEditorCodeLensItem> lenses,
        CancellationToken cancellationToken) =>
        host.PushCapabilityCodeLensResultAsync(requestId, lenses, cancellationToken);

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
