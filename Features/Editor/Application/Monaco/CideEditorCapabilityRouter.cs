using CascadeIDE.Services;
using CascadeIDE.Services.Roslyn;
using RoslynMcp.ServiceLayer;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Routes Monaco capability requests: LSP-first when ready, Roslyn fallback (ADR 0163 M8).</summary>
public sealed class CideEditorCapabilityRouter : ICideEditorCapabilityRouter
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
        var prefix = CSharpCompletionPrefix.Extract(text, line, column);

        var roslynTask = Task.Run(
            () => context.CSharpLanguage.GetCompletionItems(context.FilePath, text, line, column),
            cancellationToken);
        Task<IReadOnlyList<CideEditorCompletionItem>>? lspTask = null;
        if (context.LspReady && context.CSharpLspHost is not null)
        {
            lspTask = TryRequestLspCompletionAsync(context, text, line, column, cancellationToken);
        }

        var roslynRaw = await roslynTask.ConfigureAwait(true);
        var roslynItems = roslynRaw.Select(i => new CideEditorCompletionItem(
            i.DisplayText,
            i.InsertText,
            i.Description,
            CideEditorCompletionKindMapper.FromRoslyn(i.Kind))).ToList();

        if (lspTask is not null)
        {
            var completed = await Task.WhenAny(lspTask, Task.Delay(CompletionLspBudget, cancellationToken)).ConfigureAwait(true);
            if (completed == lspTask && lspTask.IsCompletedSuccessfully)
            {
                var lspItems = await lspTask.ConfigureAwait(true);
                if (lspItems.Count > 0)
                {
                    var merged = CideEditorCompletionMerger.Merge(lspItems, roslynItems, prefix);
                    await PushCompletionAsync(context.Host, requestId, merged, cancellationToken).ConfigureAwait(true);
                    return;
                }
            }
        }

        await PushCompletionAsync(context.Host, requestId, roslynItems, cancellationToken).ConfigureAwait(true);
    }

    private static async Task<IReadOnlyList<CideEditorCompletionItem>> TryRequestLspCompletionAsync(
        MonacoEditorCapabilityContext context,
        string text,
        int line,
        int column,
        CancellationToken cancellationToken)
    {
        try
        {
            return await context.CSharpLspHost!
                .RequestCompletionAsync(context.FilePath, text, line, column, cancellationToken)
                .ConfigureAwait(true);
        }
        catch
        {
            return [];
        }
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
                await PushDefinitionOrNavigateAsync(context, requestId, mapped, cancellationToken).ConfigureAwait(true);
                return;
            }
        }

        var location = await Task.Run(
            () => context.CSharpLanguage.TryGetDefinitionLocation(context.FilePath, text, line, column, cancellationToken),
            cancellationToken).ConfigureAwait(true);
        mapped = location is null
            ? null
            : new CideEditorDefinitionLocation(location.FilePath, location.Line, location.Column);
        await PushDefinitionOrNavigateAsync(context, requestId, mapped, cancellationToken).ConfigureAwait(true);
    }

    private static async Task PushDefinitionOrNavigateAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        CideEditorDefinitionLocation? mapped,
        CancellationToken cancellationToken)
    {
        if (mapped is not null
            && !EditorTextCoordinateUtilities.PathsReferToSameFile(context.FilePath, mapped.FilePath))
        {
            if (context.NavigateToLocationAsync is not null)
                await context.NavigateToLocationAsync(mapped).ConfigureAwait(true);
            await PushDefinitionAsync(context.Host, requestId, null, cancellationToken).ConfigureAwait(true);
            return;
        }

        await PushDefinitionAsync(context.Host, requestId, mapped, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleReferencesAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        int line,
        int column,
        CancellationToken cancellationToken)
    {
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushReferencesAsync(context.Host, requestId, [], cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        IReadOnlyList<CideEditorReferenceLocation> locations;
        if (context.LspReady && context.CSharpLspHost is not null)
        {
            locations = await context.CSharpLspHost
                .RequestReferencesAsync(context.FilePath, text, line, column, cancellationToken)
                .ConfigureAwait(true);
            if (locations.Count > 0)
            {
                await PushReferencesAsync(context.Host, requestId, locations, cancellationToken).ConfigureAwait(true);
                return;
            }
        }

        var roslyn = await Task.Run(
            () => context.CSharpLanguage.FindReferencesInFile(context.FilePath, text, line, column, cancellationToken),
            cancellationToken).ConfigureAwait(true);
        locations = roslyn.Select(r => new CideEditorReferenceLocation(r.FilePath, r.Line, r.Column)).ToList();
        await PushReferencesAsync(context.Host, requestId, locations, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleFormatAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        CancellationToken cancellationToken)
    {
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushFormatAsync(context.Host, requestId, null, cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        var formatted = await Task.Run(
            () => context.CSharpLanguage.FormatDocument(context.FilePath, text, cancellationToken),
            cancellationToken).ConfigureAwait(true);
        await PushFormatAsync(context.Host, requestId, formatted, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleCodeActionAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        int line,
        int column,
        CancellationToken cancellationToken)
    {
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushCodeActionsAsync(context.Host, requestId, [], cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        var actions = await Task.Run(
            async () =>
            {
                var list = new List<CideEditorCodeActionItem>();
                var organized = context.CSharpLanguage.OrganizeUsings(context.FilePath, text, cancellationToken);
                if (!string.Equals(organized, text, StringComparison.Ordinal))
                {
                    list.Add(new CideEditorCodeActionItem(
                        "Organize Usings",
                        "source.organizeImports",
                        organized));
                }

                var formatted = context.CSharpLanguage.FormatDocument(context.FilePath, text, cancellationToken);
                if (!string.Equals(formatted, text, StringComparison.Ordinal))
                {
                    list.Add(new CideEditorCodeActionItem(
                        "Format Document",
                        "source.formatDocument",
                        formatted));
                }

                var solutionPath = ResolveRoslynWorkspacePath(context);
                if (!string.IsNullOrWhiteSpace(solutionPath))
                {
                    var roslyn = await RoslynMcpEditorIntelligence.ListCodeActionsAsync(
                        solutionPath,
                        context.FilePath,
                        text,
                        line,
                        column,
                        cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(roslyn.Error))
                    {
                        foreach (var item in roslyn.Actions)
                        {
                            list.Add(new CideEditorCodeActionItem(
                                item.Title,
                                item.Kind,
                                Text: null,
                                ActionIndex: item.Index));
                        }
                    }
                }

                return (IReadOnlyList<CideEditorCodeActionItem>)list;
            },
            cancellationToken).ConfigureAwait(true);
        await PushCodeActionsAsync(context.Host, requestId, actions, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleCodeActionApplyAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        int line,
        int column,
        int actionIndex,
        CancellationToken cancellationToken)
    {
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushWorkspaceEditAsync(context.Host, requestId, false, "unsupported_language", [], cancellationToken)
                .ConfigureAwait(true);
            return;
        }

        var solutionPath = ResolveRoslynWorkspacePath(context);
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            await PushWorkspaceEditAsync(context.Host, requestId, false, "no_solution", [], cancellationToken)
                .ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        var result = await Task.Run(
            () => RoslynMcpEditorIntelligence.ApplyCodeActionAsync(
                solutionPath,
                context.FilePath,
                text,
                line,
                column,
                actionIndex,
                cancellationToken),
            cancellationToken).ConfigureAwait(true);

        await ApplyRoslynResultAsync(context, requestId, result, cancellationToken).ConfigureAwait(true);
    }

    private static async Task HandleRenameAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        int line,
        int column,
        string newName,
        CancellationToken cancellationToken)
    {
        if (!CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushWorkspaceEditAsync(context.Host, requestId, false, "unsupported_language", [], cancellationToken)
                .ConfigureAwait(true);
            return;
        }

        var solutionPath = ResolveRoslynWorkspacePath(context);
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            await PushWorkspaceEditAsync(context.Host, requestId, false, "no_solution", [], cancellationToken)
                .ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        var result = await Task.Run(
            () => RoslynMcpEditorIntelligence.RenameAsync(
                solutionPath,
                context.FilePath,
                text,
                line,
                column,
                newName,
                cancellationToken),
            cancellationToken).ConfigureAwait(true);

        await ApplyRoslynResultAsync(context, requestId, result, cancellationToken).ConfigureAwait(true);
    }

    private static async Task ApplyRoslynResultAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        RoslynEditorApplyResult result,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            await PushWorkspaceEditAsync(context.Host, requestId, false, result.Error, [], cancellationToken)
                .ConfigureAwait(true);
            return;
        }

        var mapped = result.Changes
            .Select(c => new CideEditorDocumentTextChange(c.FilePath, c.Text, c.IsNewFile, c.PreviousFilePath))
            .ToList();
        if (context.ApplyWorkspaceChangesAsync is not null)
            await context.ApplyWorkspaceChangesAsync(mapped).ConfigureAwait(true);

        await PushWorkspaceEditAsync(context.Host, requestId, true, null, mapped, cancellationToken).ConfigureAwait(true);
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

    private static async Task HandleSemanticTokensAsync(
        MonacoEditorCapabilityContext context,
        int requestId,
        CancellationToken cancellationToken)
    {
        if (!context.LspReady
            || context.CSharpLspHost is not { SupportsSemanticTokens: true } host
            || !CideEditorLanguageIds.SupportsRoslynIntelligence(context.FilePath))
        {
            await PushSemanticTokensAsync(context.Host, requestId, [], null, cancellationToken).ConfigureAwait(true);
            return;
        }

        var text = context.GetEditorText();
        var tokens = await host.RequestSemanticTokensFullAsync(context.FilePath, text, cancellationToken)
            .ConfigureAwait(true);
        await PushSemanticTokensAsync(
            context.Host,
            requestId,
            tokens?.Data ?? [],
            tokens?.ResultId,
            cancellationToken).ConfigureAwait(true);
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
