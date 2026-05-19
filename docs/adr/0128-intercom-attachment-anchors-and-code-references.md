# ADR 0128: Intercom — якоря вложений (code references) и канонический attach

**Статус:** Proposed  
**Дата:** 2026-05-19

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom как канал; deep links; multi-party |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Slash в composer; autocomplete; local execution |
| [0124](0124-slash-parametric-editor-line-commands.md) | `/editor line …` — **действие** в редакторе; binders строк 1-based |
| [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) | `/file open` — **навигация**; dynamic completion путей |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Event log; вложения в теле сообщения |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id`; паритет MCP |
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | Resolve member / semantic |
| [0111](0111-editor-linenumber-linerange-value-objects.md) | `LineRange` в домене |
| [0053](0053-semantic-map-control-flow-pfd.md) | Structural picker внутри метода (v2+) |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Composer в Forward |
| [0123](0123-intercom-full-skia-surface-evolution.md) | Skia-лента; fenced code в теле — `ChatMessageBodyPresentation` |

### Вне ADR (playbook)

| Документ | Роль |
|----------|------|
| [intercom-ux-reference-slack-mattermost-v1.md](../design/intercom-ux-reference-slack-mattermost-v1.md) | Slack/MM границы, flat feed, макеты — **продуктовый чертёж** |
| [intercom-design-hub-v1.md](../design/intercom-design-hub-v1.md) | Домены D1–D9 для дизайнера |

## Резюме

Зафиксировать **единую модель «упомянуть кусок кода»** в Intercom: не путь/строки в голове пользователя, а **смысловой якорь** (выделение, member, синтаксический scope, файл), с **производным** снимком `file` + `LineRange` @ send и **re-resolve** у получателя.

1. **Attach** (`/attach …`, `[…]`, chips) — payload к **черновику/сообщению**; ортогонален `/file open` и `/editor line`.
2. **Канон в wire:** `AttachmentAnchor` (shape, optional `memberKey`, optional `syntaxScope`, `excerpt`, resolved `file` + lines @ send, offsets в prose).
3. **Человек:** приоритет **H0** selection, **H0b** scope @ caret, **H1** `[M:…]`; **M0** path/lines — fallback для агента.
4. **Клик из ленты:** `intercom.reveal_attachment` — open + scroll + **рамка** (не selection); Shift → select.
5. **Строки @ send** — не контракт между участниками; устойчивее **excerpt** и **member** / **syntaxScope**.

**Не входит:** vision/policy для image в prompt агента; полный structural picker (v2); `@file` inline (запасной); реализация composer chips (отдельные задачи); полный markdown-рендер ленты (см. [0123](0123-intercom-full-skia-surface-evolution.md) фаза 3).

**Два вида «кода» в чате:** (1) **якорь на workspace** — этот ADR (`AttachmentAnchor`, chip, reveal); (2) **цитата в теле** — fenced `` ``` `` в `content`, без anchor — §11.

---

## Контекст

Оператор хочет в одной реплике сказать: *«этот метод здесь, а регрессия — там»*, в том числе **в середине предложения**. Кажется простым, но смешивает:

| Поверхность | Пример | Эффект |
|-------------|--------|--------|
| Attach | `/attach selection`, `[M:Foo]` | Payload агенту + deep link |
| Editor action | `/editor line select 5 10` | Меняет selection в буфере |
| Open file | `/file open Foo.cs` | Только вкладка |
| Agent MCP | `go_to_position` | Select в редакторе (правка) |

Без ADR неизбежны: `@cc:`-стиль, пузыри, «строка 50» как единственная правда, случайное удаление после клика из чата, рассинхрон строк у отправителя и получателя.

