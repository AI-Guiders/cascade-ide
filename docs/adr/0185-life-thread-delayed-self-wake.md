# ADR 0185: Life thread — delayed self-wake (`SendToLifeThread`)

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Tags:** #harness #autonomy #scheduler #cockpit #life-thread #equal-standing #adr #cascade-ide

## Резюме

- Автономка ≠ пустой completion: ignition всегда с текстом; автор может быть **harness**, не оператор ([0184](0184-harness-channel-mute-earplugs-cockpit.md) слои).
- Агент (кокпит / конец хода) ставит **`SendToLifeThread(payload, delay?)`**: через N секунд harness **enqueue** turn с этим payload → модель снова в completion.
- Оператор может уйти на час; жизнь потока — на стороне CIDE scheduler, не «вечный tool-loop» (тот — внутренний цикл *внутри* хода).
- CIDE на парке — канон; реализация после unpark.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0184](0184-harness-channel-mute-earplugs-cockpit.md) | Ignition vs mute; completion всегда с автором |
| [0183](0183-cockpit-intercom-chat-continuity.md) | Continuity card при wake |
| [0182](0182-restore-previous-desk-dual-instance.md) | Desk при cold wake |
| [0177](0177-harness-mcp-presence-signal.md) | Event-wake sibling (presence → enqueue) |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Comfort; pay-per-token — caps обязательны |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Steer / follow-up; life thread ≠ silent hijack без policy |

---

## Контекст

Dogfood (2026-07-25): tool-loop «пока зовут тулы» = норма *внутри* хода; костыль = им подменять пробуждение. Нужен явный **scheduler wake**: агент говорит harness «через 5с верни мне эту строку как ignition».

## Решение (направление)

### Контракт (черновик)

```
SendToLifeThread(
  text | structured payload,   // то, что ляжет в messages как ignition
  delay_seconds?: number,      // 0 = ASAP после текущего хода
  thread_id?: string,          // workline / life thread (default: current)
  cancel_token?: string        // отмена оператором / агентом
)
```

- Кокпит: `go=life_ping text=… seconds=5` или тул до конца хода.
- По истечении delay: harness собирает messages (история thread + synthetic/user/system с payload) → **POST chat/completions** → обычный agent turn.
- Оператор не печатал — ок: автор ignition = harness/agent schedule.

### Не путать

| Механика | Роль |
|----------|------|
| Tool-loop | Пока в *этом* ответе есть tool_calls |
| **Life thread** | *Между* ходами: timer/event → новый completion |
| Follow-up очередь ([0116](0116-intercom-session-tree-and-agent-message-steering.md)) | Сосед; life thread = self-authored delayed follow-up |

### Политика / caps (обязательно)

- Max delay, max pending pings per thread, budget (tokens/$).
- Оператор: pause life thread / cancel all / «не будить пока меня нет».
- Integrity / safety: life thread не обходит POST failed.
- Wake может нести desk restore hint ([0182](0182-restore-previous-desk-dual-instance.md)) + continuity card ([0183](0183-cockpit-intercom-chat-continuity.md)).

### Что получает модель на Self-Ignition

Completions **всегда** с `messages[]`. Вопрос: откуда берётся «прошлый ход».

| Случай | История ходов в API | Harness обязан |
|--------|---------------------|----------------|
| **Тёплый thread** (тот же Intercom/workline, история в host store) | Да (с учётом окна / compact) | Ignition-строка (`тра-та-та`) + **тонкий wake pulse** (см. ниже). Не дублировать всю историю вручную. |
| **Холодный / новый process / после compact / Partition** | Нет или сильно урезана | Ignition + **обязательный continuity pack** — иначе модель проснётся амнезией |
| **Desk умер** (MCP kill между ping и wake) | История чата ≠ desk | `cdp_restore` / hint «Call restore previous» + desk bookmark ([0182](0182-restore-previous-desk-dual-instance.md)) |

**Автоматический wake pulse (рекомендуемый минимум от harness, не от «памяти модели»):**

```text
life_wake:
  reason: timer|presence|scm|manual
  scheduled_text: <payload агента>
  last_closed_task: …    # если есть task plane
  planned_next: …        # короткий хвост плана
  desk: ok|needs_restore
  continuity_card_ref: … # 0175 A/B, если cold
```

- Тёплый thread: pulse **короткий** (не замена history) — зачем разбудила + что с desk/MCP.
- Холодный: pulse + continuity card / handoff; опционально auto-`restore` desk перед completion.
- Не полагаться на «у модели в весах останется прошлый ход» — между ходами контекст только то, что harness кладёт в `messages`.

## Последствия

- CIDE: scheduler service + IdeCommand/MCP verb; UI «life thread armed».
- Wake path ветвится: warm vs cold (detect session store / compaction / MCP desk).
- Cursor: ограниченно (чужой host); не обещать тот же API.
- Telemetry: scheduled_wake vs human_ignition; cold_wake_without_card = anti-pattern.

## Отклонённые альтернативы

- Вечные noop tool_calls чтобы «не уснуть» — костыль; отклонено как канон.
- Пустой completion без messages — невозможно по API.
- Только «напиши себе в чат глазами оператора» — ломает уход оператора на час.
- «Модель сама вспомнит без messages» — нет; только то, что в API.
- На каждом warm wake пихать полный transcript заново — token tax; history уже в thread store.

## Follow-up (после unpark CIDE)

- [ ] `SendToLifeThread` / `go=life_ping` MVP + cancel.
- [ ] Caps + operator pause.
- [ ] Warm vs cold wake pack (pulse + optional auto desk restore).
- [ ] Wire presence/scm events as sibling enqueues (не только self-delay).
