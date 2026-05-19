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
| [0123](0123-intercom-full-skia-surface-evolution.md) | Skia-лента |
| [0129](0129-intercom-message-body-markdown-and-fenced-code.md) | **Fenced code** и markdown в `content` — **не** этот ADR |

### Вне ADR (playbook)

| Документ | Роль |
|----------|------|
| [intercom-ux-reference-slack-mattermost-v1.md](../design/intercom-ux-reference-slack-mattermost-v1.md) | Slack/MM границы, flat feed, макеты — **продуктовый чертёж** |
| [intercom-design-hub-v1.md](../design/intercom-design-hub-v1.md) | Домены D1–D9 для дизайнера |

## Резюме

Зафиксировать **единую модель «упомянуть кусок кода в репозитории»** в Intercom: не путь/строки в голове пользователя, а **смысловой якорь** (выделение, member, синтаксический scope, файл), с **производным** снимком `file` + `LineRange` @ send и **re-resolve** у получателя.

1. **Attach** (`/attach …`, `[…]`, chips) — payload к **черновику/сообщению**; ортогонален `/file open` и `/editor line`.
2. **Канон в wire:** `AttachmentAnchor` (shape, optional `memberKey`, optional `syntaxScope`, `excerpt`, resolved `file` + lines @ send, offsets в prose).
3. **Человек:** приоритет **H0** selection, **H0b** scope @ caret, **H1** `[M:…]`; **M0** path/lines — fallback для агента.
4. **Клик из ленты:** `intercom.reveal_attachment` — open + scroll + **рамка** (не selection); Shift → select.
5. **Строки @ send** — не контракт между участниками; устойчивее **excerpt** и **member** / **syntaxScope**.

**Не входит:** vision/policy для image в prompt агента; полный structural picker (v2); `@file` inline (запасной); реализация composer chips (отдельные задачи); **fenced code / markdown в теле** — [0129](0129-intercom-message-body-markdown-and-fenced-code.md).

