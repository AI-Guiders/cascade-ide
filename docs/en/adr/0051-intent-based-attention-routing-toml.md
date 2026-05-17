<!-- English translation of adr/0051-intent-based-attention-routing-toml.md. Canonical Russian: ../../adr/0051-intent-based-attention-routing-toml.md -->

#ADR 0051: Intent-based attention routing (TOML)

**Status:** Accepted · Implemented  
**Date:** 2026-04-16

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | model of attention zones and their canonical ids |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | topology `presentation` |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | declarative placement `instrument_id` by `surface_id+slot_id` |

## Summary

- **Intent-based attention routing** in TOML - attention routing by intent.
- Communication with capabilities and zones [0021](0021-pfd-mfd-cockpit-attention-model.md).


### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | section `[attention_routing]` in bundle `UiModes/workspace.toml` and overlay `.cascade/workspace.toml`; intent-id - `AttentionRoutingIntentIds` |

---
## Context

In `UiModes/workspace.toml` and `.cascade/workspace.toml` today there is a map of the placement of UI panels by attention zones: the `[attention_zone_panels]` section specifies `panel_id -> attention_zone`.

DX problem:

- The user must know **internal identifiers** (which keys are valid).
- Names in the style of `attention_zones` / `attention_zone_panels` can easily be confused with another configuration axis: **placement of “which cockpit instrument”** in slots (see ADR [0050](0050-declarative-instrument-zone-placement-toml.md)).

It is necessary to clearly separate two independent layers:

1. **Attention routing**: *where on the stage to direct the UI intent* (example: chat - in `mfd`, editor - in `forward`).  
   This is about **attention flow and geography** (`pfd/mfd/forward/hud/eicas`), but **not** about “what tool is in the slot”.
2. **Instrument placement**: *which `instrument_id` is mounted in `surface_id+slot_id`* (ADR 0050).  
   This is about **cockpit slot content**, but **not** about panel/intent routing.

---

## Solution

Accept an intent-based attention routing configuration and **explicitly name** the section so that it does not overlap with instrument placement.

### 1) New section

Replace `[attention_zone_panels]` with:

- **`[attention_routing]`**

Reasons:

- the word *routing* directly indicates “routing/where to route”, and not “what is in the slot”;
- does not overlap in meaning with `[instrument_routing]` (ADR 0050).

### 2) Keys: “intents”, not internal panel ids

Keys are human-readable intent ids (v1 match 1:1 with the current panels, but are treated as intent):

- `solution_explorer`
- `chat`
- `git`
- `terminal`
- `editor`

Values are canonical zone ids from ADR 0021:

- `forward`, `pfd`, `mfd`, `hud`, `eicas`

Example:```toml
[attention_routing]
solution_explorer = "pfd"
chat = "mfd"
git = "mfd"
terminal = "mfd"
editor = "forward"
```
### 3) Link to implementation

Inside runtime, routing remains implemented through the `panel_id -> AttentionZone` map, but TOML specifies **intent**:

- intent id is normalized and translated into the corresponding `panel_id` (internal key).
- If the intent is unknown, an explicit diagnostic (log/status) is issued.

### 4) Layers merge (bundle/repo)

As for the current `workspace.toml`:

- bundle: `UiModes/workspace.toml`
- repo overlay: `.cascade/workspace.toml`

Precedence rule: repo over bundle for matching keys.

---

## Consequences

- The config becomes clearer: the user sees **intents**, and not “magic ids”.
- Terminology does not conflict with ADR 0050 (instrument placement).
- The code model allows for further evolution: the emergence of new intents without disclosing internal panel ids.
- `editor_hud` is fixed as an invariant of the HUD layer inside `forward` and is not a custom intent in TOML.

---

## Migration

“replace” solution:

- `[attention_zone_panels]` is deprecated and removed when `[attention_routing]` is implemented.
- Transition can support both, but in the v1 implementation it is preferable not to keep two parallel sources of truth.

---

## Open questions

- Separate custom routing section in `%LocalAppData%\\CascadeIDE\\settings.toml`: is it needed in v1, or is bundle+repo enough?