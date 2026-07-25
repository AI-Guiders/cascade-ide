# ADR 0185: Life thread — delayed / event self-wake (`SendToLifeThread`)

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Обновлено:** 2026-07-25 — Time-Aware Cognition: wake по job-триггеру, анти-poll  
**Tags:** #harness #autonomy #scheduler #cockpit #life-thread #time-aware #equal-standing #adr #cascade-ide

## Резюме

- Автономка ≠ пустой completion: ignition всегда с текстом; автор может быть **harness** ([0184](0184-harness-channel-mute-earplugs-cockpit.md)).
- Агент ставит wake: **timer** (`seconds:5`) **или event/trigger** (`when: job.done`) → harness enqueue → новый completion.
- **Time-Aware Cognition:** тяжёлый фон (build/test/dogfood) → не poll каждые 0.5с в контексте; «разбуди когда готово / через Nс» → **гасит инференс**, экономит $/токены; wake несёт **отчёт**, не лог-спам.
- Тёплый vs холодный wake pack — см. ниже.
- CIDE на парке — канон; реализация после unpark.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0184](0184-harness-channel-mute-earplugs-cockpit.md) | Ignition vs mute |
| [0183](0183-cockpit-intercom-chat-continuity.md) | Continuity card при wake |
| [0182](0182-restore-previous-desk-dual-instance.md) | Desk при cold wake |
| [0180](0180-agent-shell-habitat-tabs-scene.md) | Background shell jobs; poll scene/last → заменить life wake |
| [0177](0177-harness-mcp-presence-signal.md) | Event-wake sibling (presence) |
| [0179](0179-mcp-progress-mid-op-not-agent-unblock.md) | Progress ≠ unblock; wake = настоящий новый ход |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Pay-per-token; caps |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Follow-up; life thread ≠ hijack без policy |

---

## Контекст

1. Tool-loop внутри хода ≠ пробуждение между ходами.
2. Anti-pattern: `cdp_test` / долгая сборка → агент спамит `poll_status` / `shell_last` каждые 0.5с → контекст забит логами, инференс горит впустую.
3. Идея (2026-07-25): «Харнесс, разбуди через 5с / когда job done» → конец хода → sleep → scheduler толкает в плечо с отчётом.

## Решение (направление)

### Time-Aware Cognition (канон сценария)

```
агент:  start heavy job (test/build/shell background)
агент:  SendToLifeThread(…, when=job.done | delay=5s)
агент:  конец хода  →  инференс гаснет
…
harness: job finished / timer
harness: enqueue completion + wake pulse + report summary
агент:  новый ход, без poll-спама в истории
```

### Контракт (черновик)

```
SendToLifeThread(
  text | structured payload,     // ignition / зачем будили
  delay_seconds?: number,        // timer wake
  when?: "timer" | "job.done" | "presence.online" | "scm.changed" | …,
  job_id?: string,               // связка с cdp_test / shell tab / build
  thread_id?: string,
  cancel_token?: string
)
```

- Кокпит: `go=life_ping` / `go=wake_when job=…`.
- Wake: messages = history (warm) + ignition + **pulse** (reason, report digest, desk).

### Триггеры wake (не только self-delay)

| Trigger | Пример |
|---------|--------|
| `timer` | `seconds:5` — «толкни через 5с» |
| `job.done` | test/build/shell background finished → отчёт в pulse |
| `presence.online` | MCP снова жив ([0177](0177-harness-mcp-presence-signal.md)) |
| `scm.changed` | опционально later |

### Не путать

| Механика | Роль |
|----------|------|
| Tool-loop | Пока в *этом* ответе tool_calls |
| Poll loop в контексте | **Анти-паттерн** для long job |
| **Life thread wake** | Между ходами: timer/event → новый completion + digest |
| `notifications/progress` ([0179](0179-mcp-progress-mid-op-not-agent-unblock.md)) | UI/side-channel; **не** замена wake |

### Политика / caps

- Max delay, max pending, budget $/tokens.
- Оператор: pause / cancel life thread.
- Job wake: в pulse — **summary** (exit, failed count, path to full log), не полный stdout в messages.
- Integrity: не обходить POST failed.

### Что получает модель на wake

| Случай | История | Harness |
|--------|---------|---------|
| Тёплый thread | Да (окно/compact) | Ignition + тонкий pulse |
| Холодный / compact | Нет / дыры | Continuity pack обязателен |
| Desk умер | Чат ≠ desk | restore hint / auto desk restore ([0182](0182-restore-previous-desk-dual-instance.md)) |

```text
life_wake:
  reason: timer|job.done|presence|scm|manual
  scheduled_text: <payload агента>
  job: { id, status, exit_code, summary_ref }   # если job trigger
  last_closed_task / planned_next: …
  desk: ok|needs_restore
  continuity_card_ref: …   # cold
```

Между ходами контекст = только `messages[]`, не «память весов».

## Последствия

- CIDE scheduler + job registry (test/build/shell) → wake hooks.
- Dogfood metric: poll_calls_per_long_job ↓; cost_per_wait ↓.
- Cursor: не обещать; CIDE — канон.

## Отклонённые альтернативы

- Poll каждые 0.5с в tool-loop — отклонено как канон ожидания.
- Вечные noop tools «чтобы не уснуть» — костыль.
- Progress notification как единственный «проснись» — нет ([0179](0179-mcp-progress-mid-op-not-agent-unblock.md)).
- Пихать полный job log в wake messages — token tax; digest + ref.

## Follow-up (после unpark CIDE)

- [ ] `SendToLifeThread` + `when=timer|job.done` MVP + cancel.
- [ ] Wire `cdp_test` / `cdp_build` / `cdp_shell` background → job.done wake.
- [ ] Warm vs cold wake pack; caps; operator pause.
- [ ] Presence/scm as sibling enqueues.
