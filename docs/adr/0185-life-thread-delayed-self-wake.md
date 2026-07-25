# ADR 0185: Life thread — delayed / event self-wake (`SendToLifeThread`)

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Обновлено:** 2026-07-25 — Time-Aware Cognition; wake landing = **Deep-Link** (не путать с CodeAnchor)  
**Tags:** #harness #autonomy #scheduler #cockpit #life-thread #time-aware #deep-link #code-anchor #equal-standing #adr #cascade-ide

## Резюме

- Ignition всегда с текстом; автор может быть **harness** ([0184](0184-harness-channel-mute-earplugs-cockpit.md)).
- Wake: **timer** или **event** (`job.done`) → enqueue → новый completion.
- **Time-Aware Cognition:** long job → sleep inference (не poll); экономия $/токенов.
- **Evidence landing:** harness готовит CDP + pulse + **`[Family:navigation;…]`** Anchor ([0186](0186-anchor-families-navigation.md)). Code-family Anchor — только если цитируем код.
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
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | **Deep links** (сообщение / surface URI) |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | **CodeAnchor** / `AttachmentAnchor` — только код-locus |
| [0186](0186-anchor-families-navigation.md) | Anchor `Family:navigation` — land wire; не Deep-Link |
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
harness: enqueue: "job finished" + Deep-Link (LandingRef)
агент:  открыть deep-link → CDP на отчёте
```

### Deep-Link ≠ CodeAnchor

| Понятие | ADR | Вопрос | В life wake |
|---------|-----|--------|-------------|
| **Deep-Link** | [0080](0080-intercom-naming-and-multi-party-channel-model.md) + этот | *Куда открыть* IDE / evidence / scene / URI | **Default** `LandingRef` |
| **CodeAnchor** | [0128](0128-intercom-attachment-anchors-and-code-references.md) | *О каком куске кода говорим* (member / range; re-resolve; attach) | Только если job вернул явный код-locus |

Смешение даёт ложный re-resolve, ложные excerpt’ы и путаницу с attach chips. CSX / PlantUML / `stop_context` / test report → **deep_link**, не CodeAnchor.

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
  landing: LandingRef    # default deep_link; code_anchor опционально
  desk: prepared | needs_restore | ok
```

`LandingRef` (sketch):

```
LandingRef =
  | { kind: "deep_link", uri: string }              # intercom://… | cdp://evidence/… | file://…#L
  | { kind: "code_anchor", anchor: AttachmentAnchor }  # только код-locus
  | { kind: "none" }
```

### Триггеры

| Trigger | Пример |
|---------|--------|
| `timer` | `seconds:5` |
| `job.done` | digest + **Deep-Link landing** + prepare |
| `presence.online` | MCP back |
| `scm.changed` | later |

### Не путать

| Механика | Роль |
|----------|------|
| Poll / noop tools | Анти-паттерн |
| Progress notify | Не пробуждение |
| Wake + full log | Отклонено |
| **Wake + prepare + Deep-Link** | Канон landing |
| CodeAnchor в wake | Опциональный attach, не замена deep-link |

### Тёплый / холодный

| Случай | Harness |
|--------|---------|
| Тёплый | Ignition + pulse + deep-link |
| Холодный | + continuity pack |
| Desk мёртв | Prepare включает [0182](0182-restore-previous-desk-dual-instance.md) |

## Последствия

- Job finish pipeline: artifact → evidence surface → **deep-link** → enqueue.
- Metrics: wake_chars ↓; time-to-report-locus ↓.
- Deep-link navigate ≠ CodeAnchor attach/reveal ([0128](0128-intercom-attachment-anchors-and-code-references.md)).

## Отклонённые альтернативы

- Poll 0.5с; noop stay-awake; полный log в wake; «проснись» без locus; **один тип «якорь» на всё**.

## Follow-up (после unpark CIDE)

- [ ] `SendToLifeThread` + timer|job.done + cancel.
- [ ] Job finish → prepare CDP + `LandingRef` (deep_link default; code_anchor opt-in).
- [ ] Wire test/build/shell → job.done.
- [ ] Warm/cold pack; caps; operator pause.
