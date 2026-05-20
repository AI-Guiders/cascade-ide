#nullable enable

using System.Text.RegularExpressions;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>Inline-метки attach в теле сообщения: <c>⟦a:{id}⟧</c> (ADR 0128).</summary>
public static class IntercomAttachmentMarkers
{
    public const char MarkerOpen = '\u27E6';
    public const char MarkerClose = '\u27E7';

    private static readonly Regex MarkerRegex = new(
        @"⟦a:(?<id>[0-9a-f]{8})⟧",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex BracketRegex = new(
        @"\[(?<inner>[^\[\]]+)\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string FormatMarker(string shortId) => $"{MarkerOpen}a:{shortId}{MarkerClose}";

    public static string NewShortId() => Guid.NewGuid().ToString("N")[..8];

    public static string FormatDisplayLabel(string label) => $"【{label}】";

    /// <summary>Текст кликабельной строки в ленте Intercom (подчёркнутый <c>[label]</c>).</summary>
    public static string FormatFeedLinkLabel(string label) => $"[{label}]";

    public static string ReplaceMarkersForDisplay(string content, IReadOnlyList<AttachmentAnchor> attachments)
    {
        if (string.IsNullOrEmpty(content))
            return content ?? "";

        var byId = attachments
            .Where(a => !string.IsNullOrWhiteSpace(a.Id))
            .ToDictionary(a => a.Id!, StringComparer.OrdinalIgnoreCase);

        return MarkerRegex.Replace(content, m =>
        {
            var id = m.Groups["id"].Value;
            if (!byId.TryGetValue(id, out var anchor))
                return m.Value;
            var label = anchor.DisplayLabel;
            return string.IsNullOrWhiteSpace(label) ? m.Value : FormatDisplayLabel(label);
        });
    }

    /// <summary>Разбить prose с wire-маркерами <c>⟦a:…⟧</c> на сегменты; текст attach — displayLabel.</summary>
    public static IReadOnlyList<IntercomAttachmentFeedSegment> SplitFeedSegments(
        string contentWithMarkers,
        IReadOnlyList<AttachmentAnchor> attachments)
    {
        if (string.IsNullOrEmpty(contentWithMarkers))
            return [new IntercomAttachmentFeedSegment(IntercomAttachmentFeedSegmentKind.Prose, "")];

        var byId = attachments
            .Where(a => !string.IsNullOrWhiteSpace(a.Id))
            .ToDictionary(a => a.Id!, StringComparer.OrdinalIgnoreCase);

        var list = new List<IntercomAttachmentFeedSegment>();
        var last = 0;
        foreach (Match m in MarkerRegex.Matches(contentWithMarkers))
        {
            if (m.Index > last)
            {
                var prose = contentWithMarkers[last..m.Index];
                if (prose.Length > 0)
                    list.Add(new IntercomAttachmentFeedSegment(IntercomAttachmentFeedSegmentKind.Prose, prose));
            }

            var id = m.Groups["id"].Value;
            byId.TryGetValue(id, out var anchor);
            var label = anchor?.DisplayLabel ?? id;
            list.Add(new IntercomAttachmentFeedSegment(
                IntercomAttachmentFeedSegmentKind.Attachment,
                FormatFeedLinkLabel(label),
                anchor,
                MarkerShortId: id));
            last = m.Index + m.Length;
        }

        if (last < contentWithMarkers.Length)
            list.Add(new IntercomAttachmentFeedSegment(IntercomAttachmentFeedSegmentKind.Prose, contentWithMarkers[last..]));

        return list.Count == 0
            ? [new IntercomAttachmentFeedSegment(IntercomAttachmentFeedSegmentKind.Prose, contentWithMarkers)]
            : list;
    }

    public static void UpdateProseOffsets(string content, IList<AttachmentAnchor> anchors)
    {
        if (anchors.Count == 0)
            return;

        var byId = anchors
            .Where(a => !string.IsNullOrWhiteSpace(a.Id))
            .Select((a, i) => (a, i))
            .ToDictionary(x => x.a.Id!, x => x.i, StringComparer.OrdinalIgnoreCase);

        foreach (Match m in MarkerRegex.Matches(content))
        {
            var id = m.Groups["id"].Value;
            if (!byId.TryGetValue(id, out var idx))
                continue;
            var a = anchors[idx];
            anchors[idx] = a with
            {
                ProseStart = m.Index,
                ProseLength = m.Length,
            };
        }
    }

    public static bool TryExtractBracketSpans(string prose, out List<(int Start, int Length, string Inner)> spans)
    {
        spans = [];
        foreach (Match m in BracketRegex.Matches(prose))
        {
            if (m.Groups["inner"].Value.Contains('`', StringComparison.Ordinal))
                continue;
            spans.Add((m.Index, m.Length, m.Groups["inner"].Value));
        }

        return spans.Count > 0;
    }
}

public enum IntercomAttachmentFeedSegmentKind
{
    Prose = 0,
    Attachment = 1,
}

public readonly record struct IntercomAttachmentFeedSegment(
    IntercomAttachmentFeedSegmentKind Kind,
    string Text,
    AttachmentAnchor? Anchor = null,
    string? MarkerShortId = null);
