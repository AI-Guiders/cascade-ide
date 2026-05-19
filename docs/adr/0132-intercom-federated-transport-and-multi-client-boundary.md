# ADR 0132: Intercom — федерация, общий transport и граница multi-client (CIDE / Web / MCC)

**Статус:** Proposed  
**Дата:** 2026-05-19

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom как канал; multi-party; внешний контур (§5) |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Event log и проекции — **источник истины** на клиенте сегодня |
| [0127](0127-intercom-spine-and-topic-tabs-chrome-navigation.md) | UX навигации в CIDE (spine, tabs) — **не** transport |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | `AttachmentAnchor`, bracket `F`/`M`/`L`/`S`, re-resolve |
| [0129](0129-intercom-message-body-markdown-and-fenced-code.md) | Тело сообщения, fenced — wire в `content` |
| [0130](0130-editor-agent-range-reveal-without-selection.md) | Reveal/select из attach; MCP |
| [0131](0131-editor-slash-select-code-by-bracket-reference.md) | Bracket в редакторе без attach |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Slash — subset может жить на Web |
| [0057](0057-chat-surface-pipeline-adoption.md) | `ChatSurfaceSnapshot` — **проекция** клиента |

### Вне ADR (playbook / экосистема)

| Документ | Роль |
|----------|------|
| [intercom-ux-reference-slack-mattermost-v1.md](../design/intercom-ux-reference-slack-mattermost-v1.md) | Слой A (UX) vs B (внешний контур) — **этот ADR нормативен для B** |
| [iop-manifest-v1.md](../iop-manifest-v1.md) | IOP: наблюдаемая дельта, интенты — transport не подменяет git/kb |
| agent-notes: [note-intercom-shared-backend-v1](https://github.com/KarataevDmitry/personal-knowledge-base/blob/main/knowledge/work/projects/door-to-singularity/door-to-singularity/note-intercom-shared-backend-v1.md) | Рабочая гипотеза → канон здесь |
| Mission Control Center (идея) | Read-client трассировки + подписка на event log, не второй чат |

## Резюме

**Intercom** в экосистеме IOP — не панель Avalonia и не «ещё один мессенджер в IDE», а **контракт обмена** (event log, topics, attach, slash subset) с **несколькими клиентами**:

| Слой | Владелец | Сегодня | Целевое |
|------|----------|---------|---------|
| **Transport** | Отдельный сервис или sync-адаптер | Локальный event log в CIDE [0045](0045-agent-chat-persistence-event-log-and-projections.md) | **Общий** store для команды (client–server **или** mesh — см. §4) |
| **CIDE UX** | cascade-ide | Skia chrome, spine, attach, Roslyn, MCP | Остаётся **богатым** пилотским клиентом |
| **Web / PWA** | Отдельный репозиторий (TBD) | — | Лента, compose, attach chips, deep link «открыть в CIDE» |
| **MCC** | mission-control-center (идея) | — | **Read** + deep link; фокус SA ТЗ→код, не дублирование чата |

**Принято направление (Proposed):**

1. Не клонировать **Slack/Mattermost** как продукт; **взять паттерны** (channels/topics, threads, `@`, permissions, BFF) — см. [playbook](../design/intercom-ux-reference-slack-mattermost-v1.md) слой B.
2. **v0 команды:** централизованный **Intercom service** (REST + live channel) **или** гибрид «CIDE local + sync adapter»; **mesh/CRDT** — только при явном требовании offline без сервера.
3. **Wire** для attach и контекста — те же JSON-поля, что [0128](0128-intercom-attachment-anchors-and-code-references.md) / [0130](0130-editor-agent-range-reveal-without-selection.md); prose `[M:… S:for:2]` — convenience, не второй протокол.
4. **MCC** подписывается на transport, не владеет сообщениями.

---

## Контекст

Сейчас Intercom в Cascade IDE силён как **сессионный канал оператора** ([0080](0080-intercom-naming-and-multi-party-channel-model.md), [0127](0127-intercom-spine-and-topic-tabs-chrome-navigation.md)): event log, темы, attach к коду ([0128](0128-intercom-attachment-anchors-and-code-references.md)), slash ([0119](0119-chat-slash-commands-intercom-surface.md)). Персистентность — **локальная** проекция [0045](0045-agent-chat-persistence-event-log-and-projections.md).

Продуктовые цели экосистемы требуют **второго и третьего клиента**:

- **QA / менеджер в браузере** — те же треды и attach, что у разработчика в CIDE.
- **Mission Control Center** — ситуационная осведомлённость: обсуждение + ADR + ветка + символ, без «что у вас в чате IDE?».

[intercom-ux-reference-slack-mattermost-v1.md](../design/intercom-ux-reference-slack-mattermost-v1.md) разделяет **слой A** (UX в IDE) и **слой B** (внешний командный контур). Слой B отложили «когда будет коммит» — этот ADR фиксирует **границы** и **минимальный transport**, не откладывая бесконечно.

Bracket-ось `S:` ([0128](0128-intercom-attachment-anchors-and-code-references.md) §5.1) и reveal/select ([0130](0130-editor-agent-range-reveal-without-selection.md), [0131](0131-editor-slash-select-code-by-bracket-reference.md)) имеют смысл для команды только если **anchor JSON** и **message events** видны всем клиентам.

---

## Проблема

1. **Островной продукт:** локальный-only чат в CIDE + отдельный Web-чат + JIRA → три правды, нет сквозной SA.
2. **Дублирование scope:** перенос MM/Slack целиком — лишний продукт; игнорирование transport — потолок на «Intercom в IDE».
3. **Неясная граница:** что остаётся в Avalonia (Skia, Roslyn resolve, editor reveal), что обязано быть на сервере (ACL, retention, fan-out, id сообщений).
4. **MCC риск:** если MCC хранит свои «обсуждения», федерация с Intercom ломается.

---

## Решение

### 1. Три слоя (нормативно)

```text
┌─────────────────────────────────────────────────────────┐
│  Clients: CIDE (full) │ Web (compose+read) │ MCC (read) │
└───────────────┬─────────────────┬───────────────────────┘
                │    Intercom API / WS (transport)        │
┌───────────────▼─────────────────────────────────────────┐
│  Event log (append-only) · topics · members · ACL         │
│  Message: content, attachments[], senderWorkspaceContext  │
└───────────────┬───────────────────────────────────────────┘
                │  не подменяет
┌───────────────▼───────────────────────────────────────────┐
│  git · kb-public · hybrid index · Roslyn (на клиенте CIDE) │
└───────────────────────────────────────────────────────────┘
```

- **Transport** знает: workspace/team id, topic/thread ids, message id, timestamps, roles (human/agent/system), **structured attach** (не обязан понимать Roslyn).
- **CIDE** выполняет: re-resolve anchor, `intercom.reveal_attachment`, `/editor select code`, spine по solution — **после** получения события.
- **Web** может: показать chip + excerpt; deep link / protocol handler → CIDE для полного resolve.
- **MCC**: подписка на topics/messages + ссылки на ADR/git; **без** обязательного compose в v0.

### 2. Минимальный wire (v0 API sketch)

Согласовать с [0045](0045-agent-chat-persistence-event-log-and-projections.md) event types; REST — черновик имён:

| Ресурс | Назначение |
|--------|------------|
| `POST /workspaces/{id}/topics` | создать/привязать тему (spine key optional) |
| `GET /workspaces/{id}/topics` | список для Web/MCC |
| `POST /topics/{id}/messages` | тело + `attachments[]` + `senderWorkspaceContext` ([0128](0128-intercom-attachment-anchors-and-code-references.md) §3.1) |
| `GET /topics/{id}/messages` | лента (cursor) |
| `WS /workspaces/{id}/stream` | live append (опционально v0.1) |

**Attachment** (JSON, не prose):

```json
{
  "file": "src/Foo.cs",
  "memberKey": "Run",
  "lineStart": 50,
  "lineEnd": 100,
  "syntaxScope": { "kind": "for", "indexInParent": 2, "parentMemberKey": "Run" },
  "excerpt": "…",
  "displayLabel": "Run › for (2)"
}
```

Prose `[F:…; M:…; S:for:2]` парсится **на клиенте** в этот shape ([0131](0131-editor-slash-select-code-by-bracket-reference.md) фаза 2 — parse `S:` **done** в CIDE parser; transport принимает уже JSON).

### 3. Slack / Mattermost — что берём и что нет

| Берём (паттерн) | Не берём (продукт) |
|-----------------|-------------------|
| Topic = channel/thread hybrid | Plugin marketplace MM |
| Event log, mentions `@` | 1:1 копия Slack UI |
| BFF для Web + auth | MM как **единственный** бэкенд без своего контракта |
| Permissions: workspace / topic / role | Замена ADR/kb на «чат» |
| Read receipts / presence — **позже** | Обязательный mobile v0 |

Пересмотр [playbook](../design/intercom-ux-reference-slack-mattermost-v1.md): таблица «in scope UX» остаёт для **CIDE**; строки, требующие **shared store**, помечаются ссылкой на **этот ADR**.

### 4. Transport options

| Вариант | Когда | Заметка |
|---------|-------|---------|
| **A. Intercom service** (новый repo) | default v1 для команды | CIDE = sync client + offline queue |
| **B. Hybrid** | пилот одной команды | Local [0045](0045-agent-chat-persistence-event-log-and-projections.md) + periodic push |
| **C. Mesh / CRDT** | явное «без центрального сервера» | Отложить; дороже операционно |

**Склонение:** A или B для первого Web-клиента; C — только по отдельному ADR.

### 5. Идентичность и федерация

- **Workspace** — IOP workspace root (не путать с git repo).
- **Sender** — user id + optional `senderWorkspaceContext`: branch, commit, solution path ([0128](0128-intercom-attachment-anchors-and-code-references.md) §3.1).
- **Клиент** — `clientKind`: `cide` | `web` | `mcc` | `agent` — для телеметрии и capability flags (compose, reveal, slash).

### 6. Что не меняется в CIDE (краткосрочно)

- Skia pipeline, spine/tabs [0127](0127-intercom-spine-and-topic-tabs-chrome-navigation.md).
- Локальный event log остаёт **кэшем/офлайн** до подключения sync.
- Roslyn, hybrid index, MCP — **на стороне CIDE**, не на chat-server.

---

## Последствия

### Положительные

- Один смысловый Intercom для CIDE + Web + MCC.
- Attach и bracket-грамматика [0128](0128-intercom-attachment-anchors-and-code-references.md) масштабируются на команду.
- MCC не раздувается в мессенджер.

### Отрицательные / риски

- Операционка сервиса (хостинг, ACL, backup).
- Конфликты offline/sync (нужна политика merge).
- Два репозитория минимум: `cascade-ide` + `intercom-server` (имя TBD).

---

## Фазы (черновик)

| Фаза | Содержание | Репо |
|------|------------|------|
| **0** | Этот ADR Proposed; обновить playbook слой B | cascade-ide |
| **1** | Контракт OpenAPI/event schema; CIDE **export/import** JSON log | cascade-ide |
| **2** | Intercom service MVP: topics + messages + WS | intercom-server (new) |
| **3** | Web client read+compose; attach chips | intercom-web (new) |
| **4** | MCC read-subscription + deep links | mission-control-center |
| **5** | ACL, org boundaries, Mattermost **bridge** (optional) | service |

**Не блокирует:** текущую работу по [0128](0128-intercom-attachment-anchors-and-code-references.md) chips/prose в CIDE — локальный log остаёт valid.

---

## Критерии принятия (Accepted)

- [ ] Зафиксирован выбор A vs B для пилота (документ + issue).
- [ ] JSON schema attach согласована с `AttachmentAnchor` в коде CIDE.
- [ ] Playbook [intercom-ux-reference](../design/intercom-ux-reference-slack-mattermost-v1.md) ссылается на § transport.
- [ ] MCC one-pager не предполагает собственный message store.

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-19 | Proposed: transport vs CIDE UX vs Web/MCC; client–server default; MM/Slack patterns not product clone. |
