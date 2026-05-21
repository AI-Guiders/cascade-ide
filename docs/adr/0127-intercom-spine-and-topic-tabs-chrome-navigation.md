# ADR 0127: Intercom — spine и вкладки тем в chrome (навигация без overview)

**Статус:** Accepted · In progress (фазы **A + B** в CIDE, 2026-05-21)  
**Дата:** 2026-05-18

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Overview/detail, topic cards, intent-команды — **картотека** остаётся вторым режимом |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | Семантика spine и summary на карточках; контекст агента |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Слэши `/topic …`, `/spine …` — дополняют вкладки, не заменяют |
| [0123](0123-intercom-full-skia-surface-evolution.md) | Intercom = Skia chrome + лента; вкладки рисуются в chrome |
| [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md) | Compact: статус в toolbar вместо «хвоста» в ленте — **эволюционирует** в spine + tabs |
| [0057](0057-chat-surface-pipeline-adoption.md) | `ChatSurfaceSnapshot` — единый источник списка тем |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Forward / MFD — один `IntercomSkiaSurface`, разная плотность |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Melody/Chords для переключения вкладок |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | `load_solution`, workspace — источник «открытого» sln/slnx |
| `multi-root-kb` (параллельная работа, agent-notes / MCP) | Несколько корней knowledge под один workspace — **та же ось**, что spine по solution (см. [§9](#adr0127-p9)) |

### Вне ADR

| Документ / код | Роль |
|--------------|------|
| [`ChatSurfaceSnapshot`](../../Features/Chat/ChatSurfaceSnapshot.cs) | `Layout.Overview`, `ProductSpine`, lanes |
| [`ChatSessionMetadata`](../../Models/AgentChat/ChatSessionMetadata.cs) | Сегодня: один spine + `ThreadTitles`; **расширяется** под каталог линий |
| [`ChatPanelViewModel.ProductSpine`](../../Features/Chat/ChatPanelViewModel.ProductSpine.cs) | `ResolveDefaultProductSpineLineTitle()` — имя **корня workspace**; эволюция → stem **solution** |
| [`ChatIntercomChromeStatusPresentation`](../../Features/Chat/ChatIntercomChromeStatusPresentation.cs) | Сегодня: подзаголовок toolbar — **кандидат на упрощение** после вкладок |
| [`SkiaChatChromeRenderer`](../../Views/Chat/Skia/SkiaChatChromeRenderer.cs) | Место отрисовки spine strip + tab bar |
| [`IntentMelody/intent-catalog.toml`](../../IntentMelody/intent-catalog.toml) | `focus_next_topic`, `return_to_topic_overview`, … |

## Резюме

**Принятое направление:** гибридная навигация Intercom — **spine** (продуктовая линия) + **быстрые вкладки** для частых тем + **Topic Navigator** (боковая панель: поиск + дерево, по аналогии с Search Agents в Cursor) для масштаба и fork’ов. Всё в **Skia chrome / узком навигационном контуре**, не в скролле ленты ([0123](0123-intercom-full-skia-surface-evolution.md), [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md)).

| Слой | Назначение |
|------|------------|
| **Spine** | Якорь «над чем работаем в целом» ([0096](0096-intercom-topic-card-summary-and-product-spine.md)); при нескольких линиях — переключатель (фаза C). |
| **Вкладки** | 3–7 **недавних/закреплённых** тем — один клик, мало места (Forward compact). |
| **Topic Navigator** | Поиск + **дерево** тем (`ParentThreadId`); основной путь при 10+ темах и ветвлении (MFD comfortable). |
| **Overview** | Вторичный режим: **картотека** с summary на карточках — не ежедневное переключение ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md)). |

**Opt-in (фаза D):** `infer_product_spine_from_opened_solution` — spine и набор тем **по открытому** `.slnx`/`.sln`/`.csproj` (`CascadeIDE.slnx` → линия **CascadeIDE** и «свои» темы); default **off**.

**Раскладка по поверхностям (нормативно):**

| Поверхность | Навигация |
|------------|-----------|
| **Forward (compact)** | Spine row + короткий tab bar + **toggle «Навигатор»** (overlay / выдвижка). |
| **MFD Chat (comfortable)** | **Постоянная** боковая панель Navigator (~200–260px); вкладки опциональны (избранные). |

Единый источник данных: `ChatSurfaceSnapshot` + фильтр по `ActiveSpineKey` после фазы C/D.

---

