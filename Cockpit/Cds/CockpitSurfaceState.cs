using System.Text.Json.Serialization;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// CDS (контракт кабины), слой ADR 0036 п.2: семантический снимок без дерева контролов и без полезной нагрузки каналов
/// (см. <c>docs/design/cds-contract-v0.md</c>, ADR 0036).
/// </summary>
public sealed record CockpitSurfaceState(
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("ui_mode")] string UiMode,
    [property: JsonPropertyName("presentation_effective_line")] string PresentationEffectiveLine,
    [property: JsonPropertyName("presentation_parse_success")] bool PresentationParseSuccess,
    [property: JsonPropertyName("topology")] CockpitSurfaceTopology Topology,
    [property: JsonPropertyName("secondary_shell")] CockpitSurfaceSecondaryShell SecondaryShell,
    [property: JsonPropertyName("zones")] CockpitSurfaceZones Zones,
    [property: JsonPropertyName("instruments")] IReadOnlyList<CockpitSurfaceInstrument> Instruments);

public sealed record CockpitSurfaceTopology(
    [property: JsonPropertyName("surface_kind")] string SurfaceKind,
    [property: JsonPropertyName("mfd_host_window_open")] bool MfdHostWindowOpen,
    [property: JsonPropertyName("pfd_host_window_open")] bool PfdHostWindowOpen,
    [property: JsonPropertyName("mfd_column_visible_in_main")] bool MfdColumnVisibleInMain);

public sealed record CockpitSurfaceSecondaryShell(
    [property: JsonPropertyName("current_page")] string CurrentPage);

public sealed record CockpitSurfaceZones(
    [property: JsonPropertyName("pfd_visible")] bool PfdVisible,
    [property: JsonPropertyName("forward_visible")] bool ForwardVisible,
    [property: JsonPropertyName("mfd_visible")] bool MfdVisible,
    [property: JsonPropertyName("pfd_required_by_presentation")] bool PfdRequiredByPresentation,
    [property: JsonPropertyName("forward_required_by_presentation")] bool ForwardRequiredByPresentation,
    [property: JsonPropertyName("mfd_required_by_presentation")] bool MfdRequiredByPresentation);

public sealed record CockpitSurfaceInstrument(
    [property: JsonPropertyName("instrument_id")] string InstrumentId,
    [property: JsonPropertyName("slot_id")] string SlotId,
    [property: JsonPropertyName("schema_version")] string SchemaVersion);
