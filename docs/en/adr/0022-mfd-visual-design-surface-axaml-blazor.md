<!-- English translation of adr/0022-mfd-visual-design-surface-axaml-blazor.md. Canonical Russian: ../../adr/0022-mfd-visual-design-surface-axaml-blazor.md -->

#ADR 0022: Visual UI Development Surface (AXAML/Blazor) - WinForms benchmark, hosted on MFD

**Status:** Proposed  
**Date:** 2026-04-06  
**Updated:** 2026-04-24 - major track UI designer → [0092](0092-visual-ui-designer-major-track.md). Details - [§ History](#adr0022-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD/`forward`; [§“Plugins and attention model”](0021-pfd-mfd-cockpit-attention-model.md#plugins-attention-binding |
| [0010](0010-ui-modes-toml-configuration.md) | mode presets and panel slots |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | item 3 |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p2) | item 2 |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p3) | item 3 |

---
## Context

External **visual designers** for declarative markup (Avalonia **AXAML**, **Blazor**) are often **commercial** or poorly integrated into a single IDE loop. At the same time, the target user experience for Cascade is not necessarily “like Blend,” but rather a **cycle like WinForms**: quickly see the form, poke elements, edit properties, **without pushing** the main flow of working with code out of your attention zone.

The experience of a **WPF designer in Visual Studio** is often criticized as weak when **combined with a code editor in one screen**; at the same time, **moved** to a second display or a separate preview area, it **stops interfering** and becomes an acceptable compromise.

Cascade already has a product frame: **front** - object of work (editor); **MFD** - secondary instruments, heavy panels, conscious switching ([0021](0021-pfd-mfd-cockpit-attention-model.md)). The visual surface for marking is logical **not to occupy the frontal** by default, but to be positioned **only** as an **MFD tool** (tab/page/split in the MFD region). **Separate application window** under the preview **not** used: secondary is already expressed by the MFD zone; the second `TopLevel` would duplicate chrome, blur focus and complicate the single shot UI/agent parity without benefiting from the meaning of [0021](0021-pfd-mfd-cockpit-attention-model.md).

---

## Solution (principles)

1. **The goal of the experience is a “WinForms-like loop”, not a Blend clone.** Priority: **direct manipulation** + **property grid** + **live preview** and predictable connection to markup text. Cinematic animations, a full visual style editor and “everything with the mouse” are **not** required features of v1.

2. **Placement according to the attention model [0021](0021-pfd-mfd-cockpit-attention-model.md).** Preview of the control tree, property grid and related panels - **candidates for MFD** (tab, page, split within the MFD region). The source editor **remains on the front panel**; the designer **doesn't** have to** be stitched to one column of code on one monitor.

3. **Second monitor is a first-class scenario without a second window.** There remains **one** main IDE window: the user brings it to the external display entirely, expands it on the desired monitor, or uses the “editor on one screen, MFD on the other” layout within the **same** `TopLevel` (framework / preset geometry, see below). [0021](0021-pfd-mfd-cockpit-attention-model.md) about the multimonitor). This is not a separate `TopLevel` of the Mfd zone host by [0017](0017-multi-window-workspace-and-agent-surfaces.md) - a separate designer `TopLevel` **outside the scope** of this ADR.

4. **Two technologies - one product layer “design surface”.** **AXAML (Avalonia)** and **Blazor** differ in preview hosting and extension points, but **user script** (open file → see tree → change property → save/sync with text) - **single** at the IDE level; the discrepancies are in the adapters, not in the duplication of unrelated “masters”.

5. **Security and isolation.** Preview and design time execution - **isolated process or bounded host** (policy later detailed; guideline is not to execute arbitrary user code in the same process as a full borderless IDE shell).

---

## Phases (guideline; not a commitment to deadlines)
| Phase | Contents | Comment |
|------|-----------|--------------|
| **MVP** | **Live preview** by saving/focus + basic **tree** of elements + minimal **property grid** for a subset of properties | Already gives value “as a rendered WPF designer”, without drag-and-drop canvas |
| **Next step** | Direct **move/resize** on canvas (where applicable), sync with markup, undo | Significantly more difficult engineering |
| **Next** | Blazor specifics (routes, inject), general templates, possible designer-to-test scenarios | After stabilizing MVP |

---

## Non-targets (for clarity)

- **Don't** aim for a complete analogue of **Expression Blend** as a mandatory criterion for success.
- **Do not** confuse the role of “surface design” with **EICAS** or with **HUD** inside the editor ([0021](0021-pfd-mfd-cockpit-attention-model.md)) - these are different contours of attention.
- **Do not** promise a specific process stack in this ADR (separate exe vs AppDomain) - only the principle of isolation.

---

## Consequences

- An **explicit backlog** will appear for the infrastructure: host preview, subscription to files, selection mapping in the tree ↔ cursor/range in the text.
- **MCP Contracts / UI Snapshot** ([0008](0008-mcp-contracts-and-testable-infrastructure.md), [0012](0012-floating-workspace-chrome.md)): The target outline for this feature is **single** visual root; extension for several `TopLevel` ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) to the preview **we do not link**.
- User documentation (later): where to turn on the preview, how to attach it to a second monitor, limitations v1.

---

## Open questions

- Minimum **v1 by technology**: only AXAML, only the built-in Cascade stack, or just a template for Blazor host?
- Do I need a **separate MFD** “Design” tab in the default preset or only by Flight command/mode?
- **Two-way synchronization** policy: only “from text to preview” in MVP or immediately “click in the tree → navigation in the code”?

---

## Rejected alternatives (as the only strategy)

- **Rely only on an external commercial designer** without a built-in surface in Cascade - rejected as not meeting the "IDE with its own toolpath" goal.
- **Place the preview by default in the center of the windshield** on top of the editor - rejected in favor of [0021](0021-pfd-mfd-cockpit-attention-model.md) (the windshield is an object of work, does not compete with the heavy preview).
- **Put preview into the second application window** - rejected: the secondary is already closed by the **MFD** region in one cockpit; a separate `TopLevel` is not required and is contrary to the simplified UI snapshot contract for the agent.

---

## History of changes

<a id="adr0022-history"></a>

| Date | Change |
|------|-----------|
| 2026-04-24 | organizing work as a **separate major track** - [0092](0092-visual-ui-designer-major-track.md) (stack priority: Avalonia → Blazor; product rules - still in this ADR). Previously 2026-04-06: Binding to [0021 §“Plugins and Attention Model”](0021-pfd-mfd-cockpit-attention-model.md#plugins-attention-binding). Preview **only** in MFD of one window; **without** separate `TopLevel`; second monitor = same IDE instance, not window by [0017](0017-multi-window-workspace-and-agent-surfaces.md). |