# ADR 0199: Dual-agent isolation (client workspace primary)

**Статус:** Accepted · Implemented  
**Дата:** 2026-07-27  
**Tags:** #cdp #isolation #roots #witdb #adr #cascade-ide

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0182](0182-restore-previous-desk-dual-instance.md) | Dual-instance deploy (`cdp` / `cdp-debug`) |
| [0197](0197-cdp-mcp-cockpit-wire-parity-vs-cide.md) | Wire gap (ортогонально) |
| [0198](0198-toolchain-ensure-vs-lsp.md) | Toolchain; state-scoped recipes |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Habitat comfort |

## Резюме

- Два агента в разных пространствах **не делят** WitDB / seats / pressure thrash.
- **Primary:** MCP **client roots** (`RequestRootsAsync`) → state under `%LocalAppData%/cdp-mcp/ws/<hash>/`.
- **Fallback:** session `scm_root`/`ProjectRoot` после `cdp_open` (тот же `ws/<hash>/`).
- **Override:** `CDP_PROFILE` env (явный process profile) — когда roots недоступны или нужен ручной split.
- Один `mcp.json` entry достаточен, если клиент отдаёт разные roots на разные окна/workspaces.

## Контекст

v0 думал только про `CDP_PROFILE` + второй MCP entry. Это работает, но неуклюже при одном `mcp.json`. Изящнее брать workspace у клиента (MCP Roots).

## Решение

### Приоритет ключа изоляции

1. `CDP_PROFILE` ≠ `default` → `%LocalAppData%/cdp-mcp/profiles/{name}/`
2. иначе MCP client roots (sorted, hashed) → `…/cdp-mcp/ws/{hash12}/`
3. иначе session project/scm → тот же `ws/{hash}/`
4. иначе legacy flat `…/cdp-mcp/` (`kind=default`)

Под StateRoot: `intent-workspace.witdb`, `pressure-stash.json`, `ide-settings.json`.

### Поведение

- `CdpClientWorkspace.Wire(server)`: notification `roots/list_changed` + boot refresh.
- CallTool: throttled refresh (~20s) + session fallback.
- Смена StateRoot → invalidate settings cache + reopen WitDB на новом пути.
- `cdp_health.isolation` — диагностика (`kind`, `state_root`, `client_roots`, last error).

### Dual-instance

[0182](0182-restore-previous-desk-dual-instance.md) остаётся для live vs dogfood. Isolation — для двух рабочих пространств.

## Не делать

- Требовать второй `mcp.json` как норму.
- Считать peel DataBus заменой изоляции.

## Последствия

- Dogfood: два Cursor window с разными folder roots → разные `ws/*` без правки mcp.json.
- Если Cursor не advertises roots: session fallback после `cdp_open`; иначе `CDP_PROFILE`.
- cdp-mcp **0.5.251+**.