**Два вида «кода» в чате:** (1) **якорь на workspace** — этот ADR; (2) **цитата в `content`** (`` ``` ``) — [0129](0129-intercom-message-body-markdown-and-fenced-code.md).

---

## Контекст

Оператор хочет в одной реплике сказать: *«этот метод здесь, а регрессия — там»*, в том числе **в середине предложения**. Кажется простым, но смешивает:

| Поверхность | Пример | Эффект |
|-------------|--------|--------|
| Attach | `/attach selection`, `[M:Foo]` | Payload агенту + deep link |
| Editor action | `/editor line select 5 10` | Меняет selection в буфере |
| Open file | `/file open Foo.cs` | Только вкладка |
| Agent MCP | `go_to_position` | Select в редакторе (правка) |
| Fenced в ответе | `` ```csharp … ``` `` | Только текст в ленте — [0129](0129-intercom-message-body-markdown-and-fenced-code.md) |

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
| `resolvedAtUtc` | да | Момент resolve @ send |
| `resolveOutcome` | опционально | Кэш последнего reveal у получателя: `resolved` \| `file_missing` \| … — §9.1 |

**Принцип:** то, что видит человек в ленте — **смысл** (`displayLabel`, excerpt); path/lines — вторично (hover, агент, fallback). При `file_missing` excerpt остаётся источником правды для всех.

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

Парсер attach: только в **prose**-сегментах тела (после `SplitSegments` — [0129](0129-intercom-message-body-markdown-and-fenced-code.md) §5); **не** внутри fenced code.

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
| Цитата кода в тексте | markdown fence | нет | нет — [0129](0129-intercom-message-body-markdown-and-fenced-code.md) |

<a id="adr0128-p8"></a>

### 8. Reveal из ленты (клик по метке)

`command_id`: **`intercom.reveal_attachment`** (или эквивалент в intent catalog).

| Шаг | Поведение |
|-----|-----------|
| 1 | Open `file` (или preview для non-text) |
| 2 | Если есть `memberKey` или `syntaxScope` → **re-resolve** в solution **получателя** → `LineRange` |
| 3 | Иначе если есть lines @ send → best-effort + drift warning |
| 4a | Если `file_missing` / `member_not_found` → шаги 4–5 **не** выполнять; UI по §9.1 |
| 4 | Scroll into view + **transient range highlight** (рамка / gutter band), **не** `Selection` |
| 5 | Shift+клик или настройка → `SelectInEditor` |

Отличие от MCP `go_to_position`: reveal = **просмотр**; go_to_position = **правка** агентом.

<a id="adr0128-p9"></a>

### 9. Стабильность между отправителем и получателем

- **Excerpt** и **displayLabel** (member, `Foo › for (2)`) — общий знаменатель в ленте.
- **lineStart/lineEnd @ send** — снимок отправителя для агента и fallback; при клике у получателя — **re-resolve** при наличии `memberKey`/`syntaxScope`.
- UI может показать: «при отправке L50–100» при hover, если текущий диапазон другой.

<a id="adr0128-p9b"></a>

### 9.1 Re-resolve у получателя: другая ветка, нет файла, нет символа

**Вопрос:** отправитель сослался на `Foo.cs` / `[M:Bar]`, у получателя на другой ветке файла нет (или member переименован). Что видит человек и что получает агент?

**Принцип:** attach — это **намерение + снимок @ send**, а не гарантия, что у всех открыт **тот же** снимок workspace. Re-resolve всегда идёт в **текущем** solution/ветке **получателя**; если там нет цели — **не ломаем** ленту и не открываем пустой редактор молча.

| Исход `resolveOutcome` *(в UI / при reveal)* | Условие | Лента (chip / hover) | Клик **reveal** | Агент в prompt |
|---------------------------------------------|---------|----------------------|-----------------|---------------|
| `resolved` | Файл есть, member/scope найден | `displayLabel` | Open + рамка по **текущим** строкам; опционально «было L50–100 @ send» | `file` + lines + **excerpt** |
| `file_missing` | Путь не в workspace (другая ветка, не checkout, другой sln) | Метка + иконка ⚠; **excerpt** в tooltip / expand | Toast: «`Foo.cs` нет в текущем workspace»; **не** создавать пустую вкладку; предложить **показать excerpt** (flyout) или копировать excerpt | **excerpt** + `displayLabel` + `file` (как hint); явно: *не удалось открыть в твоём дереве* |
| `member_not_found` | Файл есть, символа нет (rename, ветка без метода) | `displayLabel` + ⚠ stale | Open файл; рамка по **lines @ send** если валидны, иначе только scroll к файлу + warning | excerpt обязателен; memberKey как hint |
| `lines_drift` | Только M0 / fallback lines, диапазон пустой или сильно сдвинулся | как выше + «drift» | Open + best-effort lines + **предупреждение** | excerpt + lines @ send |
| `excerpt_only` | Non-text / preview-only / resolve отключён | excerpt в expand | Preview или copy excerpt | excerpt |

**Почему excerpt обязателен @ send:** именно он **одинаков** у отправителя и получателя, когда репозитории разъехались. Строки и path — подсказки для IDE **после** успешного resolve у **этого** человека.

**Ветка:** отдельного поля `gitBranch` в wire v1 **не** требуем (часто устаревает и врёт). Опционально v2: `senderWorkspaceHint` (branch name, commit) **только для UI** («отправитель был на `feature/x`»), без попытки checkout.

**Не делаем v1:**

- Автоматический `git checkout` ветки отправителя по клику из чата.
- Скрывать сообщение или chip, если файл отсутствует — смысл реплики и excerpt остаются видимыми.
- Притворяться, что lines @ send — общий контракт между участниками.

**Связь с [0129](0129-intercom-message-body-markdown-and-fenced-code.md):** fenced block в ответе агента **не** зависит от файла в репо; attach chip — да. Если файла нет, получатель всё равно читает **excerpt** или fenced текст, а не «битую» ссылку.

<a id="adr0128-p10"></a>

### 10. Composer и лента (UX attach)

- Composer: **текст + chip-токены** в позиции курсора; несколько anchors в одной реплике.
- Лента: **flat feed** ([0123](0123-intercom-full-skia-surface-evolution.md)); inline-метки attach; без messenger-пузырей.
- Агенту: ordered `AttachmentAnchor[]` + prose (для LLM — excerpt обязателен для безымянных блоков).
- Отрисовка fenced / inline MD в prose между метками — [0129](0129-intercom-message-body-markdown-and-fenced-code.md).

---

## Фазы внедрения

| Фаза | Содержание | Зависимости |
|------|------------|-------------|
| **0** | Schema `AttachmentAnchor` в [0045](0045-agent-chat-persistence-event-log-and-projections.md); projection в ленту (read-only метки) | 0045 |
| **1** | `/attach selection`, `/attach file`; chips; resolve @ send; excerpt | 0125, 0111 |
| **2** | Bracket parse `[M:…]`, `[path]`; `/attach` в TOML; reveal рамка | 0058, Roslyn |
| **3** | `/attach scope`; `syntaxScope`; re-resolve @ recipient; Shift→select | 0053 опционально |
| **4** | Structural picker; `[M:Foo S:for:2]`; stale hint | 0053 |

---

## Не цели

- Политика image/vision в prompt агента.
- `@file` inline (если не понадобится после фазы 2).
- Замена Solution Explorer или fuzzy-поиска по репо в composer.
- Паритет Slack/MM server — слой B [0080 §5](0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5).
- Fenced code, полный markdown в ленте — [0129](0129-intercom-message-body-markdown-and-fenced-code.md).

---

## Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| Только `@cc:path:lines` | Путает с `@mention`; path-first |
| Только positional `[file lines]` для людей | Строки не стабильны; тяжёлый ввод |
| Клик → always selection | Риск случайного удаления |
| Отдельный тип сообщения без canonical anchor | Дубли с prose; плохо для 0045 |
| Две грамматики `T:,M:` и `type;member:` | Двойной парсер |
| Fenced block = автоматический attach | Разные сущности — [0129](0129-intercom-message-body-markdown-and-fenced-code.md) |

---

## Согласованные решения (для реализации)

| Тема | Решение |
|------|---------|
| Fenced vs anchor | Разные ADR: [0129](0129-intercom-message-body-markdown-and-fenced-code.md) vs этот |
| `[` в fenced code | Attach-грамматика **не** применяется — [0129](0129-intercom-message-body-markdown-and-fenced-code.md) §5 |
| `[` в prose | Только распознаваемые attach-токены; `\[` — фаза 2+ |
| Reveal highlight v1 | Transient **overlay / gutter band** (~3 s), не `Selection`; editor adornment — v2 |
| `memberKey` v1 | `file` + Roslyn **display/qualified name**; rename → stale + re-resolve |
| `memberKey` v2 | DocumentId / symbol id при необходимости |
| Excerpt лимит | **120 строк** или **16 KiB**, хвост `…` |
| Redaction внешний контур | Отдельное событие / политика [0080](0080-intercom-naming-and-multi-party-channel-model.md) |
| `@file` inline | **Отложено** до фазы 2 attach |
| Нет файла у получателя | Excerpt + warning; reveal **не** открывает пустой файл — §9.1 |

---

## Открытые вопросы

1. Точная JSON-schema `AttachmentAnchor` в [0045](0045-agent-chat-persistence-event-log-and-projections.md) (`schema_version` bump, имена полей).
2. Нужен ли **отдельный** `command_id` «скопировать excerpt anchor» vs общий copy selection.
3. Паритет: агент шлёт только fenced code без anchor — когда UI предлагает «attach как ссылку на файл» (heuristic, v2+; координация с [0129](0129-intercom-message-body-markdown-and-fenced-code.md)).

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-19 | Proposed: канон AttachmentAnchor, H/M слои, `/attach`, reveal, re-resolve. |
| 2026-05-19 | §11–12 fenced/MD → [0129](0129-intercom-message-body-markdown-and-fenced-code.md); attach-only scope. |
| 2026-05-19 | §9.1 исходы re-resolve: другая ветка / нет файла; excerpt как общий знаменатель. |
