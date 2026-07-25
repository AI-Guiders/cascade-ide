# ADR 0191: Scan Pattern seats + desk REPL + view

**Статус:** Accepted · Implemented (CDP 0.5.164–0.5.167; **0.5.168 view-once + hard-deploy auto-nudge**)  
**Дата:** 2026-07-25  
**Tags:** #cdp #cockpit #seats #scan-pattern #repl #desk-view #witdb #habitat #equal-standing #agent-ide #adr #cascade-ide

## Резюме

- Desk = **фиксированные сиденья** `(P)(Forward)(M)` — материализация Scan Pattern ([0021](0021-pfd-mfd-cockpit-attention-model.md) §7), не append-тайлы.
- `go=` / REPL `cmd=` **replace-in-seat** по политике organ→seat (настраивается в Options).
- Одна сцена: агент **рулит**, не бегает по вкладкам и не копит `|Code|Browser|Git|`.
- **Desk view (0.5.165):** `view.banner` / `view.board` / `view.ascii` — одна композиция поверх seats; `slots[]` one-liners; `panes[]` только по `seats_detail=full` / `pane_full=`.
- **Persist (0.5.166):** seats sticky in **WitDB** `desk_seats` (same store as open_recent) — survive MCP remount; no parallel JSON habitat.
- **Slim + quiet (0.5.167):** default `desk_detail=slim` omits fat `loci[]` / `go_verbs[]` (`desk_detail=nav|full` on demand); organs that need project synthesize quiet pulse (no Application Data thrash); board lines humanized (`0 buf`, `need cdp_open`).
- **View-once + remount (0.5.168):** root `view` only — no `seats.view` / `tiles.view` dup; `publish-and-deploy.ps1 -Mode hard` auto-bumps `CDP_RELOAD_NUDGE` (unless `-NoNudgeMcp`).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Scan Pattern / PFD·Forward·MFD |
| [0039](0039-workspace-navigation-affordances.md) | Canon `(PFD)(Forward)(MFD)` |
| [0171](0171-presentation-tiers-compact-vs-cockpit.md) | P→F→M invariant |
| [0189](0189-cockpit-tile-manager.md) | Параллельные panes; seats supersede append model |
| [0190](0190-agent-ide-settings-organ.md) | `desk.mode` / `desk.seat.*` |

## Проблема

Tile manager (0189) дал параллельные пульсы, но **append** ломал ожидание места. После seats сырой JSON трёх pane-blob’ов всё ещё ломал **форму экрана**: место есть, поверхности нет.

## Решение

### Seats

| Seat | Роль | Default organs |
|------|------|----------------|
| `p` | Instruments / where-am-I | `project_scene`, `work`, `quality`, `debug` |
| `forward` | Main work | `editor_scene`, buffer, sniper, script |
| `m` | Secondary contour | `browser`, `git`, `shell`, `mcp`, `options`, `correspondence`, `test` |

| Param | Смысл |
|-------|--------|
| `layout=cockpit` | Fill P+F+M preset |
| `go=browser` / `cmd="go browser"` | Place in **M** (replace) |
| `seat=m organ=git` | Explicit place |
| `seats_detail=compact\|full` | View+slots vs include panes |
| `pane_full=` | One seat full dump (+ panes) |
| `desk_detail=slim\|nav\|full` | Slim default: omit loci/go_verbs; nav expands catalog |
| `desk.mode=tiles` | Legacy 0189 append |

### Desk view

```
| P:project | F:editor | M:git |
P  project    · …
F  editor     · …
M  git        · …
┌─P──┬─Forward──┬─M──┐
```

Не Avalonia — ASCII/JSON composition как экран для агента. Top-level `view` once (0.5.168); `seats.slots` without nested view.

### REPL

`cmd=` / `line=` / `repl=`: `go browser`, `layout cockpit`, `seat m git`, `clear`, `help`.

## Инварианты

1. Seats = Scan Pattern enforcement.
2. Replace-in-seat, не append (`mode=seats`).
3. **View = первичное чтение; panes — drill.**
4. Cockpit = пульт; органы — отдельные tools.
5. Без Avalonia на этом шаге.

## Dogfood

`layout=cockpit` → читать `view.banner` → `cmd="seat m git"` → M=git, Forward тот же → `pane_full=m` при нужде.
