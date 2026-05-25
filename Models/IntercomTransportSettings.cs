namespace CascadeIDE.Models;

/// <summary>Federated team transport (ADR 0144). TOML: <c>[intercom.transport]</c>.</summary>
public sealed class IntercomTransportSettings
{
    /// <summary>Включить FederatedSync (нужны <c>base_url</c> и team id).</summary>
    public bool Enabled { get; set; }

    /// <summary>Базовый URL reference Intercom service, без завершающего <c>/</c>.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Last selected <c>team_id</c> (кэш, не SSOT).</summary>
    public string TeamId { get; set; } = "";

    /// <summary>Topic на сервере; пусто — topic <c>general</c> после bootstrap.</summary>
    public string DefaultTopicId { get; set; } = "";

    /// <summary>OAuth provider для Connect (<c>provider_id</c> из <c>/auth/providers</c>).</summary>
    public string OAuthProvider { get; set; } = "github";

    /// <summary>Опциональный invite token для join (ADR 0147 §2.1).</summary>
    public string InviteToken { get; set; } = "";

    /// <summary>DEV: shared team Bearer вместо JWT (только локальная разработка).</summary>
    public string DevTeamToken { get; set; } = "";

    /// <summary>Local hint per normalized repo URL. TOML: <c>[intercom.transport.workspace_hints."…"]</c>.</summary>
    public Dictionary<string, IntercomWorkspaceHintEntry> WorkspaceHints { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Начальная задержка переподключения SSE, мс.</summary>
    public int SseReconnectBackoffMs { get; set; } = 1000;

    /// <summary>При первом human send в Channel — предложить Connect (OAuth), если ещё не подключено.</summary>
    public bool AutoConnectOnSend { get; set; } = true;

    /// <summary>Синхронизировать ответы ассистента в Channel (<c>sender_role: agent</c>).</summary>
    public bool SyncAgentChannelMessages { get; set; } = true;

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(BaseUrl);
}
