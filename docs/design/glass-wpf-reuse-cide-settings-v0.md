# Glass WPF · reuse CIDE settings/topology (v0)

## Decision

`CDP.GlassCockpit.Windows` does **not** invent a second settings/topology SSOT.

| Layer | Source |
|---|---|
| Durable settings | `%LocalAppData%/CascadeIDE/settings.toml` (same as Avalonia CIDE) |
| Live desk patch | `%LocalAppData%/cdp-mcp/presentation-LATEST.json` (topology/tier/mfd) |
| Layout math | `CascadeIDE.GlassCore` → linked `Services/Presentation/*` (parser, topology flags, main-grid frame) |
| UI | WPF only — `WpfMainGridColumns.Apply` replaces Avalonia `ColumnDefinitions.Parse` + binding notify map |

## Peel0 shipped

- `CascadeIDE.GlassCore` — Tomlyn peel (`IdeGlassSettings`) + `GlassPresentationLayout`
- Glass host loads settings on start; presentation latch re-applies topology columns
- `primary_work_surface` switches Forward Intercom ↔ Editor placeholder

## Next

1. Extract full `SettingsService`/`CascadeIdeSettings` into GlassCore (drop Tomlyn peel)
2. CascadeIDE ProjectReference GlassCore (single compile of presentation)
3. Delete Avalonia UI hacks as WPF seats reach parity (Skia mirrors, `$parent` climbs, UIThread latch posts where WPF bindings work)
4. Intercom reply latch / Monaco / MFD organs
