namespace CascadeIDE.Cockpit.Channels.Eicas;

/// <summary>Одно оповещение для канала EICAS, отдельно от телеметрии контура работы.</summary>
/// <remarks>
/// Та же шкала внимания W/C/A, что и <see cref="CascadeIDE.Models.AnnunciatorLampLevel"/> на экране готовности окружения (<see cref="CascadeIDE.Models.AnnunciatorLampItem"/>);
/// это отдельный канал и коллекция сообщений, не смешивать со снимком readiness.
/// </remarks>
public sealed record EicasMessage(
    EicasSeverity Severity,
    string Text,
    string? SourceId = null,
    DateTimeOffset? CreatedUtc = null);
