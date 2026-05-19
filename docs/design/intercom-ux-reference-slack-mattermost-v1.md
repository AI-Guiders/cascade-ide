# Intercom UX: ориентиры Slack / Mattermost (границы v1)

**Статус:** черновик продуктового направления (не ADR).  
**Дата:** 2026-05-17  
**Связь:** [ADR 0080](../adr/0080-intercom-naming-and-multi-party-channel-model.md) (канал, multi-party, внешний командный контур §5), [ADR 0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) (topic cards, drill-in/back), [ADR 0119](../adr/0119-chat-slash-commands-intercom-surface.md) (слэш + autocomplete в `ChatInput`), [ADR 0031](../adr/0031-agent-chat-clarification-batches-and-threading.md), [ADR 0096](../adr/0096-intercom-topic-card-summary-and-product-spine.md), [ADR 0057](../adr/0057-chat-surface-pipeline-adoption.md), [ADR 0044](../adr/0044-avalonia-host-skia-agent-chat-surface.md), [ADR 0120](../adr/0120-primary-work-surface-intercom-or-editor.md), [north-star-cursor-mcp-cascade-workbench-v1.md](north-star-cursor-mcp-cascade-workbench-v1.md).

## Зачем этот документ

**Intercom** в CascadeIDE — не «чат с ботом», а **канал связи в сессии работы** ([0080](../adr/0080-intercom-naming-and-multi-party-channel-model.md)). Пользователи и команды уже знают **Slack** и **Mattermost**: composer внизу, слэш-команды, треды, роли сообщений.

Этот чертёж фиксирует, **какие паттерны** брать как вдохновение для **UX в IDE**, и что **сознательно не копировать** (сервер чата, «одна бесконечная лента», короткие мнемоники в слэше). Норматив по исполнению остаётся в ADR; здесь — **продуктовая рамка** для дизайна и приоритизации.

---

## Два слоя (не смешивать)

| Слой | Что это | Ориентир Slack/MM |
|------|---------|-------------------|
| **A. Intercom в IDE** | Сессия + агент + workspace + MCP; Skia pipeline [0057](../adr/0057-chat-surface-pipeline-adoption.md) | Паттерны **UI и поведения**, не бэкенд |
| **B. Внешний командный контур** | Люди вне IDE, организация, retention, мобилки | Mattermost / Matrix / корп. API — **интеграция**, не переписывание ([0080 §5](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5)) |

Документ в основном про **слой A**. Слой B — отдельный ADR интеграции, когда будет продуктовый коммит.

---

## Что заимствуем (in scope для UX)

### Composer и command line

- **Поле ввода внизу** (или закреплённый composer в Forward при [0120](../adr/0120-primary-work-surface-intercom-or-editor.md)) — привычная «точка речи» сессии.
- **Слэш-команды** с **иерархическим autocomplete** (`/` → namespace → action), как в Slack — [0119](../adr/0119-chat-slash-commands-intercom-surface.md). Discoverability **через подсказки**, не через запоминание `/br`.
- **Обычный текст** без ведущего `/` уходит агенту; слэш — **локальное** действие IDE/Intercom (Reject неизвестного `/`, без pass-through агенту).

### Навигация по темам (сильнее, чем «просто лента»)

- **Topic cards + overview/detail + back** [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) — аналог «списка каналов/тредов», но заточен под **линии работы** в сессии, а не произвольные комнаты.
- **Одна тема** → default detail; **несколько тем** → default overview карточек.
- **Main line** — одна из карточек, не скрытый режим «без карточек».

### Типы сообщений и «кто в эфире»

| Тип (продуктово) | Примеры | Заметка |
|------------------|---------|---------|
| **Human** | оператор | |
| **Agent** | LLM / ACP | |
| **System** | сборка, git, MCP-статус, ошибка инструмента | визуально отличать от диалога (как system/bot в MM) |
| *(будущее)* **Operator external** | реплика из Mattermost | слой B, синхронизация по контракту |

