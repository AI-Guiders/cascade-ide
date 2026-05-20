#nullable enable

using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Сборка исходящего сообщения: prose, bracket → markers, attachments @ send (ADR 0128, 0134).</summary>
public static class IntercomAttachmentMessageBuilder
{
    public sealed record Outbound(
        string Content,
        IReadOnlyList<AttachmentAnchor> Attachments,
        SenderWorkspaceContext? SenderWorkspaceContext);

    public static bool TryBuild(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out Outbound outbound,
        out string error) =>
        TryBuildCore(
            rawInput,
            pendingByShortId,
            editor,
            workspaceRoot,
            solutionPath,
            allowDegradedMemberResolve: false,
            skipMemberRoslynAtSend: false,
            captureSenderWorkspaceContext: true,
            warnings: null,
            out outbound,
            out error);

    public static bool TryPrepare(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out PreparedIntercomMessage prepared)
    {
        var warnings = new List<string>();
        if (!TryBuildCore(
                rawInput,
                pendingByShortId,
                editor,
                workspaceRoot,
                solutionPath,
                allowDegradedMemberResolve: true,
                skipMemberRoslynAtSend: false,
                captureSenderWorkspaceContext: true,
                warnings,
                out var outbound,
                out var error))
        {
            prepared = new PreparedIntercomMessage(
                IntercomMessagePrepareStatus.Failed,
                outbound,
                warnings,
                error);
            return false;
        }

        return finishPrepare(outbound, warnings, out prepared);
    }

    /// <summary>MCP fast-path: F/M/L/S @ send без Roslyn и без excerpt; L: строки из bracket; M/S — re-resolve при reveal.</summary>
    public static bool TryPrepareForMcp(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        out PreparedIntercomMessage prepared)
    {
        var warnings = new List<string>();
        if (!TryBuildCore(
                rawInput,
                pendingByShortId,
                editor,
                workspaceRoot,
                solutionPath,
                allowDegradedMemberResolve: true,
                skipMemberRoslynAtSend: true,
                captureSenderWorkspaceContext: false,
                warnings,
                out var outbound,
                out var error))
        {
            prepared = new PreparedIntercomMessage(
                IntercomMessagePrepareStatus.Failed,
                outbound,
                warnings,
                error);
            return false;
        }

        if (outbound.Attachments.Count > 0)
            warnings.Add("MCP fast-path (F/M/L/S): Roslyn и excerpt @ send отложены; reveal — re-resolve.");

        return finishPrepare(outbound, warnings, out prepared);
    }

    private static bool finishPrepare(
        Outbound outbound,
        List<string> warnings,
        out PreparedIntercomMessage prepared)
    {
        var hasDegraded = outbound.Attachments.Any(static a =>
            string.Equals(
                a.ResolveOutcome,
                IntercomAttachmentRevealPlan.OutcomeMemberNotFound,
                StringComparison.Ordinal));

        if (hasDegraded && !warnings.Any(static w => w.Contains("re-resolve", StringComparison.OrdinalIgnoreCase)))
            warnings.Add("Часть вложений собрана без строк Roslyn — reveal попробует re-resolve.");

        var status = hasDegraded
            ? IntercomMessagePrepareStatus.PartialSuccess
            : IntercomMessagePrepareStatus.Success;

        prepared = new PreparedIntercomMessage(status, outbound, warnings, null);
        return true;
    }

    private static bool TryBuildCore(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        bool allowDegradedMemberResolve,
        bool skipMemberRoslynAtSend,
        bool captureSenderWorkspaceContext,
        List<string>? warnings,
        out Outbound outbound,
        out string error)
    {
        outbound = new Outbound("", [], null);
        error = "";

        var trimmed = rawInput.Trim();
        if (trimmed.Length == 0)
        {
            error = "Пустое сообщение.";
            return false;
        }

        workspaceRoot = coalesceWorkspaceRoot(workspaceRoot, solutionPath);

        var resolveSession = skipMemberRoslynAtSend ? null : new IntercomAttachmentRoslynResolveSession();
        var segments = ChatMessageBodyPresentation.SplitSegments(trimmed);
        var attachments = new List<AttachmentAnchor>();
        var rebuilt = new System.Text.StringBuilder();

        foreach (var segment in segments)
        {
            if (segment.Kind == ChatMessageBodySegmentKind.Code)
            {
                rebuilt.Append(segment.Text);
                continue;
            }

            if (!tryProcessProseSegment(
                    segment.Text,
                    pendingByShortId,
                    editor,
                    workspaceRoot,
                    solutionPath,
                    resolveSession,
                    allowDegradedMemberResolve,
                    skipMemberRoslynAtSend,
                    warnings,
                    attachments,
                    rebuilt,
                    out error))
            {
                return false;
            }
        }

        var content = rebuilt.ToString();
        IntercomAttachmentMarkers.UpdateProseOffsets(content, attachments);

        var senderContext = captureSenderWorkspaceContext
            ? IntercomSenderWorkspaceContextCapture.TryCapture(workspaceRoot, solutionPath)
            : null;
        outbound = new Outbound(content, attachments, senderContext);
        return true;
    }

