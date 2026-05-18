# ADR 0126: Intercom inspect — `/topic`/`/spine` list|tree и статус в chrome (compact)

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-18

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0119](0119-chat-slash-commands-intercom-surface.md) | Слэши в `ChatInput`; `kind=help` для `/help`; расширение **`kind=report`** |
| [0123](0123-intercom-full-skia-surface-evolution.md) | Forward Intercom compact: toolbar Skia, лента без «чужого» Avalonia-хвоста |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | После `/file open` — показ редактора (MFD Editor при `intercom`) |
| [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) | `/file open` + autocomplete; клик по файлу → выполнение + `RevealEditorForOpenedDocument` |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Картотека тем (overview/detail) — **визуальный** режим |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | Spine и topic cards — источник данных отчётов |
| [0057](0057-chat-surface-pipeline-adoption.md) | `ChatSurfaceSnapshot` — канон для list/tree |

### Вне ADR

| Документ | Роль |
|----------|------|
| [`IntentMelody/intent-catalog.toml`](../../IntentMelody/intent-catalog.toml) | Маршруты `/topic list`, …, `kind = "report"` |
| `Features/Chat/ChatSlashSessionReports.cs` | Форматирование отчётов |
| `Features/Chat/ChatIntercomChromeStatusPresentation.cs` | Подзаголовок toolbar |

## Резюме

1. **Текстовый inspect** рядом с **карточным** overview ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md)): слэши **`/topic list`**, **`/topic tree`**, **`/spine list`**, **`/spine tree`** — локальный отчёт в ленте (как `/help`), без MCP.
2. В **compact Forward Intercom** ([0123](0123-intercom-full-skia-surface-evolution.md)) навигационный «хвост» (spine strip, «Темы → назад», боковые строки тем, заголовок ветки) **убран из скролла**; краткий статус — **вторая строка toolbar** (`тема · линия · сообщений`).
3. **`ChatSlashCommandExecutionKind.LocalReport`** + `kind = "report"` в TOML (без `command_id`, как `help`).

Картотека и кнопка **«Темы»** остаются для визуального drill-in; list/tree — для быстрого среза «что где» в одном текстовом блоке.

---

## Контекст

После [0123](0123-intercom-full-skia-surface-evolution.md) лента Intercom рисует spine strip, overview-навигацию и заголовки веток **в начале скролла**. При `CompactLayout = true` (Forward) это:

- съедает вертикаль до сообщений и слэш-команд;
- дублирует то, что уже есть в toolbar (**Intercom**, **Темы**) и в строке состояния.

Параллельно [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) усилил **workspace CLI**; для **структуры сессии** (темы, spine) не хватало **лёгкого текстового** обзора без переключения в overview.

---

## Проблема

1. **Плотность:** «хвост» из 3–4 карточек перед историей — антипаттерн Slack/MM-like ленты.
2. **Дублирование:** тема / spine / счётчик сообщений видны и в карточках, и в toolbar, и в ленте.
3. **Нет быстрого inspect:** overview — визуально; оператору нужен **плоский список** или **дерево веток** в чате (как `git log --oneline` vs `git log --graph`).

---

## Решение

<a id="adr0126-p1"></a>

### 1. `kind = "report"` в slash-каталоге

| Поле TOML | Значение |
|-----------|----------|
| `kind` | `report` |
| `command_id` | **не задаётся** (как у `help`) |
| Исполнение | `ChatSlashCommandRunner` → `ChatSlashSessionReports.TryFormat(path, ChatSurfaceSnapshot)` |

Маршруты (группа **Intercom**):

| Слэш | Вывод |
|------|--------|
| `/topic list` | Плоский список тем: заголовок, `[main, active]`, число сообщений, короткий id |
| `/topic tree` | Дерево по `ParentThreadId` (fork) |
| `/spine list` | Линия, фокус, вехи, флаг контекста агента |
| `/spine tree` | Линия → фокус → вехи |

Данные — из текущего **`ChatSurfaceSnapshot`** (тот же compositor, что Skia-лента), не второй источник правды.

<a id="adr0126-p1b"></a>

### 1b. `kind = "intercom"` — навигация (не только просмотр)

| Слэш | Действие |
|------|----------|
| `/topic cards` | **Картотека** (overview): topic cards + spine; `IsChatOverviewMode = true`. Каноническое имя для [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md). |
| `/topic open` | Detail выбранной темы: `SelectedChatThreadId`, `IsChatOverviewMode = false`. Аргумент — заголовок, короткий id (8 hex) или **пусто** (выбранная/активная/main). Autocomplete: `completion = session_topics`. |
| `/spine open` | То же, что `/topic cards` (алиас). |

