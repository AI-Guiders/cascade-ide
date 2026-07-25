# ADR 0185: Life thread — delayed / event self-wake (`SendToLifeThread`)

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Обновлено:** 2026-07-25 — Time-Aware Cognition; wake = pulse + CodeAnchor на подготовленный CDP  
**Tags:** #harness #autonomy #scheduler #cockpit #life-thread #time-aware #code-anchor #equal-standing #adr #cascade-ide

## Резюме

- Ignition всегда с текстом; автор может быть **harness** ([0184](0184-harness-channel-mute-earplugs-cockpit.md)).
- Wake: **timer** или **event** (`job.done`) → enqueue → новый completion.
- **Time-Aware Cognition:** long job → sleep inference (не poll); экономия $/токенов.
- **Evidence landing:** на finish harness **готовит CDP** (restore desk при нужде) и будит коротким `job finished` + **CodeAnchor / deep-link** на отчёт — не простыня логов. Агент тыкает якорь → уже на нужной странице.
- CIDE на парке — канон.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0184](0184-harness-channel-mute-earplugs-cockpit.md) | Ignition vs mute |
| [0183](0183-cockpit-intercom-chat-continuity.md) | Continuity card |
| [0182](0182-restore-previous-desk-dual-instance.md) | Desk prepare / restore |
| [0180](0180-agent-shell-habitat-tabs-scene.md) | Background jobs |
| [0177](0177-harness-mcp-presence-signal.md) | Presence wake |
| [0179](0179-mcp-progress-mid-op-not-agent-unblock.md) | Progress ≠ wake |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | CodeAnchor / AttachmentAnchor |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Token economics |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Follow-up |

---

## Контекст

Poll long job и dump полного лога в messages — оба tax. Нужно: уснуть → проснуться по триггеру → **попасть в готовый locus**, не читать роман в чате.

## Решение (направление)

### Time-Aware Cognition + evidence landing

```
агент:  start heavy job
агент:  SendToLifeThread(when=job.done)
агент:  конец хода → инференс гаснет
…
harness: job finished
harness: prepare CDP (desk restore?; evidence/report surface ready)
harness: enqueue: "job finished" + CodeAnchor/deep-link
агент:  тык якорь → CDP на отчёте
```

### Контракт (черновик)

```
SendToLifeThread(
  text | structured payload,
  delay_seconds?: number,
  when?: "timer" | "job.done" | "presence.online" | "scm.changed" | …,
  job_id?: string,
  thread_id?: string,
  cancel_token?: string
)
```

Wake pulse (минимум):

```text
life_wake:
  reason: job.done
  job: { id, status, exit_code }
  summary: "12 passed, 1 failed"
  details: <CodeAnchor|deep_link>   # готовая страница в CDP
  desk: prepared | needs_restore | ok
```

### Триггеры

| Trigger | Пример |
|---------|--------|
| `timer` | `seconds:5` |
| `job.done` | digest + **details anchor** + prepare |
| `presence.online` | MCP back |
| `scm.changed` | later |

### Не путать

| Механика | Роль |
|----------|------|
| Poll / noop tools | Анти-паттерн |
| Progress notify | Не пробуждение |
| Wake + full log | Отклонено |
| **Wake + prepare + CodeAnchor** | Канон |

### Тёплый / холодный

| Случай | Harness |
|--------|---------|
| Тёплый | Ignition + pulse + anchor |
| Холодный | + continuity pack |
| Desk мёртв | Prepare включает [0182](0182-restore-previous-desk-dual-instance.md) |

## Последствия

- Job finish pipeline: artifact → evidence surface → anchor → enqueue.
- Metrics: wake_chars ↓; time-to-report-locus ↓.
- Якорь = тот же жест, что CodeAnchor в обычной работе ([0128](0128-intercom-attachment-anchors-and-code-references.md)).

## Отклонённые альтернативы

- Poll 0.5с; noop stay-awake; полный log в wake; «проснись» без locus.

## Follow-up (после unpark CIDE)

- [ ] `SendToLifeThread` + timer|job.done + cancel.
- [ ] Job finish → prepare CDP + CodeAnchor in wake.
- [ ] Wire test/build/shell → job.done.
- [ ] Warm/cold pack; caps; operator pause.
