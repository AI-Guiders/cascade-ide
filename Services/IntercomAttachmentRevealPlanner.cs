#nullable enable

using System.Text.Json;
using CascadeIDE.Models.Editor;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Services;

/// <summary>План reveal по <see cref="AttachmentAnchor"/> у получателя (ADR 0128 §8–9.1).</summary>
public sealed class IntercomAttachmentRevealPlan
{
    public const string OutcomeResolved = "resolved";
    public const string OutcomeFileMissing = "file_missing";
    public const string OutcomeMemberNotFound = "member_not_found";
    public const string OutcomeLinesDrift = "lines_drift";
    public const string OutcomeExcerptOnly = "excerpt_only";

    public string ResolveOutcome { get; init; } = OutcomeExcerptOnly;

    public string? AbsoluteFilePath { get; init; }

    public LineRange? Lines { get; init; }

    public bool OpenFileOnly { get; init; }

    public string Message { get; init; } = "";

    public static IntercomAttachmentRevealPlan Create(AttachmentAnchor anchor, string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(anchor.File))
        {
            return new IntercomAttachmentRevealPlan
            {
                ResolveOutcome = OutcomeExcerptOnly,
                Message = "excerpt_only: нет file в anchor.",
            };
        }

        if (!AttachmentAnchorPaths.TryResolveAbsolute(anchor.File, workspaceRoot, out var absolute, out var pathErr))
        {
            return new IntercomAttachmentRevealPlan
            {
                ResolveOutcome = OutcomeFileMissing,
                Message = $"file_missing: {pathErr}",
            };
        }

        if (!File.Exists(absolute))
        {
            var hint = string.IsNullOrWhiteSpace(anchor.Excerpt) ? "" : " excerpt доступен в anchor.";
            return new IntercomAttachmentRevealPlan
            {
                ResolveOutcome = OutcomeFileMissing,
                AbsoluteFilePath = absolute,
                Message = $"file_missing: «{anchor.File}» нет в текущем workspace.{hint}",
            };
        }

        var hasMember = !string.IsNullOrWhiteSpace(anchor.MemberKey);
        AttachmentSyntaxScope.TryParse(anchor.SyntaxScope, out var syntaxScope);
        var hasScope = syntaxScope is not null;
        var hasLines = tryBuildLineRange(anchor.LineStart, anchor.LineEnd, out var lines);

        if (hasMember || hasScope)
        {
            if (AttachmentAnchorRoslynResolver.TryResolveLineRange(
                    absolute,
                    anchor.MemberKey,
                    syntaxScope,
                    out var resolved,
                    out var resolveDetail))
            {
                return new IntercomAttachmentRevealPlan
                {
                    ResolveOutcome = OutcomeResolved,
                    AbsoluteFilePath = absolute,
                    Lines = resolved,
                    Message = $"OK resolveOutcome={OutcomeResolved} lines={resolved.Start.Value}-{resolved.End.Value} ({resolveDetail})",
                };
            }

            if (hasLines)
            {
                return new IntercomAttachmentRevealPlan
                {
                    ResolveOutcome = OutcomeMemberNotFound,
                    AbsoluteFilePath = absolute,
                    Lines = lines,
                    Message = $"member_not_found: {resolveDetail}; fallback lines {lines!.Value.Start.Value}–{lines.Value.End.Value} @ send.",
                };
            }

            return new IntercomAttachmentRevealPlan
            {
                ResolveOutcome = OutcomeMemberNotFound,
                AbsoluteFilePath = absolute,
                OpenFileOnly = true,
                Message = $"member_not_found: {resolveDetail}; диапазон строк не задан.",
            };
        }

        if (!hasLines)
        {
            return new IntercomAttachmentRevealPlan
            {
                ResolveOutcome = OutcomeExcerptOnly,
                AbsoluteFilePath = absolute,
                OpenFileOnly = true,
                Message = "excerpt_only: открыт файл без подсветки диапазона.",
            };
        }

        return new IntercomAttachmentRevealPlan
        {
            ResolveOutcome = OutcomeResolved,
            AbsoluteFilePath = absolute,
            Lines = lines,
            Message = $"OK resolveOutcome={OutcomeResolved} lines={lines!.Value.Start.Value}-{lines.Value.End.Value}",
        };
    }

    private static bool tryBuildLineRange(int? start, int? end, out LineRange? lines)
    {
        lines = null;
        if (!start.HasValue || !end.HasValue)
            return false;

        if (!LineNumber.TryCreate(start.Value, out var lnStart))
            return false;
        if (!LineNumber.TryCreate(end.Value, out var lnEnd))
            return false;
        if (!LineRange.TryCreate(lnStart, lnEnd, out var range))
            return false;

        lines = range;
        return true;
    }
}
