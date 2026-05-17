<!-- English translation of adr/0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md. Canonical Russian: ../../adr/0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md -->

# ADR 0048: Chat via Cursor ACP in IDE — Cursor host parity goal, tool surface, and MCP

**Status:** Accepted (partial: Cursor ACP in IDE, auto-inject MCP; full parity — per ADR)  
**Date:** 2026-04-14  
**Updated:** 2026-04-14 — `mcp.json` ↔ CIDE; “Open folder” in UI and MCP. Details — [§ History](#adr0048-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0016](0016-agent-client-protocol-external-agent.md) | ACP, stdio, MCP boundaries |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | IDE MCP as separate contour |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | facade: chat / ACP / autonomy / external MCP |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | chat history in IDE |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | MCP transport, do not mix with ACP |

### Outside ADR

| Document | Role |
|----------|------|
| [north-star-cursor-mcp-cascade-workbench-v1.md](../design/north-star-cursor-mcp-cascade-workbench-v1.md) | moving from Cursor |

## Summary

- **Cursor ACP** chat parity inside IDE: `mcpServers`, auto IDE MCP, `mcp.json` ↔ CIDE parsing.
- Workspace “open folder” and tool gaps — recorded as product tasks.
- External agent ([0016](0016-agent-client-protocol-external-agent.md)) orthogonal to built-in chat UI.

---
## Context

A user may spend most time in **assistant dialog** (including in Cursor), rarely opening code. Target scenario for Cascade IDE — **the same class of dialog** (external Cursor agent over ACP, already logged in via CLI), but **inside the IDE**, with familiar workspace and without mandatory switch to separate Cursor app for chat.

**Cursor host** and **Cascade IDE host** are different environments:

- In Cursor the agent has a **tool and context set** defined by Cursor product (repo search, integrations, MCP from host config, etc.).
- In Cascade IDE chat with the same `cursor-agent` goes through an **ACP client** in the IDE process: basic IDE callbacks per ACP spec (e.g. fs, terminal) plus what is explicitly passed in **`session/new`** in **`mcpServers`**.

Without explicit fixation, expectation “like Cursor, but in IDE” easily mixes **dialog UX parity** with **parity of all host tools**, leading to disappointment (“why no grep / semantic search”).

## Solution

<a id="adr0048-p1"></a>

1. **Product goal (direction):** comfortable **daily dialog with the same external Cursor agent** in Cascade IDE chat panel, keeping workspace and IDE scenarios (open files, build, debug — as tools allow). Full match with **every** Cursor chat/agent UI capability is **not** declared mandatory for v1.

<a id="adr0048-p2"></a>

2. **Tools are not “built into the model”:** capabilities like code search, semantic search, repo traversal “like Cursor” come from **host** and/or **connected MCP servers**, not from merely running the LLM. In **IDE + ACP** contour the agent tool list is determined by:
   - **ACP** capabilities and **IDE client** implementation (`IAcpClient`: fs, terminal, etc. per spec);
   - **MCP server** list passed to the agent at session creation (`NewSessionRequest.mcpServers` in Agent Client Protocol);
   - **cursor-agent** policy (what it exposes to the model on top).

<a id="adr0048-p3"></a>

3. **IDE MCP (`--mcp-stdio`) and ACP link:** built-in Cascade IDE MCP server ( `ide_*` commands for external MCP client) and **cursor-agent** over ACP are **different processes and roles** ([0008](0008-mcp-contracts-and-testable-infrastructure.md), [0016](0016-agent-client-protocol-external-agent.md)). For the agent in an ACP session to call IDE tools **the same way as in Cursor** (separate stdio MCP process), the matching `stdio` server must appear in `session/new`. Source for that list — §4 (manual JSON) and §§7–9 (direction: default merge).

<a id="adr0048-p4"></a>

4. **Unified MCP list config for autonomy and ACP:** external MCP JSON array in settings (`[mcp] external_servers_json` / UI “External MCP servers”) is used by:
   - autonomous agent (MCP client in IDE — `McpClientService`);
   - **and** ACP session creation: conversion to `McpServer[]` for `session/new` (AgentClientProtocol: `stdio` / `http` / `sse`; entries without `type` — same contract as autonomy: `name`, `command`, `arguments`, `enabled`, optional `env`).

   Entry with **`"enabled": true`** is included in both contours (as for autonomy). On JSON change the ACP session resets so the next dialog picks up the new MCP list.

<a id="adr0048-p5"></a>

5. **Honest “chat in Cursor” parity wording:**  
   - **Achievable:** same agent binary, same Cursor auth, same MCP servers (if configured), plus ACP tools to IDE.  
   - **Not guaranteed without work:** identical tool and context set of **Cursor host** (grep/search UI, Cursor-specific integrations) unless reproduced by separate MCP or IDE capabilities.

<a id="adr0048-p6"></a>

6. **Chat history in IDE** remains per [0045](0045-agent-chat-persistence-event-log-and-projections.md); choosing **Cursor ACP** provider does not cancel event log and projections.

<a id="adr0048-p7"></a>

7. **Direction — “IDE tools by default” for ACP (Cursor host analog):** in Cursor the agent need not hand-build JSON: **host** merges MCP config (e.g. from `mcp.json`). For Cascade IDE as ACP chat host the **implementation direction** is: when building `McpServer[]` for `session/new`, **optionally merge** (settings flag; default on/off — separate decision at rollout) a **stdio** entry for the **current executable** with args `["--mcp-stdio"]`, path from canonical process API (e.g. running `CascadeIDE.exe` path). **Merge** with `external_servers_json` parse result: dedupe by stable key (e.g. `name`); on conflict priority to explicit user entry — fix in implementation.

<a id="adr0048-p8"></a>

8. **UX goal of §7:** agent in ACP **sees** `ide_*` tools “by default” without mandatory duplicate entry in external MCP UI **only** for IDE. Separate `--mcp-stdio` process remains **allowed** (same binary, different role); whether that process shows a window — **implementation-dependent**, not an ADR invariant.

<a id="adr0048-p9"></a>

9. **§7 boundaries:** auto-merge IDE MCP does **not** replace Cursor host tools (semantic search, specific integrations, etc.) or add them “magically”; it only makes **`ide_*` contour** predictably present in ACP session when the flag is on.

## Implementation in code (at ADR acceptance)

- Passing `mcpServers` in `NewSessionAsync` from `CascadeAcpMcpServerCatalog.FromExternalServersJson(...)`, source — external MCP settings string; ACP session cache invalidates on that string change.
- Command contract details — [MCP-PROTOCOL.md](../MCP-PROTOCOL.md).
- §§7–9 (**auto-merge IDE MCP**): **implemented** — `CascadeAcpMcpServerCatalog.MergeForAcpNewSession` adds stdio `cascade-ide` (`Environment.ProcessPath` + `--mcp-stdio`) when `[mcp] acp_auto_inject_ide_mcp` is on (default true) and `external_servers_json` has no server with the same name; `CursorAcpChatConnection` passes merged list in `NewSessionRequest.McpServers`.
- **Workspace “folder without .sln”** ([§ below](#adr0048-folder-workspace)): **implemented** — menu **File → Open folder…**, toolbar button, `LoadSolution` accepts directory path; MCP: `ide_load_solution` (folder or `.sln` path), `ide_open_folder_dialog`. Details in same §.

## Consequences

- **Long chat session wrap-up** (external agent via MCP and IDE chat contour) recorded in [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) (section “Chat session wrap-up”): supported path — `chat_export_readable` → short summary → alignment; playbook — `knowledge/playbook-session-summary-and-chat-export-v1.md` in agent-notes canon (same relative path under `knowledge/`). Without IDE MCP the same intent is covered by playbook **branch B**: `agent-transcripts` / `*.jsonl` archive, find session via `rg` on phrase, readable export via **`tools/Export-CursorJsonlTranscript.ps1`** (repo root / agent-notes canon — `tools/` directory).
- With **`acp_auto_inject_ide_mcp` on**, explicit `cascade-ide` entry in `external_servers_json` is **not required**; on name conflict **explicit** user entry wins.
- Documentation for “IDE tools in ACP by default”: flag `[mcp] acp_auto_inject_ide_mcp`, UI “Cursor ACP: automatically pass IDE MCP…”.
- Duplicate processes (IDE + separate IDE MCP process) allowed; “single process” optimization — separate architecture decision: [0082](0082-acp-ide-mcp-loopback-single-process.md) (loopback HTTP/SSE MCP in GUI instead of second `CascadeIDE --mcp-stdio` for ACP).
- Semantic search / grep “like Cursor” either from **cursor-agent** and connected MCP, or as separate MCP in config.

<a id="adr0048-tool-gaps"></a>

## Appendix: tool gaps (agent in ACP vs Cursor host)

Tables make an **explicit** map: what is often missing for the agent in **IDE + ACP + MCP from `session/new`** vs familiar **chat in Cursor**, and where it may close. **Coverage** column does **not** replace `IdeCommands` audit — on conflict [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) and code win.

**Coverage legend:** `IDE MCP` — `ide_*` with IDE server connected; `ACP` — fs/terminal via ACP; `External MCP` — entry in `external_servers_json`; `None` — no standard contour; `TBD` — not fixed.

### High priority (navigation and code picture)

| Need | Why | Coverage (orienting) |
|------|-----|----------------------|
| Semantic repo search | Find “where logic X lives” without guessing paths | **None** / **External MCP** (index, embeddings) / **TBD** `ide_*` |
| Mass text search (grep/rg) | Strings, symbols, `TODO`, patterns across tree | **ACP** — point file reads; **None** repo-wide without MCP or tool |
| Find references / symbol usages | Edits without missing call sites | **External MCP** (e.g. Roslyn) / partial **IDE MCP** — per exposed navigation/diagnostic commands |
| Solution-wide diagnostics (not only current file) | Overall error picture | **Partial IDE MCP** — depends on exposed commands; **TBD** extension |

### Medium priority (“changed — verified” cycle)

| Need | Why | Coverage (orienting) |
|------|-----|----------------------|
| Git (status, diff, log; sometimes blame) | Change context | **External MCP** (git) / **IDE MCP** — if wrappers exist |
| Build / tests with parsed output | Fast verify loop | **IDE MCP** / **ACP** terminal — per scenario |
| Debug (breakpoints, stack, step) | Beyond text-only | **External MCP** (dotnet-debug) / **IDE MCP** — per commands |
| Solution structure overview | Orient in large repo | **Partial IDE MCP** (tree/files) — per commands |

### Lower on list (noticeable, less often blocking)

| Need | Why | Coverage (orienting) |
|------|-----|----------------------|
| Library docs (Context7, MS Learn, etc.) | Fewer API mistakes | **External MCP** |
| Web (URL, UI snapshot) | Front, browser docs | **External MCP** (browser) |
| Solution-wide symbol rename | Refactoring | **External MCP** (Roslyn) / **TBD** `ide_*` |

### Top 3 gaps (subjective, backlog prioritization)

1. Semantic / codebase search across repository.  
2. Repo-wide grep (or equivalent in one–two calls).  
3. Find all references / stable Roslyn contour in MCP if not covered by `ide_*`.

Tables are **living**: update rows when new `ide_*` or external MCP appear; large additions — separate ADR or subsections here by agreement.

<a id="adr0048-mcp-json-walkthrough"></a>

### Typical Cursor host `mcp.json` walkthrough ↔ Cascade IDE (`ide_*`)

Below is a **logical** map: keys from `mcpServers` (as in Cursor config), transport type, **which gap** from [tables above](#adr0048-tool-gaps) the server closes, and **overlap with IDE’s own MCP** ([MCP-PROTOCOL.md](../MCP-PROTOCOL.md)). Paths to exe, URLs, API keys, tokens **omitted on purpose** — roles only; for ACP the same set moves to `external_servers_json` / `McpServer[]` with `stdio` / `http` / `sse` types in AgentClientProtocol.

| Key in `mcp.json` | Transport (general) | Closes (link to gaps) | CIDE (`cascade-ide` / `ide_*`) |
|-------------------|----------------------|------------------------|--------------------------------|
| `context7` | HTTP MCP | Library documentation (third-party API) | No `ide_*` analog; stays external MCP |
| `MS Learn Docs` | HTTP MCP | Official Microsoft documentation | No `ide_*` analog; external MCP |
| `Playwright` | stdio (`npx`) | Browser automation, web scenarios | In CIDE: preview/windows (`ide_show_preview`, …), not full Playwright |
| `dbhub` | stdio | SQL, configured databases | Not in `ide_*`; external MCP |
| `rentahuman` | stdio | External “human-in-the-loop” product | Not in `ide_*` |
| `roslyn` | stdio | Symbols, navigation, Roslyn diagnostics, refactorings | **Supplements** IDE: protocol has `get_current_file_diagnostics`, workspace navigation, metrics; Roslyn MCP is **deeper** C# contour |
| `dotnet-debug` | stdio | DAP debug: attach, step, variables | **Supplements** `ide_set_breakpoint` / clear and IDE debug UI |
| `tinvest` | stdio | Domain API (broker) | Not in `ide_*` |
| `cascade-ide` | stdio `--mcp-stdio` | Full IDE command surface | **Canon**: all `ide_*` and `ide_execute_command` → `IdeCommands` |
| `cascade-ide-debug` | stdio `--mcp-stdio` | Same, separate build (e.g. publish-debug) | Same; convenient not to mix with release process |
| `agent-notes` | stdio | Notes/KB, revisions, archive search | **Overlap**: CIDE has `ide_write_agent_notes`, `ide_read_agent_notes`, sections, archive, router — see `IdeCommands` in MCP protocol; agent-notes MCP may add **extra** contract (external repo) |
| `webcam-mcp` / `webcam-capture-mcp` / `webcam-analysis-mcp` | stdio | Camera, frames, analysis | Not in `ide_*`; see sensor ADRs if needed |
| `gitlab` | stdio (wrapper) | Remote GitLab: MR, wiki, pipeline | CIDE: `ide_git_*` — **local** git in solution dir; GitLab — **different** contour |
| `github` | stdio (wrapper) | Remote GitHub API | Same: local vs GitHub |
| `dotnet-build-test` | stdio | Build/test, output parsers (separate product) | **Overlap**: `ide_build`, `ide_run_tests`, `ide_run_affected_tests` already return structured JSON; dotnet-build-test MCP — **extra** policy/parsing |
| `git` | stdio | Git via GitMcp | **Overlap** with `ide_git_status`, `ide_git_diff`, `ide_git_commit`, … — different surfaces; both or one |
| `kokoro-tts-mcp` | stdio | Speech synthesis | Not in `ide_*` |
| `telegram-relay` | stdio | Telegram relay | Not in `ide_*` |

**Gap conclusion for such a host:** table rows on **find references / Roslyn**, **build/tests**, **debug**, **git (local)**, **workspace overview** are partly or fully covered by **`cascade-ide` + `roslyn` + `dotnet-debug` + `git` + `dotnet-build-test`** combo — need **mirror list** in ACP (`session/new`). Remains **narrow** if nothing else connected: **semantic search across repo** as some hosts have (not explicit in listed servers; may be built-in Cursor search or other MCP). **Repo-wide grep** — not duplicated as its own row; if needed: ACP terminal, `ide_*`, Roslyn, or future MCP.

<a id="adr0048-folder-workspace"></a>

### Workspace: “open folder” (repository), not only solution

To build **repository-centric** scenarios (git root, single agent context, future indexes and tree search), Cascade IDE needs **Open folder** mode **in addition** to **Open solution** (`.sln` / `.slnx`). From **folder root** the same **workspace root** meaning as from directory containing `.sln`: git, paths in `ide_*`, terminal, MCP and ACP (`cwd` / `GetWorkspacePath`) align via **canonical root** (`BreakpointsFileService.GetWorkspaceRoot` / equivalent: solution file → its directory; **opened folder path** → that folder).

**Implemented (code):**

- **UI:** menu **File → Open folder…**, toolbar **“Open folder”**; folder picker (`OpenFolderPickerAsync`), then same entry as solution: `MainWindowViewModel.LoadSolution(path)`.
- **Model:** `Workspace.SolutionPath` with opened folder stores **normalized directory path** (not `.sln`). **Build** button inactive without solution file — expected.
- **Explorer tree:** `FolderWorkspaceTreeBuilder` — recursive tree with depth/node caps and typical noise excludes (`.git`, `bin`, `obj`, `node_modules`, …). Root — `SolutionItem.CreateFolderWorkspaceRoot`; folder icon on root.
- **Load:** `SolutionWorkspaceViewModel.LoadSolutionTreeAsync`: if path is **directory** — folder tree; else prior `SolutionParser` for `.sln` / `.slnx` / `.slnf`.
- **Startup project / `.cascade-ide`:** `StartupProjectStore` and startup `.csproj` logic use **workspace root** via `GetWorkspaceRoot` so `.cascade-ide` and relative paths do not jump to parent when folder opened.
- **Terminal:** working directory — solution directory **or** opened folder when `SolutionPath` is directory.
- **MCP / palette:** `load_solution` (`ide_load_solution`) — `path` may be **folder** or solution file; added `open_folder_dialog` (`ide_open_folder_dialog`) — like menu “Open folder…”. Descriptions in `IdeCommands` and generated `IdeCommandsDoc` / contract updated.

**ACP link:** same computed workspace root for `NewSessionRequest.cwd` and agent settings as with open `.sln` (repo root = workspace folder).

**Possible follow-ups (non-blocking for this ADR):** fine-tune tree limits, extra extension filters, separate window title for “folder only” mode.

## Open questions

- ~~Flag name and default for §7~~ — fixed: `acp_auto_inject_ide_mcp` (default true), UI in AI/chat settings; dedupe by `name` case-insensitive, explicit JSON entry wins on conflict.
- Curated “chat like Cursor” preset (minimal MCP set + hints) on top of §7.
- Observability: show in UI **which** MCP were actually passed in `session/new` (including auto-merged; debug drift vs Cursor).
- Closing **None** / **TBD** rows in [tool gaps appendix](#adr0048-tool-gaps) — separate epics or ADRs (do not mix with §§7–9).
- **Open folder** — baseline [closed in § above](#adr0048-folder-workspace); only improvements remain (limits, filters, UI labels).

## Rejected alternatives (for now)

- **Mixing ACP and IDE MCP transports in one stdio without explicit list** — rejected for explicit config and ACP format compatibility.
- **Promising full Cursor host parity without enumerating tools** — rejected as misleading.

---

## Change history

<a id="adr0048-history"></a>

| Date | Change |
|------|--------|
| 2026-04-14 | `mcp.json` ↔ CIDE walkthrough; workspace “open folder” for repo scenarios; **implemented “Open folder” in UI and MCP** (see [§ workspace](#adr0048-folder-workspace)). |
