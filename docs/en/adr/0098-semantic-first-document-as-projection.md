<!-- English translation of adr/0098-semantic-first-document-as-projection.md. Canonical Russian: ../../adr/0098-semantic-first-document-as-projection.md -->

# ADR 0098: Semantics first; document and repository as projections (Semantic-First)

**Status:** Proposed  
**Date:** 2026-04-24

## Related ADRs

| ADR | Role |
|-----|------|
| [0039](0039-workspace-navigation-affordances.md) | navigation, **semantic map**, MCP subgraph |
| [0065](0065-instrument-categories-domain-taxonomy.md) | instrument categories and graph types (orthogonal to slot and `instrument_id`) |
| [0053](0053-semantic-map-control-flow-pfd.md) | intent map and control flow on PFD |
| [0056](0056-semantic-map-pipeline-adoption.md) | intent map as product graph |
| [0067](0067-graph-backed-surfaces-contract.md) | graph-backed surfaces |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | CDS, cockpit channel |
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | delivery bus |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | CCU — collapse to channel DTO |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | payload vs presentation projection |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | editor text — source of truth **for edit session**; see [§2.4](#adr0098-alignment-0084) |
| [0095](0095-workspace-solution-ide-health-stratification.md) | three Health levels, `stratum` |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | events + projections |
| [0009](0009-strangler-migration-and-exceptions.md) | strangler |

## Summary

- **Semantic-first:** meaning map is primary; code/docs/git are projections.
- Aligned with edit session ([0084](0084-agent-edits-editor-source-of-truth-presence-channel.md)).

---

<a id="adr0098-context"></a>

## 1. Context

In classic “IDE as file editor” **buffer text and repo tree are primary**: architecture, dependencies, and intent are **derived from** artifacts (C#, csproj, ADR, configs). Semantics is derivative, often out of sync with what the human *meant*.

Cascade already has **meaning-oriented** pieces: **intent map** ([0053](0053-semantic-map-control-flow-pfd.md), [0056](0056-semantic-map-pipeline-adoption.md)), **graph-backed** contract ([0067](0067-graph-backed-surfaces-contract.md)), navigation emphasizing graph and MCP ([0039](0039-workspace-navigation-affordances.md)). Bus, Health, and CCU ([0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md), [0095](0095-workspace-solution-ide-health-stratification.md), [0097](0097-cockpit-compute-units-transport-to-channel-dto.md)) operate on **normalized meaning** after delivery, not raw “unaddressed stream.”

This ADR states **north star**: move away from purely **document-centric** model toward **semantic-first** — without requiring immediate full product rewrite.

---

<a id="adr0098-decision"></a>

## 2. Decision (invariants)

<a id="adr0098-semantic-map"></a>

### 2.1 Semantic map is primary

- **Meaning model** (intent, boundaries, relations, states, suitable for attention routing and instruments) is **primary** system design layer.
- **Source code**, **text documents** (ADR, TOML, Markdown), and **git artifacts** are **projections and packaging**: deterministic or semi-deterministic **representations** versionable, diffable, feedable to LSP, CI, and agent.

<a id="adr0098-cds-ids-instruments"></a>

### 2.2 Cockpit channel, IDS, vector/graph instruments

- **CDS channel** ([0036](0036-cds-channel-compositor-surface-pipeline.md)), **CCU** ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)), **IDS** ([0079](0079-ide-display-system-ids-overlay-pipeline.md)), instruments and deck rest on **agreed meaning** (DTO, snapshots, `stratum`, etc.), not “whatever one-file parser guessed” as sole source.
- **Forward** (code editor) stays **powerful input channel** into this map, but **not** absolute long-term truth of the whole system.
- For **semantic map**, CCU is **input snapshot** layer (source normalization, version/freshness, derived fields), not graph UX home. Traversal, layout, selection, interaction stay in graph-backed surface ([0067](0067-graph-backed-surfaces-contract.md), [0097 §6 — CCU candidates](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-candidates-next)).

<a id="adr0098-coexistence"></a>

### 2.3 Coexistence: two truths where strangler needs it

- Transitional **two-layer** scenarios allowed: “truth in git for release” + “truth in map for cockpit/agent”, with explicit **sync** and conflict priority. Goal — **converge** to one primary semantics, not eternal split.

<a id="adr0098-alignment-0084"></a>

### 2.4 Alignment with [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md)

- [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) fixes **operational** invariant: during joint work **one buffer text** is canon **for applied edit** (human/agent parity, presence, no “second truth in chat”).
- **0098** does not cancel 0084: at edit moment **text projection** remains **input canon** for that session. Long-term **semantic map** is canon of **architecture meaning**; 0084 describes **how** to write safely into projection while round-trip **to/from** map is not one automaton.

---

<a id="adr0098-non-goals"></a>

## 3. Non-goals (explicit)

- **Not** “throw away git”, **not** “no diffs”, **not** “everything in one DB without files.”
- **Not** require full **semantic ↔ repo round-trip** in v1 of this ADR: **direction** and **invariants**; migration — strangler ([0009](0009-strangler-migration-and-exceptions.md)).
- **Not** duplicate full **Semantic Map field ontology** here — separate ADRs/contracts as introduced.
- **Not** equate semantic-first with **up-front complete** semantic or symbolic **tree of entire solution**. **Partial** snapshots, **lazy** map growth (active document, navigation/MCP subgraph, Roslyn/LSP **on demand**) allowed; **CCU** remains **input snapshot** layer ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)), not sole store of “all truth” before graph ready.
- **Not** mix **product priority of meaning** (this ADR) with **completeness** of compiler instrument graph: Roslyn/compile fullness **pulled where needed** (analysis, navigation, refactor), not **global precondition** for every IDE function.

---

<a id="adr0098-consequences"></a>

## 4. Consequences and risks

- **Plus:** single axis for CCU, channels, agent, cockpit — **same** addressable semantics; less silent file/pixel drift.
- **Risk:** **sync** complexity between projection and map; need discipline, tools, consistency tests.
- **Determinism risk:** code/doc generation from map — reproducibility and stable ordering when needed.

---

<a id="adr0098-link-0100"></a>

## 5. Link to future ADR 0100 (hint)

- Next “center” ADR (agent subjectivity, integrated meaning environment, operator role) naturally builds on **0098** as **primacy-of-meaning north star**; 0100 need not repeat this ADR — may shift focus to **subject/ecosystem**.

---

<a id="adr0098-rejected"></a>

## 6. Rejected alternative (brief)

- **Full document-centrism** as sole truth: simpler for v0, but **does not scale** to cockpit, Health aggregates, intent graphs, coherent UX ([0063](0063-instrument-deck-named-composition-one-anchor.md)–[0068](0068-deck-row-payload-and-presentation-projection.md)) without constant “catch-up” mapping.

---

<a id="adr0098-adoption-status"></a>

## 7. Adoption status

- **Proposed** — normative **intent** and boundaries; concrete modules, map storage, timelines — follow-up ADR and roadmap.

---

<a id="adr0098-faq"></a>

## 8. FAQ

**With semantic-first, must we build full semantic tree of solution up front?**  
**No** ([§3 last bullets](#adr0098-non-goals)). ADR invariant is **role** of meaning layer and agreed snapshots, not mandatory **a priori completeness** of graph. Practical semantics may grow **incrementally** and **by scope** (file, project, Language Service request), alongside file projection, while strangler applies.
