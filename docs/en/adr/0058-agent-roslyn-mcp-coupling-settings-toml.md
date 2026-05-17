<!-- English translation of adr/0058-agent-roslyn-mcp-coupling-settings-toml.md. Canonical Russian: ../../adr/0058-agent-roslyn-mcp-coupling-settings-toml.md -->

# ADR 0058: Pairing the agent and Roslyn MCP in `settings.toml` (limits, node types, timeouts, presets)

**Status:** Proposed  
**Date:** 2026-04-18

## Related ADRs

| ADR | Role |
|-----|------|
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | `settings.toml` |
| [0039](0039-workspace-navigation-affordances.md) | navigation, MCP presets; UI intent map |
| [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) | TOML pattern |
| [0053](0053-semantic-map-control-flow-pfd.md) | intent map on PFD |
| [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) | profiles, Manager, Auto-Focus/Combat/Echelon modes, EFB on third monitor - **separate ADR** |

---
## Context

The agent layer and **Roslyn** are connected through **MCP tools**. Without explicit rules about how much and what to give, the agent overloads the context or underreceives the structure. This needs to be changed **in the config** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)), without necessarily editing C#.

**Intuition (not the ADR norm):** agent layer - request circuit, Roslyn MCP - semantics gateway; The settings below set **volume, filter, timing**.

**Already available (not cancelled):**

- `[semantic_map]` - UI type/depth - [0039](0039-workspace-navigation-affordances.md), [0053](0053-semantic-map-control-flow-pfd.md).
- Presets **`get_code_navigation_context`** - [0039 § Agent/MCP](0039-workspace-navigation-affordances.md#adr0039-mcp-workspace-navigation).

This ADR is **agent ↔ Roslyn MCP pairing parameters layer** in TOML. Behavior of **profiles**, **Manager**, tactics/strategy, third monitor - **[0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md)**.

---

## Solution (principles)

<a id="adr0058-p1"></a>

### 1. One declarative scheme - several consumers

| Category | Example of meaning | Who is obliged to take into account |
|-----------|----------------|---------------------|
| Response volume limits | `max_nodes_per_query`, traversal depth limits | **Roslyn MCP** (or Graph Aggregation Adapter) |
| Filter types of nodes/symbols | `included_kinds` / exceptions | **Roslyn MCP** |
| Timeouts / “dirty” semantics | awaiting compilation vs stale graph | **Roslyn MCP** ± **IDE** |
| Query Mode Presets | conditional `ExploreMode` / `RefactoringMode` | **Roslyn MCP**; agent can override in call |

**Priority rule:** call argument > TOML section > server default - commit in implementation and [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) when fields appear.

<a id="adr0058-p2"></a>

### 2. Four groups of parameters (contract axes)

1. **Throttling & scoping** - volume and depth limits per request (`max_nodes_per_query`, `max_recursion_depth` or equivalents in the tool specification).

2. **Visibility mapping (capabilities)** - what classes of nodes/symbols are in the results.

3. **Timeouts and consistency** - wait for compilation; stale graph; time limits for heavy queries.

4. **Instrumental presets** - named sets (controlFlow / architecture / overview vs refactoring). Link to [0039](0039-workspace-navigation-affordances.md): link by name or orthogonal presets - no implicit merge.

<a id="adr0058-p3"></a>

### 3. Posting in TOML

Target section, for example **`[agent.roslyn_mcp]`**, separate from `[semantic_map]` and from **`[[code_navigation.presets]]`** ([0039](0039-workspace-navigation-affordances.md)) without explicit mapping. Exact keys - after the prototype; here the **axes** are fixed.

<a id="adr0058-p4"></a>

### 4. Versioning

Optional keys and obvious defaults; changing the required form - bump schemas ([0028](0028-user-settings-toml-localappdata-and-secrets.md)).

<a id="adr0058-p5"></a>

### 5. Minimum v0 vs deferred

**v0:** limits + one or two timeouts + one “overview vs deeper” preset without full `included_kinds`.

**Postponed:** comprehensive directory of kinds, complex recursion, megaconfig; **automation of profiles and modes** - [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md).

---

## Consequences

- Explicit PR contract: the key refers to the MCP server or IDE.
- Tests for defaults and merge priorities.

---

## Rejected alternatives

- Only env - worse reproducibility.
- Only a system prompt - there is no determinism at the tool level.

---

## Open questions

- Synchronization with the **standalone** `roslyn-mcp` config outside the IDE - one scheme or two.
- What tools are in the scope of the first implementation (navigation / semantic map / both).