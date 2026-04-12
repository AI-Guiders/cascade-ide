namespace CascadeIDE.Cockpit.Channels.Eicas;

/// <summary>Одно оповещение для канала EICAS, отдельно от телеметрии контура работы.</summary>
public sealed record EicasMessage(
    EicasSeverity Severity,
    string Text,
    string? SourceId = null,
    DateTimeOffset? CreatedUtc = null);
