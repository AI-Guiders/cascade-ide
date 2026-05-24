namespace CascadeIDE.Models;

/// <summary>Federated team transport (ADR 0144). TOML: <c>[intercom.transport]</c>.</summary>
public sealed class IntercomTransportSettings
{
    /// <summary>Включить FederatedSync (нужны <c>base_url</c> и team id).</summary>
    public bool Enabled { get; set; }

    /// <summary>Базовый URL reference Intercom service, без завершающего <c>/</c>.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Явный <c>team_id</c>; пусто — из <c>.cascade-ide/intercom-team.toml</c> в git root.</summary>
    public string TeamId { get; set; } = "";

    /// <summary>Topic на сервере; пусто — topic <c>general</c> после bootstrap.</summary>
    public string DefaultTopicId { get; set; } = "";

    /// <summary>OAuth provider для Connect: <c>github</c> (пилот).</summary>
    public string OAuthProvider { get; set; } = "github";

    /// <summary>DEV: shared team Bearer вместо JWT (только локальная разработка).</summary>
    public string DevTeamToken { get; set; } = "";

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
