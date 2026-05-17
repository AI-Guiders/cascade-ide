<!-- English translation of adr/0061-context-aware-adr-map-pfd-knowledge-indicator.md. Canonical Russian: ../../adr/0061-context-aware-adr-map-pfd-knowledge-indicator.md -->

# ADR 0061: Context-aware ADR map for code paths (GPWS for documentation) and PFD indicator

**Status:** Proposed (implementation deferred)  
**Date:** 2026-04-17

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD as attention zone |
| [0039](0039-workspace-navigation-affordances.md) | navigation, semantic map |
| [0053](0053-semantic-map-control-flow-pfd.md) | map and cursor in method |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | user settings — `settings.toml`, `%LocalAppData%\CascadeIDE\`, secrets separate |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | declarative maps in TOML |

## Summary

- Context map **ADR ↔ paths** in `workspace.toml`; indicator on PFD.
- Advisory for the agent (“GPWS for docs”); does not replace reading ADRs.

---

## Context

ADRs in `docs/adr/` are the normative layer, but when working in a specific file it is easy **not to know** which decisions apply to that area until you walk the index. Documentation risks becoming a “text graveyard”: formally present, effectively absent from working context.

The idea: treat **repository path → one or more ADRs** as a layer of **situational awareness** (analogous to **GPWS / terrain awareness** in aviation): not a mandatory full-screen document, but a **terrain signal** — “these architectural constraints apply here.”

---

## Goals

1. Fix a **declarative map** “source tree region → ADR” in workspace configuration (TOML), versioned with the repository.
2. Describe **minimal Flight UI**: unobtrusive indicator on PFD / knowledge zone and brief **intent** (ADR purpose) without mandatory file open.
3. Lay groundwork for **assistant/agent**: when changing contracts in an ADR-covered zone — soft warning in log/trace (“deviating from ADR-NNN”), not unexplained denial.

## Non-goals (first phase)

- Full automatic verification of code against ADR text (static “ADR vs implementation”) — separate line of work.
- Replacing [README](README.md) index or mandatory long ADR viewing on every save.
- Hard block on edits for “ADR violation” without human involvement.

---

## Proposed solution (draft)

### 1. Declarative map in TOML

Section in **`workspace.toml`** (or agreed name, e.g. `[workspace.adr.map]`) mapping **path prefix** (relative to repo root) to **ADR file list** or identifiers:

```toml
[workspace.adr.map]
"src/engine/mcp" = "docs/adr/0008-mcp-contracts-and-testable-infrastructure.md"
"src/ui/skia" = [
  "docs/adr/0055-skia-instrument-composition-pipeline.md",
  "docs/adr/0049-skia-surface-rollout-over-avalonia-host.md",
]
"*" = "docs/adr/0006-presentation-layers-and-feature-slices.md"   # optional global fallback
```

**Algorithm requirements (to refine on acceptance):** priority of **longest matching prefix**, then `*` fallback; consistent path separators on Windows/Linux.

### 2. Flight visualization (PFD / EFD)

- **Knowledge indicator:** when current file (or symbol under cursor) matches mapping, show a **light** sign on PFD (e.g. stylized “i” / “A” — UX details separate).
- **HUD / tooltip:** on hover or short delay — **brief intent** (ADR title + 1–2 lines from “Decision” or front-matter), without opening Markdown in editor by default.

Implementation builds on semantic map / cursor contour and Skia pipeline ([0053](0053-semantic-map-control-flow-pfd.md), [0055](0055-skia-instrument-composition-pipeline.md)), not a second source of truth for “where am I in code.”

### 3. Agent role

With mapping, the agent can:

- highlight **ADR link** in trace when changes touch a mapped zone;
- phrase warnings as **advisory** (“course deviates from ADR-NNN — confirm intent”), not build errors.

First-phase “violation” criteria are **heuristic** (public API change, key names from ADR) to avoid noise.

---

## Open questions

- TOML section name and deserialization in `CascadeIdeSettings` / workspace-only model.
- **Brief intent** source: first paragraph after title, YAML front-matter, separate ADR field — needs agreement.
- Interaction with **multi-root** solutions and out-of-repo paths.

---

## MVP (“ready to start”)

1. Parse map from TOML + resolve “current file → ADR list”.
2. One indicator kind on PFD and tooltip text from selected ADR (no full documentation editor).
3. Tests for path matching algorithm and regression on path normalization.

---

## Consequences

- **Plus:** ADRs harder to ignore in daily work; map lives next to code in git.
- **Minus:** maintaining mapping on large directory moves — review duty (possibly path migration script).

---

## Implementation status

**Not started.** On accepting direction — update header (**Accepted** / **Accepted (direction)**), then update per [status-lifecycle.md](status-lifecycle.md) when delivered.
