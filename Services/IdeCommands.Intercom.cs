namespace CascadeIDE.Services;

/// <summary>Команды Intercom (attach, reveal из ленты).</summary>
public static partial class IdeCommands
{
    /// <summary>Reveal из ленты по AttachmentAnchor: open + re-resolve + highlight (ADR 0128 §8, 0130). args: anchor_json?:object, file?:string, line_start?:integer, line_end?:integer, member_key?:string, syntax_scope?:object, duration_ms?:integer, select?:boolean; если select опущен — дефолт из settings [intercom.attachments.code].navigate; returns: text; example: {"file":"src/Foo.cs","line_start":10,"line_end":25}.</summary>
    public const string IntercomRevealAttachment = "intercom.reveal_attachment";

    /// <summary>Select в редакторе по bracket-ссылке (ADR 0131). args: code_ref:string, active_file?:string, duration_ms?:integer; returns: text; example: {"code_ref":"[M:Run]","active_file":"src/Foo.cs"}.</summary>
    public const string EditorSelectCode = "editor.select_code";

    /// <summary>Reveal в редакторе по bracket-ссылке (ADR 0131). args: code_ref:string, active_file?:string, duration_ms?:integer; returns: text; example: {"code_ref":"[M:Run]","active_file":"src/Foo.cs","duration_ms":4000}.</summary>
    public const string EditorRevealCode = "editor.reveal_code";

    /// <summary>Сообщения активной detail-ветки по фрагменту кода (ADR 0137 inferred + explicit relate). args: use_selection?:boolean, code_ref?:string, anchor_json?:object, file?:string, line_start?:integer, line_end?:integer; returns: json; example: {"use_selection":true}.</summary>
    public const string IntercomMessagesForCode = "intercom.messages_for_code";

    /// <summary>Явная связь диапазона gutter-сообщений с кодом (ADR 0137). args: start_ordinal:integer, end_ordinal?:integer, use_selection?:boolean, code_ref?:string, anchor_json?:object, file?:string, line_start?:integer, line_end?:integer; returns: json; example: {"start_ordinal":3,"end_ordinal":5,"use_selection":true}.</summary>
    public const string IntercomMessageRelate = "intercom.message_relate";
}
