# ADR 0180: Agent shell habitat (tabs / history / scene)

**Статус:** Accepted (one-shot + background long-run)  
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
| [0179](0179-mcp-progress-mid-op-not-agent-unblock.md) | Progress ≠ unblock; long-run uses poll scene/last |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Agent comfort = product |

## Контекст

Cursor Shell / terminals folder ≠ leather REPL continuity. Simulator pitch + Kolb `kj-20260723-1312` сделали gap явным. WT / ConPTY = emulator layer позже; MVP = attach to stock shells.

## Решение

### Verbs (meta, always-on)

| Tool | Job |
|------|-----|
| `cdp_shell_scene` | All tabs: id, shell, cwd, state, pid, last cmd/exit, capped preview |
| `cdp_shell_run` | One command; optional `tab` / `cwd` / `shell` / `timeout_seconds` / **`background`** |
| `cdp_shell_history` | Last N meta rows (no full stdout dump) |
| `cdp_shell_rerun` | By index or last (↑); optional `background` |
| `cdp_shell_last` | Capped body; **while running → live buffers** |
| `cdp_shell_which` | Shell kind + exe + cwd + pid/state |
| `cdp_shell_kill` | Kill process tree on tab |
| `cdp_shell_close` | Close tab (kills if needed); frees slot |

Default cwd: tab cwd → `session.ProjectRoot` → `session.ScmRoot` → process cwd. Caps: 8 tabs, 50 history/tab, stdout body / live buffer caps.

### Execution model

- **Foreground (default):** one-shot process; timeout; history append.
- **Background (`background=true`):** return immediately with `pid`; tab stays `running`; poll `cdp_shell_scene` / `cdp_shell_last`; stop with `cdp_shell_kill`. Not ConPTY — still one process per command, but long-lived.
- Parallelism = several tabs / several tool calls in one turn.
- Prefer `cdp_build`/`cdp_run`/`cdp_test` for session project lifecycle when they fit; shell for the rest of the world.
- When CDP MCP is up, prefer `cdp_shell_*` over Cursor Shell tool.
- **Deploy exception:** `publish-and-deploy.ps1` (KillRunning) must run from an **external** terminal — not `cdp_shell_*` (cannot kill own MCP process tree). Reload MCP after.

### Где жить

| Слой | Роль |
|------|------|
| `cdp-mcp/Shell/ShellHabitat.cs` | Habitat state + process spawn |
| `cdp-mcp/Program.cs` | Meta tools + dispatch |
| Cursor rule `cdp-shell-habitat.mdc` | Agent habit |
| CSX facade (later) | `Shell.*` twin |

## Последствия

- Equal standing: ↑ + multi-tab + long-run poll without terminals-folder scrape.
- Backlog: ConPTY / persistent REPL session; WT profile discovery as Operator plug; CSX `ShellFacade`.

## Отклонённые альтернативы

- **Только учить агента Cursor Shell** — отклонено: continuity не в harness CDP.
- **WT feature parity в MCP** — отклонено: wrong layer; shell first.
- **Fake in-proc string runner as “shell”** — отклонено: not equal standing with real pwsh/cmd.

## Dogfood

Kolb: `kj-20260723-1312-cdp-agent-terminal-history`. Wire: cdp-mcp **0.5.77+**.
