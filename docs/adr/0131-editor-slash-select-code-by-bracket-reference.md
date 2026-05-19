# ADR 0131: `/editor select code` — selection в редакторе по bracket-ссылке `[F:… M:…]`

**Статус:** Accepted (фаза 1)  
**Дата:** 2026-05-19

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0124](0124-slash-parametric-editor-line-commands.md) | `/editor line select 5 10` — select по **номерам строк** |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | Грамматика `F`/`M`/`L`/`S` в `[…]` (§5.1); L2 `;` — **attach** в Intercom; общий parse tree |
| [0130](0130-editor-agent-range-reveal-without-selection.md) | Reveal без selection; MCP `member_key` / `syntax_scope` (JSON) |
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | Roslyn resolve member |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Slash в composer Intercom |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Forward / editor как рабочая поверхность |

## Резюме

Добавить **editor-действие** (меняет `Selection` в буфере), вводимое из **той же bracket-грамматики**, что attach в [0128](0128-intercom-attachment-anchors-and-code-references.md), но **без** сообщения в Intercom:

- Slash: **`/editor select code [M:GetUserAsync]`**, **`/editor select code [Foo.cs M:Bar]`**, **`/editor select code [M:Run S:for:2]`**, L2 **`[F:path; M:name; L:50-100]`** / **`[F:…; M:…; S:for:2]`** ([0128](0128-intercom-attachment-anchors-and-code-references.md) §5.1).
- Опционально паритет: **`/editor reveal code […]`** — тот же parse + resolve, эффект [0130](0130-editor-agent-range-reveal-without-selection.md) (рамка, не selection).

**Не attach:** не создаёт chip, не пишет в event log чата. **Не заменяет** `/editor line select` — дополняет смысловым якорем вместо сырых строк.

**Зависимости:** общий **BracketReferenceParser** (или слой из фазы 2 [0128](0128-intercom-attachment-anchors-and-code-references.md)); **AttachmentAnchorRoslynResolver** / тот же resolve pipeline, что [0130](0130-editor-agent-range-reveal-without-selection.md) фаза 2.

---

## Контекст

Сегодня select в редакторе из чата/палитры:

| Путь | Ограничение |
|------|-------------|
| `/editor line select 5 10` [0124](0124-slash-parametric-editor-line-commands.md) | Нужны номера строк |
| `go_to_position` (MCP) | То же + агент |
| Клик attach + Shift | Только из **ленты** Intercom |

Оператор в **редакторе** или в composer уже мыслит **`[M:Foo]`** ([0128](0128-intercom-attachment-anchors-and-code-references.md) H1). Логично набрать в slash то же выражение и получить **select в активном буфере** — симметрия attach, другой эффект (как `file open` vs `attach file`).

Reveal и select **не XOR** ([0130](0130-editor-agent-range-reveal-without-selection.md) §6.1): две команды с одним parse, разным `command_id`.

---

## Решение (черновик)

### 1. Slash-поверхность

| Slash | `command_id` (черновик) | Эффект |
|-------|-------------------------|--------|
| `/editor select code <bracket-ref>` | `editor.select_code` | Parse bracket → re-resolve → `SelectInEditor` |
| `/editor reveal code <bracket-ref>` *(опционально v1)* | `editor.reveal_code` | Parse → re-resolve → `EditorAgentRangeReveal` |

`<bracket-ref>` — один токен или quoted tail по правилам 0128 §5 (пробелы в path — кавычки / completion).

**Где выполняется:** Intercom composer **и** command palette с контекстом редактора ([0120](0120-primary-work-surface-intercom-or-editor.md)); при `primary_work_surface = editor` якорь **F:** может опускаться (active file), как в 0128 H1.

### 2. Parse → resolve → act

```
BracketRef (shared with attach)
    → AttachmentAnchor-shaped resolve @ invoke (Roslyn, lines fallback)
    → editor.select_code  → SelectInEditor
    → editor.reveal_code  → EditorAgentRangeReveal
```

MCP **не обязан** парсить prose `[M:…]` — только structured args ([0130](0130-editor-agent-range-reveal-without-selection.md) фаза 2). Slash **может** принимать bracket в хвосте, потому что это **явный UI-ввод оператора**, не тело сообщения агента.

### 3. Ортогональность

| Действие | Меняет selection? | Attach в чат? |
|----------|-------------------|---------------|
| `/attach …` + `[M:…]` | нет | **да** |
| `/editor select code [M:…]` | **да** | нет |
| `/editor reveal code [M:…]` | нет | нет |
| `/editor line select 5 10` | **да** | нет |
| `reveal_editor_range` (MCP) | нет | нет |

### 4. Фазы (предварительно)

| Фаза | Содержание |
|------|------------|
| **0** | ADR; согласовать `command_id` и tail в `intent-catalog.toml` |
| **1** *(done)* | `BracketCodeReferenceParser` (`F`/`M`/`L`); `/editor select code`, `/editor reveal code`; `editor.select_code` / `editor.reveal_code` |
| **2** | Parse **`S:kind:n`** в bracket (→ `syntaxScope`); autocomplete member/path/scope; полный L2 |
| **3** | Паритет hotkey / palette melody рядом с `c:els` |

---

## Открытые вопросы

1. Один `wire_class` `editor_code_ref` vs отдельные binders для select/reveal.
2. Ошибки resolve (member_not_found) — toast как в 0128 §9.1 или тихий fallback на L:.
3. Выполнять ли команду, если целевой файл ≠ active file (open + select vs только active).

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-19 | Proposed: идея `/editor select code [F:/M:…]`; ортогонально attach и 0124 line select. |
| 2026-05-19 | Фаза 2: ось **`S:`** (syntax scope) в bracket — канон [0128](0128-intercom-attachment-anchors-and-code-references.md) §5.1. |
