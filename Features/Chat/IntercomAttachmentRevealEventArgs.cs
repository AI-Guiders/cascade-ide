#nullable enable

using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Chat;

public sealed class IntercomAttachmentRevealEventArgs(AttachmentAnchor anchor, bool select, int? messageIndex = null) : EventArgs
{
    public AttachmentAnchor Anchor { get; } = anchor;

    public bool Select { get; } = select;

    public int? MessageIndex { get; } = messageIndex;
}
