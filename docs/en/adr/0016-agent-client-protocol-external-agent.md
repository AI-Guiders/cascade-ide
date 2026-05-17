<!-- English translation of adr/0016-agent-client-protocol-external-agent.md. Canonical Russian: ../../adr/0016-agent-client-protocol-external-agent.md -->

# ADR 0016: External agent using Agent Client Protocol (stdio, Cursor CLI)

**Status:** Accepted · Implemented  
**Date:** 2026-04-05  
## Related ADRs

| ADR | Role |
|-----|------|
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP - **other** circuit: IDE tool server; not to be confused with ACP |

### Outside ADR

| Document | Role |
|----------|------|
| [note-acp-cascade-cursor-v1.md](../ui-ux/note-acp-cascade-cursor-v1.md) | terminology and specification links |
| [concept-pfd-mfd-cascade-v1.md](../ui-ux/concept-pfd-mfd-cascade-v1.md) | concept pfd mfd cascade v1 |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | Cursor ACP / stdio in chat and settings; orthogonal to MCP - see text |

---
## Context

You need to connect the built-in CascadeIDE chat with an **external** agent (primarily **Cursor** via the official CLI and ACP mode), without embedding the UI Cursor and without your own wire “from scratch”.

**Agent Client Protocol (ACP)** is an open client↔agent contract (JSON-RPC by string, typical transport **stdio**). PoC passed: chat responses, Russian text, session and callbacks to the IDE (fs, terminal) according to the specification.

## Solution

1. **Transport:** subprocess `cursor-agent` (or equivalent from the dist-package) with argument **`acp`**, **stdin/stdout** for protocol, no console window (`CreateNoWindow`), stream redirection.

2. **Encoding:** for **Windows** explicitly set **UTF-8** for redirected stdin/stdout/stderr of the agent process (and for child ACP-terminal processes), otherwise responses in Russian are decoded in OEM and give “crazy messages”.

3. **Client SDK:** library **AgentClientProtocol** (community, upstream [acp-csharp](https://github.com/nuskey8/acp-csharp)) - **vendor** in `externals/acp-csharp/` as sources, connected via `ProjectReference`. Sources under `externals/**/*.cs` are **not** compiled in the main IDE assembly (`Compile Remove` in `CascadeIDE.csproj`) so that the nested sandbox with top-level `Main` does not intercept the application entry point.

4. **Integration into the product:** separate chat provider (**Cursor ACP**), setting the path to `cursor-agent.cmd` or to the directory with `dist-package`; ACP session lifecycle and terminal/fs mirroring - in `Services/CursorAcp/`.

5. **Cursor authentication:** not interactive inside WinExe chat on first launch; the user passes the **`agent login` / API key** in a normal terminal or through environment variables (as in the Cursor documentation). Agent errors before UI - as development progresses, stderr can be duplicated into chat/log (not part of this ADR).

6. **Boundaries:** ACP is about **dialogue with an external agent**. The MCP server IDE (`--mcp-stdio`, agent tools) remains a separate loop ([0008](0008-mcp-contracts-and-testable-infrastructure.md)); combining scenarios with subsequent iterations.

## Consequences

- Upstream `acp-csharp` updates - conscious merge/vendoring; when changing the major ACP specification - checking compatibility with `cursor-agent`.
- Dependency on CLI Cursor format (argument `acp`); when Cursor documentation changes - manual regression or smoke script.
- Duplication of types when erroneously including SDK sources and package references - prevented by `Compile Remove` for `externals`.

## Rejected alternatives

- **Ollama only / local LLM only without ACP** - remains a different provider; does not cover the “same agent as in Cursor” scenario.
- **Own binary protocol over stdio** - rejected in favor of the ACP standard and a ready-made client SDK.
- **Embedding webview Cursor** is outside the scope of the ACP desktop client.

## Discussion (does not block Accepted)

- Submodule instead of vendoring `externals/acp-csharp` - migration is possible when the update process stabilizes.
- Output stderr of the agent in the chat UI in case of errors and “Authentication required”.