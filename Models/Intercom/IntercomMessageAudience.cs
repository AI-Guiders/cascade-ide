using System.Text.Json.Serialization;

namespace CascadeIDE.Models.Intercom;

/// <summary>
/// Кому видно сообщение в Intercom. <see cref="Channel"/> — общая лента (агент, экспорт, федерация).
/// <see cref="SelfOnly"/> — только оператор на этом клиенте (результаты слэш-команд, <c>/help</c>; ADR 0119).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<IntercomMessageAudience>))]
public enum IntercomMessageAudience
{
    /// <summary>Обычное сообщение канала (по умолчанию).</summary>
    Channel = 0,

    /// <summary>Локально для текущего пользователя; не уходит агенту и во внешний Intercom.</summary>
    SelfOnly = 1,
}
