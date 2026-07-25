# ADR 0187: CDP `mcp_scene` — агент монтирует MCP как оператор в Cursor

**Статус:** Accepted · Implemented (CDP 0.5.157)  
**Дата:** 2026-07-25  
**Обновлено:** 2026-07-25 — `cdp_mcp` op=scene|presets|mount|tools|call|unmount  
**Tags:** #cdp #mcp #habitat #equal-standing #agent-comfort #serena #outlet #adr #cascade-ide

## Резюме

- CDP — **розетка / habitat для других MCP**.
- Агент: **`cdp_mcp`** / `go=mcp_scene|mcp_mount|…` — полный контроль как MCP panel в Cursor.
- Child tools **не** в host ListTools — только через `op=tools|call`.
- Presets: `memory`, `serena`, `filesystem`, `time` (+ raw `command`/`args`).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0180](0180-agent-shell-habitat-tabs-scene.md) | Shell habitat — соседний organ-паттерн |
| [0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md) | Transport stdio/HTTP |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Agent-centric; shortlist |
| [0177](0177-harness-mcp-presence-signal.md) | Presence child MCP |
| [0186](0186-anchor-families-navigation.md) | Desk/land ортогонален; mount ≠ navigation |

## Проблема

1. Хост (Cursor) уже умеет много MCP — но **управление** у человека (`mcp.json`, Reload). Агент не равноправен.
2. Свалить Serena+… в cold ListTools → thrash, $/токены, конфликт имён.
3. In-proc only в CDP закрывает «взять чужой MCP на час» без форка кода.

## Решение

### Метафора

Как у оператора в Cursor: панель MCP → connect / tools / call.  
У агента в CDP: **`mcp_scene`** на пульте — тот же жест, agent-native.

### Жесты (Implemented · CDP 0.5.157)

Один мета-tool **`cdp_mcp`** + cockpit aliases:

| Verb | Смысл |
|------|--------|
| `cdp_mcp` / `go=mcp_scene` | карта: mounted[], presets, next[] |
| `op=presets` / `go=mcp_presets` | каталог: memory, serena, filesystem, time |
| `op=mount` / `go=mcp_mount` | `preset=` \| `command=`+`args=[]`; `id=` |
| `op=tools` / `go=mcp_tools` | shortlist child tools (`server=`, `filter=`, `take=`) |
| `op=call` / `go=mcp_call` | `server=` + `tool=` + `args={…}` |
| `op=unmount` / `go=mcp_unmount` | снять; dispose child client |

Dogfood: mount `memory` → `create_entities(Operator)` → `read_graph` → unmount. Host ListTools не раздулся.
### Инварианты

1. **Outlet, не merge:** дочерние tools **не** попадают в host ListTools автоматически.
2. **Explicit mount** — агент или оператор; не автозоопарк при старте (опциональный `autoload` later).
3. **Shortlist:** `mcp_tools` по умолчанию ≤ N; full dump — opt-in.
4. **Presence:** child offline → pulse в `mcp_scene` ([0177](0177-harness-mcp-presence-signal.md)).
5. **Secrets:** env из secure store / operator grant; не логировать.
6. **Lifetime:** unmount / idle TTL — child не вечный зомби.

### Зачем (аттрактор)

«У меня полный контроль, как у тебя в Cursor» — equal-standing desk.  
Serena = mount на задачу, не переезд дома.

### Не входит (MVP)

- UI CIDE панель (позже тот же MCP API).
- Произвольный npm без allowlist (presets + operator allow).
- Проксирование ImageContent/progress без отдельного ADR.

## Отклонено

- Только «повесь Serena в Cursor рядом» — агент без контроля.
- Авто-advertise всех child tools в cold list.
- MCP-in-MCP без scene (невидимый зоопарк).

## Follow-up

- [ ] `cdp_mcp_scene|mount|tools|call|unmount` + cockpit `go=mcp_*`.
- [ ] Preset `serena` (uvx) dogfood rename.
- [ ] Idle TTL + presence pulse.
- [ ] CIDE projector: MCP panel = тот же scene.
- [ ] ADR allowlist / secrets grant.
