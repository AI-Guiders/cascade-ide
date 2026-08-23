# Analytics / Fact — CIDE min export JSON for ANUI

**task:** `cide-export-shape` · **status:** done when this card exists  
**decision:** In-proc adapt existing MCP/host JSON; not OOP scrape.

## Entrypoints (live MainWindow)

| Priority | API | Returns |
|----------|-----|--------|
| Primary geometry | `GetUiLayoutAsync` / `UiLayoutSnapshot.BuildJsonAllWindows` | `{ windows:[{ role, window_type, title, is_active, root }] }` |
| Semantic zones | `GetCockpitSurfaceAsync` / `BuildCockpitSurfaceSnapshot` | `schema_version, ui_mode, topology, zones, instruments[]` |
| Optional chrome | `GetUiTheme` → subset `layout_regions` + `window_frame` | named regions with bounds/colors |

Headless (`AgentContractHeadlessRuntime`): cockpit/ide_state only — **no** ui_layout without real window.

## Min stitch schema (`cide-anui-v1`)

```json
{
  "export_schema": "cide-anui-v1",
  "windows": [ /* get_ui_layout — REQUIRED */ ],
  "cockpit": { /* get_cockpit_surface — REQUIRED for zones */ },
  "chrome_regions": { /* OPTIONAL layout_regions subset */ },
  "window_frame": { /* OPTIONAL */ }
}
```

### Layout node
`type`, `name`, `visible`, `bounds{x,y,w,h}`, `content`, `children[]`, optional `attention_zone`. Depth cap 14.

### Cockpit
`zones{pfd_visible,forward_visible,mfd_visible}`, `topology`, `instruments[{instrument_id,slot_id}]`.

## Map → ANUI EvidenceSnapshot (intent)

| ANUI | CIDE |
|------|------|
| tree nodes + bounds | `windows[].root` |
| semantic regions | `cockpit.zones` + topology |
| named chrome regions | `chrome_regions` / layout_regions |
| multi-window | `windows[].role` + theme `top_levels` |

## Non-goals

- Whole `get_ide_state` dump; PNG capture; WebAiPortal whitelist (no visual cmds); full `cascade_theme_resolved`; OOP Avalonia scrape; write automation.

## Call sequence (live)

1. `GetUiLayoutAsync()`
2. `GetCockpitSurfaceAsync()`
3. Optional theme → `layout_regions` + `window_frame` only

## Sources

- `Cockpit/Surface/UiLayoutSnapshot.cs`
- `Services/UiThemeDeepSnapshot.cs`, `UiControlAppearance.cs`
- `MainWindowIdeMcpHost` (layout/theme/cockpit)
- `AgentContractHeadlessRuntime` (cockpit-only headless)
