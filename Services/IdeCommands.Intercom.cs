namespace CascadeIDE.Services;

/// <summary>Команды Intercom (attach, reveal из ленты).</summary>
public static partial class IdeCommands
{
    /// <summary>Reveal из ленты по AttachmentAnchor: open + re-resolve + highlight (ADR 0128 §8, 0130). args: anchor_json?:object, file?:string, line_start?:integer, line_end?:integer, member_key?:string, syntax_scope?:object, duration_ms?:integer, select?:boolean; если select опущен — дефолт из settings intercom.attachment_navigate; returns: text; example: {"file":"src/Foo.cs","line_start":10,"line_end":25}.</summary>
    public const string IntercomRevealAttachment = "intercom.reveal_attachment";

    /// <summary>Select в редакторе по bracket-ссылке (ADR 0131). args: code_ref:string, active_file?:string, duration_ms?:integer; returns: text; example: {"code_ref":"[M:Run]","active_file":"src/Foo.cs"}.</summary>
    public const string EditorSelectCode = "editor.select_code";

    /// <summary>Reveal в редакторе по bracket-ссылке (ADR 0131). args: code_ref:string, active_file?:string, duration_ms?:integer; returns: text; example: {"code_ref":"[M:Run]","active_file":"src/Foo.cs","duration_ms":4000}.</summary>
    public const string EditorRevealCode = "editor.reveal_code";
}
