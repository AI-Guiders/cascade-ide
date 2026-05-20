namespace CascadeIDE.Models;

/// <summary>Диагностические логи IDE. TOML: <c>[logging]</c> и вложенные секции.</summary>
public sealed class LoggingSettings
{
    /// <summary>Intercom send / attach @ send. TOML: <c>[logging.intercom]</c>.</summary>
    public IntercomLoggingSettings Intercom { get; set; } = new();
}

/// <summary>TOML: <c>[logging.intercom]</c>.</summary>
public sealed class IntercomLoggingSettings
{
    /// <summary>Фазовый trace отправки в <c>{workspace}/.cascade-ide/intercom-send-trace.log</c>.</summary>
    public bool SendTrace { get; set; }
}
