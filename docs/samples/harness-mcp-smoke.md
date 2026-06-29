# Harness smoke via MCP (CIDE)

Когда CIDE запущен с in-proc IDE MCP (`ide_execute_command`) — **да**, smoke и мониторинг harness делаются через MCP.  
**Нет** через MCP: правка `[agent.harness]` — только `%LocalAppData%\CascadeIDE\settings.toml` или `Setup-CideHarness.ps1 -Apply`.

## Prerequisites

- `agent_notes.config_path` в settings (тот же TOML, что Cursor `--config`)
- Cloud FM или Ollama для MAF-чата (smoke L0 не требует FM)
- `[agent.harness]` из overlay (L0, checkpoint, auto-verify)

## Commands (ide_execute_command)

| Command | Проверяет |
|---------|-----------|
| `read_hot_context` | L0 in-proc (P0.1) |
| `ide_agent_status` | `harness_*`, `verify_epoch_ui_stale` (P2.3) |
| `ide_agent_verify` | AEE ladder после `.cs` edit (P0.4) |
| `chat_export_readable` | checkpoint export path |

### ide_agent_status (ожидаемые поля)

```json
{
  "harness_session_user_turn_count": 0,
  "harness_checkpoint_due": false,
  "harness_next_checkpoint_at_turn": 40,
  "harness_hot_context_loaded": true,
  "verify_epoch_ui_stale": false
}
```

## UI-only (не MCP)

| Поведение | Где видно |
|-----------|-----------|
| Checkpoint @40 user turns | сообщение `[harness checkpoint · …]` в Intercom |
| preCompact @60 msgs/topic | `[harness preCompact · …]` |
| `/topic create` brief | шаблон в поле ввода |
| Auto-verify после `.cs` | AEE панель / `ide_agent_status.active` |

## Cursor ACP vs MAF in-proc

При `suppress_acp_ide_stdio_inject = true` (default) Cursor ACP **не** поднимает второй `CascadeIDE.exe`.  
Агент в MAF/cloud режиме вызывает те же команды in-proc — **это и есть «режим MCP»** без stdio-дубликата.

Полный loopback HTTP ([ADR 0082](../adr/0082-acp-ide-mcp-loopback-single-process.md)) — backlog; interim = suppress + in-proc.

## Agent rules

Скопируй [harness-maf-project-rules.sample.md](harness-maf-project-rules.sample.md) → `.cascade-ide/maf-project-rules.md` в workspace.
