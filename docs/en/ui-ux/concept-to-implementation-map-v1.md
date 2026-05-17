# Concept → implementation map (English, Flight-focused)

**Scope:** how the **current Flight** layout (PFD · Forward · MFD) maps to code. Legacy **Focus / Balanced / Power** PNG concepts are **archival** — many rows below describe old presets, not the shipping UI.

**Code anchors:** `Views/MainWindow.axaml`, `MainWindowViewModel` partials, `TaskCockpitView`, `MfdShellView`, `MfdShellPageStack`. Full bilingual table (legacy sections): [Russian version](../../ui-ux/concept-to-implementation-map-v1.md).

## Legend

| Status | Meaning |
|--------|---------|
| ✅ | Implemented |
| 🟨 | Partial / heuristic data |
| ❌ | Not implemented |

---

## 1) Flight — global layout (shipping)

| Concept | XAML / code | Status | Notes |
|---------|-------------|--------|-------|
| Three columns PFD · Forward · MFD | `MainGrid` `ColumnDefinitions="220,4,*,4,340"` | ✅ | Splitters; PFD/MFD can collapse per VM. |
| Forward = editor dock | `DocumentsDockView` in `Zone="Forward"` | ✅ | Primary code surface today (default). |
| MFD = page stack | `MfdShellView`, `MfdShellPageStack` | ✅ | Build, terminal, Git, chat page, IDE Health, … |
| No bottom panel across window | — | ✅ | Unlike legacy mockups; streams on MFD pages. |
| Single UI mode **Flight** | `UiModes/index.toml` | ✅ | No Focus/Balanced/Power product switch. |
| Command palette | `CommandPaletteHost` | ✅ | Complements Intercom slash commands ([ADR 0119](../../adr/0119-chat-slash-commands-intercom-surface.md)). |
| Intercom / chat | MFD page + Skia surface | ✅ | See [ADR 0044](../../adr/0044-avalonia-host-skia-agent-chat-surface.md). |
| Intercom in Forward (Cursor-like) | — | ❌ Proposed | [ADR 0120](../../adr/0120-primary-work-surface-intercom-or-editor.md). |
| Slash commands in chat input | — | ❌ Proposed | [ADR 0119](../../adr/0119-chat-slash-commands-intercom-surface.md). |

---

## 2) Multi-monitor (ADR 0017)

| Concept | Code | Status |
|---------|------|--------|
| `presentation` line in settings | `PresentationParser`, VM | ✅ |
| Secondary **Mfd** window | `MfdHostWindow` | ✅ |
| Secondary **Pfd** window | `PfdHostWindow` | ✅ |
| Hide duplicate column when host open | layout VM | ✅ |

Details: [ADR 0017](../adr/0017-multi-window-workspace-and-agent-surfaces.md).

---

## 3) Legacy Focus / Balanced / Power

PNG files under `concept-generated/` and sections §2–§4 in the [Russian map](../../ui-ux/concept-to-implementation-map-v1.md) describe **historical** presets. Do not assume they match the Flight binary without checking this table and [UI layout v1](cascade-ide-ui-layout-v1.md).

---

## Next alignment (product backlog)

1. Dependency mini-map (Balanced-era idea) — not in Flight scope yet.
2. Deeper IDE Health / cockpit metrics — ongoing.
3. Power-style visual polish on tree/editor — partial; see Russian map §4.1.

---

*For MCP and automation, prefer [UI layout v1](cascade-ide-ui-layout-v1.md) control names.*
