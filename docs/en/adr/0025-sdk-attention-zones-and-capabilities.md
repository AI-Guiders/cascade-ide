<!-- English translation of adr/0025-sdk-attention-zones-and-capabilities.md. Canonical Russian: ../../adr/0025-sdk-attention-zones-and-capabilities.md -->

#ADR 0025: SDK and attention zones (PFD / Forward / MFD / EICAS / HUD)

**Status:** Proposed  
**Date:** 2026-04-08

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | semantics of attention zones and channels |
| [0024](0024-ide-sdk-and-stable-contracts.md) | SDK, capability model, `CascadeIDE.Contracts` |
| [0010](0010-ui-modes-toml-configuration.md) | overlay presentations without replacing semantics |

---
## Context

[0024](0024-ide-sdk-and-stable-contracts.md) introduces SDK as a way to maintain a **single mental model of a product** (modules, capabilities, introspection). Separately, [0021](0021-pfd-mfd-cockpit-attention-model.md) sets the **attention model** of the cockpit: three spatial anchors (`Forward`, `Pfd`, `Mfd`), the `Eicas` alert channel, the `Hud` layer on the windshield.

Today, zones are implemented in the application (`AttentionZone`, `AttentionZoneIds`, panel mapping - for example `AttentionZonePanelRuntime`), and **`CascadeIDE.Contracts` does not contain an explicit "attention zone" axis** for `UiSurfaceCapabilityDescriptor` / commands. Because of this, the SDK and cockpit geometry live side by side, without a formal bridge: the agent, diagnostics and overlay cannot rely on one canonical dictionary of zones at the contract level.

We need to fix a **separate solution**: how to connect the capability layer to the attention model without bloating the SDK or duplicating Axaml markup.

---

## Solution

1. **The canon of zones is the same as in [0021](0021-pfd-mfd-cockpit-attention-model.md) and in the code:** string ids `forward`, `pfd`, `mfd`, `eicas`, `hud` (see `AttentionZoneIds` / `AttentionZone.ToCanonicalId()`). The semantics of “EICAS is a channel, not the fourth column anchor” **does not change**.

2. **SDK must be able to express the binding of capabilities to the attention zone** as **metadata**, and not as a duplication of the visual tree:
   - minimum: an optional field on **`UiSurfaceCapabilityDescriptor`** (and, if necessary, on **`CommandCapabilityDescriptor`**, if the command is considered to be tied to the zone in meaning - for example, focus in the editor);
   - value: **canonical zone id** (string compatible with TOML overlay and capability-map), or a link to the common enum/constants in `CascadeIDE.Contracts` after removal.
   - for surfaces coinciding with the **shell panel**: optional **`HostAttentionPanelId`** (`AttentionPanelCanonicalIds`) together with **`PrimaryAttentionZoneId`** - a bridge to `AttentionZonePanelRuntime`; See [attention-zone-panel-playbook-v1](../../design/attention-zone-panel-playbook-v1.md) and checking **`CapabilityAttentionConsistency`** when building the map.

3. **Presentation is still via overlay** ([0010](0010-ui-modes-toml-configuration.md), [0024](0024-ide-sdk-and-stable-contracts.md)): visibility, size, MFD shell tab are **not** transferred entirely to the SDK. The zone in the contract answers the question **“where in the attention model does this surface live”**, and not “in which pixel”.

### Native dialogs (Open/Save) and zone metadata

- **The zone in the SDK does not specify** that the selection of a file or solution must be drawn **inside** MFD/Forward as a built-in panel. This is a separate **preset and UX** solution ([0010](0010-ui-modes-toml-configuration.md)); if necessary, we explicitly design it (onboarding, modes), and do not derive it from a single “zone” field.
- **System open/save dialogs** on the desktop are usually implemented as a **separate top-level window** (modal to the application); typical APIs are **not designed** to embed the same UI “into a cell” of the cockpit grid as a child widget. This means: the native selection is an **overlay on top of the scene**, although the command could be called from the zone (button in the MFD, palette, etc.).
- **Product Policy (default):** rely on **native OS dialog** for path/file selection, so as not to deprive the user of the familiar explorer and cloud/OS integrations; **own** built-in file browser in the shell area - only if this is a **conscious** decision of the scenario (inline experience, single scene for the agent, etc.), and not a necessary consequence of the zone model.
- **Redundancy:** There is no need to impose a filled zone “for show” on all teams; commands with a **global** meaning (palette, settings, opening a file through a system dialog) can be left **without** a primary zone in the metadata or with a zone only where it really helps the capabilities map (see open questions below).
4. **Implementation - step by step:**
   - **Phase A (document):** this ADR + link from [0024](0024-ide-sdk-and-stable-contracts.md).
   - **Phase B (contracts):** **done:** `AttentionZoneCanonicalIds` + optional `PrimaryAttentionZoneId` on `CommandCapabilityDescriptor` and `UiSurfaceCapabilityDescriptor`; string ids in the application (`AttentionZoneIds`) alias constants from Contracts.
   - **Phase C (registry):** fill in the fields when registering modules according to the meaning; `CapabilityMap` and JSON dumps contain the zone and `HostAttentionPanelId` (record serialization). With both specified, consistency with `AttentionZonePanelRuntime` (warning in Debug with `BuildMap()`). Playbook: [attention-zone-panel-playbook-v1](../../design/attention-zone-panel-playbook-v1.md).
   - **Phase D (optional):** remove remaining duplication - enum/extensions only in the application, lines - from Contracts wherever appropriate.

5. **Stability:** new fields in the `Experimental` namespace of contracts; changing the set of zones or id - only with updating [0021](0021-pfd-mfd-cockpit-attention-model.md) and this ADR.

---

## Consequences

- Capability-map and agent scripts will be able to respond: “this UI-surface is declared as an MFD”, without reading Axaml.
- There is a clear connection between the **product cockpit metaphor** and the **machine-readable SDK** - what was expected from the “mental model in the SDK”, but was taken out in a separate solution so as not to be confused with the general scope [0024](0024-ide-sdk-and-stable-contracts.md).
- Risk: unnecessary formality if registering a zone “for show”. Mitigation: field optional; The default is “not specified” until the feature consciously belongs to the zone.

---

## Rejected alternatives

- **Only tags in `Tags` without canon** - too weak for tools and overlay validation.
- **Full layout in SDK** is redundant; the markup remains in the UI, in the SDK - only the semantic axis of attention.
- **Keep everything only in [0024](0024-ide-sdk-and-stable-contracts.md)** - blurs the document and mixes two levels of solutions (general SDK vs cockpit axis).

---

## Open questions

- Is it necessary to explicitly bind the **Service** capability to the zone (usually no - a service without UI) vs only **UiSurface** (yes).
- Is a separate “zone not applicable” type needed for commands that are global (palette, settings).