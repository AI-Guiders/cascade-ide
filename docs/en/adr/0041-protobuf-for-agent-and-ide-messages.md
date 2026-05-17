<!-- English translation of adr/0041-protobuf-for-agent-and-ide-messages.md. Canonical Russian: ../../adr/0041-protobuf-for-agent-and-ide-messages.md -->

# ADR 0041: Protocol Buffers — consideration for agent and IDE messages (entry point)

**Status:** Proposed (discussion direction and criteria fixed; **not** a decision to migrate from JSON immediately)  
**Date:** 2026-04-13  

## Related ADRs

| ADR | Role |
|-----|------|
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts and infrastructure |
| [0018](0018-ide-commands-canonical-xml-documentation.md) | `IdeCommands` canon, ProtocolDocGen |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | agent facade, tools |
| [0001](0001-debug-hypotheses-json-storage.md) | JSON as chosen storage for a separate domain — do not conflate with this ADR |

### Outside ADR

| Document | Role |
|----------|------|
| [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) | current wire format for IDE commands to the agent |

---
## Context

Using **Protocol Buffers (protobuf)** instead of or alongside **JSON** for structured messages between **agent**, **IDE**, and related processes has been discussed for a long time. The decision was not fixed in the repo; without an ADR we lose an **entry point** for later discussion and effort estimates.

This ADR sets a **frame**: where protobuf fits, where JSON remains a reasonable default, which **boundaries** to separate, and which **criteria** to use when deciding — without obligating implementation in a specific product version.

---

## Problem

- **JSON** today dominates external protocols (MCP, many LLM APIs), is convenient for **logs, debugging, manual inspection**, and does not require a separate schema/codegen pipeline in simple scenarios.
- **Protobuf** gives **compact representation**, typically **fast** parse/serialize, an **explicit schema** with evolution via field numbers and **.NET** codegen — but adds a **pipeline** (`.proto`, build, version discipline) and is **less human-readable** without tools.

Without fixed **boundaries** the team risks either rejecting protobuf without criteria, or mixing “internal binary contour” with “external JSON” without explicit policy.

---

<a id="adr0041-p1"></a>

## Solution (principles)

### 1. Separation by boundary, not “all or nothing”

- **External / human-readable boundary** (specs like JSON-RPC MCP, public APIs, copy-paste in chats, quick log grep): **JSON** remains the **natural candidate** until spec or ecosystem requires otherwise.
- **Internal high-frequency or high-volume contour** (IPC between IDE processes, event streams, persistent logs of large structures, multiple languages with shared contract): **protobuf** (or other **IDL + binary** format) is a **candidate for evaluation** if measurable benefit outweighs maintenance cost.

Moving to “protobuf everywhere instead of JSON” is **not** the goal of this ADR.

<a id="adr0041-p2"></a>

### 2. Criteria for deepening protobuf work

Worth a prototype or RFC if **more than one** of the below holds as a sustained combination on metrics:

- **Data volume** or **message rate** creates noticeable CPU/IO load vs the rest of the pipeline.
- **Strict schema version compatibility** is needed with parallel client evolution (protobuf field model).
- **Multiple implementations** are planned (e.g. IDE + separate services) and a **single contract** matters without hand-written DTO drift.

If none apply, **JSON + type discipline** (and contract tests) may remain sufficient.

<a id="adr0041-p3"></a>

### 3. Hybrid

Often reasonable: **same logical contract** — different codecs on different segments (e.g. JSON on MCP wire and protobuf between internal components), with explicit **mapping** and boundary tests. Not an obligation to duplicate every field by hand forever — but the **conversion point** must be conscious and documented.

<a id="adr0041-p4"></a>

### 4. Relation to current MCP and `IdeCommands`

While [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) and executor generation describe **JSON arguments** for commands, changing **external** representation for the agent is a **separate** decision (MCP spec, client compatibility). This ADR does **not** cancel current canon; it frames **future** RFCs for internal or optional contours.

---

<a id="adr0041-non-goals"></a>

## Non-goals

- Mandatory migration of all agent exchange to protobuf in the next release.
- Dropping JSON in logs and debug dumps without viewer replacements.
- Binding to a specific gRPC library: protobuf **may** pair with gRPC, but transport choice is separate.

---

<a id="adr0041-open-questions"></a>

## Open questions

1. Which **concrete streams** in Cascade IDE are first candidates for “JSON vs protobuf” measurement — separate diagram or task after profiling exists.
2. Whether a **single `.proto` repository** (monorepo + submodules) is needed or only **internal** contracts until API stabilizes.
3. How to **document** the boundary for contributors: one paragraph in [architecture-policy.md](../architecture-policy.md) vs link only to this ADR.

---

## Consequences

- A **stable reference** for discussion and prioritization: “see [0041](0041-protobuf-for-agent-and-ide-messages.md)”.
- Protobuf implementation does **not** follow automatically from Proposed status; a separate decision with metrics or a pilot is required.
