# ADR 0193: Agent attention channels + CCL

**Статус:** Accepted · Shipped (CDP **0.5.176**)  
**Дата:** 2026-07-25  
**Tags:** #cdp #attention #ccl #script #report #alert #world #agent-ide #adr #cascade-ide

## Резюме

- Агентский кокпит — **каналы внимания**, не HDMI. Человеческие P|F|M ([0191](0191-scan-pattern-seats-desk-repl.md)) — optional projection.
- Каналы MLP: **sit** | **work** | **probe** | **report** | **world** | **alert**.
- **CCL** = Cockpit Command Line — канон ([0138](0138-cockpit-command-line-and-parametric-ranges.md)); в CDP это desk `cmd=` (schema `ccl/v1`). **CCC** = help alias.
- Probe = CSX habitat ([ScriptScene](../../../cdp-mcp/ScriptScene.cs)); report = soft organ [`IdeReportBoard`](../../../cdp-mcp/IdeReportBoard.cs) с persist тела last run (не только ok/path).

## Comfort (0.5.174)

- Cold `cdp_cockpit` без project → **auto desk bookmark restore** once/process (`no_restore=` to skip).
- Empty report → `ok:true` pulse `report · idle` (не `!`); sticky idle report on P → flip to **plan**.
- Last-run pulse/board/body capped → WitDB `script_last_run` (survives remount).

## Alert / EICAS-lite (0.5.175)

- Soft organ `go=alert` | `eicas` → [`IdeAlertChannel`](../../../cdp-mcp/IdeAlertChannel.cs).
- Aggregates: quality FAIL/WARN + disk drift + DAP stopped.
- Slim desk always carries `alert:{pulse,level,ok}`; `next[]` elevates `go=alert` when not clear.
- Clear pulse: `alert · clear` (не шум).

## World without thrash (0.5.176)

- World organs (`git` / `shell` / `browser` / `mcp`) **replace on M** — policy unchanged.
- Critical thrash was: every slim desk pulse **re-dispatched** full organ tools (git up to 3× on `go=git`).
- Fix: cockpit collects snaps once; seat pulse + scene-only `go=` reuse [`IdeWorldChannel`](../../../cdp-mcp/IdeWorldChannel.cs) (`world:true`); `pane_full=` / `go_detail=full` still dumps.
- Also: editor/script seat pulse from buffer/script snaps (no scene dispatch); `OrganNeedsProject` covers shell+mcp (quiet until `cdp_open`).
- Schema: `cockpit/v1.15`.

## Каналы

| Id | Роль | Орган / поверхность |
|----|------|---------------------|
| sit | Situation | plan pulse + session; slim |
| work | Mutate | editor_scene / buffer / sniper |
| probe | Script | script_scene (seat M by policy) |
| report | Evidence board | soft `go=report` → P |
| world | Outer | git / shell / browser / mcp (replace, snap pulse) |
| alert | EICAS-lite | soft `go=alert` + slim `alert` pulse + next |

## Projection (совместимость)

Layout `agent`: P=plan (sit), F=editor (work), M=script (probe).  
`go=report` / после run — report board (P replace или `pane_full=report`).

## CCL

- Wire: `cmd=` → IdeRepl (schema `ccl/v1`).
- Verbs: feature/task/plan (0192) + probe/script/run/check/report.
- Не второй REPL; не NL→CSX codegen в MLP.

## Related

- [0021](0021-pfd-mfd-cockpit-attention-model.md) Scan Pattern
- [0138](0138-cockpit-command-line-and-parametric-ranges.md) CCL
- [0191](0191-scan-pattern-seats-desk-repl.md) seats
- [0192](0192-agent-task-manager-plan-mode.md) Plan/Feature/Task
