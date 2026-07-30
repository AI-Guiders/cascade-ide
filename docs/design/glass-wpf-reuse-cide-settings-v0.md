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

- `CascadeIDE.GlassCore` — presentation layout + (later peels) typed `SettingsService`/`CascadeIdeSettings`
- Glass host loads settings on start; presentation latch re-applies topology columns
- `primary_work_surface` switches Forward Intercom ↔ Editor placeholder

## Peel1 shipped

- Merge path evolved to shared typed loader (peel14); thin Tomlyn peel retired (peel15)
- CascadeIDE ProjectReference GlassCore (presentation + CDS policy single compile)
- Carve plan: [glass-core-shared-carve-v0.md](glass-core-shared-carve-v0.md)

## Next

1. Typed workspace overlay (`UiModeCatalog` / `RepositoryWorkspaceToml`) still host — peel when Glass needs repo overlay parity without Avalonia chrome.
2. WPF Intercom reply / Monaco / MFD organs; drop Avalonia UI hacks as parity grows.

Cds/HostSurface/DataBus already in GlassCore (carve peels 2–3). Latch paths/IO peels 8–11; single-compile guard peel13; typed SSOT peel14–15.
