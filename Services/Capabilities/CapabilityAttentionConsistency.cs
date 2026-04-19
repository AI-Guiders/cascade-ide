using CascadeIDE.Contracts.Experimental.Capabilities;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services.Capabilities;

/// <summary>
/// Проверка согласованности <see cref="UiSurfaceCapabilityDescriptor.PrimaryAttentionZoneId"/> и
/// <see cref="UiSurfaceCapabilityDescriptor.HostAttentionPanelId"/> с текущей картой
/// <see cref="AttentionZonePanelRuntime"/> (дефолты + overlay из workspace TOML).
/// </summary>
public static class CapabilityAttentionConsistency
{
    /// <summary>
    /// Если заданы оба поля, возвращает сообщение при неизвестной панели или рассинхроне зоны с рантайм-картой; иначе <see langword="null"/>.
    /// </summary>
    public static string? TryGetUiSurfaceIssue(UiSurfaceCapabilityDescriptor d)
    {
        if (string.IsNullOrWhiteSpace(d.HostAttentionPanelId) || string.IsNullOrWhiteSpace(d.PrimaryAttentionZoneId))
            return null;

        var panelId = d.HostAttentionPanelId.Trim();
        var zoneId = d.PrimaryAttentionZoneId.Trim();

        if (!AttentionZonePanelRuntime.TryGetZone(panelId, out var zone))
        {
            return
                $"UiSurface '{d.Id}': host panel '{panelId}' is not in AttentionZonePanelRuntime (add default map entry or fix id).";
        }

        var canonical = zone.ToCanonicalId();
        if (canonical != zoneId)
        {
            return
                $"UiSurface '{d.Id}': PrimaryAttentionZoneId '{zoneId}' does not match AttentionZonePanelRuntime for panel '{panelId}' (expected '{canonical}').";
        }

        return null;
    }
}
