# ADR 0123: Intercom — эволюция к full-Skia surface (плотный Slack/MM-like UX)

**Статус:** Accepted  
**Дата:** 2026-05-18

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | Хост Avalonia + Skia для ленты; модель первична; без «весь чат Skia одним махом» |
| [0057](0057-chat-surface-pipeline-adoption.md) | Pipeline snapshot → entities → layout → render |
| [0117](0117-ide-skia-kit.md) | `Views/SkiaKit/` — примитивы; чат собирает сцену из kit |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Слэши и autocomplete в `ChatInput` |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Intercom в Forward — якорь внимания; компактный chrome |
| [0121](0121-intent-oriented-programming-paradigm.md) | Intercom — центр вокруг цели; структура важнее «ленты ради ленты» |
| [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md) | Compact: статус в toolbar; навигационный хвост убран из скролла |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Пакеты уточнений — структурированный UI |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI vs IDE chrome; Intercom — **не** «ещё один Avalonia-экран» |
| [0076](0076-ui-ux-principles-hub.md) | Плотность, токены; **UiKit** — shell/MFD-настройки, не лента Intercom |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Topic cards, overview/detail |
| [0098](0098-semantic-first-document-as-projection.md) | Редактор кода — тяжёлый контур, остаётся на AvaloniaEdit |

### Вне ADR

| Документ | Роль |
|----------|------|
| [feature-archetype-v1.md](../design/feature-archetype-v1.md) | Skia-surface: Intent → Layout → Render; примитивы из SkiaKit |
| [ide-chrome-tokens-v1.md](../design/ide-chrome-tokens-v1.md) | Токены Avalonia chrome; мост в `SkiaChatTheme` |
| [iop-manifest-v1.md](../iop-manifest-v1.md) | Intercom как hub коммуникации |

---

## Резюме

- **Принцип (согласовано с [0044](0044-avalonia-host-skia-agent-chat-surface.md)):** **Avalonia — только фюзеляж** (окно, зоны PFD/Forward/MFD, lifecycle, DPI). **Intercom** — **наш** Skia-surface (лента, composer, slash, toolbar). Стандартные Avalonia-контролы в чате (`TextBox`, `ListBox`, `CascadeSection` в ленте) — **не целевое состояние**; визуально они выбиваются из продукта («из-под топора») и противоречат разделению слоёв [0066](0066-cockpit-ui-vs-ide-presentation-layer.md).
- **На Avalonia остаются «тяжёлые» контуры:** прежде всего **редактор кода** (AvaloniaEdit), плюс узкий класс **IDS-оверлеев** с богатыми формами (пакеты уточнений [0031](0031-agent-chat-clarification-batches-and-threading.md), модалки настроек) — не как постоянная «оболочка чата».
- **Целевая картина:** один control **`IntercomSkiaSurface`** (эволюция `SkiaChatSurfaceControl`) — лента + composer + slash popup; Slack/MM-like плотность.
- **Strangler, не big-bang:** текущий `ChatPanelView` с Avalonia — **временный мост**, подлежит сужению до пустого `Panel`-хоста.
- **Код v0 (2026-05-18):** compact layout на Avalonia — **тактический** компромисс до фазы 1–2; **не** эталон UX.

---

## Контекст

После [0120](0120-primary-work-surface-intercom-or-editor.md) Intercom может занимать **Forward**. Пользователь ожидает опыт уровня **Cursor / Slack / Mattermost**: одна вертикальная «колонна разговора», минимум рамок, максимум смысла на пиксель.

**Сейчас в коде:**

| Слой | Технология | Содержимое |
|------|------------|------------|
| Лента, topic cards, spine strip | **Skia** (`SkiaChatSurfaceControl`, entity pipeline) | Сообщения, overview, ветки |
| Шапка, spine-редактор, уточнения | **Avalonia** (`ChatPanelView`, UiKit) — **долг** | Секции, TextBox, ListBox |
| Ввод + slash autocomplete | **Avalonia** — **долг** | [0119](0119-chat-slash-commands-intercom-surface.md) |

