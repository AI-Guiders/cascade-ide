# ADR 0192: Agent Task Manager (Plan Mode in CDP)

**Статус:** Accepted · Implemented (CDP **0.5.169–0.5.172**)  
**Дата:** 2026-07-25  
**Tags:** #cdp #task-manager #plan #witdb #agent-ide #adr #cascade-ide

## Резюме

- **Plan** = desk organ (canonical pin `plan`; aliases `work`|`tasks`|`tm`|`feature`|`task` → pin `plan`).
- **Feature** = Intent, **Task** = Stage (`ParentId` tree) — storage in `intent-workspace.witdb`.
- **Sticky focus** = table `work_focus` (active Intent + Stage) — survives MCP remount like `desk_seats`.
- Board: `view.banner` + tree (`*` feature, `[>]` active, `[x]` done).
- Not Cursor TodoWrite — model-first Plan Mode inside agent IDE.

## Terminology (agent surface)

| Word | Meaning | Storage |
|------|---------|---------|
| Plan | Organ / board | seat pin `plan` |
| Feature | Epic / intent | `Intent` |
| Task | Stage node (nestable) | `Stage` + `ParentId` |

Internal tool name `cdp_work` stays (escape hatch for intent_*/stage_*); desk vocabulary is Plan/Feature/Task.

## REPL

```
feature desk-comfort
task ship-omit
task under ship-omit verify
focus ship-omit
done
park
drop task ship-omit
drop feature junk
plan
```

- `feature <title>` / `task <title>` dedupe by title (+ parent for tasks).
- Nested: `task under <parentTitle> <childTitle>`.
- Cleanup: `drop task|feature <title>` (cascade children on task).

## Restore

Remount hydrates `work_focus` → cockpit `next` / seat `plan` shows `Feature › Task`.  
Legacy sticky pin `work` canonicalizes to `plan` on load.  
`cdp_restore` still restores project+buffers; plan focus is independent WitDB.

## Related

- [0191](0191-scan-pattern-seats-desk-repl.md) seats + view
- IntentWorkspace Intent/Stage entities