Минимум v1: заложить **тип сообщения** в модель событий [0045](../adr/0045-agent-chat-persistence-event-log-and-projections.md), даже если в UI пока только human + agent.

### Плотность и читаемость ленты

- Хронология **внутри темы**; ветвления — данные [0031](../adr/0031-agent-chat-clarification-batches-and-threading.md), не хаотичные прыжки UI.
- **Карточка темы** со **сводкой** [0096](../adr/0096-intercom-topic-card-summary-and-product-spine.md) — аналог preview последнего сообщения в списке каналов.
- **Product spine** — ортогонален main thread; не смешивать с обычной лентой без явного действия.

#### Лента без «пузырей» (как Slack/MM, не как Telegram)

У **Slack и Mattermost** в канале **нет** messenger-пузырей (цветные «шарики» слева/справа) — плоская **лента**: аватар (опционально) + **имя + время** + текст на фоне канала. У Cascade Intercom **целевое направление — то же**: пузыри **отвлекают** от кода и смысла реплики; лишний визуальный шум в Forward.

| Делаем | Не делаем |
|--------|-----------|
| **Большинство** реплик — одна плоская строка, **без** рамки и без заливки | Оформлять каждое сообщение как «карточку» |
| Роль human / agent / system — **типографика** (имя, цвет метки, плотность), не balloon | Залитые rounded **bubble** вокруг каждой реплики |
| **Акцент редко:** рамка / inset только когда есть смысл прервать (ошибка, блокер, результат слэша, diff) | Рамка «для красоты» или на каждую реплику агента |
| System / slash-outcome — по умолчанию **компактная строка**; рамка — только при ошибке или явном статусе | Псевдо-диалог «два собеседника в шариках» |

**Правило акцента:** если рамкой/inset помечено **больше малой доли** ленты, акцент **теряет смысл** — как в Dark Cockpit ([handbook §2.5](cide-design-handbook-v1.md)): в норме лента **тихая**, прерывание — по делу.

**Текущее состояние:** в коде/теме ещё есть legacy `message_bubble` / `ChatMessageBubbleBackground` (Avalonia/Skia) — **долг** на эволюцию к flat feed ([0123](../adr/0123-intercom-full-skia-surface-evolution.md)). В макетах для дизайнера — **не** рисовать Telegram/iMessage-пузыри; ориентир — скриншот Slack (плоская лента).

*Исключение термина:* в ADR про слэш иногда «пузырь» = **одна строка результата** `/command` в ленте (не chat-bubble); при рефакторинге лучше «slash outcome row».

### IDE-специфичное «богатое сообщение» (не копия Slack)

- Якоря на **файл / диапазон / выделение** ([0080 future](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities)) — превью, jump-to-code, мини-diff.
- Паритет: то же действие через MCP/`command_id`, что и клик в Intercom.

### Входы (ортогональность)

Один `command_id` — несколько поверхностей ([0013](../adr/0013-command-surface-and-discoverability.md), [handbook §2.6](cide-design-handbook-v1.md)):

| Вход | Роль |
|------|------|
| Pointer / hit-target | тактика на карточках |
| **Палитра (Ctrl+Q)** | репетиция: полный каталог, fuzzy-поиск |
| **CascadeChord (Ctrl+K)** + Melody `c:` | выступление: мнемоники **в палитре/аккорде**, не в слэше |
| **Chat slash** + autocomplete | канал сессии: CLI в composer ([0119](../adr/0119-chat-slash-commands-intercom-surface.md) Implemented) |
| MCP / агент | тот же `command_id` [0030](../adr/0030-command-ids-hotkeys-and-ui-registry-layers.md) |

---

## Что не копируем (out of scope / non-goals)

