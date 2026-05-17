<!-- English translation of adr/0095-workspace-solution-ide-health-stratification.md. Canonical Russian: ../../adr/0095-workspace-solution-ide-health-stratification.md -->

# ADR 0095: Three Health levels — Workspace, Solution, IDE (channel taxonomy)

**Status:** Accepted (partial: WorkspaceHealth MFD + channels; full three-level taxonomy — per ADR)  
**Date:** 2026-04-24  
**Updated:** 2026-04-25 — IDE Health TOML keys: `ide_health_*`. Details — [§ History](#adr0095-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | product name **IDE Health** and `IdeHealth*` types; **does not** remove signal mixing by meaning |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | **CCU**: where input collapse to channel DTO happens while keeping `stratum` |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | **DAL**: where external data is acquired before CCU |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | channel vs presentation slot; glossary |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | channel → CDS → compositor → surface |
| [0023](0023-environment-readiness-glance.md) | environment readiness channel (glance) — separate from IDE Health |
| [0022](0022-workspace-health-lexicon.md) | lexicon and name evolution |
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Git as cross-cutting contour |
| [0062](0062-git-submodules-semantic-map-subgraph.md) | GitMap — file/git geometry separate from solution code |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | MCP snapshots — on level split need explicit sections or schema version |

## Summary

- Three Health levels: **Workspace** · **Solution** · **IDE**; `stratum` field.
- Strangler from monolithic “workspace health”; TOML `ide_health_*` ([0010](0010-ui-modes-toml-configuration.md)).

### Outside ADR

| Document | Role |
|----------|------|
| [`environment-readiness-glance-v1.md`](../../design/environment-readiness-glance-v1.md) | **environment readiness** channel — already closer to IDE level |
| [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md) | current IDE Health strip map |

---

## Context

After [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) the channel is product-named **IDE Health**, but **by meaning** one user image (strip, cockpit, agent snapshot) still mixes signals of **different nature**:

1. **File and VCS plane** — open workspace root, git status/branch/remoteness, submodules, tree “dirt”; part of this **persists** when switching solution inside same directory and is **not** compiler diagnostics.
2. **Solution / project life** — **MSBuild** result, test counters/status, compile errors, Roslyn diagnostics **bound to open solution**; changes with `.sln` / configuration / target framework.
3. **IDE process and environment** — LSP (C#, Markdown, …), MCP transports, selected AI mode, `dotnet`/tool availability from [0023](0023-environment-readiness-glance.md); stays **true** if solution closed but IDE not reinstalled.

Mixing levels hurts:

- **agent and MCP** — one JSON easily mistaken for “folder state” vs “build state” or vice versa;
- **CDS and future channels** — one “health” without level tag scales poorly ([0036](0036-cds-channel-compositor-surface-pipeline.md));
- **UX** — operator needs different **attention contexts** ([0021](0021-pfd-mfd-cockpit-attention-model.md)): “what’s on disk”, “what’s with build”, “what’s with IDE host”.

This ADR **introduces normative taxonomy of three levels** and direction to **split channels / data sections**; it **does not** require rewriting all `IdeHealth*` in one commit (UiModes wire keys for contour — **`ide_health_*`**, [0010](0010-ui-modes-toml-configuration.md)).

---

<a id="adr0095-three-levels"></a>

## Decision: three levels (semantics)

Fix **three meaning axes** for Health and **similar** observability channels:

| Level | Working name | Question “what is it about?” | Typical signals (examples, not exhaustive) |
|-------|--------------|------------------------------|--------------------------------------------|
| **A** | **Workspace** | Workspace directory(ies) on disk and **VCS** around them | root path, git branch/ahead/behind, working tree cleanliness, submodules ([0062](0062-git-submodules-semantic-map-subgraph.md)), merge conflicts |
| **B** | **Solution** (*Solution / project*) | **Open solution and build/test artifacts** | build status/text, tests (pass/fail), compile errors, target TFM/configuration, startup project if needed |
| **C** | **IDE** | **IDE process, hosts, environment**, not source tree | LSP alive, MCP connections, AI mode, PATH/EXE checks from [0023](0023-environment-readiness-glance.md), internal services |

**Invariant:** any new “health” or observability channel **declares** level A/B/C; **forbidden** to silently mix levels in one DTO without explicit structure or discriminator.

**Machine contract field name** (JSON, MCP, CDS): recommend **`stratum`** with values `workspace` | `solution` | `ide` **plus** signal source discriminator (e.g. **`diagnostic_source`** for Roslyn branch). Do **not** use **`level`** in contracts — often read as priority, stack depth, or severity.

**Presentation (UI):** three levels **do not** automatically mean **three separate strips**. Allowed: one **composite** strip with **internal** grouping; several instruments in one anchor ([0063](0063-instrument-deck-named-composition-one-anchor.md)); separate MFD pages. Norm is **data semantics**, not v1 layout.

---

## Implementation direction (strangler)

1. **CDS / snapshots:** when cockpit contract evolves ([`cds-contract-v0.md`](../../design/cds-contract-v0.md)) — sections or subchannels with **`stratum`** or **separate** explicit channel names; detailed JSON **not** fixed by this ADR.
2. **MCP / `get_ide_state`:** on response extension — **do not** add fields “into one pile”; new blocks must carry **`stratum`** (and source if needed) or separate tools if mixing keeps confusion ([0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md), [0052](0052-agent-contract-cli-and-snapshot-tests.md)).
3. **Current IDE Health code:** gradually split input snapshot build ([`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md)) into A/B/C providers with **one** UI composition point; UiModes wire keys — **`ide_health_*`** ([0010](0010-ui-modes-toml-configuration.md)). Architectural “collapse” role — [0097](0097-cockpit-compute-units-transport-to-channel-dto.md).

<a id="adr0095-stratum-ccu-examples"></a>

### Example: separate computers per stratum (not God-object)

One **God-object** pulling Git, MSBuild, and LSP in one method is **not** target. Normative direction — **several [cockpit compute units](0097-cockpit-compute-units-transport-to-channel-dto.md) (CCU)** with narrow in/out contracts and **one** composition point for channel/UI. Below — **working engineering short names** (discussion, review, comments); **not** requirement to name C# types so in next commit.

| Level | Working name (EN) | Short | Typical collapse meaning |
|-------|-------------------|-------|--------------------------|
| **A** | Workspace Status Computation Unit | **WSCU** | workspace dirs, VCS, submodules, tree dirt |
| **B** | Solution Status Computation Unit | **SSCU** | open solution, build, tests, TFM/config |
| **C** | IDE Status Computation Unit | **ISCU** | IDE process, LSP, MCP, environment ([0023](0023-environment-readiness-glance.md)) |

**ISCU** — level **C** and *IDE host status*; do **not** confuse with **IDS** ([0079](0079-ide-display-system-ids-overlay-pipeline.md) — *Ide Display System*, overlays). When writing “ISCU in same sentence as IDS”, spell **ISCU** fully or disambiguate abbreviations.

---

## Boundaries and open questions

**Boundaries (not goals of this ADR):**

- Concrete type names (`IWorkspaceHealthChannel` vs three interfaces), further DTO schema evolution, immediate AXAML split into three controls.
- **Debug** as separate **product** contour ([0002](0002-debug-human-agent-parity.md)) — orthogonal; B/C link (solution session vs IDE process) refined when modeling, **without** replacing full debug ADR.

**Open questions:**

- **Git** spans A (branch, ahead/behind) and may reflect build in mixed scenarios — need UI priority and duplicate rules.
- **Roslyn:** part diagnostics — solution (B), part — analyzers/environment (C); see [clarification below](#adr0095-roslyn-b-vs-c).
- **Multiple disk roots** — how to map level A when one window is not one “folder on disk” meaning; see below.

<a id="adr0095-roslyn-b-vs-c"></a>

### Clarification: Roslyn — B vs C and unified UI

**Unified UI** for user (one strip, one diagnostic list) **does not contradict** “Presentation (UI)” in [level table](#adr0095-three-levels): A/B/C are **data semantics**, not obligation for three visual strips.

**For implementation** B/C split by Roslyn is **often extractable** from model (compiler vs analyzer id, project, rules) but **not always semantically obvious**: same UI error may reflect code and environment (package restore, TFM, SDK); solution-package analyzers are formally B but feel like “policy/setup”; analyzer load failure is clearly C.

**For user** boundary is **cognitively** often unclear: everything merges into “IDE complains”, risk of **wrong attribution** (fix code vs environment). So snapshots for CDS/MCP/agent should **keep** **`stratum`** and source (e.g. **`diagnostic_source`**), even if UI is **merged**; optional light UI markers (environment vs code group) or “details” expand.

<a id="adr0095-umbrella-vs-multiroot"></a>

### Clarification: “umbrella” solution and true multi-root

**One** `.sln` / `.slnx` entry point listing projects with **relative** paths **outside** solution file directory is **normal for level B**: build/test graph is defined by **solution manifest** whole, regardless of where sources sit on disk.

For **level A** same scenario **does not** replace window multi-root: question reduces to **git geometry** — one repo covering all paths (then **one** A: one worktree and one dirt picture), or **several** nested/sibling repos (then **multiple A sources** and explicit policy: session primary root, secondaries, aggregate **without** silent merge into one field).

**Multi-root window** meaning “several independent roots **without** single solution file as entry” — **different** product mode; open for UX and which directory is sole “root” for A snapshot, but **not mixed** in this ADR with umbrella solution above.

---

## Rejected alternatives

- **Keep one “IDE Health” channel without internal taxonomy** — rejected as tech debt for CDS, MCP, new channels.
- **Rename levels to “Workspace Health / Solution Health / IDE Health” as three products** — rejected: word *workspace* in user name again confuses with directory ([0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md)); product strings prefer “folder and Git”, “build and tests”, “IDE environment” while keeping **A/B/C** as **internal** ids and contract keys.

---

## Consequences

- New features and ADRs touching observability **reference** this ADR and state level A/B/C (or conscious violation with rationale).
- `IdeHealth*` refactor becomes **planned** with per-level tracking, not cosmetic rename only.

---

## Change history

<a id="adr0095-history"></a>

| Date | Change |
|------|--------|
| 2026-04-25 | TOML refs: IDE Health keys in `UiModes` — **`ide_health_*`** ([0010](0010-ui-modes-toml-configuration.md)); migration wording for `workspace_health_*` aligned with repo. |
