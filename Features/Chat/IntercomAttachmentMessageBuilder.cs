#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Сборка исходящего сообщения: prose, bracket → markers, attachments @ send (ADR 0128).</summary>
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

        var resolveSession = new IntercomAttachmentRoslynResolveSession();
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
                    attachments,
                    rebuilt,
                    out error))
            {
                return false;
            }
        }

        var content = rebuilt.ToString();
        IntercomAttachmentMarkers.UpdateProseOffsets(content, attachments);

        var senderContext = IntercomSenderWorkspaceContextCapture.TryCapture(workspaceRoot, solutionPath);
        outbound = new Outbound(content, attachments, senderContext);
        return true;
    }

    private static bool tryProcessProseSegment(
        string prose,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        IntercomAttachmentRoslynResolveSession resolveSession,
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

                if (!IntercomAttachmentResolveAtSend.TryResolveBracket(
                        reference,
                        editor.CurrentFilePath,
                        workspaceRoot,
                        solutionPath,
                        resolveSession,
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
                        out var resolved,
                        out error))
                {
                    return false;
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
