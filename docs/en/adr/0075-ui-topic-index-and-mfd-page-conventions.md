<!-- English translation of adr/0075-ui-topic-index-and-mfd-page-conventions.md. Canonical Russian: ../../adr/0075-ui-topic-index-and-mfd-page-conventions.md -->

# ADR 0075: UI Subject Index (`docs/adr/UI/`) and MFD Page Conventions

**Status:** Proposed  
**Date:** 2026-04-20  
## Related ADRs

| ADR | Role |
|-----|------|
| [0076](0076-ui-ux-principles-hub.md) | Center for UI/UX Principles; coherent introductory text |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | secondary circuit / MFD |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | payload vs projection |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | keyboard-first, Command Melody |
| [0074](0074-settings-ui-mfd-compact-layout-overflow.md) | density and location in MFD |
| [0013](0013-command-surface-and-discoverability.md) | palette |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id` |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | index of topics in Intercom, summary on card |
| [0077](0077-tech-principles-hub.md) | TECH - the center of principles (connected text from the canon) |

### Outside ADR

| Document | Role |
|----------|------|
| [`TECH/README.md`](TECH/README.md) | index of TECH principles |

---
## Context

Discussions of **visual presentation** (list vs table, scroll, tooltips) without a fixed standard in the repository give **friction**: restoration “as it was” from memory, different interpretations of terms (including product vs ADR), dispute about priorities **pointer** vs **keyboard**.

At the same time, the repo already has the **payload vs presentation** standard ([0068](0068-deck-row-payload-and-presentation-projection.md)) and **keyboard-first** inputs ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md)).

---

<a id="adr0075-p1"></a>

## 1. Solution: folder `docs/adr/UI/`

1. Enter **thematic index** [`docs/adr/UI/README.md`](UI/README.md): a table of links to existing ADRs on the topic UI/MFD/chromium/palette.  
2. **Do not** introduce separate numbering and **do not** duplicate the text of regulatory ADRs inside `UI/` - the canon is still the files `docs/adr/NNNN-*.md` and the main [index](README.md). Navigation **map of principles** (where to look for the full text) - [`UI/principles.md`](UI/principles.md).  
3. Subfolder `UI/` - **not** classification by ADR status (see agreements in [README § “Agreements”](README.md)); only convenient navigation on the topic.

---

<a id="adr0075-p2"></a>

## 2. Secondary Flow Page Conventions (MFD)

Normatively rely on:

| Principle | Source |
|--------|----------|
| One **payload** of lines (order, `command_id`/DTO) changes in the VM/service; **projection** (cards, table, density) - in View without changing row semantics | [0068](0068-deck-row-payload-and-presentation-projection.md) |
| **Keyboard-first:** The meaning of the command is available through the /Melody(`c:`)/Chords palette with the same `command_id`; **hover-only** do not count as the only channel for mandatory meaning | [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md), [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) where appropriate |
| Narrow **MFD**—width/height limitation; scroll / compact / fallback strategies - coordinate with the direction [0074](0074-settings-ui-mfd-compact-layout-overflow.md) for forms and similarly for “long” lists |

**Example (illustration, not a separate canon):** page “environment readiness” - collection `AnnunciatorLampItem`; switching "compact / wide" - only **layout** (`EnvironmentReadinessPresentationResolver`), line order - **payload**.

---

<a id="adr0075-p3"></a>

## 3. Consequences

- Agents and people can refer **“see. ADR/UI"** as in [`docs/adr/UI/README.md`](UI/README.md), without mixing with flat index cancellation.  
- Controversial UI decisions on a product are still documented as a **separate numbered ADR** or editing an existing one, and not just a note in `UI/`.

---

## 4. Rejected alternatives

- **Wiki only / chat only** - without a link from the repo, reproducibility is lost.  
- **All UI-ADRs only in the `UI/` subfolder** - breaks the current `NNNN-*.md` naming convention in one directory and makes it more difficult to search by number.