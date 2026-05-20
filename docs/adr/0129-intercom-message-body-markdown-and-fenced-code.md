# ADR 0129: Intercom — тело сообщения: Markdown, fenced code и preview

**Статус:** Accepted · In progress  
**Дата:** 2026-05-19

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0123](0123-intercom-full-skia-surface-evolution.md) | Skia-лента; фазы отрисовки сообщений |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | **AttachmentAnchor** — ссылка на workspace; ортогонально fenced |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | `content` сообщения в event log |
| [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | Полный Markdown: Markdig, MFD / окно |
| [0023](0023-markdown-diagrams-language-tooling.md) | Диаграммы, Kroki (опционально в preview сообщения) |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Composer в Forward |

### Вне ADR (playbook)

| Документ | Роль |
|----------|------|
| [intercom-ux-reference-slack-mattermost-v1.md](../design/intercom-ux-reference-slack-mattermost-v1.md) | Flat feed, макеты ленты |
| [intercom-design-hub-v1.md](../design/intercom-design-hub-v1.md) | Домены D1–D9 |

## Резюме

Зафиксировать, как **тело реплики** (`content` как markdown-строка) показывается в ленте Intercom и когда открывается **полный** preview — **отдельно** от [0128](0128-intercom-attachment-anchors-and-code-references.md) (attach / chip / reveal в репозиторий).

1. **Fenced code** (`` ```lang … ``` ``) — первый класс: сегменты prose + code, mono strip в Skia; не `AttachmentAnchor`.
2. **Prose в ленте** — inline subset (`**`, `*`, `` ` ``) через `SkiaMarkdownLayout`; не полный CommonMark в scroll.
3. **Полный MD** — reuse [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) по действию на сообщении, не Markdig в каждой строке ленты.
4. **Парсер attach** ([0128](0128-intercom-attachment-anchors-and-code-references.md) §5): `[…]` **не** разбирать внутри fenced code-сегментов.

**Не входит:** attach, chips, `/attach`, reveal в редактор; composer WYSIWYG; syntax-highlight engine с нуля (stretch — lang tag).

---

## Контекст

Агент и человек шлют в Intercom **markdown**: заголовки, списки, fenced listings, иногда таблицы. В CIDE уже есть:

- **Лента:** `ChatMessageBodyPresentation.SplitSegments` + `SkiaMonoCodeStrip` + `SkiaMarkdownLayout` ([0123](0123-intercom-full-skia-surface-evolution.md) фаза 3).
- **Preview:** `MarkdigMarkdownPreviewRenderer`, MFD tool, MCP `ide_show_preview` ([0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md)).

Без ADR смешивают: «код в чате» = attach к файлу; или требуют полный Markdig в scroll (perf, hit-test); или парсят `[M:Foo]` внутри `` ``` `` в ответе агента.

**Граница с [0128](0128-intercom-attachment-anchors-and-code-references.md):**

| | Этот ADR (0129) | [0128](0128-intercom-attachment-anchors-and-code-references.md) |
|--|-----------------|-------------------------------------------------------------------|
| Смысл | Цитата / форматирование **в тексте** сообщения | **Указатель** на артефакт workspace |
| Wire | `content` (строка) | `AttachmentAnchor[]` + offsets в prose |
| Клик | Копировать / развернуть fence; «Открыть как MD» | Reveal в редакторе |

---

## Проблема

1. **Один парсер на всё:** attach-токены и markdown fences конкурируют за `[` и `` ``` ``.
2. **Два ожидания рендера:** «как Slack» в ленте vs «как документ» для длинного ответа.
3. **v1 ограничения кода:** один fence, без lang-highlight — не задокументированы → кажется багом.
4. **Дублирование движка:** второй Markdig в Skia ломает [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) и [0123](0123-intercom-full-skia-surface-evolution.md).

---

## Решение

<a id="adr0129-p1"></a>

### 1. Канон хранения

- Тело сообщения в [0045](0045-agent-chat-persistence-event-log-and-projections.md): поле **`content`** — UTF-8 markdown source (как пришло от агента/человека).
- **Не** дублировать fenced blocks в отдельных файлах в v1 (тяжёлые вложения — отдельная политика [0045](0045-agent-chat-persistence-event-log-and-projections.md) §формат).
- Проекция UI: `ChatMessageBodySegment[]` — вычисляется при render, не обязательно в event log v1.

<a id="adr0129-p2"></a>

### 2. Fenced code (канон)

**Fenced — да:** `` ```optional-lang … ``` `` разбивает тело на сегменты:

| `ChatMessageBodySegmentKind` | Отрисовка |
|------------------------------|-----------|
| `Prose` | `SkiaChatBubbleRenderer` + `SkiaMarkdownLayout` (inline subset) |
| `Code` | `SkiaMonoCodeStrip` — inset, mono, перенос |

| Аспект | Fenced | [0128](0128-intercom-attachment-anchors-and-code-references.md) attach |
|--------|--------|------------------------------------------------------------------------|
| Привязка к репо | нет (снимок текста) | да (resolve / re-resolve) |
| Клик по умолчанию | копировать / expand | reveal → файл |
| В prompt агента | текст блока | + `excerpt`, `memberKey`, file |

**v1 (код):** `SplitSegments` — **первый** fenced block + tail prose; без lang-highlight.

**v1.1:** все fences в сообщении; copy chip на блоке; опционально lang → лёгкая подсветка или тот же strip.

**v2:** collapse длинного fence (порог строк — открытый вопрос).

<a id="adr0129-p3"></a>

### 3. Inline Markdown в prose (лента)

Подмножество в `SkiaMarkdownLayout` ([0123](0123-intercom-full-skia-surface-evolution.md)):

- `**bold**`, `*italic*`, `_italic_`, `` `code` ``
- Заголовки `#`, списки, таблицы — **не** в v1 ленте (видны как plain или через preview)

Лимит тела при measure: trim ~32k символов в `SkiaChatBubbleRenderer` (защита scroll).

<a id="adr0129-p4"></a>

### 4. Полный Markdown (reuse preview)

**Целый `.md` в `content` допустим.** Рендер:

| Поверхность | Механизм |
|-------------|----------|
| Лента (steady) | §2–3 — subset + fences |
| Preview | [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md): `MarkdownPreviewPayloadBuilder` → `MarkdigMarkdownPreviewRenderer` |
| MCP | `ide_show_preview`, `show_markdown_preview_page`, `chat_export_readable` |

**Не** встраивать Markdig/WebView в каждую строку Skia-ленты.

**Действие** *(фаза 2)*: `command_id` **`intercom.open_message_markdown_preview`** — `content` сообщения → тот же pipeline, что [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md); заголовок «Сообщение #N».

**Эвристика UI** *(фаза 3, опционально)*: chip «Открыть как Markdown», если в теле есть `##` / таблица / длина > N строк.

Composer остаётся **source** markdown; WYSIWYG в composer — не цель v1.

<a id="adr0129-p5"></a>

### 5. Согласование с attach-парсером ([0128](0128-intercom-attachment-anchors-and-code-references.md))

Порядок разбора тела сообщения для UI:

1. `SplitSegments` → prose | code regions.
2. Attach bracket parse **только** в prose-сегментах (и в composer до send).
3. В code-сегментах `[M:Foo]` — литералы, не anchors.

Экранирование `\[` в prose — фаза 2+ [0128](0128-intercom-attachment-anchors-and-code-references.md).

---

## Фазы внедрения

| Фаза | Содержание | Зависимости | CIDE |
|------|------------|-------------|------|
| **0** | Документировать v1 ограничения; тесты `SplitSegments` (несколько fence) | — | **да** — `ChatMessageBodyPresentation.SplitSegments` (первый fence + tail); `ChatMessageBodyPresentationTests` |
| **1** | Несколько fenced blocks; copy на блоке | 0123 | **нет** |
| **2** | `intercom.open_message_markdown_preview` → [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | 0069 | **нет** |
| **3** | Collapse long fence; chip «Открыть как MD» по эвристике | 0123 | **нет** |
| **4** *(stretch)* | Lang highlight в strip; Kroki в preview тела сообщения | 0023 | **нет** |

**Граница с attach ([0128](0128-intercom-attachment-anchors-and-code-references.md)):** prose vs fenced — в коде v1; bracket attach в prose **не** парсится в ленте (только slash/MCP reveal-select по [0131](0131-editor-slash-select-code-by-bracket-reference.md)).

---

## Не цели

- AttachmentAnchor, `/attach`, reveal ([0128](0128-intercom-attachment-anchors-and-code-references.md)).
- Замена [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) для файлов `.md`.
- CommonMark-совместимый рендер **внутри** scroll ленты.
- Подсветка синтаксиса как в VS Code (отдельный проект / lib).

---

## Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| Markdig в каждой строке ленты | Perf, layout, hit-test; дубли [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) |
| Fenced = attach автоматически | Разный смысл; агент часто шлёт учебный snippet без файла |
| Plain text only в ленте | Уже есть markdown от агента; хуже читаемость |
| Отдельный event type «code_block» | Дубли `content`; fences выводятся из markdown |

---

## Согласованные решения

| Тема | Решение |
|------|---------|
| Fenced в ленте | **Да**, канон; расширять парсер |
| Полный MD | В `content` **да**; полный рендер — [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) по действию |
| Attach `[` в fence | **Не парсить** |
| v1 fences | Один блок — известное ограничение до фазы 1 |

---

## Открытые вопросы

1. Порог collapse fenced block (строки; по умолчанию свёрнут >40?).
2. `open_message_markdown_preview`: MFD tool vs отдельное окно по умолчанию.
3. Kroki / diagram expansion для тела сообщения в preview ([0023](0023-markdown-diagrams-language-tooling.md)).
4. Несколько `` ``` `` подряд без prose между — merge или отдельные strips.
5. Lang tag: игнорировать vs отображать в заголовке strip.

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-19 | Proposed: вынесено из [0128](0128-intercom-attachment-anchors-and-code-references.md) §11–12; fenced + MD preview. |
| 2026-05-20 | **Accepted · In progress**; фаза 0 в CIDE; колонка CIDE; граница с attach/0131. |
