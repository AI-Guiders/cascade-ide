#nullable enable

using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Chat;

public sealed class IntercomAttachmentRevealEventArgs(AttachmentAnchor anchor, bool select) : EventArgs
{
    public AttachmentAnchor Anchor { get; } = anchor;

    public bool Select { get; } = select;
}