## Контекст

После [0123](0123-intercom-full-skia-surface-evolution.md) и [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md) compact Intercom убрал навигационный «хвост» из скролла ленты. Остались:

- кнопка **«Темы»** → overview-картотека ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md));
- подзаголовок toolbar `тема · линия · сообщений`;
- слэши `/topic list|open|cards` ([0126](0126-intercom-inspect-slash-and-compact-chrome-status.md)).

Для **ежедневной** работы с 2–8 темами это лишний шаг: модель **«вкладки»** (Slack channels, IDE editor tabs, browser tabs) привычнее, чем «выйти в картотеку → выбрать карточку → вернуться».

**Spine** ([0096](0096-intercom-topic-card-summary-and-product-spine.md)) сегодня одна на сессию (`ChatProductSpine` в snapshot), но концептуально **ортогонален** темам: оператору нужен **постоянный якорь линии** над вкладками тем, а не только строка в подзаголовке.

В мульти-репозиторном workspace (например `financial-open` с `cascade-ide` и `agent-notes-core`) оператор по очереди открывает **разные** `*.slnx` — логично, чтобы у каждого solution была **своя** линия и **свой** набор вкладок-тем, а не одна смешанная лента.

Сегодня [`ResolveDefaultProductSpineLineTitle`](../../Features/Chat/ChatPanelViewModel.ProductSpine.cs) берёт имя **корня workspace**, а не файла решения — для привязки к `CascadeIDE.slnx` нужен отдельный контракт (ниже).

---

## Проблема

1. **Лишний режим:** переключение темы через overview — второй UI-мир при том, что данные уже в `Layout.Overview` / `Lanes`.
2. **Слабая иерархия:** spine и активная тема конкурируют в одной строке статуса; не видно «все темы сразу».
3. **Масштаб:** при росте числа тем слэш-picker и картотека остаются нужны, но **не** как единственный быстрый путь.

---

## Решение

<a id="adr0127-p1"></a>

### 1. Гибридная иерархия навигации (нормативно)

Три механизма **дополняют** друг друга; ни один не отменяет слэши и overview.

#### 1a. Forward (compact) — вкладки + выдвижной Navigator

```
┌─────────────────────────────────────────────────────────────┐
│ Intercom                    [☰ Nav] [+] [Картотека…] [⋯]    │
├─────────────────────────────────────────────────────────────┤
│ Spine: CascadeIDE · фокус: …                                │
├─────────────────────────────────────────────────────────────┤
│ [MFD UI] [Channel] [Skia tabs] [+2]                         │  ← недавние / pin (3–7)
├─────────────────────────────────────────────────────────────┤
│ лента + composer                                            │
└─────────────────────────────────────────────────────────────┘
     │
     └── [☰ Nav] → overlay / drawer: [🔍 поиск] + дерево тем
```

#### 1b. MFD Chat (comfortable) — постоянный Navigator

```
┌──────────────┬────────────────────────────────────┐
│ [🔍 поиск…]  │ Intercom toolbar                   │
│ Spine: CIDE  │ (spine в шапке панели или toolbar) │
├──────────────┼────────────────────────────────────┤
│ ▾ темы…      │  лента + composer                  │
│   ├ fork…    │                                    │
└──────────────┴────────────────────────────────────┘
```

| Зона | Поведение |
|------|-----------|
| **Spine** | Строка или блок в Navigator / toolbar; (фаза C) список линий; (фаза D) привязка к `Workspace.SolutionPath` при infer on. |
| **Topic tabs** | Короткий ряд **недавних/pin**; не дублировать всё дерево во вкладках. |
| **Topic Navigator** | Поиск (фильтр по title/summary) + дерево; клик → `SelectedChatThreadId`, detail. |
| **Лента** | Только сообщения выбранной темы; **без** навигационного хвоста в скролле ([0126](0126-intercom-inspect-slash-and-compact-chrome-status.md)). |

**Инвариант:** Navigator, вкладки и overview читают **один** snapshot; VM — один `SelectedChatThreadId` / `IsChatOverviewMode`.

<a id="adr0127-p2"></a>

### 2. Семантика вкладки тем (v1)