| Не делаем | Почему |
|-----------|--------|
| **Сервер Slack/MM внутри Cascade** | [0080 §5](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5): отдельный продукт |
| **WebView полного клиента MM/Slack** в IDE без ADR | «Две правды», фокус, SSO — только осознанно |
| **Одна бесконечная лента** как единственный режим | Ломает topic cards [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) |
| **Короткие слэш-мнемоники** (`/br`) | Discoverability только autocomplete; сжатие — `c:` Melody |
| **Messenger-пузыри** (Telegram/iMessage-style) | Отвлекают; у Slack/MM их нет — flat feed; акцент — рамка/inset, см. §«Лента без пузырей» |
| **Реакции, GIF, emoji-first** | Шум для IDE-цикла; отложить |
| **Полный поиск по истории** как в MM Enterprise | v2+; не блокер v1 Intercom |
| **Паритет каждой фичи MM** (threads в threads, workflows) | Берём **ментальную модель**, не feature checklist |

---

## Карта паттернов: Slack/MM → Cascade

| Паттерн Slack / Mattermost | В Cascade Intercom |
|----------------------------|-------------------|
| Channel list | Topic overview (карточки) [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) |
| Thread | Тема / `ThreadNode` [0031](../adr/0031-agent-chat-clarification-batches-and-threading.md) |
| `/command` + picker | [0119](../adr/0119-chat-slash-commands-intercom-surface.md) |
| @mention | Якорь на код / workspace context (будущее) |
| Сообщение в канале (flat) | Строка ленты: имя + время + текст; **без** balloon |
| Bot / system message | System role + build/git/MCP lines (компактно, не пузырь) |
| Pin / bookmark | Spine, session tree [0116](../adr/0116-intercom-session-tree-and-agent-message-steering.md) |
| Unread badge | IDE Health / уведомления кабины (не дублировать MM) |

---

## Skia и «ощущение Slack»

[0044](../adr/0044-avalonia-host-skia-agent-chat-surface.md) / [0057](../adr/0057-chat-surface-pipeline-adoption.md): вдохновение — **иерархия экранов, composer, типографика, плоская лента + системные строки**, а не HTML-виджеты Slack и не messenger-пузыри. Pipeline **Intent → Layout → Render** сохраняет intent-first [0072 §5](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md): pointer и слэш не дергают Skia напрямую.

**Визуальные ориентиры v1 (мягкие):**

- human / agent — различимые **роли** (цвет имени, иконка), **без** balloon; **без** рамки на обычной реплике;
- system / slash-outcome — по умолчанию компактная строка; **рамка/inset — исключение** (ошибка, fail, требует действия);
- topic card — заголовок + сводка + метаданные (время, непрочитанное — по мере готовности модели).

---

## Приоритеты внедрения (согласовано с ADR)

| Приоритет | Содержание | ADR |
|-----------|------------|-----|
| P0 | Topic cards, drill-in/back, adaptive default | 0072 |
| P0 | Слэш + **обязательный** autocomplete; без коротких слэш-алиасов | 0119 |
| P1 | Типы сообщений (system/agent/human) в ленте | 0045, 0080 |
| P1 | Intercom-centric Forward (опционально) | 0120 |
| P2 | Rich anchors (file:line, selection) | 0080 future, 0039 |
| P3 | Внешний MM/Slack как канал команды | 0080 §5, отдельный ADR |

---

## Открытые вопросы

1. **Sidebar vs overview в Forward:** при [0120](../adr/0120-primary-work-surface-intercom-or-editor.md) — карточки тем слева (MM-style) или полноэкранный overview?
2. **Имя в UI:** только «Intercom» или подзаголовок «как в командном чате» на первый релиз? [0080 open](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-open).
3. **Минимальный набор system-сообщений v1:** только build fail/success или шире (git, index)?
4. **Критерии выбора внешнего провайдера (слой B):** self-host, API, SSO — до ADR интеграции.

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-17 | Черновик: Slack/Mattermost как UX-ориентир; границы A/B; in/out of scope; карта паттернов. |
| 2026-05-19 | Лента без messenger-пузырей (flat feed как Slack); акцент редко (рамка/inset); legacy bubble — долг. |
