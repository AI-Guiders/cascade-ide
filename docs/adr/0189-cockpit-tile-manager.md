# ADR 0189: Cockpit as tile manager

**Статус:** Accepted · Implemented (CDP 0.5.160)  
**Дата:** 2026-07-25  
**Tags:** #cdp #cockpit #tiles #habitat #equal-standing #agent-ide #adr #cascade-ide

## Резюме

- **`cdp_cockpit`** становится тайловым менеджером: 2–3 органа в одном round-trip (`tiles.panes[]`).
- Не новый tool — расширение пульта (reuse loci + `go=` dispatcher).
- Человеческий жест «код слева, браузер справа» → `layout=code+net` / `pins=editor,browser`.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Cockpit attention / MFD |
| [0180](0180-agent-shell-habitat-tabs-scene.md) | Shell organ |
| [0188](0188-agent-internet-browser-lynx-scene.md) | Browser organ → locus `browser:net` |
| [0182](0182-restore-previous-desk-dual-instance.md) | Desk continuity |

## Проблема

Агент опрашивает сцены по одной (`go=editor` → `go=browser`) — tax на контекст и ритм. У человека тайлы параллельны.

## Решение

### Wire

| Param | Смысл |
|-------|--------|
| `layout=` | preset: `code+net`, `code+shell`, `code+git`, `net+shell`, `desk` (sticky) |
| `pins=` / `tiles=` | явный список (`editor,browser,shell`) — sticky |
| `pin=` | добавить в sticky |
| `pin_clear=` | сбросить |
| `pane_full=` | один pin с `go_detail=full` |
| `go=` | по-прежнему один organ drill |

Ответ: `tiles: { layout, pins, panes:[{ pin, full, pane }] }` + `loci[]` (включая `browser:net`) + `layouts[]`.

### Инварианты

1. Cockpit остаётся **пультом**, не монолитом органов.
2. По умолчанию panes = **pulse**; full только явно.
3. ≤ 4 tiles.
4. MFD (`nav|sys|chk`) ортогонален tiles.

## Отклонённые альтернативы

- **Новый `cdp_tiles`** — лишнее существительное; loci уже плитки.
- **Всегда full dump всех panes** — раздувает контекст.

## Dogfood

`cdp_cockpit layout=code+net` → panes editor + browser pulses; `pane_full=browser` при чтении страницы.

**Follow-up:** append tiles → Scan Pattern seats — [0191](0191-scan-pattern-seats-desk-repl.md) (CDP 0.5.164). `desk.mode=tiles` keeps this ADR's model.