    private static string? coalesceWorkspaceRoot(string? workspaceRoot, string? solutionPath)
    {
        if (!string.IsNullOrWhiteSpace(workspaceRoot))
            return workspaceRoot.Trim();
        var dir = WorkspaceDirectoryFromSolutionPath.Resolve(solutionPath ?? "");
        return dir.Length > 0 ? dir : null;
    }

    private static bool tryProcessProseSegment(
        string prose,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        IntercomAttachmentRoslynResolveSession? resolveSession,
        bool allowDegradedMemberResolve,
        bool skipMemberRoslynAtSend,
        List<string>? warnings,
        List<AttachmentAnchor> attachments,
        System.Text.StringBuilder rebuilt,
        out string error)
    {
        error = "";
        var spans = new List<(int Start, int Length, string Inner, bool IsBracket)>();

        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                     prose,
                     @"⟦a:(?<id>[0-9a-f]{8})⟧"))
        {
            spans.Add((m.Index, m.Length, m.Groups["id"].Value, false));
        }

        if (IntercomAttachmentMarkers.TryExtractBracketSpans(prose, out var brackets))
        {
            foreach (var (start, length, inner) in brackets)
                spans.Add((start, length, inner, true));
        }

        spans.Sort((a, b) => a.Start.CompareTo(b.Start));
        var last = 0;
        foreach (var span in spans)
        {
            if (span.Start < last)
                continue;

            if (span.Start > last)
                rebuilt.Append(prose[last..span.Start]);

            if (span.IsBracket)
            {
                if (!BracketCodeReferenceParser.TryParse(span.Inner, out var reference, out error))
                    return false;

                if (!IntercomAttachmentResolveAtSend.TryResolveBracketDraft(
                        reference,
                        editor.CurrentFilePath,
                        workspaceRoot,
                        out var draft,
                        out error))
                {
                    return false;
                }

                var shortId = IntercomAttachmentMarkers.NewShortId();
                if (!IntercomAttachmentResolveAtSend.TryAssignIdAndResolve(
                        draft,
                        shortId,
                        workspaceRoot,
                        solutionPath,
                        resolveSession,
                        allowDegradedMemberResolve,
                        skipMemberRoslynAtSend,
                        out var resolved,
                        out error))
                {
                    return false;
                }

                if (allowDegradedMemberResolve
                    && string.Equals(
                        resolved.ResolveOutcome,
                        IntercomAttachmentRevealPlan.OutcomeMemberNotFound,
                        StringComparison.Ordinal))
                {
                    warnings?.Add(
                        $"Вложение «{resolved.DisplayLabel}»: member не разрешён @ send, будет re-resolve при клике.");
                }

                attachments.Add(resolved);
                rebuilt.Append(IntercomAttachmentMarkers.FormatMarker(shortId));
            }
            else
            {
                if (!pendingByShortId.TryGetValue(span.Inner, out var pending))
                {
                    error = $"Неизвестный attach id «{span.Inner}» в черновике.";
                    return false;
                }

                var shortId = span.Inner;
                if (!IntercomAttachmentResolveAtSend.TryAssignIdAndResolve(
                        pending,
                        shortId,
                        workspaceRoot,
                        solutionPath,
                        resolveSession,
                        allowDegradedMemberResolve,
                        skipMemberRoslynAtSend,
                        out var resolved,
                        out error))
                {
                    return false;
                }

                attachments.Add(resolved);
                rebuilt.Append(IntercomAttachmentMarkers.FormatMarker(shortId));
            }

            last = span.Start + span.Length;
        }

        if (last < prose.Length)
            rebuilt.Append(prose[last..]);

        return true;
    }
}
