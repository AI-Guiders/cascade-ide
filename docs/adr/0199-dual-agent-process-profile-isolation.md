# ADR 0199: Dual-agent process profile isolation

**Статус:** Accepted  
**Дата:** 2026-07-27  
**Tags:** #cdp #isolation #profile #witdb #adr #cascade-ide

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0182](0182-restore-previous-desk-dual-instance.md) | Dual-instance deploy (`cdp` / `cdp-debug`) |
| [0197](0197-cdp-mcp-cockpit-wire-parity-vs-cide.md) | Wire gap (ортогонально) |
| [0198](0198-toolchain-ensure-vs-lsp.md) | Toolchain; profile-local recipe cache later |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Habitat comfort |

## Резюме

- Два агента в разных пространствах (Cursor habitat vs SSCAD/Dashspec) **не делят** WitDB / seats / pressure / project_root thrash.
- Граница изоляции v0: **process profile** (`CDP_PROFILE`), не session-tenant в одном процессе.
- Dual-instance dogfood (`cdp` vs `cdp-mcp-debug`) — прецедент; обобщаем до именованного профиля.

## Контекст

Сейчас один `%LocalAppData%/cdp-mcp/intent-workspace.witdb` и общий pressure-stash на процесс. Два окна-агента на одном MCP = взаимный overwrite desk/focus.

## Решение

### Process profile

| Env | Эффект |
|-----|--------|
| `CDP_PROFILE` (default `default`) | Суффикс путей состояния |
| WitDB | `…/cdp-mcp/{profile}/intent-workspace.witdb` |
| pressure stash | `…/cdp-mcp/{profile}/pressure-stash.json` |
| user toolchain presets | под тем же profile root |
| MCP mount name | оператор: отдельные entries `cdp` / `cdp-sscad` с разным `CDP_PROFILE` |

### Не session-tenant v0

Один процесс + `agent_id` в WitDB — отложено: сложнее, выше риск thrash на shared buffers/shell tabs.

### Связь с dual-instance

[0182](0182-restore-previous-desk-dual-instance.md) остаётся для **source vs live** deploy. Profile isolation — для **двух рабочих агентов**. Можно комбинировать: live+profile.

## Не делать

- Смешивать с wire remap / toolchain ensure в одном «бог-коммите».
- Force-push / один WitDB «на двоих» как норму.

## Последствия

- Thin slice: читать `CDP_PROFILE` при `EnsureWorkspaceDb` / pressure path (cdp-mcp).
- Документировать mcp.json example для второго агента.
- Toolchain user recipes — profile-scoped when paths land.
