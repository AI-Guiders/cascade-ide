# ADR 0187: CDP `mcp_scene` — агент монтирует MCP как оператор в Cursor

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Tags:** #cdp #mcp #habitat #equal-standing #agent-comfort #serena #outlet #adr #cascade-ide

## Резюме

- CDP — не только in-proc backends, а **розетка / habitat для других MCP**.
- Агент получает **`mcp_scene`** (как `git_scene` / `shell_scene`): список, mount, tools shortlist, call, unmount — **полный контроль**, зеркало того, что человек делает в Cursor MCP panel.
- Cold ListTools хоста **не** раздувается: дочерние tools живут за сценой, наружу — короткие `cdp_mcp_*` / `go=mcp_*`.
- Equal-standing: «я подключаю Serena на rename, потом снимаю» — без просьбы человека править `mcp.json`.

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

### Жесты (черновик)

| Verb | Смысл |
|------|--------|
| `cdp_mcp_scene` / `go=mcp_scene` | карта: mounted[], health, transport, tool_count |
| `cdp_mcp_mount` | подключить: preset \| command+args \| url; id= |
| `cdp_mcp_tools` | shortlist/peek tools дочернего (filter, limit) |
| `cdp_mcp_call` | `server=` + `tool=` + args |
| `cdp_mcp_unmount` | снять; kill child process |
| `cdp_mcp_presets` | serena, context7, … (кураторский каталог) |

Wire sketch:

```text
mcp_scene =
  { servers: [{ id, kind: preset|stdio|http, status, tools_hint }],
    next: [mount, tools, call, unmount] }

mount =
  { id: "serena",
    preset: "serena" | { command, args, env?, cwd? } | { url } }
```

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
