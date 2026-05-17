<!-- English translation of adr/0082-acp-ide-mcp-loopback-single-process.md. Canonical Russian: ../../adr/0082-acp-ide-mcp-loopback-single-process.md -->

# ADR 0082: ACP and MCP IDE - one copy of the process (loopback HTTP/SSE instead of the second `CascadeIDE --mcp-stdio`)

**Status:** Proposed  
**Date:** 2026-04-20  
## Related ADRs

| ADR | Role |
|-----|------|
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | chat Cursor ACP, `mcpServers`, auto-mixing IDE MCP |
| [0016](0016-agent-client-protocol-external-agent.md) | ACP, stdio, borders with MCP |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP IDE as a circuit |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | agent façade |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | MCP transport; orthogonal to ACP, but useful in case of failures |

### Outside ADR

| Document | Role |
|----------|------|
| [`externals/acp-csharp/.../McpServer.cs`](../../externals/acp-csharp/src/AgentClientProtocol/Schema/McpServer.cs) | vendor ACP SDK (McpServer) |

---
## Context

When **`acp_auto_inject_ide_mcp`** is enabled, **`StdioMcpServer`** is mixed into `session/new` with the same binary as the GUI and the arguments **`--mcp-stdio`**. The **`cursor-agent`** process brings up a **separate** Cascade IDE instance as an MCP server over stdio.

These are **two different processes**: the second has its own life cycle, **does not** have a common `MainWindowViewModel`, open editor tabs and other state of “that” session in which the user is chatting.

**Important:** the **`--mcp-stdio` mode in the current implementation is not headless** - a full-fledged Avalonia desktop lifetime starts with **`MainWindow`** and the same `MainWindowViewModel`, in parallel MCP is raised to stdio ([`App.axaml.cs`](../../App.axaml.cs): `RunMcpStdio` → window + `RunMcpServerAsync`). What appears on the screen is **a second complete copy of the application**; with a multi-window workspace configuration, this easily looks like **several extra windows** (for example three), duplicating the chrome of a separate instance. The `ide_*` tools are served by **this** second copy, not the window where the chat with ACP is open - hence the discrepancy with the expectation "the agent controls **this** window".

In [0048 § consequences](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) it is already fixed: duplication of processes is acceptable; “single process” optimization is a **separate** architectural solution. This ADR is just that.

---

## Problem

1. **State parity:** user and agent must rely on the **same** IDE context (files, focus, selection) when dealing with `ide_*` commands.
2. **Extra process and duplication of UI:** the second `CascadeIDE` is not a background host, but a **complete second instance** with windows; excess memory, disk, point of failure; confusion during debugging and visual noise (“what process am I looking at in the Task Manager”, “why another main window”).
3. **The current stdio path remains valid** for external hosts (Cursor launches the IDE only with `--mcp-stdio`) - the new solution **doesn't** have to break it.

---

## Solution (direction)

<a id="adr0082-p1"></a>

### 1. Purpose

For a **chat session with Cursor ACP inside the Cascade IDE GUI**, mixing **`cascade-ide`** into `McpServers` should by default (or by explicit flag) point the agent to an **MCP server in the same process** as the chat window - without running a second `CascadeIDE.exe`.

<a id="adr0082-p2"></a>

### 2. Transport: loopback HTTP or SSE

The Agent Client Protocol scheme already has not only `stdio`, but also **`http`** / **`sse`** (`url` + `headers`). Direction of implementation:

- In the **main process** IDE, raise the **local** MCP endpoint (compatible with what the MCP client expects in the **cursor-agent** for the selected server type): for example **`127.0.0.1`**, dynamic or configurable port, **loopback** only (not `0.0.0.0`).
- In **`MergeForAcpNewSession`** (or equivalent when generating a list for ACP) generate **`HttpMcpServer` or `SseMcpServer`** with this `url` and, if necessary, **`headers`** (see §3).

The choice of **streamable HTTP vs SSE** is to be fixed upon implementation by actual support in **cursor-agent** and in the used MCP library on the IDE side (a single contract with the existing `IdeMcpServer` / `McpServer.Create`).

<a id="adr0082-p3"></a>

### 3. Security
- Only **localhost**; Don't listen to external interfaces without a separate solution.
- **Session secret:** pass to `headers` (for example `Authorization`) a **one-time or short-lived token** issued by the GUI at the start of an ACP session, so that another local process cannot connect to a neighboring PID without knowing the token.
- Port: avoid a fixed global port that conflicts with other applications; if necessary, select a free port + write to the snapshot for `session/new`.

<a id="adr0082-p4"></a>

### 4. Coexistence with stdio

- **`--mcp-stdio`** mode in a separate process **save** for the scenario “external host (Cursor) launches IDE as MCP” ([MCP-PROTOCOL.md](../../MCP-PROTOCOL.md)).
- For ACP inside the IDE: settings switch or heuristic “if the main window is already loopback; otherwise stdio" - specify in the implementation; Explicit keys in **`[mcp]`** are allowed (for example, a transport for auto-mixing in ACP).

<a id="adr0082-p5"></a>

### 5. Alternative (not priority): proxy in the second process

Leave the second process, but teach it to **proxy** calls to the main one via IPC. Higher in complexity, duplicates contracts; consider only if the loopback in **cursor-agent** turns out to be too expensive.

---

## Consequences

- A **second way** of MCP hosting in the IDE will appear: along with **stdio-only** in a separate process - **in the GUI process** (initialization at the start of an ACP session or lazily), with the correct **shutdown** when changing the provider / closing the chat.
- Documentation [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) and Environment Readiness tips - update when a stable flag/port appears.
- **Tests:** "single process" integration scripts should not regress the external `--mcp-stdio` script.

---

## Open questions

1. Which type of **`McpServer`** in `session/new` is guaranteed to support **cursor-agent** for the MCP IDE (test priority by experiment).
2. Do I need **stable binding** of the port in the user settings for the firewall/scripts?
3. Behavior with **multiple windows** Cascade IDE: one endpoint per application process vs binding to a window - check with model [0017](0017-multi-window-workspace-and-agent-surfaces.md).