После клика по подсказке `/topic open …` — **автовыполнение** (как `/file open`). Из list/tree **переключиться нельзя** — только смотреть; для перехода — **`/topic open`**; для картотеки — **`/topic cards`** (или «Темы» в toolbar).

<a id="adr0126-p2"></a>

### 2. Compact chrome: статус вместо хвоста в ленте

При **`compactLayout && !overviewMode`** (`ChatSurfaceEntityFactory`):

- **не** рисуются: spine strip, «Назад к обзору», боковые строки тем, заголовок активной ветки;
- **рисуются** только сообщения / слэш-команды / уточнения.

В **`SkiaChatChromeRenderer`** (toolbar 52px при подзаголовке):

```
Intercom                                    [Темы]
тема: … · линия: … · сообщений: N
```

В режиме **картотеки** (`overviewMode`) — полоса «Картотека тем» под toolbar без изменения; подзаголовок: `тем: N · atp/atn …`.

<a id="adr0126-p3"></a>

### 2b. Редактор во вторичном контуре (Mfd / `MfdHostWindow`)

При `primary_work_surface = intercom` и вынесенном **MfdHostWindow** у одного файла может быть **два** `TextEditor` (скрытый Forward + видимый Mfd). **`EditorActiveDockResolver`** выбирает видимый экземпляр для TextMate, `/editor line select`, MCP `select`. TextMate ставится на каждый dock через `MainWindow.EnsureDockEditorTextMate` (не только при предке `MainWindow`).

### 3. Связь с [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) (открытие файла)

После выбора файла в slash-autocomplete:

- подстановка + **автовыполнение** `/file open` / `/solution load` (Enter и клик);
- **`DocumentsWorkspaceViewModel.OpenOrActivateDocument`** → **`RevealEditorForOpenedDocument`**: при `primary_work_surface = intercom` — MFD **Editor**, фокус в редактор.

Это **не** часть `LocalReport`, но закрывает UX «файл открылся, а экран остался на Solution Explorer».

---

## Альтернативы

| Вариант | Почему нет |
|---------|------------|
| Только карточки, без list/tree | Нет быстрого текстового среза для агента/оператора в ленте |
| MCP `chat_list_threads` | Лишний round-trip; данные уже в snapshot |
| Оставить хвост в ленте + статус в toolbar | Дублирование и потеря плотности |
| Отдельное окно «Session inspector» | Избыточно для v1; слэш достаточен |

---

## Последствия

- **+** Лента compact начинается с сообщений; inspect по запросу.
- **+** Единый snapshot для Skia и отчётов.
- **−** В compact нельзя кликнуть боковую строку темы в ленте — только **Темы** / `/overview` / `/topic list`.
- **Расширение:** `kind=report` для `/session tree` ([0116](0116-intercom-session-tree-and-agent-message-steering.md)) — отдельно.

---

## Критерии приёмки

- [x] `/topic list`, `/topic tree`, `/spine list`, `/spine tree` в `intent-catalog.toml`, `kind = report`.
- [x] `/topic cards`, `/topic open`, `/spine open` (алиас cards) — `kind = intercom`; autocomplete тем для `/topic open`.
- [x] Ответ в ленте как сообщение слэш-команды (успех + текст).
- [x] Compact: нет spine/nav/заголовка ветки в скролле; подзаголовок toolbar с темой/линией/счётчиком.
- [x] Тесты: `ChatSlashSessionReportsTests`, `ChatIntercomChromeStatusPresentationTests`.
- [x] Autocomplete включает новые пути (статический каталог).

---

## Реализация (ориентиры в коде)

| Компонент | Путь |
|-----------|------|
| Отчёты | `Features/Chat/ChatSlashSessionReports.cs` |
| Статус toolbar | `Features/Chat/ChatIntercomChromeStatusPresentation.cs` |
| Скрытие хвоста | `Views/Chat/Skia/ChatSurfaceEntityFactory.cs` (`hideNavChromeInFeed`) |
| Toolbar | `Views/Chat/Skia/SkiaChatChromeRenderer.cs` |
| Runner | `Features/Chat/ChatSlashCommandRunner.cs` (`LocalReport`, `LocalIntercom`) |
| Навигация | `Features/Chat/ChatSlashIntercomActions.cs`, `ChatSlashTopicResolver.cs` |
| Autocomplete тем | `SessionTopicSlashCompletionProvider.cs` |
| Loader | `Services/IntentCatalogLoader.cs` (`report`, `intercom`, `session_topics`) |
| TOML | `IntentMelody/intent-catalog.toml` |

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-18 | Accepted · Implemented: report slash, compact chrome status, cross-fix open file UX |
