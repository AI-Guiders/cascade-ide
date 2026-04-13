namespace CascadeIDE.Models;

/// <summary>Настройки семантической навигации (<c>[workspace_navigation_context]</c> в <c>settings.toml</c>).</summary>
public sealed class WorkspaceNavigationContextSettings
{
    /// <summary>
    /// JSON-объект: ключ — id пресета, значение — <c>{"include_kinds":[...], "exclude_kinds":[...]}</c> (поля опциональны).
    /// </summary>
    public string PresetsJson { get; set; } = DefaultPresetsJson;

    /// <summary>Встроенные пресеты по умолчанию (можно переопределить в TOML).</summary>
    public const string DefaultPresetsJson =
        """
        {
          "peers_only": { "include_kinds": ["partial_peer", "project_peer"] },
          "no_namespace_noise": { "exclude_kinds": ["same_namespace", "same_directory"] },
          "tests_and_peers": { "include_kinds": ["partial_peer", "project_peer", "test_counterpart"] },
          "structure_only": { "include_kinds": ["partial_peer", "project_peer", "xaml_codebehind_pair", "same_directory"] }
        }
        """;
}
