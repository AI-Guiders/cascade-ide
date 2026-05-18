namespace CascadeIDE.Features.Chat;

/// <summary>Статус локальной слэш-команды в ленте Intercom.</summary>
public enum ChatSlashCommandStatus
{
    Running = 0,
    Succeeded = 1,
    Failed = 2,
    Cancelled = 3,
}

/// <summary>Угол иконки статуса (в будущем — из настроек).</summary>
public enum ChatSlashCommandStatusIconPlacement
{
    TopRight = 0,
    TopLeft = 1,
    BottomRight = 2,
    BottomLeft = 3,
}