**Процесс:** домен проговаривали с агентом как с собеседником до кода (ветвления attach/клик/re-resolve) → этот ADR и [intercom-design-hub](../design/intercom-design-hub-v1.md); капитан и ревью — у человека — [philosophy §8](../design/cascadeide-philosophy-v1.md#8-агент-как-партнёр-для-проектирования-до-кода).

---

## Проблема

1. **Когнитивная нагрузка:** требовать path + line numbers в prose.
2. **Две правды по строкам:** ветка, локальные правки, другой монитор/контекст — L50 у отправителя ≠ L50 у получателя.
3. **Гранулярность:** `[M:Foo]` на 500 строк не покрывает «второй `for`» без имени.
4. **Разрыв входов:** slash, скобки, chips, MCP должны сходиться в **один** wire-тип для [0045](0045-agent-chat-persistence-event-log-and-projections.md).

---

## Решение

<a id="adr0128-p1"></a>

### 1. Термины

| Термин | Значение |
|--------|----------|
| **Attachment** | Ссылка на workspace-артефакт в **теле** реплики (не отдельная «полоса вложений» только сверху) |
| **AttachmentAnchor** | Каноническая структура в event log |
| **Chip** | UI-токен в composer; после send — inline-метка в flat feed |
| **Resolve @ send** | Roslyn/редактор вычисляют `file`, `LineRange`, `excerpt` в момент отправки |
| **Reveal** | Навигация по клику из ленты (рамка, не selection по умолчанию) |

<a id="adr0128-p2"></a>

### 2. `attachmentShape` (обязательное поле)

| Shape | Когда | Строки в resolve |
|-------|--------|------------------|
| `selection` | Выделение в редакторе | да |
| `text-range` | Текстовый файл + диапазон / positional M0 | да |
| `whole-file` | Медиа, бинарники, «весь файл» | нет |
| `member` | Resolve по `memberKey` (весь член или уточнённый scope) | да (снимок @ send) |
| `syntax-scope` | Innermost `ForStatement` / `IfStatement` @ caret (H0b) | да (снимок @ send) |

Для `.png`, `.dll` и т.д. — только `whole-file`; slash `[path] [start] [end]` для binary → **ошибка** (как I3 у [0124](0124-slash-parametric-editor-line-commands.md)).

<a id="adr0128-p3"></a>

### 3. Канон `AttachmentAnchor` (wire / event log)

Минимальный контракт для [0045](0045-agent-chat-persistence-event-log-and-projections.md) (имена полей — при реализации в JSON/schema):

| Поле | Обязательность | Смысл |
|------|----------------|--------|
| `id` | да | Стабильный id внутри сообщения |
| `attachmentShape` | да | см. §2 |
| `displayLabel` | да | Короткая метка в ленте (`GetUserAsync`, `Foo › for (2)`) |
| `file` | почти всегда | Workspace-relative path после resolve |
| `lineStart`, `lineEnd` | если применимо | 1-based inclusive @ send — **hint**, не контракт между участниками |
| `memberKey` | опционально | Roslyn-qualified или согласованный stable key |
| `syntaxScope` | опционально | `{ kind: "for", indexInParent: 2, parentMemberKey }` — v2 |
| `excerpt` | рекомендуется | Текст фрагмента @ send — **устойчивый** для всех участников |
| `proseStart`, `proseLength` | да | Offset inline-метки в теле сообщения |
| `resolvedAtUtc` | да | Момент resolve |

**Принцип:** то, что видит человек в ленте — **смысл** (`displayLabel`, excerpt); path/lines — вторично (hover, агент, fallback).

<a id="adr0128-p4"></a>

### 4. Ввод для человека (слои H / M)

Приоритет **не** «сначала путь и строки»:

| Слой | Ввод | `command_id` / механизм |
|------|------|-------------------------|
| **H0** | Выделение → attach | `attach_selection` → `/attach selection` |
| **H0b** | Каретка в `for` → attach scope | `attach_scope` → `/attach scope` *(Proposed)* |
| **H1** | `[M:Method]`, `[Foo.cs M:Method]` | Parse bracket → resolve member |
| **H2** | `[diagram.png]`, `[appsettings.json]` | `whole-file` |
| **M0** | `[Foo.cs 50 100]`, `[F:…; L:…]` | Агент, paste; positional fallback |

**Внутри длинного метода** без имени: H0, H0b, или H1 + prose («второй for»); v2 — `syntaxScope` / picker ([0053](0053-semantic-map-control-flow-pfd.md)).

<a id="adr0128-p5"></a>

### 5. Inline `[…]` — вторая поверхность, тот же anchor

- `[M:GetUserAsync]` — **F** из active file или autocomplete.
- `[Foo.cs M:…]` — явный файл.
- Не путать: `@` — **люди**; `[` — **артефакты**; не markdown `[](url)`.
- **L2** для агента: `[F:path; M:name; L:50-100]` — один parse tree с L1; разделитель полей **`;`** (единственный канон).

Парсер: не каждые `[` в цитате кода — только распознаваемые attach-токены (грамматика в playbook, детали реализации в коде).

<a id="adr0128-p6"></a>

### 6. Slash namespace `/attach`

| Slash | `command_id` | Эффект |
|-------|--------------|--------|
| `/attach selection` | `attach_selection` | Chip из active editor selection |
| `/attach scope` | `attach_scope` | Chip из innermost syntax @ caret |
| `/attach file <path>` | `attach_file` | whole-file или prompt range для text |
| `/attach file <path> <start> <end>` | `attach_file` | `text-range` (text-only) |

- Autocomplete путей — **переиспользовать** [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md); другой `command_id`, чем `file_open`.
- ~~`/attach code`~~ — **не** использовать как корень.
- ~~`@cc:`~~ — **не** использовать.

Запись в `intent-catalog.toml` — по [0119](0119-chat-slash-commands-intercom-surface.md) / [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) при внедрении.

<a id="adr0128-p7"></a>

### 7. Ортогональные команды (не смешивать)

| Действие | Команда | Меняет буфер? | Attach? |
|----------|---------|---------------|---------|
| Открыть файл | `/file open` [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) | нет | нет |
| Select/delete lines | `/editor line` [0124](0124-slash-parametric-editor-line-commands.md) | **да** | нет |
| Прикрепить к реплике | `/attach …`, `[…]` | нет | **да** |
| Агент идёт править | MCP `go_to_position` | **да** (select) | нет |

<a id="adr0128-p8"></a>

### 8. Reveal из ленты (клик по метке)

`command_id`: **`intercom.reveal_attachment`** (или эквивалент в intent catalog).

| Шаг | Поведение |
|-----|-----------|
| 1 | Open `file` (или preview для non-text) |
| 2 | Если есть `memberKey` или `syntaxScope` → **re-resolve** в solution **получателя** → `LineRange` |
| 3 | Иначе если есть lines @ send → best-effort + drift warning |
| 4 | Scroll into view + **transient range highlight** (рамка / gutter band), **не** `Selection` |
| 5 | Shift+клик или настройка → `SelectInEditor` |

Отличие от MCP `go_to_position`: reveal = **просмотр**; go_to_position = **правка** агентом.

<a id="adr0128-p9"></a>

### 9. Стабильность между отправителем и получателем

- **Excerpt** и **displayLabel** (member, `Foo › for (2)`) — общий знаменатель в ленте.
- **lineStart/lineEnd @ send** — снимок отправителя для агента и fallback; при клике у получателя — **re-resolve** при наличии `memberKey`/`syntaxScope`.
- UI может показать: «при отправке L50–100» при hover, если текущий диапазон другой.

<a id="adr0128-p10"></a>

### 10. Composer и лента (UX)

- Composer: **текст + chip-токены** в позиции курсора; несколько anchors в одной реплике.
- Лента: **flat feed** ([0123](0123-intercom-full-skia-surface-evolution.md)); inline-метки; без messenger-пузырей.
- Агенту: ordered `AttachmentAnchor[]` + prose (для LLM — excerpt обязателен для безымянных блоков).

<a id="adr0128-p11"></a>

### 11. Fenced code в теле сообщения (не AttachmentAnchor)

Отдельно от attach: агент или человек вставляет **фрагмент кода как текст** в `content` (markdown `` ```lang … ``` ``). Это **не** ссылка на файл в workspace, пока оператор явно не сделал attach / `[M:…]`.

| Аспект | Fenced block в `content` | `AttachmentAnchor` |
|--------|--------------------------|-------------------|
| Источник | stream / paste в сообщение | `/attach`, `[…]`, chip в composer |
| Wire | строка `content` (+ сегменты в проекции UI) | массив anchors в payload [0045](0045-agent-chat-persistence-event-log-and-projections.md) |
| Стабильность | снимок на момент send; не re-resolve в репо | member / scope → re-resolve у получателя |
| Клик по умолчанию | **копировать** / развернуть блок; **не** open file | **reveal** в редакторе (§8) |
| Парсер `[…]` | **внутри fenced code не парсить** attach-токены (§5) | только вне code-сегментов |

**Реализация v1 (есть в коде):** `ChatMessageBodyPresentation.SplitSegments` — prose + **первый** fenced block; отрисовка — mono strip в Skia ([0123](0123-intercom-full-skia-surface-evolution.md) фаза 3). Ограничения v1: один блок `` ``` `` на сообщение; без подсветки синтаксиса по языку (stretch).

**Связь с attach:** «посмотри этот метод» в prose + chip; длинный listing в ответе агента — fenced block. Не смешивать: fenced block **не** подменяет excerpt для агента, если нужна привязка к репо — нужен anchor.

---

## Фазы внедрения

| Фаза | Содержание | Зависимости |
|------|------------|-------------|
| **0** | Schema `AttachmentAnchor` в [0045](0045-agent-chat-persistence-event-log-and-projections.md); projection в ленту (read-only метки) | 0045 |
| **1** | `/attach selection`, `/attach file`; chips; resolve @ send; excerpt | 0125, 0111 |
| **2** | Bracket parse `[M:…]`, `[path]`; `/attach` в TOML; reveal рамка | 0058, Roslyn |
| **3** | `/attach scope`; `syntaxScope`; re-resolve @ recipient; Shift→select | 0053 опционально |
| **4** | Structural picker; `[M:Foo S:for:2]`; stale hint | 0053 |
| **3b** *(stretch)* | Несколько fenced blocks; lang tag → подсветка; copy chip на блоке | 0123 |

---

## Не цели

- Политика image/vision в prompt агента.
- `@file` inline (если не понадобится после фазы 2).
- Замена Solution Explorer или fuzzy-поиска по репо в composer.
- Паритет Slack/MM server — слой B [0080 §5](0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5).

---

## Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| Только `@cc:path:lines` | Путает с `@mention`; path-first |
| Только positional `[file lines]` для людей | Строки не стабильны; тяжёлый ввод |
| Клик → always selection | Риск случайного удаления |
| Отдельный тип сообщения без canonical anchor | Дубли с prose; плохо для 0045 |
| Две грамматики `T:,M:` и `type;member:` | Двойной парсер |

---

## Согласованные решения (для реализации)

| Тема | Решение |
|------|---------|
| Fenced vs anchor | Разные сущности (§11); fenced не open file по клику |
| `[` в fenced code | Attach-грамматика **не** применяется внутри code-сегмента |
| `[` в prose | Только распознаваемые attach-токены; литеральные `[` в v1 — редкий кейс, экранирование `\[` — фаза 2+ |
| Reveal highlight v1 | Transient **overlay / gutter band** (~3 s), не `Selection`; editor adornment — v2 |
| `memberKey` v1 | `file` (workspace-relative) + Roslyn **display/qualified name** строкой; rename → stale + re-resolve |
| `memberKey` v2 | DocumentId / symbol id при необходимости |
| Excerpt лимит | По умолчанию **120 строк** или **16 KiB** (что меньше по объёму), хвост `…`; агенту в prompt — полный excerpt в пределах лимита |
| Redaction внешний контур | Отдельное событие / политика [0080](0080-intercom-naming-and-multi-party-channel-model.md), не в slash UX |
| `@file` inline | **Отложено** до фазы 2 attach; приоритет `/attach` + `[path]` |

---

## Открытые вопросы

1. Точная JSON-schema `AttachmentAnchor` в [0045](0045-agent-chat-persistence-event-log-and-projections.md) (`schema_version` bump, имена полей).
2. Нужен ли **отдельный** `command_id` «скопировать excerpt anchor» vs общий copy selection.
3. Collapse длинного fenced block по умолчанию (порог строк) — продуктовый порог.
4. Паритет: агент шлёт только fenced code без anchor — когда UI предлагает «attach как ссылку на файл» (heuristic, v2+).

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-19 | Proposed: канон AttachmentAnchor, H/M слои, `/attach`, reveal, re-resolve. |
| 2026-05-19 | §11 fenced code vs anchor; согласованные решения; уточнены открытые вопросы. |
