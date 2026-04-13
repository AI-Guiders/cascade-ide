namespace CascadeIDE.Models;

/// <summary>Настройки семантической навигации (<c>[workspace_navigation_context]</c> в <c>settings.toml</c> и опционально в <c>.cascade/workspace.toml</c>).</summary>
public sealed class WorkspaceNavigationContextSettings
{
    /// <summary>
    /// Overlay поверх шипнутого бандла и поверх репо: <c>[[workspace_navigation_context.presets]]</c>.
    /// Пустой список — без пользовательского слоя; совпадающий по <see cref="WorkspaceNavigationPresetEntry.Id"/> пресет заменяет нижележащие слои (репо и IDE).
    /// </summary>
    public List<WorkspaceNavigationPresetEntry> Presets { get; set; } = [];
}
