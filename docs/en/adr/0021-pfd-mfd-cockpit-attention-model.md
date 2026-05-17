# ADR 0021: PFD / MFD — Cascade IDE cockpit attention model

**Status:** Accepted  
**Date:** 2026-04-06  
**Updated:** 2026-04-15 — reference to [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md#adr0037-zone-vs-surface) (PFD geography vs strict surface). Details — [§ History](#adr0021-history).

**Supersedes:** [`concept-pfd-mfd-cascade-v1.md`](../ui-ux/concept-pfd-mfd-cascade-v1.md) (draft concept → formalized here).

## Summary

Cascade IDE organizes the main window as a **cockpit**, not “editor + side panels”. Three **attention anchors** — **PFD** (navigation), **Forward** (primary work: editor or Intercom), **MFD** (secondary instruments as **pages**: terminal, build, Git, chat page, IDE Health) — stay stable while presets change visibility and density. **EICAS** is the single summary channel for workspace health; subsystems feed it instead of spamming the editor. Legacy Focus/Balanced/Power names describe *attention flow* on top of these anchors; shipping layout is **Flight** (see [UI layout](../ui-ux/cascade-ide-ui-layout-v1.md)).

- **PFD / Forward / MFD / EICAS / HUD** — attention anchors and UI density policy, not a promise to “embed every app in the world”.
- **Presets** (TOML, [0010](0010-ui-modes-toml-configuration.md)) define *where* tools live; **modes** Focus/Balanced/Power define *how* to manage attention flow on top of anchors.
- **EICAS** — one consolidated alerting channel; subsystems emit events instead of competing with toasts in the forward editor.
- **ARINC 661** ideas — one compositor and zone boundaries; certification and full 661 profile **out of scope**.
- External agents and chats — **bridges** ([0016](0016-agent-client-protocol-external-agent.md)), without a second “cockpit” in the PFD zone.

## Related ADRs

| ADR | Role |
|-----|------|
| [0010](0010-ui-modes-toml-configuration.md) | UI modes, TOML |
| [0012](0012-floating-workspace-chrome.md) | floating chrome |
| [0013](0013-command-surface-and-discoverability.md) | commands and discoverability |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | multi-window |
| [0016](0016-agent-client-protocol-external-agent.md) | ACP / external agent |
| [0005](0005-defer-dynamic-plugins-mef.md) | plugins deferred; when they appear — §“Plugins and attention model” below |
| [0022](0022-workspace-health-lexicon.md) | naming lexicon |

### Outside ADR

| Document | Role |
|----------|------|
| [`power-mode-concepts-v1.md`](../ui-ux/power-mode-concepts-v1.md) | UX: Power modes |
| [`cascade-ide-ui-layout-v1.md`](../ui-ux/cascade-ide-ui-layout-v1.md) | UX: UI layout |
| [`concept-to-implementation-map-v1.md`](../ui-ux/concept-to-implementation-map-v1.md) | Concept → code map |
| [`command-palette-ux-concept-v1.md`](../ui-ux/command-palette-ux-concept-v1.md) | Command Palette UX |
| [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md) | IDE Health / EICAS implementation map |

---

## Context

A developer in an IDE juggles dozens of signals: code, build, tests, agent, git, safety. Without an explicit attention model, the UI drifts to two extremes: “everything hidden” (Focus without feedback) or “everything visible” (Power with banner blindness).

Aviation solved this by splitting instrument panels by **role in managing attention**: PFD (primary flight display), MFD (multi-function display), EICAS/CAS (crew alerting). This ADR maps that model to an IDE and fixes principles, design criteria, and open decisions.

### Philosophy: context switching and a stable attention contour

The main **tax** on developer productivity is **context switching** and forced **leaving the IDE**: not only Alt-Tab, but lost flow, “where was I looking”, which communication channel matters now. Ideally the IDE aims for a **single stage**: code plus the minimum needed for the current task and communication, so those switches happen less often.

**“Everything in one place”** here is a **goal**, not a requirement to literally embed every external service in the binary. You cannot endlessly embed apps for practical reasons: there are many different chats; full integration of each leads to **web views** (limits and trust), **product bloat** (support, security, updates), or **third-party plugins** (responsibility boundary, quality, fragmentation). A tenth chat inside the IDE does not “add a feature” — it adds another input model and notification source.

**PFD / MFD / HUD / EICAS** in this ADR are not a promise to “embed everything”, but a **stable attention contour**: predictable gaze anchors, what is visible by default, what requires a conscious switch to MFD, what stays **outside** (second monitor, external client) and connects via a **bridge** ([ADR 0016](0016-agent-client-protocol-external-agent.md), clipboard, deep link) without duplicating a full second “cockpit” in PFD. “One window” in a mature sense is a **coherent contour**, not a monolith of all apps in the world.

<a id="cognitive-load-neurodivergence-product-note"></a>

### Cognitive load and neurodivergence (product link, not medicine)

**Why mention in an ADR:** the principles above follow **attention ergonomics** and fewer context switches. We separately record a **product** line: for some users the same switches and competing signals cost **more** — including with **ADHD** and other neurodivergence. This is **not** a claim that the UI “treats” conditions, **not** new technical invariants for §1–§18, and **not** a change to §18 criteria; it is **rationale** for why the topic belongs in onboarding, presets, and UI density discipline (see §16, §19).

**Link to the model (idea, not a norm):** explicit anchor hierarchy (forward / PFD / MFD), a **consolidated** alerting channel (EICAS), HUD inside forward, and **Dark Cockpit** in nominal mode aim to reduce **unnecessary** friction when returning to flow after interruption and when sorting signals. Individual differences are large; **presets**, flexibility, and noise off-ramps remain mandatory.

**Related public sketch** (outside this repo, short narrative for readers): [Attention, friction, and neurodivergence in the IDE](https://karataevdmitry.github.io/writing/attention-contour-neurodivergence.html) (EN); [Внимание, трение и нейроотличие в IDE](https://karataevdmitry.github.io/ru/writing/attention-contour-neurodivergence.html) (RU). Do **not** duplicate a long product discussion in the ADR — pointer only.

<a id="arinc-661-borrow"></a>

### Ideas from ARINC 661 (portable, not a copy of the standard)

In avionics **ARINC 661** defines the interface between **application systems** and the **display system** (**CDS**, Cockpit Display System): data and commands go into a common path; **composition** of the screen, layers, and priorities belong to one display runtime. Cascade **does not** implement a 661 profile or require certification under it; we port **architectural principles** that reduce “every subsystem paints on top of everything”.

| 661 idea | How we use it in this ADR |
|----------|---------------------------|
| **One “glass” compositor** — many data sources | **EICAS** and attention channel placement — one contour; build, Git, agent, MCP, etc. **feed events and state**, not a separate toast layer on the editor without rules (see §5, §6). |
| **Responsibility boundaries** | Zones `forward` / `pfd` / `mfd` / `eicas` and presets ([§2](#2-mapping-to-cascade-zones), [§2.1](#21-configuration-layers-product-user-workspace-repository)): what may appear in PFD vs only in MFD — **policy**, not a plugin race for z-order. |
| **Priority and layers as a scheme** | Warning / Caution / Advisory levels (§5) and Dark Cockpit (§6): attention hierarchy is **explicit**, not initialization order of extensions. |
| **Declarative description + data binding** | Orientation for `workspace.toml`, `AttentionZonePanelRuntime`, and future layer merge: **what** is on screen and **where** values come from — easier to align with the repo and test than scattered imperative coloring. |

**We do not port:** certification requirements, avionics supplier compatibility, full 661 widget model as such. **DO-178C** (onboard software safety evidence process) is not imported into the product — if needed later, parts of the agent contour may live under **other** assurance discipline; that is out of scope for this ADR.

### Zone architecture (fixed conclusions)

<a id="anchors-vs-attention-flow"></a>

#### Anchors and attention flow (do not mix)

**Spatial anchors** — **where** in the window (or on which monitor) a zone sits: three regions **forward / PFD / MFD**, plus **EICAS** channel and **HUD** layer inside forward. A preset defines **placement** of tools in those regions. Multiple panels assigned to **one** zone (e.g. both to `pfd`) should form **one anchor’s content** — composition inside the region (tabs, pages, stack), not two independent screen positions that merely share the same id. The zone string id does not encode “left / center / right” (see §“Zone identifiers” below) — geometry comes from the window frame and preset — but the id **semantics** are “which anchor region”, expected **place** on stage, not an arbitrary label in data without a matching region on screen (implementation goal — explicit layout binding to anchors).

**Attention flow** — **how** focus and priority are managed: Focus / Balanced / Power modes, EICAS strip, escalation, conscious move to secondary. This is **policy on top of** anchors; it cannot replace physical zone layout and must not be mixed with it in docs and code.

On **one screen** the target attention model is **three zones** (three “gaze anchors”), not scattering across arbitrary windows. Zone **content does not move** between PFD and MFD at runtime: each zone’s role is set by **preset** (e.g. TOML, [ADR 0010](0010-ui-modes-toml-configuration.md)) with optional **preset override** — user and/or **workspace repository** (see §2.1), not free drag-and-drop between semantics.

| Zone | Role | Content (orientation) |
|------|------|-------------------------|
| **Forward** | Dominates time and area; work object | Editor (active document). **HUD** (§9) — not a separate zone: inline hints, in-place diagnostics, ghost text, etc. **enrich forward**, not a fourth attention anchor. |
| **PFD** | Current flight context: where we are in the workspace, what we work on, what blocks | Solution explorer, position/context in solution, task/session as “route”, Problems/diagnostics when present, compact “what is critical now” indicators if the preset places them here (details in preset). |
| **MFD** | Everything secondary, switched consciously | Git, auxiliary info, docs, embedded browser, full chat/trace, terminal, long logs, debug, extended agent operations — per preset list. |

**PFD zone geography ≠ one engineering contract for every panel in the column.** The **PFD** attention region may hold interactive navigation (e.g. solution explorer) and compact “instruments” (agent status, EICAS, workspace health). Strict code invariants (input lock, weight, channels) apply only to components **explicitly** marked as strict PFD surfaces; see [0037 § Attention zone and strict surface](0037-pfd-surface-invariants-and-roslyn-enforcement.md#adr0037-zone-vs-surface).

<a id="anchor-pfd-mfd-content-vs-telemetry-page"></a>

**PFD/MFD anchor content is not limited to one IDE Health contour.** Any tool aligned with the zone role (flight context vs secondary) may live in the anchor region per preset: build/tests/debug/git summary as a **page** instead of a strip, dependency graph, symbol browser, embedded browser, Git, etc. — including **side by side** (tabs, stack, split inside the region). The term **Page** in **IDE Health channel** config (`ide_health_surface` → dedicated page vs strip; blueprint [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md)) means **only** the **presentation layer for that channel** (large area in the anchor instead of a bottom strip), not “what may live in PFD/MFD at all”.

**EICAS** (§5) is an **alerting and prioritization channel** (W/C/A), not a third “column” beside forward / PFD / MFD. On-screen placement is an **open preset choice**: horizontal strip, overlay, compact list, or other **presentation layout**; on a small screen do not confuse **channel role** with **geometry of one implementation**.

**Multi-monitor:** the same three semantics **spread across displays**. **Small screen:** little area — **modes, panel stack, collapse** allowed without mixing zone roles and without moving tools between PFD and MFD without a preset change.

<a id="plugins-attention-binding"></a>

### Plugins and the attention model (future)

When **plugins / third-party UI extensions** appear ([0005](0005-defer-dynamic-plugins-mef.md) — timeline open), they **cannot** be “just another window on top” without semantics: otherwise the PFD/MFD/EICAS/HUD model **loses meaning** and z-order / attention races return — what this ADR steers the product away from.

**Rule:** anything an extension **wants to show** in the IDE UI **must** bind to the **attention model** — explicitly: an **anchor** (`forward` / `pfd` / `mfd`), a **channel** (e.g. EICAS, IDE Health), or a **layer** (HUD on forward), within a **preset** ([0010](0010-ui-modes-toml-configuration.md), [§2.1](#21-configuration-layers-product-user-workspace-repository)). Wording for extension authors: **“decide what you are in the cockpit”** — otherwise integration fails the contract.

Consequences: the extension load contract includes a **role declaration** (not only a technical entry point); arbitrary floating panels without zone binding are **not** the target pattern.

---

## 1. Terms (aviation → IDE)

| Aviation | Definition | Cascade analog |
|----------|------------|----------------|
| **Forward** (not an aviation term; zone role here) | Direct “outside” view — main focus | Editor; inside it — HUD (§9) |
| **PFD** (Primary Flight Display) | Indications needed to hold context and continue work safely without losing orientation | Current context zone: solution, where we are, what we work on, problems/diagnostics (see table above) |
| **MFD** (Multi-Function Display) | Heavy switchable views not competing with primary context and forward for constant focus | Git, docs, browser, long logs, chat/trace, terminal, etc. — per preset |
| **EICAS / CAS** (Crew Alerting System) | Consolidated prioritized message list | Single alert summary (§5); not a “fourth zone” among the three anchors above |
| <a id="glossary-kvs"></a>**PIC** (*pilot in command*) | In aviation — person **responsible** for flight safety and crew decisions | **Metaphor** in Cascade docs: **human operator** in the IDE loop (not the agent), owning the final decision. **Not** synonymous with **PF** (§1.0: “hands on the controls” / active flying). Wording like “PIC signals” in [Incapacitation / presence](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) refers to **operator state** (presence, attention), **orthogonal** to repository telemetry (IDE Health, EICAS channel about code/build). |
| **HUD** (Head-Up Display) | Projection on the windscreen without looking away | **Only inside the editor** (§9); does not form a separate zone |

### 1.0 PF/PM as Driver/Navigator (mental model)

Aviation PF/PM map almost directly to pair programming:

- **PF (Pilot Flying) ≈ Driver**: “hands on the controls” — active actions and a fast edit loop. In the IDE this is mainly **forward/editor** and minimal in-place HUD hints.
- **PM (Pilot Monitoring) ≈ Navigator**: holding the big picture, risks, checklists, coordination, deviation control. In the IDE this is mainly **PFD/MFD/EICAS** as secondary surfaces: diagnostics, plan, logs, Git, agent trace — per preset.

This mapping is language for tradeoffs: what must stay PF/Driver-friendly (not compete with forward) vs what is a PM/Navigator tool in secondary zones.

### 1.1 Channel, presentation layer, code names (canonical)

<a id="glossary-channel-presentation"></a>

Orientation for implementation blueprints and code comments: same words — same meanings. Do not mix three different “hosts”: see **ProcessHost** row and note on **channel** vs **View**.

<a id="glossary-cds-contract"></a>

| Term | Meaning |
|--------|---------|
| **ProcessHost** | External **process host** that starts or connects to CascadeIDE (e.g. Cursor/IDE, IDE launch with `--mcp-stdio` for MCP, ACP client). **Process and transport lifecycle**, not window layout and not EICAS / IDE Health channel. In MCP/ACP/stdio docs say **ProcessHost** explicitly when needed so it is not confused with `*Host*` on controls. |
| **Channel** | Semantic contour of **data and priority**: what we show and why (e.g. **EICAS channel** — W/C/A; **IDE Health channel** — build/tests/debug/git, **separate** from CAS). A channel is **not** a UserControl name and not screen geometry. |
| **Presentation layer** | How and where in **markup** the same data appears: strip, zone page, overlay, card — per preset (`ide_health_surface`, AXAML). Not an event bus, not message routing, not an “event host”. |
| **`EicasAlertsBar` / `eicas_alerts_bar`** | Stable ids in code and TOML: **enable presentation slot** for the **EICAS channel** in the current layout. Control: `EicasAlertsBarView`. This is **not** the channel itself: the channel is W/C/A warnings and priority; the type is a **UI container**. No *Host* suffix to avoid ProcessHost confusion; no *Channel* in View name — in code *Channel* is for **model/provider**, not AXAML containers. |
| **`WorkspaceChromeBandView`** | **Chrome strip** container above the bottom dock: grid like `MainGrid`, `EicasAlertsBarView` slot on top, `IdeHealthStripView` below. Markup for **IDE Health channel** (segments from `IdeHealthSurfaceCompositor`) and EICAS slot; see [implementation blueprint](../../design/workspace-health-implementation-map-v1.md). |
| **CDS (cockpit contract)** | In avionics **CDS** (Cockpit Display System) is the display system; in Cascade **contract sense** — agreed description of **cockpit semantics**: attention zones, presentation topology (`presentation`), which `TopLevel`s are active, active secondary page, region visibility — **without** listing every control. **Not** synonymous with “one code class” and **not** a replacement for channels (IDE Health, EICAS, readiness). Details — [`cds-contract-v0.md`](../../design/cds-contract-v0.md). |
| **`UiLayoutSnapshot` / `ide_get_ui_layout`** | Snapshot of the **visual tree** (including multiple windows, roles `main` / `mfd_host` / …) for inspection and automation. **Orthogonal** to CDS: same screen has cockpit semantics and a control tree; different jobs (attention vs find by name/hierarchy). Implementation: `Cockpit/Surface/UiLayoutSnapshot.cs`; see [0017](0017-multi-window-workspace-and-agent-surfaces.md). |

**Channel vs code containers:** domain **channel** is meaning and message flow; **`EicasAlertsBarView`**, **`WorkspaceChromeBandView`** are **where to draw** in the window. Renaming them to `*Channel*` would mislead: *Channel* in code is expected on **model/provider**, not AXAML containers. For IDE Health the meaning is already in `IIdeHealthChannel` / `IdeHealthInputSnapshot` / `IdeHealthSurfaceCompositor`.

<a id="glossary-presentation-vs-channel"></a>

### 1.2 Presentation slot and channel content (two levels)

In discussion and docs, **do not mix** two abstraction levels.

| Level | Question | Example wording |
|--------|--------|------------------|
| **Presentation slot** (region surface) | *Where* and *how large* to show a tool: strip, full MFD region page, card, overlay? | “full MFD page”, “IDE Health bottom strip”, MFD **full-page** surface |
| **Channel / content** | *Which* data stream or tool fills the slot? | IDE Health (build/tests/debug/git), EICAS alerts, environment readiness (LSP, model, agent transport), Git, chat |

Colloquial **“separate page”** usually means the **first** level (large slot instead of strip). **What** is on that page is the **second** level; another MFD page (environment vs IDE Health) is a **different channel**, not a different slot type.

**Config/code name:** values like **`DedicatedPage`** on `ide_health_surface` / `IdeHealthUiSurface.DedicatedPage` set **presentation layer only for the IDE Health channel** (same segments as the strip, in page layout instead of `IdeHealthStripView` — see [blueprint](../../design/workspace-health-implementation-map-v1.md)). **Not** synonymous with “any full-screen MFD page”; in text prefer **“IDE Health page (Page mode)”** or **“IDE Health on full MFD page”**. Other full-screen views (environment, docs, etc.) are named by **content**, not by overloading `DedicatedPage`.

**Environment readiness (channel separate from IDE Health):** quick “is everything OK” — LSP, agent transport, required executables (including via PATH on Windows/Linux), and **only env vars/paths the IDE itself uses**; not a full `environ` dump. Not a replacement for settings screens when editing. ADR: [0023](0023-environment-readiness-glance.md); blueprint: [`environment-readiness-glance-v1.md`](../../design/environment-readiness-glance-v1.md).

**IDE Health** — canonical product/ADR name after [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) (formerly *Workspace Health*). Implementation types `IdeHealth*` (see [implementation blueprint](../../design/workspace-health-implementation-map-v1.md)). Russian UI copy may use **состояние IDE**. Lexicon: [0022](0022-workspace-health-lexicon.md).

---

## 2. Mapping to Cascade zones

Source for panel and mode names: [`cascade-ide-ui-layout-v1.md`](../ui-ux/cascade-ide-ui-layout-v1.md), [`ui-modes-overview-v1.md`](../ui-ux/ui-modes-overview-v1.md), [ADR 0010](0010-ui-modes-toml-configuration.md).

| Layer | Role in IDE | Cascade candidates |
|------|------------|---------------------|
| **Forward** | Work object; dominates | Active document, caret; HUD: inline diagnostics, ghost text, gutter (§9) |
| **PFD** | Flight context without tab hunting | Solution Explorer; breadcrumbs / position in solution; task/session; Problems; compact next-step / agent confirmation indicators if preset places them here, not EICAS/MFD |
| **MFD** | Exploration and secondary streams | Git; full chat/trace; bottom dock (terminal, build output, tests, debug); docs; embedded browser; extended Agent Operations in Balanced/Power |
| **EICAS** | Alert consolidation | One W/C/A channel (§5); visually — list, strip, or overlay per preset, **not** a fourth tool column |

**Cutoff rule:** if an element is **not needed to hold workspace context and orientation** (where we are, what breaks work, what we edit) and is not part of the edit object in the editor — default **MFD** or **EICAS**, not duplication in PFD. **Moving** a panel between PFD and MFD — only by **preset** change (or user preset override), not session drag-and-drop.

**Role stability:** preset defines which tool is in which semantic zone; not “move Git to PFD for a day” without config change.

### Zone identifiers (machine-readable)

For data ([ADR 0010](0010-ui-modes-toml-configuration.md)), code, and future panel binding use **stable string ids** (lowercase Latin, no spaces). Geometry “left / center / right” is **not** encoded in the id — window frame and preset define it.

| Identifier | Purpose | Type |
|---------------|------------|-----|
| `forward` | Forward: editor area (active document) | **Spatial zone** (anchor #1) |
| `pfd` | PFD: current workspace context | **Spatial zone** (anchor #2) |
| `mfd` | MFD: secondary tools and long streams | **Spatial zone** (anchor #3) |
| `eicas` | EICAS / CAS: alerts and prioritization | **Attention channel**, not a third column like `pfd`/`mfd`; may be strip, overlay, compact list (§5) |
| `hud` | Inline layer on editor | **Layer inside `forward`**, not a fourth peer zone with `pfd`/`mfd` |

**Naming rules**

1. In configs and APIs use **only** canonical ids from the table; do not introduce synonyms (`primary_flight`, `cas` as replacement for `eicas`) in the same field without explicit schema migration.
2. Changing id value or meaning is a **breaking change** for `UiModes/` bundle → bump **`schema_version`** in the index ([ADR 0010](0010-ui-modes-toml-configuration.md)).
3. Binding a specific panel to a zone in TOML is a separate implementation task (e.g. `attention_zone = "pfd"` on a panel record); this ADR fixes the **dictionary of allowed values**.

**Minimum set for attention map:** `forward`, `pfd`, `mfd`; optionally `eicas` and `hud` bound to `forward`.

**Code:**

- `Features/UiChrome/AttentionZone.cs` — `AttentionZoneIds`, `AttentionZone`, `TryParseCanonicalId` / `ToCanonicalId`, `IsSpatialAnchor` / `IsAlertingChannel` / `IsHudLayer`.
- `Features/UiChrome/AttentionPanelIds.cs` — stable surface ids for `workspace.toml` keys.
- `Features/UiChrome/AttentionZonePanelRuntime.cs` — surface→zone map: code defaults, override via **`[attention_zone_panels]`** in `UiModes/workspace.toml` (loaded with chrome metrics).

### 2.1 Configuration layers: product, user, workspace (repository)

Besides **personal** preferences (global settings, user `UiModes` bundle if installation policy allows), a **project** layer makes sense: optimal **placement** of the same tools across PFD/MFD/EICAS can differ by repo (monorepo vs library, service with mandatory agent chat vs CLI utility, team agreement to keep tests “closer to context”, etc.). That does not break the attention model — only **fills** zones within the same semantic roles.

| Layer | Purpose | Example |
|------|------------|--------|
| Code defaults | Safe values without config | `AttentionZonePanelRuntime` |
| Product bundle | Single base for all installs | `UiModes/workspace.toml` shipped with Cascade |
| User | Personal flow, bundle override if supported | User `UiModes` directory / override |
| **Workspace repository** | Team convention, travels with code | TOML fragment with `[attention_zone_panels]` (and chrome metrics if needed), bound to open solution/workspace |

**Merge contract (target):** when multiple sources define the same surface, priority is **higher for more “local” task context**: **workspace repository → user bundle → product bundle → code defaults**. Zone semantics (`pfd` / `mfd` / …) do not change; only **which panel id maps to which zone**, within stable `AttentionPanelIds`.

**DX meaning:** less friction “reconfigure per repo every time” and onboarding aligned with the **project cockpit** — like `.editorconfig` for repo style.

**Today:** on startup only **`UiModes/workspace.toml`** from the app bundle directory (`UiModeCatalog`) loads, without automatic merge from the open repository. **Next:** stable path to **per-repo** config (candidate: `.cascade/workspace.toml` at repo root or next to `.sln`), same `UiWorkspaceToml` schema and **merge on top of** bundle when active workspace changes. Until merge, teams rely on the shared bundle or duplicate the section manually.

---

## 3. Link to Focus / Balanced / Power modes

Modes shift balance “minimum noise” vs “full cockpit” ([`power-mode-concepts-v1.md`](../ui-ux/power-mode-concepts-v1.md)). **Three zone** semantics (forward / PFD / MFD) do not change; **visibility and area** of MFD and chrome density do.

- **Focus** — maximum area for **forward**; PFD compact; MFD (chat, dock, Git) on demand or hidden; editor HUD per mode policy.
- **Balanced** — forward + visible PFD + one default MFD layer (e.g. chat with Agent Operations).
- **Power** — explicit **cockpit**: more **MFD** and IDE Health indicators open at once; **forward** still dominates, PFD readable; not “inflate PFD with foreign content”, but more secondary layers per preset.

---

## 4. Agent and MCP

- **Short next step, safety level (L1–L3), dangerous operation confirmation** — in **forward** (near code / HUD) and/or **PFD** (context), per preset; do not spread across MFD without conscious switch.
- **MFD level:** full conversation, trace timeline, long tool-call logs, secondary workspace overview.

**ProcessHost (MCP / ACP):** the side that **starts the IDE process or holds transport** (stdio, socket) to agent protocols — [§1.1 **ProcessHost**](#glossary-channel-presentation), not `EicasAlertsBarView` or cockpit presentation layer.

MCP contracts and control names — [`cascade-ide-ui-layout-v1.md`](../ui-ux/cascade-ide-ui-layout-v1.md); this ADR fixes **attention priorities**, not the protocol.

---

## 5. EICAS / CAS — alerting channel

EICAS is **not a fourth anchor** beside forward / PFD / MFD and **not synonymous** with “bottom strip”: it is an **alerting channel** with prioritization (W/C/A). It may be a strip, overlay, compact list, etc. — see §“Placement” below. In code and TOML **presentation enablement** is **`EicasAlertsBar` / `eicas_alerts_bar`** ([§1.1](#glossary-channel-presentation)). That is **not** “the strip”: a strip is one layout option. On a small screen placement and minimum salience are separate UX (also Dark Cockpit, §6).

### Problem

System failures (MCP disconnect, build failure, agent safety breach, test regression) are scattered across tabs and badges. The user can miss a critical event when looking at the wrong panel.

### Solution: three annunciation levels

Like EICAS, all system messages stack into **one prioritized list**:

| Level | Color | IDE behavior | Examples |
|---------|------|-----------------|----------|
| **Warning** (red) | Immediate action | Appears in **EICAS zone** or overlay on forward/PFD per preset; blocks autonomous agent actions until acknowledged | MCP server disconnected at L3; build failed on a file the agent is editing |
| **Caution** (amber) | Awareness, no block | IDE Health strip badge color change; CAS list entry | Tests partly failed; agent exceeded tool-call budget |
| **Advisory** (neutral) | Information | CAS list only, not PFD | Git: upstream updated; LLM latency above normal |

**Everyday words (like IDE diagnostics), EICAS and `AnnunciatorLampLevel`:** no separate “I” tier — **Information** = **Advisory** (A). Lamp colors — `CockpitPrimitivesPalette.Annunciator` ([ADR 0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)).

| Everyday (IDE, docs) | EICAS | `AnnunciatorLampLevel` | Color / role |
|---------------------------|-------|------------------------|-------------|
| Error; critical failure / service unavailable | Warning (W) | `Critical` | Red |
| Warning “pay attention”, not catastrophe | Caution (C) | `Caution` | Amber |
| Information | Advisory (A) | `Advisory` | Blue |

**TCAS (different contour, same aviation):** *Traffic Advisory* (TA) warns of nearby traffic; *Resolution Advisory* (RA) demands immediate maneuver. In **our** W/C/A canon **RA** is closer to **Warning (W)** / `Critical` (act now), **TA** to **Caution (C)** (awareness without the same compulsion as RA). **Do not confuse** with EICAS column **Advisory (A)**.

**Why “Advisory” in both:** not the same sign in different scales. English *advisory* is a common word; **TCAS** named *Traffic Advisory* historically. In **EICAS** letter **A** is the third, softest system message tier (W/C/**A**). The word coincided; **priority slots did not**: TA by urgency is closer to **Caution** than EICAS Advisory (A).

**Historical note:**

- **ACAS / TCAS (ICAO).** Normative texts describe crew output as **two kinds of advisories** — *Traffic Advisory* and *Resolution Advisory*. Here *advisory* is a **general** term; *Traffic* / *Resolution* split **early** preparation (TA) from **commanded** maneuver (RA). A **different** axis than EICAS tiers.
- **EICAS.** Crew alerting for engines and systems built on **Warning**, *Caution*, and softer *Advisory* — routine state without W/C immediacy. Letter **A** in W/C/A is that third **system** tier, not TCAS.
- **Takeaway:** the same English word from two standards (mid-air collision vs integrated display). Our ADR canon **W/C/A** is the **EICAS-like** IDE channel; TCAS is an **anti-example** for naming confusion.

### Placement

**One v1 option:** compact block between IDE Health strip (`IdeHealthStripView`) and bottom dock — not the only **presentation placement** for the channel. Alternatives: **popup** overlay on PFD/forward for Warning, separate card in MFD, etc. Not a permanent panel — appears **only when active alerts exist** (Dark Cockpit, §6).

### Escalation

**Goal:** if **Warning** means “risk at current agent autonomy” and the user **did not take responsibility** (explicitly) and the condition **did not clear** — the system must not leave the agent in full autonomy forever. Analog dead-man / acknowledgement in aviation, not “punishment for a missed banner”.

**What counts as acknowledged (ack):**

- **Explicit UI action** — e.g. “Understood / Reduce autonomy / Continue consciously” (copy — UX) recording that the user **saw** Warning and accepts behavior at current risk; or
- **Automatic Warning clear** — cause fixed (MCP back, build green, conflict resolved): timer resets, no escalation.

**“Not acknowledged within N seconds”** means: Warning **still active** (critical condition still true), and within **N** seconds neither ack nor auto-clear.

**Timeline:**

| Moment | Behavior |
|--------|-----------|
| T=0 | Warning in EICAS/overlay; per policy — **block dangerous** autonomous agent steps until ack or cause cleared. |
| 0…N | Visual (optional sound per §5) draw attention; no infinite blink (§6). |
| T=N with Warning still active and no ack | **Downgrade** agent safety (e.g. L3→L2 or L3→L2→L1), **stop** autonomous actions until explicit raise. **Safety invariant**, not hidden merge. |

**N** may differ by event class: “MCP offline” shorter than “build failed” if policy allows calm fix without immediate downgrade.

**Merge and other flows:** merge/Git is a separate contour until policy ties it to EICAS. Timer N applies to **that** Warning, not a separate merge timer.

**Limit:** without extra signals the system **cannot distinguish** “went for coffee” from “consciously accepted risk”. Default safer expression: **downgrade autonomy**, not irreversible file actions. Full auto escalation and exact N — **after** manual level confirmation in early versions (see §16 “Not v1 goals”).

### Voice alerts (GPWS / TAWS motivation)

In aviation GPWS/TAWS is **learnable safety audio**, not atmosphere: short standard phrases, priority over other audio. In the IDE only as **optional audio layer** to the same prioritization as EICAS, not “sound for mood”.

**When appropriate:** draw attention to **Warning** when the user is not looking at EICAS; before confirming **dangerous** agent step; **accessibility** (voice duplicate of critical message — with quality TTS and languages). **When not:** voice for Advisory/Caution, reading long logs, any sound “for vibe”.

**Contract (if built):**

- **Off** by default; explicit **opt-in** in settings.
- Gating by severity: normally only **Warning** (or agreed critical subset); do not make voice a second spam stream beside CAS.
- Short **fixed** phrases or narrow templates — not TTS of full alert text.
- Settings copy must consider **open office** and neighbors.

**Status:** not **v1**; stable visual EICAS and §18 metrics first — then separate product/UX decision on audio (see §16).

---

## 6. Dark Cockpit Principle

### Principle

In normal flight the cockpit is **dark** — lamps off while nominal. An indicator draws attention **only on deviation**.

### Application in IDE

**PFD** and IDE Health strip (where preset places it) in nominal state are **calm**: neutral badges, no extra color accents, CAS list empty and hidden.

On problem — **active attention**:

- Build/Test/Debug badge → color shift (neutral → Warning/Caution).
- CAS list appears with short description.
- One pulse animation (not infinite blink — habituation and ignore).

### Design rule

> Every **primary context** element (PFD and compact IDE Health strip per preset) has **two visual states**: “nominal” (quiet, almost invisible) and “deviation” (noticeable). If it always looks the same — it is decoration, not attention discipline.

### EICAS presentation and settings

**Presentation layer** for EICAS/CAS (strip, MFD tab or page, overlay, compact indicators, dedicated zone in heavy preset) may be set by **presets and user preferences** — screen density and habit; **channel semantics** (§5) unchanged.

**Invariants** not taste: **Dark Cockpit** when nominal and **escalation salience** on Warning/Caution — critical alerts must not be lost because “I removed the panel for minimalism”. W/C/A policy and visibility on escalation — **§5–§6**; geometry — preset.

---

## 7. Scan Pattern

### Concept

Pilots learn a predictable instrument scan (T-scan / cross-check). IDE layout should support, not break, natural scan.

### Target Cascade scan (three zones + HUD)

Base model — **three attention anchors** on one screen (left/right order from preset):

```
  [ PFD: context ]    [ Forward: editor + HUD ]    [ MFD: secondary ]
        ↑                        ↑                            ↑
   short glances         main work time            conscious switches
```

- **PFD** — “where in solution, what breaks work”: explorer, Problems, compact task context; no tab hunting inside this semantics.
- **Forward** — dominates; **HUD** strengthens it without a separate gaze window.
- **MFD** — Git, long logs, full chat, docs, browser: not constant scan in one row with code.

**EICAS** with active warnings joins scan as a **short axis** (strip/badge/overlay), not a fourth permanent tool column.

### Design criterion

When reviewing any new UI element: **“Which of the three semantic zones (or EICAS) does this live in per preset? Does it break gaze predictability?”** If an element needs constant gaze **past** forward and PFD without conscious MFD switch — revisit placement or preset.

---

## 8. Mode Awareness and Mode Confusion

### Problem

**Mode confusion** — one of aviation’s deadliest issues: the pilot thinks the autopilot is in one mode when it is in another. Several accidents happened because of this.

In the IDE: the user may not realize the agent is in L3 Autonomous, or miss Focus → Power switch.

### Measures

1. **Peripheral cue:** each mode has its own window border accent color (partly implemented in Power with neon rims). Works on peripheral vision without looking at the badge.

2. **Transition handoff:** when moving toward higher agent autonomy (L1→L2→L3, Focus→Power) — brief “handoff callout” (toast or badge animation), like aviation “my controls” / “your controls”. Reverse (L3→L1) — quieter (reducing autonomy is safe).

3. **Alertness check:** if the user does not interact for a long time (configurable threshold) in L3 Autonomous — signal in **PFD or EICAS** (per preset): “Agent is running autonomously. Still monitoring?” Dead-man’s switch analog. No response → automatic downgrade to L2.

---

## 9. HUD layer in the editor

### Concept

In aviation HUD projects critical data on the windscreen — the pilot does not look away from the outside world.

In the IDE: PFD information may be **overlaid on the editor** without looking at side panels.

### HUD candidates

| Element | Placement | Purpose |
|---------|-------------|------------|
| Inline diagnostics | In code body | Errors / warnings in place (partly implemented) |
| Agent ghost text | In code body | Suggested next change |
| Gutter agent indicator | Next to line number | Icon: “agent will change this file / region” |
| File-level banner | Above text, below tab | “Agent is actively editing this file” — thin strip like collaborative editing |

**Terminology:** product name **Editor HUD** for the inline layer (diagnostics, ghost text, gutter, caret hints / inlays) and explicit distinction from **HUD banner** (file-level strip above text) — [ADR 0085](0085-editor-hud-inline-layer-and-hud-banner.md).

### Principle

HUD elements must **not block** editing and must be visually **lighter** than main text. They **enrich forward** (and may duplicate PFD/EICAS signals), not replace zones — the user can disable them without losing safety if critical state remains in PFD and EICAS.

---

## 10. Degraded Mode / Graceful Degradation

### Principle

In aviation, instrument failures have procedures. PFD **always shows something** — even “last known state”.

### Degradation table

| Failure | PFD indication | Automatic action |
|------|---------------|------------------------|
| MCP server disconnected | MCP badge → red/gray; last known state + disconnect timestamp | Safety auto-downgrade to L1; CAS Warning |
| LLM provider unavailable | Chat → “offline” stub; plan frozen | Agent read-only; editor and build work normally |
| Build tool not responding | Build segment in IDE Health → “stale” with timer | CAS Caution; does not block editing |
| All MCP servers down | IDE Health strip → “degraded mode” | L1 forced; “retry all” button |

### Design rule

> PFD never shows emptiness or an endless spinner without explanation. Minimum: **last known value + data age + source status**.

---

## 11. Situational Awareness (Endsley model)

Three levels of situational awareness:

| SA Level | Aviation | IDE | Served by |
|----------|---------|-----|---------------|
| **L1 — Perception** | Instrument readings | Build status, test count, agent state, git diff count | **PFD** — IDE Health badges, CAS list |
| **L2 — Comprehension** | “I’m too low for this approach phase” | “3 tests failed because of my PaymentService refactor” | **PFD** (brief) + **MFD** (detail) |
| **L3 — Projection** | “In 30 seconds I need to go around” | “If I merge — CI will break” | **MFD** — Agent plan, dependency graph, chat |

### Design criterion for PFD badges

PFD badges should deliver **not bare L1** (“Build: FAIL”) but lower **L2** — brief **comprehension**:

| Instead of (L1) | Better (L1 + L2) |
|-------------|------------------|
| Build: FAIL | Build: 2 errors in PaymentService.cs |
| Tests: 3 failed | Tests: 3 failed (RetryPolicy*) |
| Git: 5 files | Git: 5 files, 2 unstaged |

One line — but the user already understands **meaning**, not only fact.

---

## 12. Human-Agent Resource Management (CRM → HARM)

### Analogy

Aviation Crew Resource Management splits roles:

- **Pilot Flying (PF)** — flies the aircraft.
- **Pilot Monitoring (PM)** — watches parameters, intervenes on deviation.

### Mapping to Safety Levels

| Safety Level | Human role | Agent role | Aviation analog |
|-------------|---------------|-------------|-------------------|
| **L1 Read-only** | PF (full control) | PM (observes, hints) | Manual flight, copilot monitoring |
| **L2 Confirm edits** | PM (monitors, confirms) | PF (proposes, waits OK) | Autopilot engaged, pilot confirms mode changes |
| **L3 Autonomous** | PM (monitors, intervenes on issue) | PF (acts alone) | Full autopilot, pilot ready to intervene |

### PFD role indication

**PFD** should **show** current role split. Candidate: compact line in PFD context or Safety Level badge:

- L1: `Human: editing · Agent: advising`
- L2: `Agent: proposing · Human: confirming`
- L3: `Agent: acting · Human: monitoring`

This makes implicit “who is flying” explicit and readable in peripheral vision.

---

## 13. Multi-window and multi-monitor

Physical separation strengthens the metaphor: **forward, PFD, and MFD** need not live in one column of one window when screens exist; zone semantics stay.

### Typical scenario (three monitors)

| Screen | Semantic zone | Content (example) |
|------|---------------------|---------------------|
| **Left** | PFD | Solution explorer, Problems, task context, compact indicators — “corner of the eye” |
| **Center** | Forward | Editor + HUD |
| **Right** | MFD | Git, full chat/trace, docs, terminal — switchable modes per preset |

**Left/right** order can change per preset — important **not to mix roles**: forward stays editor, PFD flight context, MFD secondary.

On **one monitor** the same three semantics fold into **three regions** of one desktop; on **small diagonal** — stack, tabs, collapse **without** moving tools between PFD and MFD at runtime. Multi-window ([ADR 0017](0017-multi-window-workspace-and-agent-surfaces.md)) can put e.g. all of MFD on a second display.

---

## 14. Command Palette

Command Palette fits the three zones (details: [`command-palette-ux-concept-v1.md`](../ui-ux/command-palette-ux-concept-v1.md)):

- **Hands on keyboard** — typical actions without aiming at panel buttons.
- Palette is a **temporary layer over the desktop**: it does not replace explorer (PFD) or chat (MFD), but fast command access.
- **PFD** shows *context and state*, **forward** — *work object*, palette — *action without moving mouse focus*.

**Anti-pattern:** navigation noise in the palette (breadcrumbs, long file context) that belongs in **PFD**. Show in palette only for explicit scenarios.

“Everything important from the palette” reduces pressure to keep toolbars always visible and fits the PFD primary widget limit.

---

## 15. External agent and separate applications

Real environments often include more than Cascade: external agent, Cursor, another IDE — on a separate monitor.

### Principles

1. **Separate “crew displays”.** Cascade owns editing and the built-in loop. External chat/agent — separate MFD screen, not a mandatory panel inside Cascade.
2. **Do not compete for one attention band.** If Cursor is on the right monitor, Cascade’s right MFD can stay narrow or for docs — do not duplicate “second chat”.
3. **Bridge without capturing forward.** Short signals from external agent (confirmation, build failure) — candidates for Cascade **PFD or EICAS** per preset; full dialog — in the external tool (MFD there).
4. **Command Palette** may include “open external session”, “paste from clipboard” — external tool reachable from keyboard.

**Technical link:** Agent Client Protocol — [`note-acp-cascade-cursor-v1.md`](../ui-ux/note-acp-cascade-cursor-v1.md).

---

## 16. Risks and limitations

| Risk | Mitigation |
|------|------------|
| Inflate PFD to “everything at once” — lose metaphor | Explicit limit on visible PFD context items (**5–7±2**); scan pattern (§7); roles fixed by preset, not drag-and-drop |
| Drag panels between PFD and MFD in session | Forbidden by model: preset / config override only |
| Dark Cockpit → user forgets panel | CAS list with escalation reminds; Alertness check (§8.3) |
| Mode confusion on frequent switch | Peripheral cue + transition handoff + mode badge |
| EICAS becomes “another panel” | Shown **only with active alerts**; invisible when nominal (Dark Cockpit) |
| HUD overloads editor | HUD lighter than main text; disable without losing safety |
| Voice alerts without discipline | Worse DX than silence: only §5 (opt-in, Warning, short phrases); not “sound on every event” |

### Not v1 goals

- Pixel-lock every panel — via UX review.
- Full automatic CAS escalation — manual level confirmation first.
- Bind presets to monitor config (display name/id) — open for [ADR 0017](0017-multi-window-workspace-and-agent-surfaces.md).
- **GPWS-like voice alerts** — see §5; not v1 until minimal visual EICAS and separate opt-in/gating decision.

---

## 17. Next steps

1. ~~Bind panels to zones in TOML~~ — done: `workspace.toml` → `[attention_zone_panels]`, `AttentionZonePanelRuntime`; next — **use map in UI** (highlight, IDE Health, dock limits) as controls mature, with **layout bound to spatial anchors**, not only metadata ([§“Anchors and attention flow”](#anchors-vs-attention-flow)).
2. Agree short **zone invariants** list (3–5 items) for Focus and Power separately.
3. Define **minimal v1 CAS** (EICAS): list format, layout on one small screen vs multi-monitor, which sources.
4. Prototype **Dark Cockpit transition**: nominal badge → Warning (color + animation).
5. Fix **scan pattern** (§7) as checklist for new UI review.
6. When implementing L3 Autonomous — **Alertness check** (§8.3): threshold, downgrade policy, UI.
7. Update [`concept-to-implementation-map-v1.md`](../ui-ux/concept-to-implementation-map-v1.md) with “forward / PFD / MFD / EICAS / HUD” column for key controls.
8. Optional: window layout presets for two/three monitors (§13) — not required v1 product, but target scenario.
9. **Zone routing** policy for multi-window — separate UX or continuation of [ADR 0017](0017-multi-window-workspace-and-agent-surfaces.md).
<a id="adr0021-s17-p10"></a>
10. Fix **numeric budgets** §18 by profiling on reference hardware; until then §18 is checklist without mandatory SLA.
11. Implement **workspace repository merge** with bundle for `[attention_zone_panels]` (and chrome metrics if needed): file path, load moment on solution open, priorities §2.1.
12. **Onboarding** content per §19: short video scenarios (zones, modes, agent), unified “Why / What for / What I get” frame in hints and help; growing ideas — [`onboarding-first-run-v1.md`](../../design/onboarding-first-run-v1.md).
13. After stable EICAS: evaluate **voice layer** §5 (needed?, opt-in, Warning only, office/accessibility) — separate from “atmosphere”.
14. **Skin bundles** — deferred; idea and boundaries vs themes/presets: [ADR 0024](0024-ui-skin-bundles-deferred.md).

### Zone layout editor (visual, FancyZones-inspired)

External UX reference: [FancyZones](https://learn.microsoft.com/en-us/windows/powertoys/fancyzones) (PowerToys) — custom zone layouts on the desktop and snapping windows to zones.

**In Cascade this is a different layer:** not replacing the OS window manager, but **preset layout** for the IDE (column ratios, chrome visibility, IDE Health strip vs page, etc. — [0010](0010-ui-modes-toml-configuration.md), blueprint [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md)). ADR invariant: **do not** drag PFD/MFD/EICAS semantics between zones in normal session — only preset / config change (§16).

| Level | Content |
|---------|------------|
| **MVP** | Preset preview, numeric shares / limited templates, edit via TOML with validation |
| **Extension** | Simplified grid editor in separate “preset setup” mode (not in edit flow) |
| **Not default goal** | Full FancyZones parity (arbitrary grid, multi-zone drag, hotkeys for all monitors) — high cost and risk duplicating OS role and [ADR 0017](0017-multi-window-workspace-and-agent-surfaces.md) |

**Status:** principles fixed; detailed UI spec in `docs/design/` when implementation appears. Separate ADR not needed until zone invariants change.

---

## 18. DX acceptance: measurable criteria

The attention model (§1–§17) sets a **qualitative** bar. For release and regressions we need a **bridge to numbers**: what is “good enough” DX beyond “feels fine”. Thresholds below are **draft**; final values after profiling and user runs (see [§17 item 10](#adr0021-s17-p10)).

### 18.1 Editor core (orthogonal to zones, blocks all DX)

| Metric | Target (draft) | How to measure |
|--------|-----------------|------------|
| Input latency to character on screen in forward | p95 ≤ **16 ms** on reference machine | UI thread / frame timing instrumentation |
| Time to first Roslyn diagnostic on saved file | p95 ≤ **500 ms** (order of magnitude; tune per project) | Analyzer log / test solution |
| Cold start to editable open document | threshold **TBD** (seconds) | Stopwatch launch → editable |

If core is slow, cockpit and zones cannot compensate — **blocking** for “ready for broad use”.

### 18.2 Time to meaningful action (onboarding / flow)

| Metric | Target (draft) | How to measure |
|--------|-----------------|------------|
| **Time to first edit** — IDE start to first saved character in project file | **TBD** (e.g. ≤ 60 s for experienced user with template) | Test scenario / optional product metrics |
| **Time to first build feedback** — “run build” to first message in output/EICAS | **TBD** | Manual scenario + log |
| **Time to first safe agent action** — enable agent to first **confirmed** safe step (L2) or conscious L3 with visible badge | **TBD** | Scenario + §8 checklist |

### 18.3 Attention model (qualitative + regression control)

Axis “**where** on screen (anchors)” vs “**how** attention behaves (flow, modes, EICAS)” — [§“Anchors and attention flow”](#anchors-vs-attention-flow); do not mix in review.

| Criterion | Check |
|----------|----------|
| **Scan** | New UI element has zone per preset (§7); no mandatory constant gaze past forward+PFD without conscious MFD. |
| **Zone roles** | No panel move between PFD and MFD without config change (§2, §16). |
| **EICAS on critical path** | Agent/build **Warning** noticeable without tab hunting (§5); small screen — explicit UX review scenario. |
| **Dark Cockpit** | No extra color accents when nominal; noticeable transition on deviation (§6). |

Suitable for **UX review and QA checklist**, not mandatory CI in v1.

### 18.4 Trust and agent autonomy

| Criterion | Check |
|----------|----------|
| Current **safety level (L1–L3)** distinguishable without opening MFD | Peripheral cue / badge (§8). |
| L2→L3 (or Focus→Power when autonomy rises) gives **brief handoff** | Not silent (§8.2). |
| Degradation (MCP offline, etc.) | PFD/EICAS not empty and not endless spinner (§10). |

### 18.5 Discoverability (secondary layer)

| Criterion | Check |
|----------|----------|
| Actions without mouse | Critical commands from Command Palette or hotkeys ([ADR 0013](0013-command-surface-and-discoverability.md)). |
| Navigation noise not in palette | As §14. |

### 18.6 How to use

- **Before release:** walk §18.3–§18.5 checklist; §18.1–§18.2 measure if possible, else record “not measured”.
- **Dispute “is DX ideal”:** core first (§18.1), then attention and agent (§18.3–§18.4); §18.2 and discoverability enhance but do not replace core.

---

## 19. Onboarding: reducing model tax and audience dialogue

**Living blueprint** (First Run ideas, flag vs UI mode, PFD + checklist, palette): [`onboarding-first-run-v1.md`](../../design/onboarding-first-run-v1.md).

Rich attention model (zones, modes, EICAS, agent levels) adds **onboarding tax**: time and attention to learn. Reduce it with **short, task-oriented explanations** in the product, not long ADR text.

### 19.1 Short videos (ClickUp-style orientation)

- **Format:** a few minutes each, one scenario / one idea (“where context lives”, “what is in MFD”, “how to switch mode”, “what EICAS warning means”).
- **Goal:** show **how** to use the cockpit without reading the spec; duplicate links from first run, empty states, help.
- **Does not replace** power-user docs or cancel §18 metrics — complements **entry**.

### 19.2 Required frame for any “in your face” explanation

Without answers to three questions you **cannot dialogue** with the audience — the user stays in “terms were forced on me”. Any noticeable onboarding (video, first modal, help block, zone hint) must close explicitly or briefly:

| Question | Meaning |
|--------|--------|
| **Why?** | What problem this UI part solves (overload, lost context, unsafe agent, etc.). |
| **What for?** | Its role in the **overall** Cascade model (attention zone, alert channel, noise-limiting mode). |
| **What do I get?** | Concrete win: fewer switches, faster see error, clearer agent boundary — not vague “more convenient”. |

Review copy and video scripts: **if the third column is empty or generic** — rework; otherwise onboarding does not do its job.

### 19.3 Link to §18

Time-to-first-edit and time-to-first-safe-agent-action (§18.2) improve when the user **need not** learn avionics first: video + “Why / What for / What I get” frame is product, not “docs later”.

---

## Rejected alternatives

- **Keep PFD/MFD as informal concept without ADR** — rejected: attention model affects every layout decision; ADR makes principles mandatory in UI review.
- **Separate ADR per aspect (EICAS, Dark Cockpit, Scan Pattern, etc.)** — rejected: all are parts of one attention model; splitting loses coherence.
- **DO-178C certification baseline and literal ARINC 661** — out of scope. **Portable 661 ideas** (compositor, zone contracts, explicit hierarchy) — fixed in [context](#arinc-661-borrow) above; cockpit metaphor guides UX, not avionics compliance.

---

## Status after acceptance

Status **Accepted**; [`concept-pfd-mfd-cascade-v1.md`](../ui-ux/concept-pfd-mfd-cascade-v1.md) is marked “Superseded by ADR 0021”. Layout changes require review per scan pattern (§7) and PFD limit (§16).

---

## Change history

<a id="adr0021-history"></a>

| Date | Change |
|------|-----------|
| — | §1 terms table: **PIC** (operator metaphor; anchor `#glossary-kvs`); link [0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md). |
| — | §1.1 **ProcessHost** (MCP/ACP) vs UI; channel ≠ `*Host*` View. |
| — | §1.1 glossary channel / presentation / code names; §5: EICAS as channel (not strip synonym / not third column); “Placement” clarified. |
| — | §1.2 presentation slot vs channel; `DedicatedPage` / `ide_health_surface` (IDE Health only). Anchor: `#glossary-presentation-vs-channel`. |
| — | §17 FancyZones-style layout editor; portable ARINC 661 ideas; §5 Escalation; voice; §2.1; §19; §16/§17; three zones, HUD, §18 DX acceptance. |
| — | §19: link living blueprint [`onboarding-first-run-v1.md`](../../design/onboarding-first-run-v1.md). |
| — | §ARINC 661: **CDS** (Cockpit Display System) spelled out. |
| — | §“Zone architecture”: PFD/MFD anchor content vs IDE Health Page term. Earlier under §“Zone architecture”: **anchors vs attention flow** (do not mix). |
| — | “operational telemetry” / “work telemetry” wording. |
| 2026-04-06 | §“Plugins and attention model”: mandatory extension binding to zone/channel/preset. |
| 2026-04-11 | Text uses **Workspace Health**; **canonical now** — **IDE Health** ([0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md)); Russian copy **состояние IDE**, avoid confusion with workspace folder on disk. |
| 2026-04-12 | §1.1: **CDS (cockpit contract)** vs **`UiLayoutSnapshot`**; living blueprint [`cds-contract-v0.md`](../../design/cds-contract-v0.md). |
| 2026-04-12 | §Context: **“Cognitive load and neurodivergence”** — product link (not medicine, not new §1–§18 invariants); public author site sketch. |
| 2026-04-15 | After zone table: link [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md#adr0037-zone-vs-surface) (PFD geography vs strict surface by marker). |
