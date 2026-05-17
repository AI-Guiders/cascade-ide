<!-- English translation of adr/0022-workspace-health-lexicon.md. Canonical Russian: ../../adr/0022-workspace-health-lexicon.md -->

# ADR 0022: Lexicon and canon of names - IDE Health (evolution of names; ADR file saved as 0022)

**Status:** Accepted  
**Date:** 2026-04-11  
**Updated:** 2026-04-24 - major track UI designer → [0092](0092-visual-ui-designer-major-track.md). Details - [§ History](#adr0022-history).
## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | attention model, channel vs presentation slot |
| [0010](0010-ui-modes-toml-configuration.md) | TOML keys and capabilities |
| [0012](0012-floating-workspace-chrome.md) | placement of strip and lower zone |
| [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | **renaming** product and types: *Workspace Health* → **IDE Health**, `WorkspaceHealth*` → `IdeHealth*` |

### Outside ADR

| Document | Role |
|----------|------|
| [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md) | code and data flow |

---
## Context

1. The word **telemetry** in English UI and in engineering speech is overloaded: product analytics, “agent telemetry”, instrument readings in the cockpit metaphor, etc.
2. The **task status channel in the workspace** (build, tests, debugging session, git) was needed with a **stable name** in the code, config and ADR without collisions.
3. Renaming **without backward compatibility** for TOML keys and types was carried out in the repository; This ADR captures the **solution and scope** rather than the step-by-step migration. **0089** recorded a change of **product** name to **IDE Health** and type prefix **`IdeHealth*`**; wire mode keys for this circuit are **`ide_health_*`** in TOML ([0010](0010-ui-modes-toml-configuration.md), `UiModes/*.toml`), formerly `workspace_health_*`.

---

## Solution (canon)

| Layer | Canon | Russian wording in UI/docs | Where recorded |
|------|--------|-------------------|----|
| Product/ADR | **IDE Health** (formerly *Workspace Health*) | Nearby is **IDE status** / "build and environment summary" (to avoid confusion with the *workspace* directory on disk) | **0089**, this ADR, **0021** §1.1–1.2 |
| Code | Type prefix **`IdeHealth*`** (`IdeHealthSurfaceCompositor`, `IdeHealthStripView`, ...); some VM names/bindings may still contain `WorkspaceHealth*` before stripping | — | Repository, drawing implementation map |
| Modes config | Keys **`ide_health_*`** (`ide_health_strip`, `ide_health_surface`, `ide_health_on_terminal_tab`, `ide_health_main_column_span`) - wire format in TOML | — | **0010**, `UiModes/*.toml` |

**Semantics:** one **channel of meaning** (what happens to the task: build/tests/debug/git), several **layers of presentation** (bar under the editor, MFD page, double on the terminal tab - by preset). Sense composer - `IdeHealthSurfaceCompositor`; screen area and chrome - by **0021** and presets.

---

## Not to be confused with IDE Health

| Name/Key | Meaning | Why didn't they rename it in the same entry |
|------------|--------|------------------------------------------|
| **`autonomous_agent_telemetry`** (TOML), **`AutonomousAgentTelemetry`** (capabilities) | Cockpit Power: explicit access to **output** (terminal), hints when the terminal is hidden | Another product circuit; the word *telemetry* here refers to “session collection/output”, not about the build/tests/debug/git channel. See **0010** and UX tables. |
| Lines like **"Telemetry: on"** in UI | Binding to a terminal in Power | Localizing and renaming VM properties is a separate task. |
| Stable **anchor ids** in markdown (e.g. `#anchor-pfd-mfd-content-vs-telemetry-page`) | Permalinks from other docs | Change breaks external links; the meaning of the anchor is described in **0021**. |

---

## Evolution (briefly)

| Was (outdated) | Became (canon) |
|----------------|--------------|
| Drafts: "job telemetry", "operational telemetry" | **IDE Health** / IDE health |
| `WorkspaceTelemetry*` | `IdeHealth*`(types); TOML keys `ide_health_*` |
| Keys `telemetry_*` in TOML modes | `ide_health_*` |
| Drawing `workspace-telemetry-compositor-implementation-v1.md` | [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md) |
| Product name *Workspace Health* | **IDE Health** ([0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md)) |

History of wording in headers **0021** and in UX docs may refer to old names in **past tense** - this is the norm.

---

## Consequences
- New features for this channel - type names **`IdeHealth*`**, TOML keys in **`[capabilities]`** - prefix **`ide_health_*`** ([0010](0010-ui-modes-toml-configuration.md)).
- Overview UX docks: [`cascade-ide-ui-layout-v1.md`](../ui-ux/cascade-ide-ui-layout-v1.md), [`concept-to-implementation-map-v1.md`](../ui-ux/concept-to-implementation-map-v1.md), [`ui-modes-overview-v1.md`](../ui-ux/ui-modes-overview-v1.md) - aligned to the term **IDE Health**; in case of discrepancy **priority goes to 0089, this ADR and 0021**.

---

## Open items (do not block canon)

- Separate renaming of **`autonomous_agent_*`**/UI lines if the product chooses a single English glossary without "telemetry" for the Power cockpit.
- RU/EN localization for all visible lines is outside the scope of this ADR.
- Complete cleanup of residual `WorkspaceHealth*` names in VM/AXAML without the need to maintain wire compatibility for external integrations.

---

## History of changes

<a id="adr0022-history"></a>

| Date | Change |
|------|-----------|
| 2026-04-25 | TOML contour keys **IDE Health** in modes: canon **`ide_health_*`** (see [0010](0010-ui-modes-toml-configuration.md)). |