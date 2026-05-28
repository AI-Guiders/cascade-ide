<!-- English translation of adr/0050-declarative-instrument-zone-placement-toml.md. Canonical Russian: ../../adr/0050-declarative-instrument-zone-placement-toml.md -->

#ADR 0050: Declarative map "tool → zone/slot" in TOML

**Status:** Accepted · Implemented  
**Date:** 2026-04-16

## Related ADRs

| ADR/document | Role |
|----------------|------|
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | `presentation` - display topology, not slots |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | `instrument_id` + `slot_id` |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | Personal `settings.toml` |
| [0010](0010-ui-modes-toml-configuration.md) | UiModes, bundles |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | CDS, surface tools |
| [`cds-contract-v0.md`](../../design/cds-contract-v0.md) | CDS drawing |
| [`[code_navigation]` in samples](../samples/settings.toml) | Layer analogy: bundle + repo + user |

## Summary

- Declarative map **tool → zone/slot** in TOML.
- Merge bundle/repo/user; `InstrumentPlacementRuntime` + compositor.


### Implementation snapshot

| Element | Meaning |
|---------|----------|
| TOML | `[instrument_routing]`, merge bundle/repo/user |
| Runtime | `InstrumentPlacementRuntime`, `MainWindowHostSurfaceCompositor` |

---
## Context

Today **where is which cockpit tool** (decision tree vs Semantic Map vs mount preview, etc.) in the main window is set by **composer logic** (`MainWindowHostSurfaceCompositor`, `DefaultSurfaceSlotInstrumentBindingProvider`, placement rules) and Avalonia bindings. The **`presentation`** line specifies **how many anchors and on what screens**, but **not** the declarative choice "in slot `pfd` show tool A or B" for a specific repository or user.

Need: **separate presentation topology** from **filling attention slots** and allow the team or user to **override the instrument map** without code edits - in the spirit of the cockpit data already used for CDS ([0047](0047-cockpit-instrument-descriptor-and-slot-composition.md)).

## Solution

**Adopt** a separate **configurable layer** - **tool placement map** for the main cockpit slots, stored in **TOML** and merged according to fixed priority rules.

**Invariant 1 - border with `presentation`:** line **`presentation`** / **`[presentation_grammar]`** ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) still only defines **topology** (how many groups of `(...)`, which anchors are on which display, optional column weights). The tool map **doesn't** set the number of monitors and **doesn't** replace anchors; runtime matches **semantic slots** (`pfd_primary`, `mfd_primary`) with specific **`surface_id` + `slot_id`** pairs within the product (including when changing the MFD host window), without the requirement to write `surface_id` in the user/repository TOML.

**Invariant 2 - public values (alias):** in `workspace.toml` and in `[display.instrument_routing]` **human-readable** tokens are specified, which runtime leads to canonical **`instrument_id`** (`CockpitStandardInstrumentIds`):

| Meaning in TOML | Canonical `instrument_id` |
|----------------|------------------------------|
| `solution_explorer` | `solution_explorer_tree` |
| `workspace_map` | `workspace_navigation_map` |
| `ide_health` | `ide_health_status_v1` |

It is also acceptable to specify **already canonical** `instrument_id` (as in CDS), if you need a copy-paste from the diagnostics.

**Invariant 3 - merge layers and conflict of one key:**

Three data sources (from general to specific):

1. Built-in **bundle** IDE (grocery card default).  
2. **Repository** layer - **`.cascade/workspace.toml`** with merge on top of the bundle ([0021](0021-pfd-mfd-cockpit-attention-model.md) §2.1).  
3. **User** layer - **`[display.instrument_routing]`** in `%LocalAppData%\CascadeIDE\settings.toml` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)).

**Within one table layer** `[instrument_routing]` keys are unique (`pfd_primary`, `mfd_primary`).

**Between sources** default: **user layer is stronger than repository layer, repository layer is stronger than bundle** (`user > repo > bundle`) for the same slot semantics.

**Flag in custom `settings.toml`:** **`prefer_repo_instruments_placement`** (bool). If **`true`** for matching keys, the **repository/bundle** layer wins; if **`false`** or if the key is not specified - **`user > repo`**.
**Invariant 4 - validation:** unknown slot key, unknown alias/`instrument_id` - **explicit diagnostics** when loading settings (validation `[display]`); for workspace - debug log when a line is ignored.

**Invariant 5 - without `surface_id` in public TOML:** user and repository set only **`[instrument_routing]`** / **`[display.instrument_routing]`**; mapping to `main_window_docked_grid` / `main_window_plus_mfd_host_top_level` surfaces is done in `InstrumentPlacementRuntime`.

### Public v1 contract

**Bundle/repo** (`UiModes/workspace.toml`, `.cascade/workspace.toml`):```toml
[instrument_routing]
pfd_primary = "solution_explorer"
mfd_primary = "workspace_map"
```
**User** (`settings.toml`):```toml
[display.instrument_routing]
pfd_primary = "workspace_map"
```
- `pfd_primary` and `mfd_primary` are product level keys.  
- Values ​​are **alias** from the table above or the canonical `instrument_id`.

Low-level array `[[instrument_placement_rules]]` with fields `surface_id` / `slot_id` **not used** in public contract v1 (no external consumers; DX - only table above).

## Why not just code

- **Repositories** differ in what is considered "main" in the PFD (map vs tree vs both in different presets).  
- **Flight** experiments and product modes should switch the map with **data** rather than branches with different composers.  
- **Agents and observability:** CDS already projects a list of tools ([0036](0036-cds-channel-compositor-surface-pipeline.md)); a single source in TOML makes it easier to reconcile “what’s claimed” and “what’s in the snapshot.”

## Alternatives (rejected as v1 main path)

| Alternative | Why not the basic choice |
|--------------|------------------------|
| Only changes to `MainWindowHostSurfaceCompositor` | There is no user and repo layer without a fork. |
| Extend only `presentation` with tool literals | Mixing display topology and widget selection; the line will become unreadable and break the parser's meaning. |
| JSON only in settings | TOML is already canon for IDEs and workspaces; one style is preferable. |

## Consequences

- Host composer reads **effective map** after merge.  
- Tests: unit to merge dictionary, alias resolver, scenario “another tool in PFD with the same `presentation`”.  
- Documentation: samples next to `settings.toml`.

## Implementation status

**Implemented**:
- bundle/repo: `UiModes/workspace.toml` + `.cascade/workspace.toml` via `UiWorkspaceTomlMerger` and `UiModeCatalog.ApplyRepositoryWorkspaceTomlOverlay`;
- user: `[display.instrument_routing]` and `prefer_repo_instruments_placement` in `%LocalAppData%\CascadeIDE\settings.toml`;
- single runtime: `InstrumentRoutingAliasResolver`, `InstrumentPlacementRuntime`, `MainWindowHostSurfaceCompositor`.

## Open questions

- **Numerical priority** for a string is not required in v1: a **merge** layer between the bundle/repo/user is sufficient; if fine-tuning is necessary in one file, later, in a separate field.  
- Is it necessary to version the section schema (`schema_version` inside the block) for migrations.  
- Connection with the **mount layer** (`InstrumentMountPolicyRules`, `use_skia_instrument_mount`): **orthogonal** - style remains about the visual, the map from this ADR is about *which* `instrument_id` is in the slot.