Два визуальных «мира» в одной панели: Skia выглядит как продукт, Avalonia-остров — как чужой Fluent-виджет. При `primary_work_surface = intercom` это блокирует позиционирование Intercom как центра сессии ([0121](0121-intent-oriented-programming-paradigm.md)).

---

## Проблема

1. **Чужой chrome:** стили `cascadeSection` / Fluent `TextBox` не дают плотного «мессенджерного» ритма; полировка UiKit **не** решает — нужен **свой** render-слой.
2. **Нарушение [0044](0044-avalonia-host-skia-agent-chat-surface.md):** в зоне агента снова вырастает полноценный Avalonia-UI вместо «фюзеляж + прибор».
3. **Два скролла / два ритма:** лента Skia vs composer Avalonia — нет **единой колонны**.
4. **Ограничение роста:** группировка, meta-строки, inline code, slash-popup — в Skia; наращивание Avalonia-контролов — тупик.
5. **Риск big-bang:** IME/ввод/доступность — инженерная задача фазы 2, а не повод оставлять `TextBox` навсегда ([0044](0044-avalonia-host-skia-agent-chat-surface.md) отверг только «одним махом без модели», не Skia-composer как цель).

---

## Целевое UX (ориентиры Slack / MM)

Не копировать пиксель-в-пиксель, а зафиксировать **инварианты плотности**:

| Инвариант | Смысл |
|-----------|--------|
| **Одна колонна** | Вертикальный поток: (опционально) toolbar → лента → composer; один scroll для истории |
| **Роли визуально различимы** | user / assistant / system / tool — фон, выравнивание или полоска, без «простыни одного стиля» |
| **Meta одной строкой** | Автор · время · ветка — 11–12px, приглушённый цвет; тело 13–14px |
| **Группировка** | Подряд идущие сообщения одного автора — без повторения заголовка (фаза 2+) |
| **Composer всегда снизу** | Фиксированная зона ввода; slash-list **над** полем, не отдельная «карточка» |
| **Overview не ломает колонну** | Картотека тем — режим той же поверхности (уже есть `OverviewMode`), не отдельная «страница Avalonia» |

**Spine:** в Forward — **только** компактная Skia-полоска ([0096](0096-intercom-topic-card-summary-and-product-spine.md)); полный редактор spine — MFD / палитра / modal (IDS), не Expander в ленте.

---

## Решение

<a id="adr0123-p0"></a>

### Принцип: фюзеляж vs наш surface

| Слой | Технология | Примеры |
|------|------------|---------|
| **Фюзеляж** | Avalonia | `MainWindow`, `AttentionZoneContainer`, splitters, `MfdHostWindow`, **пустой** host `Panel` для Intercom |
| **Тяжёлый контент** | Avalonia + специализированные контролы | **AvaloniaEdit**, Terminal, Git diff views, Rich settings |
| **Intercom (цель)** | **Skia** (+ SkiaKit) | Лента, topic cards, composer, slash popup, toolbar chip |
| **IDS / редкие формы** | Avalonia overlay **поверх** Skia | Clarification batch, мастера; не встроенные `Expander` в ленте |

**Запрещено как steady state:** `ChatPanelView` с `CascadeSection` + `TextBox` + `ListBox` как основной UX Intercom.

<a id="adr0123-p1"></a>

### Поэтапный strangler

### Фаза 0 — тактический долг (2026-05-18)

- `primary_work_surface = intercom`, `SkiaChatDensity` / `CompactLayout`.
- Урезанный Avalonia chrome — **временно**, до выноса toolbar/composer в Skia.

### Фаза 1 — **Единая Skia-колонна (лента + chrome)**

`IntercomSkiaSurface` (rename от `SkiaChatSurfaceControl`) на **весь** клиент зоны; `ChatPanelView` → `Content="{Binding …}"` одного control или `Panel` без дочерних Avalonia-виджетов:

- Toolbar, chip загрузки, кнопка overview — **рисуются в Skia** (или одна строка в layout engine).
- Никаких `CascadeSection` / `Expander` spine в Forward.
- Один вертикальный scroll истории внутри surface.

**Критерий:** в дереве визуала под Forward для Intercom — **нет** `TextBox`/`ListBox`/`Button` Fluent-стиля (кроме будущего IDS-overlay).

### Фаза 2 — **Skia composer + slash (целевой ввод)**

- Composer: Skia-рамка, placeholder, кнопка send, многострочный рост по содержимому.
- **Ввод текста:** **`ITextInputMethodClient`** на Avalonia **12** (`TopLevel.TextInputMethod.SetClient`) + отрисовка composer в Skia из `ChatInput` (preedit через `SetPreeditText`). **Без** видимого Fluent `TextBox` в steady state.
- Slash: `SkiaPopupList` (SkiaKit), hit-test; данные из `ChatSlashAutocomplete` ([0119](0119-chat-slash-commands-intercom-surface.md)).
- Клавиши: `ChatSendKeyMatcher`, Tab/↑/↓/Esc — на `IntercomSkiaSurface`.

**Критерий:** пользователь не видит «чужих» Avalonia-контролов в Intercom; ввод не хуже нативного по кириллице/IME на Windows (ручной чеклист).

**Отклонено для steady state:** «прозрачный `TextBox` поверх Skia» — оставляет Fluent-семантику и дублирует фюзеляжный UI в продуктовой зоне.

### Фаза 3 — **Богатая лента (ёмкость)**

- Группировка сообщений, схлопнутые блоки кода (mono strip), сворачиваемый «thinking».
- ~~Markdown subset в Skia~~ → **v1.1:** `SkiaMarkdownLayout` в prose; fenced — `ChatMessageBodyPresentation` — канон [0129](0129-intercom-message-body-markdown-and-fenced-code.md).
- ~~Копирование выделения~~ → **v1.1:** выбранное сообщение + Ctrl+C → clipboard (`ChatSurfaceSnapshotMessageLookup`).

### Фаза 4 — **Только тяжёлые IDS-оверлеи на Avalonia**

- **Clarification batch** ([0031](0031-agent-chat-clarification-batches-and-threading.md)) — отдельный **overlay** / flyout (IDS), не секция в `ChatPanelView`; допустимы Avalonia-формы как «тяжёлый» контур.
- **AI settings**, **Terminal**, **Editor** — без изменений по технологии.
- **MFD → Chat page:** после стабилизации Forward — **тот же** `IntercomSkiaSurface` (comfortable density), **не** отдельный UiKit-экран.

<a id="adr0123-p2"></a>

### Архитектурные границы (не менять)

```
ChatSurfaceSnapshot (Features/Chat, CCU)
        ↓
ChatSurfaceEntityFactory → ISkiaChatEntity[]
        ↓
SkiaChatLayoutEngine → SkiaChatPlacedEntity[]
        ↓
Skia draw (Views/Chat/Skia, SkiaKit)
```

- ViewModel **не** импортируется из Skia-слоя ([0117](0117-ide-skia-kit.md)).
- Новые примитивы composer/slash — сначала **SkiaKit** (например `SkiaComposerStrip`, `SkiaPopupList`), затем использование в чате.
- Токены: `SkiaChatTheme` / `SkiaKitThemeBridge` ← `CascadeTheme.*` ([ide-chrome-tokens-v1.md](../design/ide-chrome-tokens-v1.md)).

### Плотность (один surface, два пресета)

| Пресет | Когда | Метрики |
|--------|-------|---------|
| **Compact** | Forward + `primary_work_surface = intercom` | `SkiaChatDensity` |
| **Comfortable** | MFD Chat, узкая колонка | чуть больше padding, те же примитивы |

Один `IntercomSkiaSurface`, без второго Avalonia-макета.

---

## Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| **Дальше полировать UiKit в чате** | Не достигает Slack/MM плотности; закрепляет «два мира»; против [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) |
| **Прозрачный Avalonia `TextBox` в composer** | Всё ещё Fluent-контрол в продуктовой зоне; против принципа фюзеляжа |
| **Всё на Avalonia** `ItemsControl` | Против [0044](0044-avalonia-host-skia-agent-chat-surface.md) |
| **Full-Skia для clarification forms** | Слишком дорого; оверлей IDS на Avalonia — ок |
| **Big-bang без фаз 1–2** | Риск IME; [0044](0044-avalonia-host-skia-agent-chat-surface.md) |
| **Отдельное окно чата** | Ломает [0017](0017-multi-window-workspace-and-agent-surfaces.md), [0120](0120-primary-work-surface-intercom-or-editor.md) |

---

## Последствия

- **Плюсы:** единый визуальный язык, плотность, проще анимации и hit-test; лучше стык с semantic/graph surfaces ([0067](0067-graph-backed-surfaces-contract.md)).
- **Минусы:** две реализации composer (compact/comfortable) до схождения; тесты UI сложнее — опора на snapshot/layout unit tests + немного headless Skia bounds tests.
- **MCP / агент:** без изменений wire; `ChatInput` для MCP может оставаться VM-строкой, UI — проекция.

---

## Метрики успеха (для принятия Implemented)

1. Forward + intercom: **один** визуальный scroll-контур для истории.
2. В Intercom **нет** видимых Avalonia `TextBox` / `ListBox` / `CascadeSection` (кроме IDS-overlay уточнений).
3. Плотность: ≥ на 20% больше видимых строк истории при 1080p на той же истории (эвристика, сравнение до/после скриншота).
4. Регрессии: [0119](0119-chat-slash-commands-intercom-surface.md) autocomplete, Enter-send, overview/detail — зелёные тесты/ручной чеклист.

---

## Открытые вопросы

1. **Platform input:** `InputMethod` + offscreen presenter vs отдельный нативный HWND для composer на Windows — спайк в фазе 2.
2. *(закрыто — [0129](0129-intercom-message-body-markdown-and-fenced-code.md))* лента: Skia subset + fenced; полный MD — preview [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) по действию на сообщении.
3. **Clarifications IDS:** bottom sheet vs боковая панель (оба — Avalonia overlay, не встроенный блок).
4. **Приоритет:** фаза 1 (убрать Avalonia-остров) перед фазой 3 (группировка сообщений) — **рекомендуется**.

---

## История изменений

| Дата | Изменение |
|------|-----------|
| 2026-05-18 | Proposed: full-Skia evolution, фазы 0–4, открытые вопросы. |
| 2026-05-18 | Уточнение: Avalonia только фюзеляж + тяжёлые контуры; отказ от steady-state Avalonia в Intercom; composer через `ITextInputMethodClient` (Avalonia 12), не Fluent TextBox. |
| 2026-05-18 | **Accepted.** Фаза 1 в коде: Forward shell, Skia toolbar, `intercomComposer` strip (временный TextBox до фазы 2). |
| 2026-05-17 | **Implemented (фазы 1–3 v1):** `IntercomSkiaSurface`, Skia composer + `SkiaPopupList` + IME client; MFD на том же surface; группировка ленты, `SkiaMonoCodeStrip`, double-click thinking toggle. Markdown subset и copy — открыто. |
| 2026-05-18 | **Render fix:** offscreen `WriteableBitmap` + `DrawImage` (не `SKCanvas.Clear` на leased canvas окна — [Avalonia #5932](https://github.com/AvaloniaUI/Avalonia/issues/5932)). |
| 2026-05-18 | **Фаза 3 (v1.1):** inline Markdown subset (`**` / `*` / `` ` ``) в prose; Ctrl+C копирует тело выбранного сообщения; MFD Chat на `IsSkiaIntercomHostVisible` (comfortable `CompactLayout=false`). |
| 2026-05-20 | **Flat feed в ленте:** `SkiaChatBubbleKind.Feed` — user/agent/thinking/tool, slash outcome, clarification, nav-строки без messenger-пузыря; акцент — selection / branch / ошибка. |