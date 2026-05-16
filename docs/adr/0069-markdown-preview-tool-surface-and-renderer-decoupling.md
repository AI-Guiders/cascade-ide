# ADR 0069: Markdown Preview — инструмент MFD, renderer-first decoupling и отказ от inline preview в документе

**Статус:** Accepted  
**Дата:** 2026-04-19

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD vs MFD; длинные тексты и вторичные потоки — в MFD |
| [0023](0023-markdown-diagrams-language-tooling.md) | Markdown authoring, диаграммы, export expanded |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | прежний канон размещения; этим ADR **superseded** по архитектуре surface/placement |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | WebView на MFD как отдельный доверенный/ограниченный слой, не базовый renderer |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | канал → surface |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | инструмент vs хром IDE |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | разделение payload / projection / slot |

## Контекст

Текущая реализация preview Markdown выросла как **виджет внутри документа** (`DockDocumentView`, `forward_split`) с дополнительным окном и незавершённым `mfd`-маршрутом из [0026](0026-markdown-preview-surfaces-and-placement.md). На практике это привело к неверной связности:

1. **Документ зависит от preview-renderer'а.** Падение или бинарная несовместимость preview-библиотеки ломает открытие файла как таковое.
2. **Placement и renderer смешаны.** Inline preview в документе одновременно решает *где* показывать и *чем* рендерить.
3. **`forward_split` противоречит модели внимания.** Для длинных Markdown/docos превью в лобовом редакторе отнимает площадь у primary work surface, хотя по [0021](0021-pfd-mfd-cockpit-attention-model.md) чтение длинных вторичных материалов естественнее для **MFD**, а не для **PFD** и не для primary forward surface.
4. В продукте уже существует **authoring-линия Markdown**: include, Kroki/diagram expansion, export expanded, потенциальное authoring-расширение поверх Markdown ([0023](0023-markdown-diagrams-language-tooling.md)). Preview должен быть **потребителем результата authoring-пайплайна**, а не архитектурным хозяином Markdown-файла.

Дополнительный триггер: inline preview, жёстко зависящий от стороннего Avalonia Markdown-renderer, стал источником runtime-крэша при открытии документа. Это подтвердило, что preview нужно изолировать как **инструмент**, а не как обязательную часть жизненного цикла вкладки документа.

---

## Решение

<a id="adr0069-p1"></a>

### 1. Preview Markdown больше не живёт внутри `DockDocumentView`

`DockDocumentView` и редактор документа **не должны** содержать обязательный preview-renderer в визуальном дереве по умолчанию.  

**Инвариант:** файл Markdown открывается и редактируется **даже если** ни один preview renderer недоступен или временно падает.

`forward_split` как основная архитектурная форма **снимается с канона**. Мы не вводим deprecated-режим ради совместимости: продукт пока не обязан сохранять старую UX-топологию.

<a id="adr0069-p2"></a>

### 2. Preview Markdown становится отдельным инструментом (tool surface)

Preview рассматривается как **отдельный инструмент / вторичная поверхность**, а не как “часть документа”. Базовое целевое размещение:

- **Primary:** `mfd_tool`
- **Secondary:** `separate_window`

PFD для preview не используется: по [0021](0021-pfd-mfd-cockpit-attention-model.md) PFD держит situational awareness и текущий контекст, а не длинные тексты/доки.

<a id="adr0069-p3"></a>

### 3. Placement и renderer — две независимые оси

Нужно явно разделить:

| Ось | Вопрос |
|-----|--------|
| **Placement** | Где показать preview: MFD tool, отдельное окно, в будущем иная secondary surface |
| **Renderer** | Чем рендерить markdown: native renderer, WebView renderer, fallback-заглушка |

**Инвариант:** смена renderer'а не должна менять семантику surface, а смена placement не должна требовать другой модели данных preview.

<a id="adr0069-p4"></a>

### 4. Канон renderer'ов: native first, WebView optional

Базовый канон:

