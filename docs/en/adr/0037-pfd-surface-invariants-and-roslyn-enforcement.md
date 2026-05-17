<!-- English translation of adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md. Canonical Russian: ../../adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md -->

# ADR 0037: PFD — surface invariants and Roslyn enforcement (weight, input lock, channels)

**Status:** Proposed  
**Date:** 2026-04-12  
**Updated:** 2026-04-16 — [naming canon](#adr0037-naming) for strict surface: `[PfdStrict]`, `PfdStrictControl`. Details — [§ History](#adr0037-history).  

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD zone in attention model |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | channel → CDS → compositor → surface |
| [0025](0025-sdk-attention-zones-and-capabilities.md) | capabilities and zones |
| [0039](0039-workspace-navigation-affordances.md) | navigation affordances in attention zone |

## Summary

- **PFD strict surface:** weight/input lock invariants; Roslyn analyzers.
- Canon **`[PfdStrict]`** / `PfdStrictControl` — attention zone geography vs strict surface ([0021](0021-pfd-mfd-cockpit-attention-model.md#adr0037-zone-vs-surface)).

### Outside ADR

| Document | Role |
|----------|------|
| [`CascadeIDE.ArchitectureAnalyzers/README.md`](../CascadeIDE.ArchitectureAnalyzers/README.md) | CASCOPE*; extension — per this ADR |

---
## Context

In the aviation metaphor **PFD** is the primary “instrument”: dense, authoritative context picture without tab hunting ([0021](0021-pfd-mfd-cockpit-attention-model.md)). At **pure code** level Roslyn does **not** know whether a widget is “light”: weight is product policy, not an IL property.

To **formalize** PFD discipline in code we need **explicit contracts** (attributes, base types, narrow channel subscription interfaces). Then analyzers become **invariant guards**, not heuristics on class names.

Already fixed: layer boundaries **channel / CDS / compositor** without Avalonia in `Cockpit/Channels`, `Cds`, `Composition` ([0036](0036-cds-channel-compositor-surface-pipeline.md), CASCOPE001/002). This ADR adds a layer for components **declared as strict PFD surface** (see below: this is **not** all PFD region geography).

<a id="adr0037-zone-vs-surface"></a>

## Attention zone vs strict surface (0021 vs 0037)

**Two axes that must not be mixed:**

| Axis | What it is | Where fixed |
|------|------------|-------------|
| **Attention zone (screen geography)** | Which **region** is the gaze anchor: forward, **PFD**, MFD, EICAS as channel, etc. What **physically** sits left/next to the editor per preset. | [0021](0021-pfd-mfd-cockpit-attention-model.md) (“Zone architecture”, anchors vs attention flow). |
| **Strict PFD surface (engineering contract)** | Which **code component** voluntarily takes the primary-contour “instrument” role: indicators only, allowed weight, allowed data channels. | This ADR (§1–§3), explicit markers in code. |

**Why separate:** aviation strictness must not turn the IDE into an ascetic terminal where “you cannot even click a file in the PFD region”. Solution navigation is **“where am I”** context; banishing it to MFD only for formal “PFD = read-only” raises **tax** on gaze and context switches ([0021](0021-pfd-mfd-cockpit-attention-model.md) — switches as productivity cost).

**Hybrid picture (“layer cake”):** in the **same** PFD attention region on screen you can have:

- **Interactive navigation** (e.g. solution explorer, workspace anchor): user **clicks, expands nodes, picks files** — **navigation context**, not a “second forward”. These components do **not** get mandatory strict PFD surface marker **0037**; the **Roslyn analyzer for this ADR does not touch them** (no marker — no §1–§3 contract).
- **Strict PFD surfaces** — critical-contour indicators: agent status, EICAS/alerts, compact workspace health, etc. Mark with **`[PfdStrict]`** and/or base type **`PfdStrictControl`** ([naming canon](#adr0037-naming)). **Inside** such types the analyzer **forbids** arbitrary input, **WebView**, and other **Input Lock** and **Weight** violations (§1–§2): “slap on the wrist” for extra interactivity and heavy embeds where policy demands instruments only.

**One line for contributors:** PFD **attention zone** may contain **interactive** navigation; components **explicitly** marked strict PFD surface (**`[PfdStrict]`** or inherit **`PfdStrictControl`**) **must** obey this ADR’s **Input Lock** and **Weight Limit** invariants. The analyzer acts **only** on the marker, not on “drawn on the left”.

<a id="adr0037-glossary-pfd-dual"></a>

### Glossary (two meanings of “PFD”)

| Term | One line |
|------|----------|
| **PFD as attention zone** | Screen region in preset ([0021](0021-pfd-mfd-cockpit-attention-model.md)): *where* the “flight context” anchor lies. Geography alone does **not** auto-apply 0037 invariants. |
| **Strict PFD surface** | Code component with explicit [`[PfdStrict]`](#adr0037-naming) or base `PfdStrictControl`: §1–§3 and Roslyn apply. **Not** synonymous with “everything drawn in the PFD column”. |

**Link phrasing:** “lies in PFD zone” ≠ “under 0037 contract” until the type is marked strict.

**“Where am I in code” navigation:** examples use **solution explorer** as a familiar anchor; not the only form. Navigation by **symbols**, **type hierarchy**, **breadcrumbs**, **file structure**, etc. can be **more effective** for some tasks — still **“where am I”** anchor per [0021](0021-pfd-mfd-cockpit-attention-model.md), not [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md) instruments until the widget is marked strict. Concrete tools and presets are **product** choice; this ADR only separates **zone geography** and **marker contract**. Product direction “multiple views and related files” — [0039](0039-workspace-navigation-affordances.md).

## Solution

PFD surface invariants are set in **three** rule classes. Concrete **diagnostic IDs**, forbidden type lists, and **exceptions** come at implementation (separate commits); **meaning** of invariants is fixed here.

<a id="adr0037-naming"></a>

### Naming canon for strict surface (for repo code)

**Adopted for implementation:**

| Element | Name | Note |
|---------|------|------|
| Attribute (use) | **`[PfdStrict]`** | Full C# type name: **`PfdStrictAttribute`**, PascalCase; **not** `[PFD_Strict]` — consistent with .NET and other repo attributes. |
| Base type (optional) | **`PfdStrictControl`** | Avalonia `Control` base; descendants are strict PFD surface **without** duplicating attribute on every subtype (analyzer treats **attribute on type** or **inheritance from `PfdStrictControl`** — same semantics). |
| Do not confuse with | **`PfdSurface`** | “surface” in ADR text is **role** in UI; no separate **`[PfdSurface]`** for “any panel in zone”; without strict marker the analyzer does not apply. |

**In-code glossary:** XML docs on `PfdStrictAttribute` and `PfdStrictControl` should link this ADR and briefly repeat [dual PFD meaning](#adr0037-glossary-pfd-dual) (attention zone ≠ automatic contract).

<a id="adr0037-p1"></a>

### 1. Weight control / dependency allowlist

**Rule:** a view or class **explicitly marked** as **strict** PFD surface (see [above](#adr0037-zone-vs-surface); [`[PfdStrict]`](#adr0037-naming) or [`PfdStrictControl`](#adr0037-naming)) **must not** directly use known heavy containers and embeds: e.g. **virtualized grids as PFD substitute**, **WebView**, **rich ItemsControl** with arbitrary templates where policy wants primitives only.

**Logic:** PFD in product is a **dense indicator** (text, icons, simple shapes, compact progress), not a secondary work area. The analyzer does **not** score FPS; it checks **forbidden types/namespaces** in the marked type and related AXAML (if `.axaml` / codegen analysis is wired).

**Caveat:** generic **`Grid`** as layout is often needed; banning “all Grid” is not the goal — only a **concrete list** of heavy types and scenarios agreed with the team (allowlist of permitted controls — optional phase two).

<a id="adr0037-p2"></a>

### 2. Input lock / read-only display

**Rule:** for **strict** PFD surface (by marker) in the aviation analogy — **read display** in the typical scenario; arbitrary input and focus capture in such a component **competes** with **forward** (editor, terminal) and breaks attention model. (Navigation in PFD **without** marker — outside this rule; see [above](#adr0037-zone-vs-surface).)

For **so marked** components the analyzer **must** flag explicit **pointer, keyboard, and focus** handlers (e.g. Avalonia input event subscriptions), except **explicitly listed** product-policy exceptions (one confirm button, safety — separate ADR/checklist).

**Why:** keep **strict** PFD surface an **indicator**, not an interactive panel stealing input from the primary work surface.

<a id="adr0037-p3"></a>

### 3. Data graph restriction (channels / streams)

**Rule:** a **strictly marked** PFD component **may** subscribe only to **allowed** primary-contour data streams (working names: e.g. **workspace health**, **agent status**, **next step** — exact types and interfaces fixed in code and [cds-contract-v0](../design/cds-contract-v0.md) as they stabilize).

Subscribing to **secondary** saturation streams (e.g. long **git history**, **full solution tree as sole instrument source**, **full filesystem dump** as main source for **strict** PFD surface) is **instrument role mismatch**: that content belongs to **MFD / unmarked navigation / side panels**. (Separately: **solution explorer** in PFD without 0037 marker is **not** bound to this channel list — it does not claim instrument role.)

**Logic:** analyzer checks **types** of subscription / channel factory calls in the marked component **if** channels are **stable compile-time API** (interfaces, generic factories). String DI keys, reflection, and untyped “general bus” are **outside** this ADR — need review tests, not Roslyn-only.

## Consequences

- Split **[0021](0021-pfd-mfd-cockpit-attention-model.md) (zone geography)** and **0037 (marker contract)** removes false “PFD interactive vs PFD read-only only”: interactivity is allowed in the attention region; **strictness** only on explicitly marked instruments.
- **Explicit** “strict PFD surface in code” discipline: without marker the analyzer is silent — **conscious** contract.
- Rules migrate in stages to [`CascadeIDE.ArchitectureAnalyzers`](../CascadeIDE.ArchitectureAnalyzers/README.md) (new IDs beside CASCOPE*) and **unit tests** for diagnostics like the existing test project.
- UX conflicts (e.g. one “OK” on PFD) resolved via **exception list** or attribute subtype, not canceling base invariant.

## Non-goals

- Avionics certification or full **ARINC 661** / hardware **CDS** reproduction.
- Replacing **human** design review: analyzer catches **known violation classes**, not “pretty/ugly”.
- Automatic **`Grid`** ban without exceptions and without agreed type list.

## Open questions (before Accepted)

1. Minimal **allowlist** of channels for PFD v1 and mapping to existing `Cockpit/Channels`.
2. Whether **separate** `.axaml` analysis is needed or partial class + codegen suffices.

**Naming off the table:** canon — [`[PfdStrict]` / `PfdStrictAttribute`](#adr0037-naming) and optional [`PfdStrictControl`](#adr0037-naming); see table in that section.

---

## Change history

<a id="adr0037-history"></a>

| Date | Change |
|------|--------|
| — | [glossary](#adr0037-glossary-pfd-dual); navigation and [0039](0039-workspace-navigation-affordances.md). |
| — | “Attention zone vs strict surface” ([0021](0021-pfd-mfd-cockpit-attention-model.md)). |
| 2026-04-16 | [naming canon](#adr0037-naming) for strict surface: `[PfdStrict]`, `PfdStrictControl`. |
