namespace CascadeIDE.Services;

/// <summary>Команды Intercom (attach, reveal из ленты).</summary>
public static partial class IdeCommands
{
    /// <summary>Reveal из ленты по AttachmentAnchor: open + re-resolve + transient highlight (ADR 0128 §8, 0130). args: anchor_json?:object, file?:string, line_start?:integer, line_end?:integer, member_key?:string, syntax_scope?:object, duration_ms?:integer, select?:boolean; returns: text; example: {"file":"src/Foo.cs","line_start":10,"line_end":25}.</summary>
    public const string IntercomRevealAttachment = "intercom.reveal_attachment";
}
