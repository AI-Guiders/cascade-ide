<!-- English translation of adr/0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md. Canonical Russian: ../../adr/0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md -->

# ADR 0059: Roslyn MCP, Manager, Tactics/Strategy and EFB (MFD) Profiles

**Status:** Proposed  
**Date:** 2026-04-18

## Related ADRs

| ADR | Role |
|-----|------|
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | section `[agent.roslyn_mcp]`, axes of limits/kinds/timeouts/presets - **this ADR does not duplicate** TOML keys, but describes **behavior and scripts** on top of them |
| [0010](0010-ui-modes-toml-configuration.md) | UI modes - separate axis |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | windows, `presentation`, MFD |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | tool → slot |
| [0051](0051-intent-based-attention-routing-toml.md) | intent routing |
| [0053](0053-semantic-map-control-flow-pfd.md) | Semantic Map, control flow |
| [0055](0055-skia-instrument-composition-pipeline.md) | declutter on PFD |

---
## Context

[0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) introduces **declarative parameters** for agent ↔ Roslyn MCP coupling in `settings.toml`. Separately, we need to fix **product** behavior: several **named profiles**, **who** switches the active profile, **modes** (tactical environmental signals) and the use of **additional monitor** as a “strategic” map - without bloat [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md).

---

## Solution

<a id="adr0059-p1"></a>

### 1. Multiple profiles in TOML and Manager

In addition to the flat set of keys in `[agent.roslyn_mcp]` ([0058](0058-agent-roslyn-mcp-coupling-settings-toml.md)), **several named pairing profiles** are assumed - integral sets of parameters (limits, `depth`, kinds filters, timeouts, MCP request presets), **pre-described and verifiable** in TOML. Names like **`Profile.Flight`**, **`Profile.Debug`**, **`Profile.DeepScan`**, **`Profile.GlobalMap`** - **illustrations**; in the implementation - tables like `[[agent.roslyn_mcp.profiles]]` with the `id` field (scheme when implemented).

**Invariant:** switching the active profile - **selecting one of the pre-valid sets** for the contract with Roslyn MCP, and not replacing arbitrary keys “on the fly” as with an aggressive hot-reload of the entire config. Layer **Manager** (working name) in the IDE:

- keeps a link to **current profile**;
- applies it to requests to Roslyn MCP and to the consistent Semantic Map layer: **tactics** - PFD/Forward ([0053](0053-semantic-map-control-flow-pfd.md)); **strategy (EFB)** - **MFD**, not PFD (see [§3](#adr0059-p3));
- does not generate non-deterministic combinations of parameters bypassing TOML.

**UI axis [0010](0010-ui-modes-toml-configuration.md)** and **mating profile axis** are **different**. Independent switching, mapping policy “UI mode → default profile”, and an explicit table in TOML are acceptable. Without an explicit policy, **not** consider `Profile.Flight` to be synonymous with `mode = "Flight"` in workspace.

Switching tactical modes (§2) - **by intention** and/or **automatically** by environmental signals; **hysteresis and signal priorities** apply **only** to this loop (Forward/PFD) and not to the EFB on the MFD.

<a id="adr0059-p2"></a>

### 2. Modes: Auto-Focus, Combat, Echelon

| Mode (working names) | Trigger (idea) | Effect on contract/card |
|-----------------------|----------------|----------------------------|
| **Auto-Focus** | Cursor movement / character change in **Forward** | Contract narrowing: semantics **around the current method**; neighbors within scoping without a graph of the entire solution. |
| **Combat Mode** | Roslyn diagnostics (error in relevant block) | **Extension** of the contract (`depth++`, additional kinds/edges) to search for the cause in dependencies; on PFD - connections hidden by declutter in the quiet profile ([0055](0055-skia-instrument-composition-pipeline.md)). |
| **Echelon** | Calm input without “war” of diagnostics | **Minimum noise:** Update the graph less often, do not pulsate the map for every keystroke. |

**Name Echelon:** calm “echelon” of work (even layer), without confusion with **glide path** ILS; draft name *Glide Slope* discarded as ambiguous.

Communication with [0051](0051-intent-based-attention-routing-toml.md): profile switching from a UI command, MCP or “intent → profile” policy - fixed explicitly, without hidden magic.

<a id="adr0059-p3"></a>

### 3. EFB on MFD (third monitor): strategic profile
**Zone Norm:** **EFB is an MFD mode and surface**, not a PFD. **PFD** ([0021](0021-pfd-mfd-cockpit-attention-model.md)) remains a **tactics** zone (current context, control flow, combat-declutter - §2). **MFD** - zone of the **strategic** map (global/layered skeleton), including on a separate monitor ([0017](0017-multi-window-workspace-and-agent-surfaces.md)).

**Intention:** display on the **third monitor** (typically **second TopLevel** under MFD) the profile in **static** mode - in the meaning of the **Electronic Flight Bag (EFB)**: a different scale of **time and space** than that of the **PFD** in front (in aviation: PFD - often, route board - less often; here the analogue: tactics on the PFD vs map on the **MFD**).

| Zone | Role | Profile (idea) | Update Rate |
|------|------|-----------------|-------------------|
| **Forward / PFD** | **Tactical** | Dynamic profile, narrow MCP, cursor graph / `controlFlow` ([0053](0053-semantic-map-control-flow-pfd.md)). | High (including Echelon/debounce). |
| **MFD** (including third monitor / `TopLevel`) | **Strategic, EFB** | Conditionally **`Profile.GlobalMap`**: module/layer skeleton. | **Low:** not from every cursor movement; after **background compilation** (structure changed) or **explicit request**; switching the profile on this circuit is **by intention**, not in the general circuit of heuristics §2. |

**Invariant:** The EFB screen **doesn't** "blink" to the rhythm of the cursor and **doesn't** share the same chain of automatic heuristics with the PFD - the sense of **static** and intentional updating. Separate frequencies - **surface mapping** ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) and **profile**, not duplicating one state.

**Open point:** binding "profile → window / `surface_id`" ([0050](0050-declarative-instrument-zone-placement-toml.md)) and fallback without a third monitor.

---

## Consequences

- Implementation of Manager and modes can occur **after** section [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) appears in the code.
- Tests: profile switching scenarios, no strategic view blinking during tactical events (policy).

---

## Open questions

- **Combat Mode:** hysteresis when the error disappears.
- **Echelon vs Auto-Focus:** priority when moving the cursor and typing at the same time.
- Table **UI-mode [0010](0010-ui-modes-toml-configuration.md) ↔ profile**: global policy or only user overlay.
- Linking **strategic** profile to **TopLevel** / `surface_id`; behavior without a third monitor.