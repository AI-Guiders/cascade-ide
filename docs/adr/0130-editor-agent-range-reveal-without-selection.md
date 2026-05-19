# ADR 0130: Подсветка диапазона кода для агента без изменения selection

**Статус:** Proposed  
**Дата:** 2026-05-19

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | Якоря в Intercom; **reveal из ленты** (`intercom.reveal_attachment`) — тот же presentation mode |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id`; паритет MCP / UI |
| [0013](0013-command-surface-and-discoverability.md) | Discoverability; `highlight_control` как прецедент |
| [0111](0111-editor-linenumber-linerange-value-objects.md) | `LineRange` 1-based в домене и MCP args |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | Редактор — источник правды; reveal не подменяет правку |
| [0124](0124-slash-parametric-editor-line-commands.md) | `/editor line select` — **меняет** selection |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Активный редактор в Forward / Mfd |

### Вне ADR (playbook)

| Документ | Роль |
|----------|------|
| [intercom-ux-reference-slack-mattermost-v1.md](../design/intercom-ux-reference-slack-mattermost-v1.md) | Reveal = рамка, не selection; паритет с `highlight_control` |
| [cascade-ide-ui-layout-v1.md](../ui-ux/cascade-ide-ui-layout-v1.md) | `AgentHighlightOverlay` для контролов |

## Резюме

Зафиксировать **безопасный показ участка кода человеку по запросу агента** (MCP), **без** сообщения в Intercom и **без** изменения `Selection` / каретки в буфере редактора.

1. **`command_id`:** `editor.reveal_range` · MCP: `reveal_editor_range` / `ide_reveal_editor_range`.
2. **Эффект:** при необходимости open файла → scroll into view → **transient** фон строк (gutter-band, `IBackgroundRenderer`) ~3 с; опционально рамка по цвету агента (`#FF6B9D`, как `highlight_control`).
3. **Не делает:** `SelectInEditor`, `ApplyEdit`, attach в event log.
4. **Ортогонально:** `go_to_position` (агент готовит правку → select); `intercom.reveal_attachment` (клик человека из ленты по `AttachmentAnchor`).

**v1 реализация:** line-based `file_path` + `start_line` + `end_line`; общий сервис `EditorAgentRangeReveal` на активном `TextEditor`.

**Не входит (v2+):** `memberKey` / re-resolve Roslyn; `syntaxScope`; overlay-рамка поверх глифов; настройка длительности в TOML; паритет hotkey для человека.

---

## Контекст

Сегодня агент, чтобы «показать» фрагмент, вынужден вызывать **`go_to_position`**, который внутри вызывает **`SelectInEditor`**. Это меняет выделение в буфере: оператор может случайно затереть фрагмент одной клавишей после подсказки агента.

Для **контролов** уже есть **`highlight_control`**: transient рамка (`UiAgentHighlight` / `AgentHighlightOverlay`), без чата, без изменения состояния контрола.

Для **кода** [0128](0128-intercom-attachment-anchors-and-code-references.md) описывает тот же UX для **клика по attach** в Intercom, но **не выделяет** отдельный MCP для агента — этот ADR закрывает пробел.

---

## Решение

<a id="adr0130-p1"></a>

### 1. Термины

| Термин | Значение |
|--------|----------|
| **Reveal (editor)** | Показать диапазон в редакторе: scroll + transient highlight, **не** selection |
| **Select (editor)** | Изменить `Selection` / каретку — `go_to_position`, `/editor line select` |
| **Attach** | Payload в сообщении Intercom — [0128](0128-intercom-attachment-anchors-and-code-references.md) |

<a id="adr0130-p2"></a>

### 2. `command_id` и MCP

| Слой | Идентификатор |
|------|----------------|
| Канон | `editor.reveal_range` |
| MCP wire | `reveal_editor_range` |
| Префикс IDE MCP | `ide_reveal_editor_range` |

**Аргументы v1 (1-based):**

