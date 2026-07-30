# Glass WPF · reuse CIDE settings/topology (v0)

## Decision

`CDP.GlassCockpit.Windows` does **not** invent a second settings/topology SSOT.

| Layer | Source |
|---|---|
| Layout math | `CascadeIDE.GlassCore` → linked `Services/Presentation/*` (parser, topology flags, main-grid frame) |
| Durable settings (user) | `%LocalAppData%/CascadeIDE/settings.toml` |
| Repo overlay | `<repo>/.cascade/workspace.toml` |
| Live desk patch | `%LocalAppData%/cdp-mcp/presentation-LATEST.json` (→ usually persists into settings, not workspace) |
| UI | WPF only — `WpfMainGridColumns.Apply` replaces Avalonia `ColumnDefinitions.Parse` + binding notify map |

Inventory: [glass-core-settings-inventory-v0.md](glass-core-settings-inventory-v0.md).

## Peel0 shipped

- `CascadeIDE.GlassCore` — Tomlyn peel (`IdeGlassSettings`) + `GlassPresentationLayout`
- Glass host loads settings on start; presentation latch re-applies topology columns
- `primary_work_surface` switches Forward Intercom ↔ Editor placeholder

## Peel1 shipped

- Merge: defaults → `.cascade/workspace.toml` → user `settings.toml`
- CascadeIDE ProjectReference GlassCore (presentation + CDS policy single compile)
- Carve plan: [glass-core-shared-carve-v0.md](glass-core-shared-carve-v0.md)

## Next

1. Peel Cockpit Cds/HostSurface/DataBus into GlassCore
2. Grow toward full settings path without OutWit in GlassCore
3. WPF Intercom reply / Monaco / MFD organs; drop Avalonia UI hacks as parity grows