| Вопрос | Решение v1 |
|--------|------------|
| Что на вкладке? | **Один корневой тред** из `Layout.Overview` (или эквивалент из compositor): `Title` / `ThreadId`. |
| Fork / дочерние ветки | **Не** отдельная вкладка на каждый fork в v1; форк остаётся в detail-ленте / picker `/topic tree`. v2: вложенные вкладки или breadcrumb под tab bar — отдельное решение. |
| Main thread | Вкладка наравне с остальными; можно визуально пометить «main». |
| Порядок | `Overview` order / `ChatThreadNode.Order` — как в картотеке. |
| Переключение | Pointer на вкладку → `SelectedChatThreadId = tab.ThreadId`, `IsChatOverviewMode = false`, `InvalidateVisual`. |
| Создание темы | Кнопка **`+`** в tab bar → тот же путь, что `/topic create` / intent (без обязательного overview). |

<a id="adr0127-p3"></a>

### 3. Spine: одна линия (v1) и каталог линий (фаза C)

**v1 (данные уже есть):** одна [`ChatProductSpine`](../../Features/Chat/ChatSurfaceSnapshot.cs) на snapshot — spine row показывает `ChatProductSpinePresentation.ResolveLineTitle` + усечённый `CurrentFocus`. Переключателя линий **нет**; все темы сессии в одном tab bar.

**Фаза C:** **каталог product spine** в одной chat-сессии + **активная линия**; tab bar показывает **только темы этой линии**.

<a id="adr0127-p3b"></a>

### 3b. Привязка линии к открытому solution (opt-in)

**Мотивация:** продуктовая линия в CIDE часто совпадает с **решением**, над которым работают: `CascadeIDE.slnx` ↔ работа над Cascade IDE; `AgentNotesMcp.slnx` ↔ agent-notes MCP — **много тем** внутри одной линии, но темы **другого** solution не должны смешиваться во вкладках.

#### Ключ spine (стабильный идентификатор)

| Источник | Правило v1 |
|----------|------------|
| Открытый файл решения | Нормализованный абсолютный путь к `.slnx` / `.sln` / `.csproj` (тот же канон, что `load_solution` / `Workspace.SolutionPath`) |
| **SpineKey** | Внутренний ключ: нормализованный путь (предпочтительно) **или** хеш пути при хранении |
| **DisplayTitle** | Stem файла без расширения: `CascadeIDE.slnx` → **`CascadeIDE`**, `AgentNotesMcp.slnx` → **`AgentNotesMcp`** |
| Коллизия имён | Два разных пути с одним stem → разные SpineKey (путь), в UI при коллизии — уточнение (подпапка / короткий путь) |

**Не путать** с именем корня workspace ([`ResolveDefaultProductSpineLineTitle`](../../Features/Chat/ChatPanelViewModel.ProductSpine.cs)): при включённом infer **приоритет у solution**, иначе fallback как сегодня.

#### Настройка (opt-in, по умолчанию off)

| Параметр | Значение |
|----------|----------|
| Имя | `infer_product_spine_from_opened_solution` (алиас в docs/settings: `infer_from_opened_solution`) |
| Default | **`false`** |
| Где хранить | Настройки IDE / chat (`.cascade` или существующий store настроек) — конкретный файл в реализации |
| Когда **on** | После успешного `LoadSolution` / смены `Workspace.SolutionPath`: вычислить SpineKey + DisplayTitle → **активировать** или **создать** запись в каталоге линий |
| Когда **off** | Поведение как сейчас: одна линия в metadata, ручной `/spine` и поля в legacy UI; смена sln **не** переключает линию |

**Инвариант opt-in:** автоматика **никогда** не включается молча при обновлении IDE; только явное включение оператором.

#### Запоминание тем по линии

Расширение [`ChatSessionMetadata`](../../Models/AgentChat/ChatSessionMetadata.cs) (schema ≥ 3, детали в [0045](0045-agent-chat-persistence-event-log-and-projections.md)) — **направление**:

```text
ActiveSpineKey: string?
SpineCatalog: {
  "<spineKey>": {
    LineTitle, CurrentFocus, Milestones, IncludeInAgentContext,
    LastActiveThreadId?,
    TopicThreadIds: [ guid, ... ]   // порядок вкладок
  }
}
ThreadSpineKey: { "<threadId>": "<spineKey>" }   // вместе с ThreadTitles
```

| Событие | Поведение |
|---------|-----------|
| `/topic create` при активной линии L | Новый `thread_id` → `ThreadSpineKey[thread]=L`, добавить в `SpineCatalog[L].TopicThreadIds` |
| Переключение solution (infer on) | `ActiveSpineKey` = ключ нового sln; tab bar = темы из каталога для L; `LastActiveThreadId` восстанавливается |
| Сообщения в треде | Как сейчас (event log); привязка к линии — **метаданные**, не дублирование истории |
| Старые сессии без каталога | Миграция: одна линия `legacy`, все существующие `ThreadTitles` → эта линия |

