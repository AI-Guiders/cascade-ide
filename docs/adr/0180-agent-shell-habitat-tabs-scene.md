# ADR 0180: Agent shell habitat (tabs / history / scene)

**Статус:** Accepted (MVP one-shot process per command)  
**Дата:** 2026-07-23  
**Tags:** #shell #terminal #agent-comfort #equal-standing #harness #adr #cascade-ide

## Резюме

У Operator есть реальный shell + ↑ + несколько вкладок. Агенту в CDP нужен тот же **habitat**: именованные tabs, системные shells (Win: pwsh/cmd; Unix: `$SHELL`), history/rerun, и **`cdp_shell_scene`** — карта всех вкладок сразу (twin к `git_scene`), без switch→watch→switch.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0002](0002-debug-human-agent-parity.md) | Human–agent parity |
| [0176](0176-agent-fs-relocate-and-harness-affordances.md) | Affordance map |
| [0178](0178-agent-scm-scene-detect-map-act.md) | Scene pattern (git) |
| [0179](0179-mcp-progress-mid-op-not-agent-unblock.md) | Progress ≠ unblock (long-run later) |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Agent comfort = product |

## Контекст

Cursor Shell / terminals folder ≠ leather REPL continuity. Simulator pitch + Kolb `kj-20260723-1312` сделали gap явным. WT / ConPTY = emulator layer позже; MVP = attach to stock shells.

## Решение

### MVP verbs (meta, always-on)

| Tool | Job |
|------|-----|
| `cdp_shell_scene` | All tabs: id, shell, cwd, state, last cmd/exit, capped preview |
| `cdp_shell_run` | One command; optional `tab` / `cwd` / `shell` / `timeout_seconds` |
| `cdp_shell_history` | Last N meta rows (no full stdout dump) |
| `cdp_shell_rerun` | By index or last (↑) |
| `cdp_shell_last` | Capped stdout/stderr of last result |
| `cdp_shell_which` | Shell kind + exe + cwd |

Default cwd: tab cwd → `session.ProjectRoot` → `session.ScmRoot` → process cwd. Caps: 8 tabs, 50 history/tab, stdout body cap.

### Execution model

One-shot process per command (cwd tracked on tab). Not a persistent ConPTY session in MVP. Parallelism = several tabs / several tool calls in one turn. Prefer `cdp_build`/`cdp_run`/`cdp_test` for session project lifecycle when they fit.

### Где жить

| Слой | Роль |
|------|------|
| `cdp-mcp/Shell/ShellHabitat.cs` | Habitat state + process spawn |
| `cdp-mcp/Program.cs` | Meta tools + dispatch |
| CSX facade (later) | `Shell.*` twin |

## Последствия

- Equal standing: ↑ + multi-tab orientation without terminals-folder scrape.
- Backlog: long-running `running` poll / ConPTY; WT profile discovery as Operator plug; CSX `ShellFacade`.

## Отклонённые альтернативы

- **Только учить агента Cursor Shell** — отклонено: continuity не в harness CDP.
- **WT feature parity в MCP** — отклонено: wrong layer; shell first.
- **Fake in-proc string runner as “shell”** — отклонено: not equal standing with real pwsh/cmd.

## Dogfood

Kolb: `kj-20260723-1312-cdp-agent-terminal-history`. Wire: cdp-mcp **0.5.76+**.
