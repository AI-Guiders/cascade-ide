<!-- English translation of adr/0086-ui-theme-toml-canonical-json-mcp-wire.md. Canonical Russian: ../../adr/0086-ui-theme-toml-canonical-json-mcp-wire.md -->

# ADR 0086: UI theme - canon in TOML, JSON as MCP transport (strangler from `Themes/*.json`)

**Status:** Proposed  
**Date:** 2026-04-21  
## Related ADRs

| ADR | Role |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | `settings.toml`, user path |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | canon on disk, UI as a façade |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | overlays; orthogonal to brush colors |

### Outside ADR

| Document | Role |
|----------|------|
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | `ide_get_ui_theme`, `ide_set_ui_theme` - JSON |

---
## Context

Now **applying the palette** to `Application.Resources` comes from **JSON**: `UiThemeApply.Apply(themeJson)` is the same format that MCP returns and accepts (`ide_get_ui_theme` / `ide_set_ui_theme`). The "light/dark/..." presets read `Themes/*.json` files next to the exe or inline text ([`UiThemeApply`](../../Services/UiThemeApply.cs)). Default brushes also live in **`App.axaml`** (`CascadeTheme.*`).

Along the product line ([0028](0028-user-settings-toml-localappdata-and-secrets.md), [0029](0029-configuration-toml-canonical-ui-facade.md)) **man and repository** rely on **TOML** as a single canon of settings. Two parallel “sources of truth” for colors (JSON on disk + TOML for everything else) worsen discoverability and **agent ↔ user** parity: the agent edits the JSON string in the tool, the human does not.

## Decision (intention)

<a id="adr0086-p1"></a>

1. **Canon for a person and for a repo:** description of the UI theme (brushes `CascadeTheme.*`, compatible with `UiThemeApply.Keys`) - **first of all** in the user **`settings.toml`** (or an agreed section name, for example **`[ui_theme]`** with flat or nested keys under the same semantic groups as in JSON today: `main_window`, `editor`, `menu`, …). The comments in the file are part of the DX, not a separate "secret" JSON.

<a id="adr0086-p2"></a>

2. **JSON does not disappear like the MCP contract:** `ide_get_ui_theme` / `ide_set_ui_theme` remains **transport** for the agent: on reading - serialization of **resolved** themes (merge defaults + TOML + optional overlays); on the record - use the same merge pipeline as when saving TOML from the UI. This way, “agent on equal terms” parity is maintained without requiring a human to edit the JSON manually.

<a id="adr0086-p3"></a>

3. **Strangler:** `Themes/*.json` files and built-in presets are **temporary** sources of defaults until the presets are transferred to the built-in TOML/resources or generated from one table; new brushes (such as the command palette) **first** end up in `App.axaml` and the TOML schema, not just in a separate JSON without a key in `UiThemeApply`.

<a id="adr0086-p4"></a>

4. **Implementation in stages:** (a) TOML section parser → the same internal dictionary that is currently being built from JSON, → `UiThemeApply.Apply` without duplicating the `Set(res, key, hex)` logic; (b) loading at startup: merge `App.axaml` → preset → `settings.toml` `[ui_theme]`; (c) the Theme menu and MCP call the same service; (d) MCP documentation: “canon on disk - TOML; JSON - snapshot."

## Consequences

- One look at `settings.toml` - you can see both the AI mode and the palette; fewer questions “where to change the background.”
- MCP agents continue to work via JSON without changing the semantics of the `ide_get_ui_theme` response.
- Migration/compatibility will be required: if only old `Themes/*.json` is available - behavior as now until the user saves the theme in TOML.

## Rejected / deferred alternatives

- **JSON only as canon** - contradicts [0029](0029-configuration-toml-canonical-ui-facade.md) for user preferences; Leave it as wire format only.
- **Breaking MCP and replacing it with a TOML string in the body of the tool** in one step - high tax on agents and scripts; We don’t do it without a protocol version.

## Open questions

- Exact form of TOML section: flat `ui_theme.editor_background` vs nested tables ` [ui_theme.editor]` / `background` - align with snake_case and merge from [0028](0028-user-settings-toml-localappdata-and-secrets.md).
- Is an **explicit** `ui_theme.preset = "dark"` needed for partial field overlay.
- Versioning of the `ide_get_ui_theme` response (if the `canonical_path: settings.toml` field is added) - if necessary, outside of this ADR.