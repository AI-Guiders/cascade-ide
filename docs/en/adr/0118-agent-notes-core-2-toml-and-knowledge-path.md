<!-- English translation of adr/0118-agent-notes-core-2-toml-and-knowledge-path.md. Canonical Russian: ../../adr/0118-agent-notes-core-2-toml-and-knowledge-path.md -->

# ADR 0118: Agent Notes Core 2.0 - TOML, `knowledge_path`, parity with agent-notes-mcp

**Status:** Accepted · Implemented  
**Date:** 2026-05-16

## Related ADRs

| ADR | Role |
|-----|------|
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Generic Core Use Case (git-mcp) |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | TOML settings, secrets |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML-first IDE configuration |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Surface `IdeCommands` |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP Contracts |

**Outside the repo:** [agent-notes-mcp ADR 014](https://github.com/AI-Guiders/agent-notes-mcp/blob/main/docs/adr/014-agent-notes-local-settings-toml-v1.md) (release **2.0**, `--config`); NuGet **AIGuiders.AgentNotes.Core** 2.x.

---
## Context

**agent-notes-mcp 2.0**:

- Mandatory **`--config`** → TOML (`[knowledge]`, `[workspace]`, `[status]`).
- MCP tools: **`knowledge_path`** argument (instead of `canon_path`).
- **AgentNotes.Core** 2.0: `LocalSettingsLoader`, `AgentNotesRuntime`; without META JSON / **`AGENT_NOTES_CANON_PATH`** in supported path.

**Cascade IDE** before implementation:

- NuGet **`AIGuiders.AgentNotes.Core` 1.0.0**.
- In-proc **`McpAgentNotesService`**; knowledge commands with **`canon_path`**.
- Separately env **`AGENT_NOTES_CANON_PATH`** and overlay KB-Base.

---

## Solution

1. **Dependency Core ≥ 2.0.0** - in open-monorepo: `ProjectReference` on `../agent-notes-core`; in CI after publishing NuGet - `PackageReference`.

2. **SSOT configuration** - in `settings.toml`:   ```toml
   [agent_notes]
   config_path = "D:/agent-notes-mcp/agent-notes-mcp.toml"
   ```
Same file as **`--config`** in `mcp.json` for Cursor. The relative path is from `%LocalAppData%\CascadeIDE\`.

   Loading: **`AgentNotesRuntimeLoader.EnsureInitialized`** → `LocalSettingsLoader.Load` → **`AgentNotesRuntime.Initialize`**.

3. **`canon_path` → `knowledge_path`** in `IdeCommands.Knowledge.*`, executor, generated docs. JSON args accepts legacy alias `canon_path` (read in `McpCommandJsonArgs.KnowledgePath`).

4. **KB-Base overlay** (`kb_base_overlay_path`) - orthogonal to primary root from TOML; without `AGENT_NOTES_CANON_PATH`.

5. **Environment readiness:** line “agent-notes config (TOML)” - the file exists, TOML is loaded, primary root is on disk.

---

## Out of scope

- **[status]** HTTP in IDE - only in MCP process ([ADR 013](https://github.com/AI-Guiders/agent-notes-mcp/blob/main/docs/adr/013-localhost-status-surface-v1.md)).
- Auto-generation of TOML from UI.

---

## Implementation

| Component | Path |
|-----------|------|
| Loader | `Services/AgentNotesRuntimeLoader.cs` |
| Settings | `Models/AgentNotesSettings.cs` - `config_path` |
| Example | `docs/samples/settings.localappdata.example.toml` |

---

## Consequences

- **Plus:** one TOML for Cursor MCP and CIDE in-proc; scope/workspace from `[workspace]`.
- **Disadvantage:** breaking rename args; requires published NuGet 2.0 for builds without an adjacent `agent-notes-core`.

---

## Rejected alternatives

- **Duplicate TOML in `%LocalAppData%\CascadeIDE\agent-notes.toml`** - discrepancy with MCP; rejected in favor of **`config_path`** per file.
- **Only env `AGENT_NOTES_CANON_PATH`** - against MCP 2.0.