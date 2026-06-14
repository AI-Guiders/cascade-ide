# Features/Forge — vertical client slice (agent-forge)

**Normative:** [ADR 0161](../../docs/adr/0161-cide-spine-and-forge-vertical-feature-module.md), [ADR 0160](../../docs/adr/0160-forge-slash-catalog-runtime-overlay.md), [ADR 0158](../../docs/adr/0158-forge-lens-crs-overlay.md).

## Одна строка

CIDE **client** for forge: connect, server-driven slash overlay, command execute, CRS Lens, MCP handlers, bracket refs — **not** forge host.

## Layout (F2–F5 done)

| Path | Concern |
|------|---------|
| `Infrastructure/` | Capabilities fetch, slash overlay, execute, secrets, bracket parsers, open/nav |
| `Lens/` | Connect (OAuth/device), CRS client, workspace config, write API |
| `Mcp/` | `ForgeMcpHandlers` — all `forge_*` / `forge.artifact.goto` MCP tools |
| `Models/` | `ForgeLensSecrets` TOML model |
| `ForgeFeatureModule.cs` | `ICascadeFeatureModule` — single MCP registration site |
| `CascadeFeatureModules.cs` | Explicit module list (no MEF, ADR 0005) |

## Spine vs vertical

- **Spine:** bundled `intent-catalog.toml`, `SlashLineResolver`, chat slash UI — **no** `/forge *` entries when using overlay ([0160](../../docs/adr/0160-forge-slash-catalog-runtime-overlay.md)).
- **Vertical:** everything that talks to forge HTTP/MCP for this workspace.

## Guardrail

**CASCOPE043** — `using CascadeIDE.Features.Forge.Infrastructure|Lens` only from this tree + allowlisted hooks (`Features/Chat`, `IdeMcp`, `WorkspaceNavigation`, bracket consumers).

## Config

`.cascade/workspace.toml` → `[workspace.forge]` (`base_url`, `repo`). See [0158](../../docs/adr/0158-forge-lens-crs-overlay.md).
