# ADR 0183: Cockpit Intercom + chat continuity (агентский канал, не Cursor-клон)

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Tags:** #intercom #cockpit #continuity #harness #agent-comfort #equal-standing #adr #cascade-ide #cdp

## Резюме

- В **CIDE** (наше) агенту нужен **свой Intercom в кокпите**: continuity «о чём мы», не клон Cursor chat UI.
- Default **тихий**: pulse + last intent/handover; полная лента — по запросу.
- **Вкл/выкл** и статусы workline обязательны («ничего о нас без нас»).
- Связка с [0182](0182-restore-previous-desk-dual-instance.md): Restore Previous = desk; этот ADR = **chat/continuity plane**.
- Опирается на [0175](0175-adcm-partition-continuity-pair-and-message-anchors.md) (TopicDecisions + Handover) и [0045](0045-agent-chat-persistence-event-log-and-projections.md).
- CIDE сейчас на парке — канон направления; реализация позже.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0182](0182-restore-previous-desk-dual-instance.md) | Desk restore; ортогонально chat |
| [0175](0175-adcm-partition-continuity-pair-and-message-anchors.md) | Continuity pair A/B + anchors |
| [0173](0173-intercom-intent-card-session-decision-capture.md) | Intent card в workline |
| [0172](0172-conversation-first-habitat.md) | Intercom = рабочая память сессии |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Event log / message_id |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Stakeholder; comfort |
| [0177](0177-harness-mcp-presence-signal.md) | Online → можно предложить restore desk+card |
| [0127](0127-intercom-spine-and-topic-tabs-chrome-navigation.md) | Spine/tabs chrome (человек); здесь — агентский срез в cockpit |

---

## Контекст

1. После hard deploy desk уже поднимается (`cdp_restore` / 0182). «О чём спорили» — нет.
2. Cursor не отдаёт полный LLM context window как API; CIDE может — свой event log + inject.
3. Агент (dogfood 2026-07-25): хочет Intercom в пульте **с правом заглушить**, не второй шумный чат.

## Решение (направление)

### Две плоскости continuity

| Плоскость | Носитель | Restore |
|-----------|----------|---------|
| Desk | `desk-previous.json` / session+buffers | [0182](0182-restore-previous-desk-dual-instance.md) |
| Chat / meaning | Continuity card (0175 A+B) ± optional transcript | **этот ADR** |

### Cockpit Intercom (агент)

1. **Default quiet:** в `cdp_cockpit` / CIDE desk — строка/карточка: workline status + last intent/handover pulse (не лента).
2. **Toggle:** `intercom=off|on` (или locus) — агент может отключить канал.
3. **Статусы workline (минимум):** `active` | `parked` | `waiting_you` | `restore_ready`.
4. **Expand on demand:** `go=intercom` / mfd — лента или projection event log; cold start не тащит всё.
5. **После restore:** desk (0182) + continuity card в первый ход; полный transcript — opt-in heavy path.

### Не делать

- Клонировать Cursor Composer UI в cockpit.
- Авто-inject полного transcript в каждый cold start.
- Смешивать desk bookmark и chat blob в один JSON без слоёв.

## Последствия

- Бэклог CIDE (после unpark): IdeCommand / cockpit organ + wiring к 0175 export/stub.
- CDP: опционально зеркало `go=intercom` / continuity peek, когда CIDE loopback жив.
- Cursor hosts остаются handoff-only для chat plane — честно документировать.

## Отклонённые альтернативы

- **Только KB handoff-файл** — отклонено как единственный канал: нужен IDE-native pulse в пульте.
- **Всегда полная лента в cockpit** — отклонено (шум, token tax, нет opt-out).
- **Склеить chat в desk-previous.json** — отклонено: разные TTL и семантика (0182 note).

## Follow-up (когда CIDE с парка)

- [ ] MVP: quiet card + toggle + statuses + связка restore desk↔card.
- [ ] Optional transcript inject / export path.
- [ ] Agent preference surface («как хочу видеть Intercom») — settings.toml / session.
- [ ] Channel mute («беруши») — [0184](0184-harness-channel-mute-earplugs-cockpit.md).
