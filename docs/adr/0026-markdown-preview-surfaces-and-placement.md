# ADR 0026: Markdown — поверхности превью и размещение (`workspace.toml`)

**Статус:** Superseded  
**Дата:** 2026-04-08  
**Обновлено:** 2026-04-11 — подраздел «Внутренние отсылки»; ортогонально [0023](0023-markdown-diagrams-language-tooling.md). Подробности — [§ История](#adr0026-history).

> **Superseded — актуальный канон:** [ADR 0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) (MFD tool surface, renderer-first, без `forward_split`). Ниже — **история** размещения через `workspace.toml` и внутренние отсылки; для новых решений опираться только на **0069**.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0010](0010-ui-modes-toml-configuration.md) | `workspace.toml`, merge бандла `UiModes/` и overlay репозитория `.cascade/workspace.toml` |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | превью как вторичная поверхность относительно лобового редактирования |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | отдельное окно как второй `TopLevel` |
| [0022](0022-mfd-visual-design-surface-axaml-blazor.md) | перспектива вкладки/региона на MFD |
| [0023](0023-markdown-diagrams-language-tooling.md) | LSP, диаграммы, Kroki, export — **ортогонально** размещению виджета превью |

## Замена прежних формулировок

- **Этот ADR superseded** новым каноном в [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md): preview больше рассматривается как отдельный tool surface с renderer/placement decoupling, а `forward_split` снят с канона.
- **Исторический канон по размещению превью Markdown** — этот ADR; актуальный канон для surface/renderer-decoupling — [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md).
- Ранее единственное упоминание «Markdown preview» в контексте UX в [0023](0023-markdown-diagrams-language-tooling.md) § «Детали UX» **снято с канона** там и **заменено ссылкой сюда** ([0023](0023-markdown-diagrams-language-tooling.md) остаётся про языковой опыт и диаграммы).
- [0023](0023-markdown-diagrams-language-tooling.md) **не** superseded целиком — только пересечение по «где показывать превью».

---

## Контекст

Нужна одна точка правды для **где** в UI монтируется отрендеренный Markdown: рядом с текстом во **forward** (лобовое), в отдельном **окне**, или в зоне **MFD** (вторичное внимание). Это решение про **хром и топологию виджета**, а не про LSP, include или Kroki.

Конфигурация логично живёт в **`workspace.toml`** вместе с остальными глобальными метриками хрома ([0010](0010-ui-modes-toml-configuration.md)): один merge-слой с бандлом и overlay репозитория, без обратной записи динамического ресайза в шипнутые файлы.

---

## Решение

1. **Ключ TOML:** `markdown_preview_placement` в корне merged **`UiWorkspaceToml`** (модель и snake_case → PascalCase — как у остальных полей `workspace.toml`).

2. **Допустимые строковые значения** (регистр строки для пользователя не важен; в коде парсинг нормализует):
   - **`forward_split`** — вторая колонка у активного документа редактора (`EditorContentGrid` в `DockDocumentView`), inline `MarkdownScrollViewer`. Неактивные вкладки держат ширину колонки превью **нулевой**, чтобы не копить «залипшую» раскладку на фоне.
   - **`separate_window`** или синоним **`window`** — существующее окно `MarkdownPreviewWindow` (вторичный `TopLevel` в смысле [0017](0017-multi-window-workspace-and-agent-surfaces.md), без обязательной привязки к мультиоконной дорожной карте целиком).
   - **`mfd`** — **целевое** размещение во вкладке/регионе зоны MFD ([0021](0021-pfd-mfd-cockpit-attention-model.md), перекрёстно с [0022](0022-mfd-visual-design-surface-axaml-blazor.md)). Пока отдельная вкладка под превью в MFD **не подключена**, поведение — **явный fallback** на отдельное окно (как зафиксировано в коде и комментариях), без молчаливого «как forward».

3. **Значение по умолчанию** до загрузки merged TOML и при сбросе тестов: **`forward_split`** (`MarkdownPreviewPlacementRuntime`).

4. **Связь с моделью внимания [0021](0021-pfd-mfd-cockpit-attention-model.md):** превью остаётся **вторичной** поверхностью относительно набора текста в лобовом редакторе; выбор `markdown_preview_placement` меняет только **геометрию монтирования**, не переопределяя семантику зон PFD/MFD/EICAS.

5. **Не путать** с будущим ключом **общей** «топологии презентации» нескольких `TopLevel` (обсуждение в [0010](0010-ui-modes-toml-configuration.md) / [0017](0017-multi-window-workspace-and-agent-surfaces.md)): `markdown_preview_placement` — **узкий** ключ только для превью Markdown.

### Глубина превью (намерение для v1)