| Поле | Обязательность | Смысл |
|------|----------------|--------|
| `file_path` | да | Путь к файлу; при необходимости open/activate вкладку |
| `start_line` | да | Первая строка диапазона (inclusive) |
| `end_line` | да | Последняя строка (inclusive); ≥ `start_line` |

**Возврат:** `OK` или текст ошибки (файл не открыт, нет редактора, невалидный диапазон).

<a id="adr0130-p3"></a>

### 3. Поведение (паритет `highlight_control`)

| Шаг | Действие |
|-----|----------|
| 1 | Нормализовать `file_path`; если вкладки нет — `open_file` / activate document |
| 2 | Найти `TextEditor` для пути ([`EditorActiveDockResolver`](../Services/EditorActiveDockResolver.cs)) |
| 3 | Прокрутить диапазон в видимую область **без** смены `Caret.Offset` и **без** `Selection` |
| 4 | Установить transient highlight на строки `start_line`…`end_line` (`EditorAgentRangeRevealBackgroundRenderer`) |
| 5 | Снять highlight через ~3 с (как `UiAgentHighlight`) |

**Цвет v1:** фон `Color.FromArgb(48, 255, 107, 157)`; **одна** рамка pen `#FF6B9D` вокруг **единого** прямоугольника на весь диапазон строк (не построчные «плитки») — визуальная связь с `AgentHighlightOverlay`.

<a id="adr0130-p4"></a>

### 4. Ортогональность

| Действие | Меняет selection? | Сообщение в Intercom? |
|----------|-------------------|------------------------|
| `editor.reveal_range` | **нет** | нет |
| `go_to_position` | **да** | нет |
| `intercom.reveal_attachment` | нет (по умолчанию) | контекст из ленты |
| `/attach selection` | нет | **да** (черновик/отправка) |

<a id="adr0130-p5"></a>

### 5. Связь с [0128](0128-intercom-attachment-anchors-and-code-references.md)

- **Общий presentation mode:** transient range highlight, не selection.
- **Разные входы:**
  - **0128** — `AttachmentAnchor` + клик / будущий reveal по anchor id;
  - **0130** — прямой MCP от агента по `file_path` + lines (или позже member).
- Реализация **должна переиспользовать** `EditorAgentRangeReveal` (один `IBackgroundRenderer` + таймер), чтобы не плодить два визуальных языка.

<a id="adr0130-p6"></a>

### 6. Фазы

| Фаза | Содержание |
|------|------------|
| **0** *(текущий PR)* | ADR; `reveal_editor_range`; `EditorAgentRangeReveal` + line range |
| **1** | `intercom.reveal_attachment` вызывает тот же сервис после re-resolve anchor ([0128](0128-intercom-attachment-anchors-and-code-references.md) §8) |
| **2** | Те же поля, что у `AttachmentAnchor` в wire: опциональные **`memberKey`**, **`syntaxScope`** (JSON-объект, не prose `[M:…]`), **`duration_ms`**; re-resolve member/scope у получателя (Roslyn). MCP **не** парсит скобки в тексте сообщения — только структурированные args / `anchor_json` |
| **3** | Настройка `intercom.attachment_navigate` / agent default: reveal vs select |

---

## Последствия

**Плюсы:** безопасный «укажи глазами» для агента; симметрия с `highlight_control`; меньше случайных правок после `go_to_position`.

**Минусы:** второй путь навигации — агенту нужен промпт/док: *показать* → `reveal_editor_range`, *править* → `go_to_position`.

---

## Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| Флаг `highlight_only` у `go_to_position` | Смешивает navigate-for-edit и reveal; ломает контракт существующего тула |
| Только attach в чат | Агент не должен слать сообщение, чтобы подсветить код |
| Всегда selection | Небезопасно для оператора — [0128](0128-intercom-attachment-anchors-and-code-references.md) §8 |

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-19 | Proposed: отдельный ADR; v1 MCP + `EditorAgentRangeReveal`. |
