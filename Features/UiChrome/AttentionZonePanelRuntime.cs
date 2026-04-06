using System.Collections.Immutable;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Карта «поверхность → зона внимания», загружаемая из <c>UiModes/workspace.toml</c> (секция <c>attention_zone_panels</c>, ADR 0021).
/// Дефолты совпадают с семантикой ADR; TOML накладывает переопределения.
/// </summary>
public static class AttentionZonePanelRuntime
{
    private static ImmutableDictionary<string, AttentionZone> _byPanel = BuildDefaultMap();

    private static ImmutableDictionary<string, AttentionZone> BuildDefaultMap()
    {
        var b = ImmutableDictionary.CreateBuilder<string, AttentionZone>(StringComparer.OrdinalIgnoreCase);
        b.Add(AttentionPanelIds.SolutionExplorer, AttentionZone.Pfd);
        b.Add(AttentionPanelIds.ChatPanel, AttentionZone.Mfd);
        b.Add(AttentionPanelIds.Git, AttentionZone.Mfd);
        b.Add(AttentionPanelIds.TerminalDock, AttentionZone.Mfd);
        b.Add(AttentionPanelIds.Editor, AttentionZone.Frontal);
        return b.ToImmutable();
    }

    /// <summary>Сброс к дефолтам из кода (тесты, повторная загрузка каталога).</summary>
    internal static void ResetToCodeDefaults() =>
        _byPanel = BuildDefaultMap();

    /// <summary>Применяет метрики workspace и карту панелей из распарсенного TOML.</summary>
    internal static void ApplyWorkspaceToml(UiWorkspaceToml? w)
    {
        var map = BuildDefaultMap();
        if (w?.AttentionZonePanels is { Count: > 0 } overrides)
        {
            var b = map.ToBuilder();
            foreach (var kv in overrides)
            {
                var key = kv.Key.Trim();
                if (key.Length == 0)
                    continue;
                var raw = kv.Value?.Trim();
                if (string.IsNullOrEmpty(raw))
                    continue;
                if (AttentionZoneExtensions.TryParseCanonicalId(raw, out var zone))
                    b[key] = zone;
                else
                    global::System.Diagnostics.Debug.WriteLine($"AttentionZonePanelRuntime: unknown zone id — {raw} (panel {key})");
            }

            map = b.ToImmutable();
        }

        _byPanel = map;
    }

    /// <summary>Зона для известной поверхности; <see langword="false"/>, если id не задан в карте.</summary>
    public static bool TryGetZone(string panelId, out AttentionZone zone) =>
        _byPanel.TryGetValue(panelId, out zone);

    /// <summary>Текущая карта (после загрузки <c>workspace.toml</c>).</summary>
    public static IReadOnlyDictionary<string, AttentionZone> CurrentMap => _byPanel;
}
