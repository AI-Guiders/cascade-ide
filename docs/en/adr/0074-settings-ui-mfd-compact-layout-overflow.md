<!-- English translation of adr/0074-settings-ui-mfd-compact-layout-overflow.md. Canonical Russian: ../../adr/0074-settings-ui-mfd-compact-layout-overflow.md -->

# ADR 0074: Settings UI - more compact, anchored on MFD; lack of space in the P+F+M layout

**Status:** Proposed  
**Date:** 2026-04-19  
## Related ADRs

| ADR | Role |
|-----|------|
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML canon; holistic settings center **deferred**; dot UI = canon façade |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | `settings.toml` |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | anchors PFD / Forward / MFD |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | `presentation`, layout invariants |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | multi-window, `display.screens` |
| [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | example **MFD-first** tool surface |

### Outside ADR

| Document | Role |
|----------|------|
| [`Models/MfdShellPage.cs`](../../Models/MfdShellPage.cs) | enum MFD pages |

---
## Context

**User expectation:** settings are logical to look for in the **secondary circuit** of the “home” tools - **MFD** ([0021](0021-pfd-mfd-cockpit-attention-model.md)), and not as the only heavy modal layer on top of the editor, if the goal is **not to lose the context** of the cockpit.

**Code fact:** some of the settings are already available as the **`AiChatSettings`** page in **`MfdShellView`**; the full set still opens **in a separate window** (menu “Settings → AI and chat settings...” → `OpenSettingsCommand` → `ShowSettingsWindow`). [0029](0029-configuration-toml-canonical-ui-facade.md) does not prohibit UI development, but does **deferred** a separate "full-screen settings center as a separate product"; the transfer **to MFD** is not a second truth on disk, but a **surface** above the same `CascadeIdeSettings`.

**Density issue:** in a typical **P + F + M** (PFD | Forward | MFD) layout, the MFD column is **limited in width**; long forms and many sections give **scroll**, cutting off or a feeling of “crowding” - an explicit policy is needed **if there is not enough space**.

---

## Proposed direction (draft)

1. **Target anchor:** main **UI for editing settings** (or most of it) - **inside the MFD** (secondary outline), **compact** layout: groups, accordions, shortcuts to rare keys, link “open `settings.toml`” without duplicating the canon.
2. **Separate window:** leave as **fallback** (large monitor, side comparison, accessibility) or **collapse** to rare scenarios - **product solution** after MFD prototype.
3. **Agree with [0029](0029-configuration-toml-canonical-ui-facade.md):** any UI is still **facade** model and files; MFD screen expansion **doesn't** add a second source of truth.

---

<a id="adr0074-overflow"></a>

## Open questions: lack of space (P + F + M, etc.)

Fix **one** or **hierarchy** of strategies (to be selected):

| # | Strategy | Meaning | Risk |
|---|-----------|------|------|
| 1 | **Vertical scroll** inside MFD page | Simple baseline | Long forms “run away” down; need sticky section navigation |
| 2 | **Minimum MFD width** + **custom** column resize | Maintain readability | On narrow screens conflict with Forward (editor) |
| 3 | **Full screen mode** only for MFD (expand column / temporarily max width) | Compromise without a separate window | Need explicit exit/restore preset gesture |
| 4 | **Separate window** as fallback for `ActualWidth < threshold` | Predictable on small displays | Two path codes or adaptive container |
| 5 | **Second window** (already exists for MFD host in [0017](0017-multi-window-workspace-and-agent-surfaces.md)) - duplicate settings there | Lots of monitors | Synchronizing focus and "where the truth is revealed" |
| 6 | **Collapse PFD** (auto or by policy) when focusing on settings | More space for uniforms | The attention model changes without explicit user action - carefully ([0021](0021-pfd-mfd-cockpit-attention-model.md)) |

**Recommendation for discussion:** start with **(1) + (2)** and an explicit **breakpoint** for **(4)**; **(6)** - only by explicit user command, not automatically in v1.

---

## Solution

**Fix as Proposed:** direction “**settings → MFD, more compact**” and a list of **open** overflow strategies - **not** as an immediate change [0029](0029-configuration-toml-canonical-ui-facade.md) (canon on disk does not change), but as **product and UX** selection of the next iteration.
**Next step:** prototype or refactor the settings page markup in **`MfdShellView`**; measure minimum width and behavior on **1366x768** and **ultra-wide** screens; write the selected policy from the overflow table to this ADR or to **presentation**/capabilities ([0046](0046-presentation-layout-authority-and-cockpit-invariants.md)).

---

## Consequences

- **Advantage:** consistency with the “settings near chat/terminal” mental model on MFD; less separation from the cockpit.
- **Minus:** engineering work on adaptive layout and tests; you need to avoid breaking users who are accustomed to a separate window - **migration path** (menu item → MFD first, “Open in a separate window” optional).

---