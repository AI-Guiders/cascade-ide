namespace CascadeIDE.Models;

/// <summary>Заметки агента / knowledge MCP в IDE. TOML: <c>[agent_notes]</c>.</summary>
public sealed class AgentNotesSettings
{
    /// <summary>
    /// Путь к TOML agent-notes (тот же файл, что <c>--config</c> в <c>mcp.json</c> для agent-notes-mcp).
    /// Относительные пути — от <c>%LocalAppData%\CascadeIDE\</c>. TOML: <c>config_path</c>.
    /// </summary>
    public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Корень канона с каталогом <c>knowledge/</c> поверх встроенного KB-Base (чтение: файлы здесь переопределяют встроенные).
    /// Относительные пути считаются от каталога <c>%LocalAppData%\CascadeIDE\</c>. TOML: <c>kb_base_overlay_path</c>.
    /// </summary>
    public string KbBaseOverlayPath { get; set; } = "";
}
