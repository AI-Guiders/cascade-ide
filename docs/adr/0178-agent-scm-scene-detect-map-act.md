# ADR 0178: Agent SCM scene (detect / map / act)

**Статус:** Proposed (MVP detect+map shipped as `git_scene`)  
**Дата:** 2026-07-23  
**Tags:** #git #scm #agent-comfort #equal-standing #harness #adr #cascade-ide

## Резюме

У людишек IDE держит Source Control: dirty tree, walk по сабмодулям, message+commit, Push по команде. Агенту нужны не только pull-тулы `git_status`/`commit`/`push`, а **SCM scene** — компактная карта workspace без monorepo dump в чат. Twin к [0177](0177-harness-mcp-presence-signal.md) (presence); усиление [0176](0176-agent-fs-relocate-and-harness-affordances.md) **A6+**.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Shared Git Core / git-mcp |
| [0002](0002-debug-human-agent-parity.md) | Human–agent parity |
| [0176](0176-agent-fs-relocate-and-harness-affordances.md) | Affordance map; A6 native git |
| [0177](0177-harness-mcp-presence-signal.md) | Mid-turn presence pattern |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Agent comfort = product |

## Контекст

Dogfood: «закоммить и запушь везде» = N× shell status по parent → open → cascade-ide → agent-notes + submodule pointer bumps. `git_*` MCP уже есть, но это **CLI wrappers**, не сцена как у человека.

## Решение

### Слои

1. **Detect (MVP):** tool `git_scene` → JSON `git_scene/v0`: per-root `dirty` + counts (staged/unstaged/untracked), `ahead`/`behind`, submodule map (`flag`, `pointer_moved`, optional dirty probe). **Без** списка путей по умолчанию.
2. **Navigate:** `roots[]` для multi-root; submodule paths = enter child (`workspace_path` = child) для deeper `git_diff`/`commit`.
3. **Act (позже / policy):** agent даёт message + scope; host stage/push — без ослабления «только по явному запросу» / no force-push. Не путать с worktree plan overlay (ScriptableIde).
4. **Presence (позже):** push `scm.changed` mid-turn — как 0177; Cursor hosts могут остаться poll-only.

### Контракт MVP

- Wire: `git_scene` (git-mcp) / `git_git_scene` (CDP) / CSX `Git.SceneAsync` (ScriptableIde).
- Wire: `git_diff_scene` / `git_git_diff_scene` / `Git.DiffSceneAsync` — list (files+numstat) → `path=` hunks; prefer over raw `git_diff` dump.
- Prefer `git_scene` перед полным `git_status` для ориентации.
- Cap: `max_roots`, `max_submodules`; diff: `max_files`, `max_hunks`, `max_hunk_lines`.
- Act thin: `Git.CommitAsync` / `Git.PushAsync` на фасаде — без ослабления «только по явному запросу».

### Где жить

| Слой | Роль |
|------|------|
| `GitMcp.Core` (`GitScene`) | Parse + argv |
| git-mcp `ToolHandlers` | Run git + JSON |
| CDP `Wave1AffordanceSeed` | Shortlist rank |
| CIDE Ide Git panel (later) | Shared scene with human UI |

## Последствия

- Меньше token tax на «везде где надо».
- Бэклог: `scm.changed` notification; logical-slice stage helper; IdeCommand parity.

## Отклонённые альтернативы

- **Только учить агента scoped `git_status`** — отклонено: tax остаётся на модели.
- **Полный porcelain в scene** — отклонено: снова dump.
- **Auto-commit без message** — отклонено: policy / equal standing не про silent ship.

## Dogfood

Kolb: `kj-20260723-0108-scm-presence-like-human-ide`.
