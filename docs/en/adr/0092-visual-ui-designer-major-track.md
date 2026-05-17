<!-- English translation of adr/0092-visual-ui-designer-major-track.md. Canonical Russian: ../../adr/0092-visual-ui-designer-major-track.md -->

# ADR 0092: **Visual UI** track (layout designer) — separate major program line in CIDE

**Status:** Accepted (direction)  
**Date:** 2026-04-24

## Related ADRs

| ADR | Role |
|-----|------|
| [0022](0022-mfd-visual-design-surface-axaml-blazor.md) | visual UI dev surface (AXAML / Blazor) — WinForms-oriented reference, placement on MFD |

---

<a id="adr0092-context"></a>

## Context

Visual designer / live preview for **Avalonia (AXAML)**, **Blazor**, and (optionally) **Razor** is **not** “one sprint feature”: preview host, isolation, text sync, tree, property grid, eventually drag-and-drop. It needs its **own planning horizon** and **explicit MVP slicing**, or it competes with critical IDE path or **gets lost** without an owner.

Discussion (2026-04-24): record a **track** in ADR so backlog and talk are clear — this is **CIDE Visual UI track** (working name); UX and placement details stay in **0022**.

---

<a id="adr0092-decision"></a>

## Decision

<a id="adr0092-p1"></a>

1. **Accept** direction of a **separate major program line** **Visual UI** in Cascade IDE: visual work with declarative user UI markup (primarily **.NET** stack in CIDE focus), with **backlog** and **done criteria** by phase aligned with [0022](0022-mfd-visual-design-surface-axaml-blazor.md).

<a id="adr0092-p2"></a>

2. **Canonical product/UX ADR** for this line — **[0022](0022-mfd-visual-design-surface-axaml-blazor.md)** (attention model, MFD, no second `TopLevel` for preview, MVP / next / later phases, non-goals). **0092** answers “**how to run it as a track**”, not “**how it looks in cockpit**” — on conflict, **0022** wins for product rules.

<a id="adr0092-p3"></a>

3. **Stack order (priority plan, not dates):**
   - **First wave (target):** **Avalonia / AXAML** — closer to existing CIDE host, predictable preview contour.
   - **Second wave:** **Blazor** — separate host (browser / WebView / isolate), harder design-time boundaries; same IDE-level user scenario per [0022 §4](0022-mfd-visual-design-surface-axaml-blazor.md).
   - **Razor (MVC/Pages) and adjacent:** **not** mandatory MVP of track; separate decision (spike / epic) — often **HTML+templates**, not visual control tree — otherwise blurs track boundary.

<a id="adr0092-p4"></a>

4. **Agent and contract link:** track **orthogonal** to [0008](0008-mcp-contracts-and-testable-infrastructure.md) and [0052](0052-agent-contract-cli-and-snapshot-tests.md): as stable **snapshots/commands** for design surface appear (file open, preview mode, node selection) — **MCP/CLI extensions** added **deliberately** with same JSON parity requirement, **without** duplicating logic. Track **not blocked** on “all agent tools first.”

<a id="adr0092-p5"></a>

5. **Boundary with other tracks:** do **not** put EICAS/HUD, “cloud inline”, and other contours listed in 0022 as **non-goals** for attention zone into this track; do **not** confuse **Cockpit UI** (CIDE instruments) with **user application designer** — [0066](0066-cockpit-ui-vs-ide-presentation-layer.md).

---

<a id="adr0092-consequences"></a>

## Consequences

| Pros | Cons / risks |
|------|----------------|
| Clear **bucket**: “in Visual UI track / outside track” | Need **discipline** not to inflate track with “all UI in the world” |
| **0022** stays single product-rules place; 0092 does not duplicate phase table | Two ADRs — but less “process vs product” drift |
| Avalonia → Blazor → (opt.) Razor order lowers **simultaneous** stack explosion risk | **0022 open questions** (minimal v1 per tech, MFD tab, two-way sync) — still in 0022; track does not auto-close them |

---

<a id="adr0092-open-questions"></a>

## Open questions

- Explicit **backlog/label names** (GitLab line / tags) — repo discretion; ADR fixes semantic name *Visual UI* / *design surface* with 0022.
- **SDK / plugins** ([0024](0024-ide-sdk-and-stable-contracts.md)): if designer ever moves to extension — **separate** ADR, not 0092.

---

<a id="adr0092-rejected"></a>

## Rejected alternatives

- **Treat designer as only part of Markdown preview or WebView ADR** — rejected: different subject (author Markdown vs **user** AXAML/Blazor).
- **Extend 0022 with “how to run backlog” table** — possible, but moved to 0092 so 0022 stays **short** product ADR.