**Composable** строит snapshot: `ProductSpine` = запись активной линии; `Overview` / tab bar = фильтр `Layout` по `ThreadSpineKey == ActiveSpineKey`.

#### UX chrome при нескольких линиях

- Spine row: **DisplayTitle** активной линии + chevron / список известных линий в сессии (ручной выбор без смены solution).
- Tab bar: только темы **активной** линии (может быть **много** вкладок — overflow фаза B).
- Открытие другого sln (infer on) ≈ переключение «пространства тем» — как смена проекта в IDE.

#### Отклонено для v1 infer

| Идея | Почему нет |
|------|------------|
| Всегда infer без настройки | Ломает ручной spine и смешанные сценарии «одна сессия — несколько sln без разделения» |
| Spine = имя папки workspace | Уже есть как fallback; не отражает `CascadeIDE.slnx` vs monorepo root |
| Отдельная chat-сессия на каждый sln | Дублирует persistence; каталог в одной сессии проще для IOP и carry-forward [0096](0096-intercom-topic-card-summary-and-product-spine.md) |

<a id="adr0127-p4"></a>

### 4. Связь с overview и слэшами

| Механизм | После 0127 |
|----------|------------|
| **Overview / «Темы»** | Режим **картотеки** (summary, плотная сетка карточек) — не единственная навигация. |
| `/topic cards`, `/spine open` | По-прежнему включают `IsChatOverviewMode = true`. |
| `/topic open <id>` | Эквивалент клика по вкладке. |
| `/topic list\|tree` (текст) | Без изменений ([0126](0126-intercom-inspect-slash-and-compact-chrome-status.md)). |
| Подзаголовок toolbar | **Упростить:** при видимых вкладках — только счётчик сообщений / индикатор loading; «тема» и «линия» не дублировать. |

<a id="adr0127-p5"></a>

### 5. Реализация (Skia, границы)

- **Отрисовка:** [`SkiaChatChromeRenderer`](../../Views/Chat/Skia/SkiaChatChromeRenderer.cs) (+ примитивы **SkiaKit**: `SkiaTabBar`, `SkiaSpineStrip` — имена ориентировочные).
- **Hit-test:** вкладки регистрируют hit в chrome band **над** clip ленты (отдельный список hit targets или расширение chrome API).
- **Плотность:** `CompactLayout` — меньше высота tab/spine; comfortable (MFD) — те же примитивы, больше padding ([0123](0123-intercom-full-skia-surface-evolution.md)).
- **Запрещено steady-state:** Avalonia `TabControl` / `ItemsControl` для tab bar в продуктовой зоне Intercom.

<a id="adr0127-p6"></a>

### 6. Клавиатура и intent (минимум)

Расширить [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) без дублирования логики:

| Intent / жест | Эффект |
|---------------|--------|
| `focus_next_topic` / `focus_previous_topic` | Следующая/предыдущая **вкладка** в tab bar (циклически по видимым). |
| `enter_focused_topic` | Уже в detail при выбранной вкладке — no-op или «подтвердить»; при overview — открыть сфокусированную. |
| Ctrl+Tab (опционально) | То же, что next topic — через hotkeys.toml, не хардкод в control. |

Melody/Chords и MCP `ide_execute_command` — те же `command_id`, что для overview-навигации.

<a id="adr0127-p7"></a>

### 7. Контекст агента ([0096](0096-intercom-topic-card-summary-and-product-spine.md))

Вкладки **не меняют** политику промпта: в запрос по-прежнему уходит **активная тема**, spine — сжато или по явному действию. Переключение вкладки оператором **не** обязано auto-inject всю линию spine в следующее сообщение.

<a id="adr0127-p8"></a>

### 8. Topic Navigator (дерево + поиск) — нормативная часть гибрида

