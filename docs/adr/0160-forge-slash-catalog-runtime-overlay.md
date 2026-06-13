# ADR 0160: Forge slash catalog runtime overlay (Phase D)

**Status:** Accepted  
**Date:** 2026-06-13  
**Related:** [0153](0153-slash-catalog-only-resolution.md), [0158](0158-forge-lens-crs-overlay.md), [0159](0159-bracket-forge-artifact-reference.md), FORGE-ADR-0015 (agent-forge)

## Context

Bundled `intent-catalog.toml` listed forge slash paths (`/forge artifact goto`, future `/forge issue open`, …) manually. Forge host already exposes `GET /api/v1/capabilities.commands[]` with DOI paths. Dual maintenance drifted from server truth.

## Decision

After `forge_lens.connect` (or OAuth/device login with `[workspace.forge]`):

1. **Fetch** `GET /api/v1/capabilities` from forge host.
2. **Overlay** `ForgeSlashCatalogOverlay` — synthetic `SlashRouteEntry` for each command `pathAliases` starting with `/forge `.
3. **Merge** in `SlashLineResolver` / `ChatSlashCommandCatalog`: bundled TOML + overlay; overlay wins on `/forge *` prefix when active.
4. **Execute** most forge commands via `POST /api/v1/commands/execute` (`ForgeCommandExecuteClient`).
5. **Exception:** `forge.artifact.goto` — slash path from overlay, execution stays **local** CIDE MCP (`forge.artifact.goto`, was `forge_lens.open`) for browser + code-tail navigation.

Offline / disconnected: no `/forge *` in picker (no codegen snapshot for autocomplete).

## Removed from bundled TOML

- `/forge artifact goto` — now server-driven only when forge connected.
- Other `/forge *` paths — never duplicated in TOML; overlay only.

## MCP naming

- CIDE local: `forge.artifact.goto` (DOI aligned with forge `commandId`).
- Forge server MCP tools: `forge.{object}.{intent}` (FORGE-ADR-0015 §3.0); legacy snake_case removed.

## Consequences

- Single catalog source for forge slash when connected.
- CIDE repo owns overlay wiring + local artifact navigation; forge repo owns command descriptors and execute API.
