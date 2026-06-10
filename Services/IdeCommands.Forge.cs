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
}
