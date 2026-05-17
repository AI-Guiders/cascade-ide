<!-- English translation of adr/0112-command-palette-query-modes-strategy.md. Canonical Russian: ../../adr/0112-command-palette-query-modes-strategy.md -->

# ADR 0112: Command palette query modes (`f:` / `t:` / `m:` / `x:` / `c:`) — mode model, strategies, and workspace search **backends**

**Status:** Accepted · Implemented  
**Date:** 2026-05-11 · updated 2026-05-12

## Related ADRs

| ADR | Role |
|-----|------|
| [0013](0013-command-surface-and-discoverability.md) | palette, keyboard-first |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `IdeCommands`, palette catalog |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Command Melody `c:` and CascadeChord — orthogonal to palette **line** prefixes |
| [0070](0070-command-palette-direct-overlay-surface.md) | palette as overlay |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS, overlay hints |
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | parametric tail after alias in `c:` |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | melody catalog |
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | Hybrid Codebase Index (core + MCP) for C# stacks with Roslyn truth |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | HCI integration in CascadeIDE |

### Outside ADR

| Document | Role |
|----------|------|
| [intent-melody-language-v1.md](../../intent-melody-language-v1.md) | Intent Melody Language v1 |

**Code touchpoints (actual baseline):** `IdeCommandPaletteFilterOrchestrator`, `IntentMelodyAliases.TryGetTail`, `GoToAllQueryParser`, `CommandPaletteChromeProjection`, `IdeCommandPaletteExecutionOrchestrator`.

### Implementation snapshot

| Item | Value |
|------|-------|
| — | stages 1–4: mode parse model, chrome, rg/hci/auto backends, TOML |
| — | per-mode strategies — see `CommandPaletteParsedQueryParser` and orchestrator branches |

## Summary

- Palette (**Ctrl+Q**): query line modes (`t:`/`m:`/`x:`), backend strategies.
- Workspace search contract and switching in `settings.toml`.
- Direct overlay surface — [0070](0070-command-palette-direct-overlay-surface.md).

---

## Context

Palette hotkey (default **Ctrl+Q**) opens **one query line** and **one result list**, but input semantics are **not uniform**:

| Prefix | Working name | Query meaning | Where defined today |
|--------|--------------|---------------|---------------------|
| *(none)* | command catalog | fuzzy on title/`command_id` | orchestrator default branch |
| `f:` | go to file | filter solution files | `GoToAllQueryParser` + `RefreshGoToPaletteFilter` |
| `t:` | go to type | search types in `.cs` | same |
| `m:` | go to member | heuristic member search in `.cs` | same |
| `x:` | text | ripgrep over workspace | same |
| `c:` | Command Melody | alias → `command_id`, parametric `:start:end` | `IntentMelodyAliases.TryGetTail` + `RefreshMelodyPaletteFilter` |

Footer/placeholder hints duplicate this set in text (**`CommandPaletteChromeProjection`**), while **mode branching** sits in **`if` chain at start of `RefreshCommandPaletteFilter`**: first `c:`, then `f|t|m|x`, else catalog.

**Problem:** mode is an **unnamed concept in code**; check order, async go-to cancel (`goToHandle.Cancel()`), row result types, and UI quirks (`IdeCommandPaletteRowViewModel`: melody vs catalog vs go-to) are spread without one “palette mode” contract.

Risks on growth: new prefix or priority change (e.g. future `w:`) touches many places; mode tests are unstructured.

---

## Decision

Introduce two aligned layers:

1. **Query line mode** (what user typed) — **`CommandPaletteQueryMode`** / **`CommandPaletteParsedQuery`** (§1–2).
2. **Workspace search backend** for `t:` / `m:` / `x:` submodes — **separate contract** with pluggable implementations and switch config (§7). Not a UI mode: same prefix can use different engines without changing palette syntax.

Mode handling as **strategy per mode** (Strategy pattern / small registry — implementation detail not fixed here). Go-to strategy for `t:`/`m:`/`x:` **delegates** to chosen backend (or fallback chain), not direct `rg` calls from orchestrator.

<a id="adr0112-p1"></a>

### 1. Model