Превью в IDE — **вспомогательная** поверхность ([0021](0021-pfd-mfd-cockpit-attention-model.md)): целевая планка **«достаточно хорошо, чтобы доверять черновику и диаграммам»** (структура, кодовые блоки, ссылки, рендер диаграмм по правилам приватности/Kroki), а не **отдельный продукт-класса Typora/GitHub**.

**Выше приоритетом**, чем «ещё круче визуально в панели превью», держим **контракт публикации**: собранный/развёрнутый документ для наружу — [0023](0023-markdown-diagrams-language-tooling.md) (export expanded, include, согласованность с внешним Markdown).

**Не цель v1** (пока нет явного запроса и боли): пиксель-в-пиксель как GitHub, полноценный WYSIWYG вместо редактора, тяжёлый синхронный скролл с подсветкой блока «как в редакторах только превью». Узкий **forward_split** заведомо отнимает ширину у редактора — кто принципиально не хочет делить лобовое место, выбирает **`separate_window`** / **`window`** или (когда будет) **`mfd`**.

### Внутренние отсылки в длинных документах (peek / «Show Definition»)

**Проблема:** в ADR и длинных спеках часто встречаются отсылки **«см. п. 6 выше»** / **«п. 6»** без якорной ссылки — читателю или автору приходится **крутить** документ, чтобы вспомнить содержание пункта.

**Намерение (не смешивать с языковым слоем [0023](0023-markdown-diagrams-language-tooling.md)):** в **виджете превью** (тот же хост, что задаётся `markdown_preview_placement` выше) — UX в духе **Peek Definition** / hover: наведение или жест на отсылку к **внутреннему** фрагменту текущего файла показывает **краткий оверлей** с целевым абзацем (или переход по клику), **без** обязательной прокрутки редактора.

**Резолв цели:**

- **Предпочтительно в авторинге:** обычные Markdown-ссылки — `[см. п. 6](#adrNNNN-p6)` к якорю `<a id="adrNNNN-p6"></a>` в том же ADR; канон имён — [snippets/adr-anchors-policy.md](snippets/adr-anchors-policy.md) (краткая отсылка: [README ADR](README.md#adr-anchors-policy)).
- **Эвристика (опционально):** распознавание текста вида «п. 6» / «§6» относительно **нумерованного списка** в секции «Решение» и подобных — только если явно окупает поддержку; при **неоднозначности** — **на стороне пользователя** (выбор из кандидатов или переход к якорю), без обязательной магии «лучший экран».

**Приоритет:** ниже **базового** качества превью и **контракта публикации** ([0023](0023-markdown-diagrams-language-tooling.md), export); может войти в **последующую** итерацию после стабильного рендера и размещения по этому ADR.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | отдельное окно превью — уже вторичный `TopLevel` |
---

## Реализация (ориентир по коду)

- Модель: `UiWorkspaceToml.MarkdownPreviewPlacement`, merge: `UiWorkspaceTomlMerger`.
- Рантайм: `MarkdownPreviewPlacement`, `MarkdownPreviewPlacementParser`, `MarkdownPreviewPlacementRuntime`; подключение при загрузке каталога режимов — `UiModeCatalog` (рядом с `UiWorkspaceLayoutRuntimeMetrics`, `AttentionZonePanelRuntime`).
- UI: `DockDocumentView` — сетка редактора и inline-превью; `MainWindow` — ветвление команд превью по `MarkdownPreviewPlacementRuntime.Current`.

---

## Последствия

- Документация для агента/MCP: при смене размещения снимок UI может показывать превью в разных регионах; контракт мульти-корня ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) применим к отдельному окну превью.
- Расширение **`mfd`** без изменения ключа: отдельная поставка — вкладка/хост в MFD shell и снятие fallback.
- Реализация **внутренних отсылок** (подраздел выше): парсер/хост превью или слой поверх рендера; при необходимости — договорённости для авторов ADR (якоря vs «п. N») в [README индекса ADR](README.md), без нового ADR.

---

## Отклонённые альтернативы

- **Держать размещение превью только в пользовательском `settings.toml`** — отклонено для пресета «как у проекта»: команда должна иметь возможность зафиксировать поведение в repo overlay рядом с остальным `workspace.toml`.
- **Сливать с [0023](0023-markdown-diagrams-language-tooling.md) одним ADR** — отклонено: смешивает языковой опыт и геометрию UI, усложняет навигацию и эволюцию по независимым осям.

---

## История изменений

<a id="adr0026-history"></a>

| Дата | Изменение |
|------|-----------|
| 2026-04-08 | подраздел «Глубина превью»: целевая планка v1, non-goals, приоритет export из [0023](0023-markdown-diagrams-language-tooling.md). |
| 2026-04-11 | подраздел **«Внутренние отсылки»** (hover/peek по «см. п. N» и якорям); ортогонально [0023](0023-markdown-diagrams-language-tooling.md). |