- **`native_markdig`** — целевой основной renderer.
- **`webview_html`** — отдельный renderer/adaptor, допустимый **только** как secondary MFD-oriented surface или отдельное окно; не базовый путь.
- **`disabled` / `unavailable`** — явный fallback без падения документа.

**Почему `native_markdig`:**

- лучше контролируется из Avalonia-host без жёсткой зависимости от веб-стека;
- легче держать внутри модели surfaces/tools;
- проще встроить в authoring pipeline и подчинить продуктовым ограничениям;
- не ломает PFD/MFD-семантику и не делает preview “маленьким браузером по умолчанию”.

**Почему `webview_html` всё равно нужен:**

- он уже планируется по [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md);
- он полезен для richer HTML-like rendering, когда MFD сознательно выступает вторичной rich surface.

Но **WebView не становится каноном preview как такового**: это только один renderer-adaptor.

<a id="adr0069-p5"></a>

### 5. Preview потребляет authoring pipeline, а не владеет им

Preview-слой получает уже подготовленный **source payload**:

- raw markdown,
- expanded markdown (после include / diagram expansion),
- metadata (title, source file, origin, возможно link-map / anchor map),
- renderer options.

Authoring-расширение Markdown остаётся отдельной линией продукта:

- оно отвечает за authoring affordances;
- preview отвечает только за **presentation** уже сформированного payload;
- contract публикации / `export expanded markdown` из [0023](0023-markdown-diagrams-language-tooling.md) остаётся приоритетнее, чем “ещё один красивый runtime renderer”.

<a id="adr0069-p6"></a>

### 6. Рекомендуемая модель в коде

Вводится явная абстракция наподобие:

- `MarkdownPreviewSource` / `MarkdownPreviewPayload`
- `IMarkdownPreviewRenderer`
- `MarkdownPreviewToolViewModel`
- `MarkdownPreviewToolView`

Минимальный renderer-contract:

1. принять payload preview,
2. либо вернуть Avalonia `Control`,
3. либо обновить уже существующий host,
4. при сбое деградировать в “preview unavailable”, а не бросать исключение наружу в документ/tool host.

---

## Последствия

### Позитивные

- Открытие документа перестаёт зависеть от preview renderer'а.
- Архитектура лучше совпадает с [0021](0021-pfd-mfd-cockpit-attention-model.md): длинное чтение и rich preview — в MFD, не в PFD и не в forward core.
- Легче добавлять несколько renderer'ов без перешивания всей UI-топологии.
- Authoring extension Markdown может эволюционировать отдельно от визуального preview.

### Стоимость

- Придётся снять старый `forward_split` путь и переподключить UX-команды preview.
- Нужен новый tool/page в secondary shell.
- Нужен собственный native renderer поверх `Markdig` вместо reliance на случайный сторонний Avalonia Markdown control как фундамент.

---

## Не-цели

- Не делать сейчас полноценный WYSIWYG-редактор Markdown.
- Не обещать пиксельную совместимость с GitHub/VS Code preview.
- Не превращать preview в общий браузерный runtime для любых rich surfaces.
- Не переносить preview в PFD.

---

## Изменение канона по сравнению с ADR 0026

Этот ADR **superseded** [0026](0026-markdown-preview-surfaces-and-placement.md) в части:

- `forward_split` как канонического placement,
- embedding preview внутрь `DockDocumentView`,
- implicit coupling между placement и renderer.

Из [0026](0026-markdown-preview-surfaces-and-placement.md) сохраняют смысл, но читаются через новый слой:

- preview как secondary surface, а не часть authoring semantics;
- внутренние ссылки/peek как UX-возможность preview;
- отдельное окно как допустимый secondary placement.

---

## Альтернативы

| Вариант | Почему отклонён |
|--------|------------------|
| Оставить inline preview в документе и только поменять библиотеку | Не решает архитектурную связность: документ всё ещё зависит от preview |
| Сделать WebView базовым renderer'ом для всего preview | Слишком тяжёлый и вторгающийся центр тяжести; хуже для native surface-модели |
| Держать только отдельное окно и не делать MFD tool | Противоречит целевой роли MFD как secondary instrument surface |