- **`CommandPaletteParsedQuery`** (or equivalent): discriminated union:
  - **Melody** — normalized tail after `c:` (as `TryGetTail` today);
  - **GoTo** — `GoToAllQuery` (prefix `f` | `t` | `m` | `x` + term);
  - **Catalog** — string without reserved mode prefix (trim + fuzzy on catalog).

Document **parse priority** (compatibility with current behavior):

1. `c:` — first (so `c:` does not collide with go-to and catalog);
2. then `f:` / `t:` / `m:` / `x:`;
3. else catalog.

<a id="adr0112-p2"></a>

### 2. Strategy per mode

Per mode — **one entry**: filter context (solution, roots, file path, editor text, hotkeys, UI mode family, …) + **action**:

- clear/fill `filteredEntries`;
- set selected index;
- call `refreshCommandPaletteSurfaceSnapshot`;
- for go-to — manage **cancellation** of background search (`CommandPaletteGoToAsyncHandle`), as today.

**Refactoring goal:** `RefreshCommandPaletteFilter` becomes **dispatcher**: `parse → lookup strategy → execute`, without duplicating “who cancels go-to” rules.

<a id="adr0112-p3"></a>

### 3. Link to execution (Enter)

**Execution** of selection is partly unified via `IdeCommandPaletteRowViewModel.RowKind` and `IdeCommandPaletteExecutionOrchestrator` (MCP command vs go-to navigation). Filter mode and RowKind must stay **aligned**; introducing strategies must not change user-facing row contract without separate ADR.

<a id="adr0112-p4"></a>

### 4. Chrome and discoverability

**`CommandPaletteChromeProjection`** texts (footer / placeholder) should **build from canonical mode list** (prefix name + short label) so new mode is added **in one data place**, not by copying `"f: file · …"`.

<a id="adr0112-p5"></a>

### 5. Testability

- Unit tests on **parse** raw string → mode + payload (edge cases: `c:` without tail, `x:` only spaces; no expected `c:` vs catalog conflict in v1).
- Where possible — tests on **strategy order** (regression: `c:something` does not fall through to catalog as plain text without product decision).

<a id="adr0112-p6"></a>

### 6. Go-to backend: ripgrep vs **HCI** (Hybrid Codebase Index)

Using **`SearchHybridAsync`** / index instead of **`RipgrepWorkspaceSearchService`** for `t:` / `m:` / `x:` is **not automatically** “faster and better” for all modes — depends on **mode goal** and **index freshness**.

| Prefix | Today (baseline) | HCI as candidate | Nuances |
|--------|------------------|------------------|---------|
| `f:` | solution tree, in-memory path filter | usually **not needed** | Bottleneck is not disk search; index little help. |
| `x:` | literal via ripgrep over workspace | **often fits**: FTS on indexed chunks | Plus: no `rg` process, predictable delay with **warm** index. Minus: until reindex — **desync** with disk; FTS semantics (tokens, AND) ≠ “like rg”; literal editor-like search may need **rg fallback**. |
| `t:` / `m:` | **regex on `.cs`** (`GoToPaletteRipgrepPatternBuilder`) | **debatable without hit model work** | HCI indexes **text fragments**, not necessarily same quality as Roslyn/VS “Go to Type/Member”. Extra/missed lines vs current regex heuristic. Target accuracy for “type/member” closer to **Roslyn** (separate track) than “just FTS”. |
| (optional) | — | HCI **`semantic`** | Good for **similar meaning**, not strict symbol-name contracts — do not replace `t:`/`m:` without explicit product mode. |

Contract details and switching — §7.

<a id="adr0112-p7"></a>

### 7. First-class workspace search **backend** (`t:` / `m:` / `x:`)

**Boundary:** “backend” applies to **async search in file contents under workspace root** for **`t:`**, **`m:`**, **`x:`**. Mode **`f:`** (path filter on solution tree) **not in this contract** — separate synchronous strategy without “rg-like” engine swap.

**Contract (working name):** abstraction like **`ICommandPaletteGoToSearchBackend`** with unified in/out:

- **In:** normalized `GoToAllQuery`, workspace root, limits (`MaxRipgrepMatches` / equivalent), index scope (aligned with [HCI scope](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)), `CancellationToken`.
- **Out:** ordered list of **canonical palette hits** (path, line, column, short label/category — compatible with `IdeCommandPaletteRowViewModel` / existing `CommandPaletteGoTo*NavRowsProjection`).

**Implementations (minimum set):**

| Implementation | Role |
|----------------|------|
| **Ripgrep** | Current behavior: `GoToPaletteRipgrepPatternBuilder` + `RipgrepWorkspaceSearchService`. |
| **HCI-FTS** | Query Hybrid Index (semantic optional), map hit → same DTO. |
| **Composite / Auto** | Chain: e.g. HCI → on empty, error, or “index not ready” — **fallback** to Ripgrep. Policy explicit, not hidden in orchestrator. |

**Switch configuration** — only in user **`settings.toml`** (`%LocalAppData%\CascadeIDE\`, [0028](0028-user-settings-toml-localappdata-and-secrets.md)): same tree as `CascadeIdeSettings` / `SettingsService.Load`, **not** repo `.cascade/workspace.toml` (`UiWorkspaceToml` — zones, navigation presets, etc.). “How palette searches” is **workstation preference**, not per-repo artifact.

**Schema placement (aligned with existing keys):**

| Option | Verdict |
|--------|---------|
| **`[hybrid_index]`** | **Do not** use for `backend`: with value **`rg`** setting is not about index; mixes HCI enablement and independent palette engine choice. `auto` ↔ HCI link — only in facade **code**. |
| **`[workspace]`** | **Do not** use: in code `WorkspaceSettings` — panels, `Flight`, splitters; unrelated to search backend. |
| **`[command_palette…]`** | **Yes**: new top-level property on `CascadeIdeSettings`, nested TOML table like `[display.screens.grammar]` — snake_case keys. |

Implementation mapping (guide): `CascadeIdeSettings.CommandPalette.GoToSearch.Backend` → **`[command_palette.go_to_search]`**, field **`backend`**.

Canonical backend values:

| Value | Meaning |
|-------|---------|
| **`rg`** | **Ripgrep only** (current baseline; recommended **default** until HCI path stable). |
| **`hci`** | **Hybrid Codebase Index only** (FTS hit → palette rows). |
| **`auto`** | HCI first; on index not ready, error, or empty response of relevant kind — **fallback** to `rg`. Fallback policy in Composite facade code, not scattered in UI. |

Example (same user file as `[hybrid_index]`):

```toml
[command_palette.go_to_search]
backend = "rg"   # "rg" | "hci" | "auto"
```

- **Per-prefix overrides** (`x:` ≠ `t:`) — optional **second iteration** if v1 can ship without them.

**Support and testing:**

- New backend = **one interface implementation** + DI/factory registration; orchestrator does not branch `if (useHci)`.
- Orchestrator unit tests on **fake backend**; separate tests per implementation (hit ↔ row mapping).

**UX (optional):** short source label in subtitle (“rg” / “index”) or diagnostics only — product choice; user need not see backend name by default.

---

## Rejected / deferred alternatives

- **Plugin prefixes from TOML in v1** — heavier protocol and validation; defer until second prefix source besides code.
- **Merge `c:` and go-to into one grammar** — breaks IML and VS-style go-to mental model.
- **Single regex parser for entire line** — possible implementation detail; architecturally **mode type and strategy** matter.

---

## Consequences

- **Positive:** one explicit entry for mode docs; easier prefix and side effects (go-to cancel); less chrome/code drift; **engine switch** (`rg` ↔ HCI ↔ chain) without orchestrator `if` explosion.
- **Negative:** refactor scope — orchestrator, DI, settings, tests; after interface extraction user behavior in v1 should stay **parity** with Ripgrep backend by default (change only via setting — intentional).

---

## Implementation status

Implemented in app and tests: `CommandPaletteParsedQuery` / `CommandPaletteParsedQueryParser`, `ICommandPaletteGoToSearchBackend` (+ `rg` / HCI / `auto`), `[command_palette.go_to_search]` in settings, canonical hints `CommandPaletteChromeModeHints`.

Further improvements (plugin prefixes, extended `x:` semantics) — open directions per this ADR, not blockers for current scope.
