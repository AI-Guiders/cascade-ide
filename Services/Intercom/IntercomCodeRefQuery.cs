#nullable enable

using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>
/// Нормализованный запрос кода для <c>/intercom message find</c> / relate (ADR 0137).
/// Идентичность — <see cref="ResolvedAnchor"/> (memberKey, syntaxScope, shape); строки — снимок @ query time, не ключ.
/// </summary>
public sealed record IntercomCodeRefQuery(
    string File,
    int? LineStart,
    int? LineEnd,
    AttachmentAnchor? ResolvedAnchor = null)
{
    public bool HasLineRange => LineStart is not null && LineEnd is not null;

    public string? MemberKey => ResolvedAnchor?.MemberKey;

    public static IntercomCodeRefQuery FromAnchor(AttachmentAnchor anchor)
    {
        var file = (anchor.File ?? "").Replace('\\', '/');
        int? start = anchor.LineStart;
        int? end = anchor.LineEnd ?? anchor.LineStart;
        if (end is not null && start is not null && end < start)
            end = start;

        return new IntercomCodeRefQuery(file, start, end, anchor);
    }
}
