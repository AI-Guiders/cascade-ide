# ADR 0175: ADCM Partition — continuity pair (TopicDecisions + Handover ops) + message anchors

**Статус:** Proposed (наметки / направление)  
**Дата:** 2026-07-18

## Резюме

При тактике **Partition** в ADCM ([0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md), KB `playbook-agent-driven-context-management-v1.md`) агенту нужен не «пустой новый чат» и не один гибридный handoff, а **пара слинкованных артефактов** плюс **проверяемая лента**:

| Слой | Имя (рабочее) | Правда | TTL |
|------|----------------|--------|-----|
| A | **TopicDecisions** (Lane / topic decisions) | смысловой срез цикла | долгий (память ветки) |
| B | **Handover ops** | бытовой хвост хода | короткий (устаревает быстро) |
| C | **Export + message anchors** | точные фразы / места в ленте | как у экспорта; якоря стабильны |

Исполнитель и исследователь — **не две роли**, а фазы одного цикла одного агента. Меняются цель хода, угол и подход к данным. Переход между окнами опирается прежде всего на **выводы** (A), не на runbook (B). B — «записка на подушке»; без A это ориентиры выжившего.

Якоря в духе [0128](0128-intercom-attachment-anchors-and-code-references.md) / message id: Decisions цитируют span; при споре — прыжок в экспорт, как CodeAnchor в код. Разметку может инициировать **harness** на Partition и/или **агент** операционно.

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | ADCM signals; «ничего о нас без нас»; Partition как тактика |
| [0173](0173-intercom-intent-card-session-decision-capture.md) | Мелкие сессионные решения (intent card); TopicDecisions может **агрегировать** карточки, не заменять их |
| [0174](0174-sedm-software-engineering-decision-making-ux-spine.md) | Цикл Perceive→…; continuity pair = носитель между фазами/окнами |
| [0172](0172-conversation-first-habitat.md) | Topics / worklines; spin-off |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | CodeAnchor / AttachmentAnchor — паттерн якоря; переиспользовать, не invent schema |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Event log; message_id |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Fork / steer |

### Вне ADR

| Документ | Роль |
|----------|------|
| KB `playbook-agent-driven-context-management-v1.md` | Канон ADCM 5P |
| KB `note-dialogue-delta-effectiveness-v0.md` | Research: осмысленность хода ↔ Δ; round-trip economy (не Accepted) |
| KB `playbook-session-summary-and-chat-export-v1.md` | Export → резюме → согласование |
| CIDE `chat_export_readable` | Проверяемый экспорт ленты |

---

## Контекст

### Проблема

1. Silent compact / один «summary» в новом окне — антипаттерн ADCM.
2. Чистый **ops seed** (пути, SHA, first tools) восстанавливает инвентарь, не понимание — как записка «ужин в холодильнике» после провала памяти.
3. Чистый **analysis** без якорей и ops ломается, когда следующий ход — implement.
4. Intent card [0173](0173-intercom-intent-card-session-decision-capture.md) фиксирует *одно* решение в потоке; при **смене topic/окна** нужен **срез lane**, не только последняя карточка.

### Эмпирика сессии (2026-07-18)

Оператор ожидал смысловой срез checkpoint; агент выдал runbook-seed. Вывод дизайна: default Partition payload не должен быть ops-only; слои **развести и слинковать**.

---

## Решение (направление)

### 1. Continuity pair (обязательный минимум Partition)

При создании sibling topic / смене окна под ADCM Partition:

1. Пишется (или обновляется) **TopicDecisions** для lane/topic.
2. Пишется **Handover ops**, слинкованный на Decisions (и обратно).
3. Новый чат **стартует с Decisions** (или кратким указателем на него); ops — по ссылке, когда ход исполнительный.
4. Default UI focus: **soft create** (sibling без auto-steal фокуса), если не оговорено иное — детали IdeCommand вне этого ADR-наброска.

### 2. Поля TopicDecisions (v0 наметки)

Не схема JSON wire — смысловые слоты:

| Слот | Вопрос |
|------|--------|
| Frame | О чём была речь / объект |
| Rejected | Что отклонили (анти-решения) |
| Accepted | Что приняли / инварианты |
| New | Какая новая информация вошла в картину |
| Next | Куда движется цикл (цель следующего хода), не обязательно tool list |
| Anchors | Ссылки на message/export spans, питающие Accepted/Rejected |

Связь с [0173](0173-intercom-intent-card-session-decision-capture.md): карточки могут быть **источниками** Accepted/Rejected; TopicDecisions — **оглавление lane** на момент перехода.

### 3. Поля Handover ops (v0 наметки)

Короткий TTL: пути, SHA/ветки, dirty/open implement items, optional first moves. Явный линк на TopicDecisions. Не дублировать Frame/Rejected/Accepted.

### 4. Export + message anchors (слой C)

На Partition (или по согласию checkpoint):

1. **Export** читаемой ленты (`chat_export_readable` или эквивалент) — проверяемый артефакт.
2. **Разметка якорей** на ключевые реплики (message_id + optional excerpt/span) — в духе CodeAnchor; **не** новая конкурирующая schema, а reuse [0128](0128-intercom-attachment-anchors-and-code-references.md) / event ids [0045](0045-agent-chat-persistence-event-log-and-projections.md).
3. Кто инициирует: **harness** (ровный stub: export + placeholder links) и/или **агент** (смысловая привязка «это решение ← эта фраза»).

Итог: Decisions без галлюцинации истории; при необходимости — восстановление «как письмо со сносками».

### 5. Роли (без раздвоения агента)

| Кто | На Partition |
|-----|----------------|
| Harness | Сигнал pressure; может запустить export + stub pair; dual-channel inject |
| Agent | Выбирает Partition; наполняет Decisions; по необходимости якоря и ops |
| Оператор | Видит акт перехода; согласует смысл Accepted/Rejected |

---

## Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| Один гибридный handoff-файл | Слои мимикрируют; ops топит выводы |
| Только ops seed | «Похмельный инвентарь» без рамки |
| Только prose analysis без export/якорей | Непроверяемо; нельзя восстановить точную фразу |
| Silent host summary как переход | Анти-ADCM |
| Новая schema якорей вместо 0128/0045 | Дублирование; уже есть CodeAnchor / message_id |
| Два разных «агента» (researcher vs executor) | Один цикл; меняется ход, не личность |

---

## Последствия

### +

- Partition становится первоклассной ADCM-тактикой с носителем, а не «скажи человеку создай чат».
- Согласуется с intent card / SEDM / habitat без подмены ADR.
- История восстанавливаема через якоря + export.

### − / риски

- Нужны соглашения об именах путей/хранении pair (scratch vs session tree) — отдельный spike.
- IdeCommand (`intercom_topic_create` + continuity) — product work; этот ADR пока **наметки**.
- Агент может снова свалиться в ops-only — нужен default шаблон «A затем B» в harness/rules.

### Не в scope этого ADR

- Полный wire schema JSON.
- UI wizard «CASA/Neumann T1».
- Автоматический LLM-summary ленты без якорей как единственный носитель.

---

## Критерий готовности направления

Оператор и агент согласны одной фразой: *«Partition кладёт TopicDecisions + Handover ops + (желательно) export/якоря; новый чат читает выводы первыми»* — и указывают этот ADR + ADCM playbook.
