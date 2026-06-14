# Features/Forge — vertical client slice (agent-forge)

**Normative:** [ADR 0161](../../docs/adr/0161-cide-spine-and-forge-vertical-feature-module.md), [ADR 0160](../../docs/adr/0160-forge-slash-catalog-runtime-overlay.md), [ADR 0158](../../docs/adr/0158-forge-lens-crs-overlay.md).

## Одна строка

CIDE **client** for forge: connect, server-driven slash overlay, command execute, CRS Lens, MCP handlers, bracket refs — **not** forge host.

## Ownership map (migration target)

| Concern | Current path | Target |
|---------|--------------|--------|
| Capabilities fetch | `Services/Forge/ForgeCapabilitiesClient.cs` | `Features/Forge/` |
| Slash overlay | `Services/Forge/ForgeSlashCatalogOverlay.cs` | `Features/Forge/` |
| Command execute | `Services/Forge/ForgeCommandExecuteClient.cs` | `Features/Forge/` |
| Connect | `Features/WorkspaceNavigation/Application/ForgeLens*Connect*.cs` | `Features/Forge/Lens/` |
| CRS client | `Features/WorkspaceNavigation/Application/ForgeLensCorrespondenceClient.cs` | `Features/Forge/Lens/` |
| MCP handlers | `Features/IdeMcp/Execution/IdeMcpCommandExecutor.Handlers.Forge.cs` | `Features/Forge/Mcp/` |
| Bracket refs | `Services/Forge/BracketForgeReferenceParser.cs` | `Features/Forge/` |
| Slash merge hook | `Features/Chat/SlashLineResolver.cs` | spine (calls overlay API) |

**F0 (done):** overlay + execute wired. **F2–F5:** physical move per [0161 §5](../../docs/adr/0161-cide-spine-and-forge-vertical-feature-module.md).

## Spine vs vertical

- **Spine:** bundled `intent-catalog.toml`, `SlashLineResolver`, chat slash UI — **no** `/forge *` entries when using overlay ([0160](../../docs/adr/0160-forge-slash-catalog-runtime-overlay.md)).
- **Vertical:** everything that talks to forge HTTP/MCP for this workspace.

## Config

`.cascade/workspace.toml` → `[workspace.forge]` (`base_url`, `repo`). See [0158](../../docs/adr/0158-forge-lens-crs-overlay.md).
