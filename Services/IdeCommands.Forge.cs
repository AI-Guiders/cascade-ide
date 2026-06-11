namespace CascadeIDE.Services;

/// <summary>Forge Lens в CRS (ADR 0158, FORGE-ADR-0010).</summary>
public static partial class IdeCommands
{
    /// <summary>Device login к forge (браузер + approve, как Intercom OAuth). args: base_url?:string; returns: text; example: {"base_url":"http://127.0.0.1:8770"}.</summary>
    public const string ForgeLensConnect = "forge_lens.connect";

    /// <summary>Удалить сохранённый Bearer для forge host. args: base_url?:string; returns: text; example: {"base_url":"http://127.0.0.1:8770"}.</summary>
    public const string ForgeLensDisconnect = "forge_lens.disconnect";

    /// <summary>Статус Forge Lens auth для host. args: base_url?:string; returns: text; example: {"base_url":"http://127.0.0.1:8770"}.</summary>
    public const string ForgeLensAuthStatus = "forge_lens.auth_status";

    /// <summary>Создать issue в Forge (write gate B). args: title:string, body?:string, repo?:string, base_url?:string, file_path?:string, line_start?:integer, line_end?:integer, member_key?:string; returns: text; example: {"title":"Zone leak","file_path":"src/Zones.cs","line_start":10}.</summary>
    public const string ForgeLensCreateIssue = "forge_lens.create_issue";

    /// <summary>Создать merge request в Forge. args: title:string, source_branch:string, target_branch?:string, repo?:string, base_url?:string, file_path?:string, line_start?:integer, line_end?:integer; returns: text; example: {"title":"feat: zones","source_branch":"feat/zones"}.</summary>
    public const string ForgeLensCreateMergeRequest = "forge_lens.create_merge_request";
}
