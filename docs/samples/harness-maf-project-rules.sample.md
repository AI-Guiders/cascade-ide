# Harness MAF project rules (sample)

Copy to workspace: `.cascade-ide/maf-project-rules.md`  
Or merge fragments from `docs/samples/harness-maf-project-rules.sample.md`.

## Checkpoint (automation-first)

При **≥40 user turns**, **ADCM context pressure**, смене epic или конце блока работы — **без ожидания оператора**:

1. `chat_export_readable` (или export JSONL в Cursor)
2. Краткое резюме решений и open items
3. Согласование с пользователем

Ответ «пропустить» — уважать. Silent host summary **не** считать checkpoint.

## L0 / KB

На старте сессии hot уже в контексте (`read_hot_context` in-proc). При смене темы — обновление автоматически.  
Глубина: `read_knowledge_file` / hub route, не дублировать KB в чат.

## Verify epoch (AEE)

После правок `.cs` verify может стартовать автоматически (`[agent.harness]`).  
**Green diagnostics = текущий verify epoch.** Перед «готово» — `ide_agent_status`; если `verify_epoch_ui_stale: true` — verify снова.

## Threading

Epic / spike / meta — разные topics. Spike ≤ одна сессия → новый topic с 3-line brief.