По аналогии с **Search Agents** в Cursor ([§1b](#adr0127-p1)): панель **слева от ленты** (MFD) или **по toggle** (Forward).

| Возможность | Реализация |
|-------------|------------|
| Поиск | Одна строка; фильтр узлов дерева по `Title` / summary (клиентски, без отдельного индекса). |
| Дерево | `ChatSurfaceState.Threads` + `ParentThreadId` — тот же смысл, что `/topic tree` ([0126](0126-intercom-inspect-slash-and-compact-chrome-status.md)). |
| Выбор узла | `SelectedChatThreadId`, `IsChatOverviewMode = false` — как клик по вкладке или `/topic open`. |
| Fork | Виден в дереве; **отдельная вкладка на fork** — по-прежнему не в v1 (§2). |
| Ширина | MFD: ~200–260px постоянно; Forward: collapsible, по умолчанию скрыт. |

**Overview vs Navigator:** не три списка. **Steady-state** — вкладки + Navigator; **overview** — режим картотеки с **summary** на карточках ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md), когда нужен обзор «на плитках», а не иерархия в sidebar.

**Рендер:** предпочтительно **Skia** (`SkiaTreeList`, `SkiaSearchField` в SkiaKit); допустим узкий Avalonia-контур только для Navigator ([0123](0123-intercom-full-skia-surface-evolution.md) — навигационный «тяжёлый» контур, не лента).

<a id="adr0127-p9"></a>

### 9. Согласование с multi-root knowledge (параллельная работа)

В соседнем контуре внедряется **multi-root-kb** (несколько корней `knowledge/` под один workspace / MCP). Для Intercom это **та же продуктовая ось**, что spine по solution:

| Контур | «Корень» | Пример |
|--------|----------|--------|
| Knowledge (multi-root-kb) | Каталог KB на диске | `agent-notes/knowledge`, второй root для другого репо |
| Intercom spine (фаза D) | Открытый solution | `CascadeIDE.slnx` → spine **CascadeIDE**; `AgentNotesMcp.slnx` → **AgentNotesMcp** |
| Темы чата | Внутри spine | Много тем на одну линию; `ThreadSpineKey` не смешивает темы разных sln |

**Инвариант:** при `infer_product_spine_from_opened_solution = on` переключение solution меняет **и** активный spine Intercom, **и** (на стороне агента) ожидаемый primary knowledge root по политике multi-root-kb — **без** автоматического слияния контекста разных продуктов в один промпт. Детали привязки KB — в документации multi-root-kb; этот ADR фиксирует только **симметрию модели** для оператора.

Согласование с [0017](0017-multi-window-workspace-and-agent-surfaces.md) и [0095](0095-workspace-solution-ide-health-stratification.md): несколько solution / git-корней — **явная** политика, не silent merge в одно поле.

---

## Рекомендуемый MVP vs stretch

| Уровень | Состав |
|---------|--------|
| **MVP (Forward)** | Фазы **A + B**: spine row + tab bar (недавние/pin), Navigator по toggle — минимально дерево+поиск в overlay |
| **MVP (MFD)** | Фаза **E** (постоянный Navigator) **или** сразу после A: comfortable без узкого tab bar |
| **Stretch** | **C + D**: каталог линий, `infer_product_spine_from_opened_solution`, запоминание тем per spine |
| **Позже** | Pin/reorder вкладок, summary в узле дерева, интеграция с multi-root-kb в UI настроек |

---

## Поэтапная поставка

| Фаза | Содержание | Критерий готовности |
|------|------------|---------------------|
| **A** | Spine row + tab bar (недавние/pin) из `Overview`; клик → detail | 3+ темы без overview на Forward |
| **B** | Overflow вкладок, `+`, badge; subtitle без дубля «тема·линия» | 12+ тем в одной линии |
| **E** | **Topic Navigator** (поиск + дерево); Forward: toggle; MFD: постоянная панель | 15+ тем: поиск по имени; fork в дереве |
| **C** | Каталог линий (`SpineCatalog`); фильтр вкладок и дерева по `ActiveSpineKey` | Две линии в сессии, темы разделены |
| **D** | **`infer_product_spine_from_opened_solution`** + `ThreadSpineKey` + restore при `LoadSolution` | `CascadeIDE.slnx` ↔ свои темы; `AgentNotesMcp.slnx` ↔ свои |

Фазы **A–B–E** (UI) не требуют schema ≥ 3. **C–D** — metadata schema ≥ 3 и hook `Workspace.SolutionPath`.

**Порядок поставки (согласовано):** **A → B** на Forward; **E** параллельно или сразу на MFD Chat; **C → D** когда нужен multi-sln в одной сессии.

Зависимость: после стабилизации [0123](0123-intercom-full-skia-surface-evolution.md) фазы 3 (markdown/copy) и offscreen-рендера ленты.

---

## Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| **Только overview, без вкладок** | Статус-кво; лишний шаг при частом переключении ([0126](0126-intercom-inspect-slash-and-compact-chrome-status.md)). |
| **Вкладки внутри скролла ленты** | Снова «хвост», прокручивается away; против [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md). |
| **Avalonia TabControl** | Два UI-мира, плотность, [0123](0123-intercom-full-skia-surface-evolution.md). |
| **Вкладка на каждый fork** | Взрыв числа вкладок; v1 откладываем. |
| **Убрать overview полностью** | Теряем картотеку summary и inspect для больших сессий. |
| **Новая chat-сессия на каждый sln** | Дублирование; хуже carry-forward и общий IOP-контекст сессии. |
| **Только sidebar, без быстрых вкладок** | На 3–5 темах лишние клики и ширина; гибрид предпочтительнее. |
| **Только tabs, без поиска при 20+ темах** | Плохо масштабируется; фаза E или overview. |

---

## Последствия

- **Плюсы:** привычная модель; меньше кликов; spine всегда на виду; согласовано со Skia-only Intercom.
- **Минусы:** вертикаль chrome +40–72px (compact — минимизировать); нужен дизайн overflow; фаза C — работа по persistence.
- **Тесты:** unit на порядок вкладок из snapshot; headless hit-test bounds tab bar (опционально); intent-тесты переключения без изменения overview-флага.

---

## Согласованные решения (закрытые вопросы)

| Тема | Решение |
|------|---------|
| Overview + вкладки | В режиме картотеки (`IsChatOverviewMode`) **скрывать** tab bar; Navigator — свернуть или только дерево без смены detail (реализация: не дублировать три панели). |
| Forward vs Navigator | **Только overlay/drawer** по toggle; постоянная колонка — **MFD comfortable**. |
| Overview после Navigator | **Картотека остаётся** для summary на карточках; Navigator — ежедневная навигация и поиск. |
| Infer при `LoadSolution` | При **on** — переключать `ActiveSpineKey` на **каждый** успешный load (без «закрепления» в v1). |
| Темы до первого sln | Spine **`legacy`** (или workspace fallback), миграция в каталог при первом infer. |
| `.csproj` без sln | Stem `.csproj` как линия — **да** (v1). |
| Multi-spine в CIDE | **Да**, фаза C — типичный сценарий `financial-open` + несколько slnx. |

## Открытые вопросы

Остаётся уточнить в реализации:

| # | Вопрос | Склонение по умолчанию |
|---|--------|------------------------|
| 1 | Закрытие вкладки («×») | v2; v1 — только через слэш/VM |
| 2 | Drag-reorder вкладок / узлов дерева | v2 |
| 3 | Summary в узле Navigator (одна строка) | фаза после E; overview хранит развёрнутый summary |
| 4 | Единый hotkey «Навигатор» / фокус в поиск | intent + hotkeys.toml |
| 5 | Точный контракт UI multi-root-kb ↔ spine при infer on | совместная заметка с владельцами multi-root-kb |

---

## Метрики успеха (для Accepted / Implemented)

1. Переключение между **тремя** заранее созданными темами — **≤ 1 клик**, без `IsChatOverviewMode = true`.
2. В compact Forward **≥ 90%** высоты центральной колонки под ленту+composer (эвристика на 1080p при 5 вкладках).
3. Регрессии: [0119](0119-chat-slash-commands-intercom-surface.md) `/topic open`, [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md) report-слэши, intent `focus_next_topic` — зелёные.

---

## История изменений

| Дата | Изменение |
|------|-----------|
| 2026-05-18 | **Proposed:** spine row + topic tabs в Skia chrome; фазы A–C; связь с 0072/0096/0123/0126. |
| 2026-05-18 | **Уточнение:** opt-in `infer_product_spine_from_opened_solution` — spine по stem `.slnx`/`.sln`/`.csproj`; каталог линий и запоминание тем per spine (фаза D). |
| 2026-05-18 | **§8 / фаза E:** боковая панель Topic Navigator (дерево + поиск, аналог Search Agents); гибрид с tab bar. |
| 2026-05-18 | **Согласовано:** гибрид (§1, §8) — Forward: tabs + Nav toggle; MFD: постоянный Navigator; MVP/stretch (§«MVP vs stretch»); **§9** — ось multi-root-kb ↔ spine по solution. |
