# Intercom — справка

Локальное сообщение (`audience: self`): видно только тебе на этом клиенте, не уходит агенту и во внешний канал.

## Навигация и темы (`/intercom`)

- `/intercom overview` · `/intercom show` — картотека тем
- `/intercom topic open <id|заголовок>` — detail темы
- `/intercom topic create <название>` · `/intercom topic rename <название>` — текущая тема (detail)
- `/intercom topic list` · `/intercom topic tree` — списки (текст: `… list text`); в Navigator: ПКМ или двойной клик по теме — переименовать
- `/intercom spine list` · `/intercom spine tree` · `/intercom spine set focus=…` — product spine

## Номера сообщений (gutter) и выбор

В **detail**-ленте активной ветки слева номера **1, 2, 3…** (как line numbers в редакторе). Это **не** глобальный индекс в `ChatMessages`.

| Способ | Координаты |
|--------|------------|
| Gutter, ПКМ «Выбрать сообщение #n» | 1-based в ветке |
| `/intercom message select <n>` или `n m` / `n:m` | 1-based; активным — **конец** диапазона |
| `/intercom message select clear` | Сбросить multi-highlight и активное сообщение в detail-ленте |
| MCP `chat_select_message` с `ordinal` | то же, что slash (нужна открытая ветка) |
| MCP `chat_select_message` с `index` | 0-based по **всему** списку сообщений сессии (legacy) |
| `/intercom message find selection` | Сообщения ветки с attach на текущее выделение (inferred) |
| `/intercom message find L:10-20` | По строкам открытого файла (M0 fallback) |
| `/intercom message 3:5 relate selection` | Явная связь #3–#5 с кодом → event `message_range_related` (`AttachmentAnchor`) |
| `/intercom message anchors list` | Якоря выбранного сообщения и черновика: `a:abcd1234`, статус, путь |
| `/anchor peek <id>` | Reveal по short id (8 hex с chip или list), без hit-test |
| MCP `intercom.messages_for_code` | Find: JSON `use_selection`, `code_ref`, `anchor_json`, или `file`+`line_*` |
| MCP `intercom.message_relate` | Relate: `start_ordinal`, `end_ordinal?`, тот же code-ref |

После выбора: `chat_get_selected_message` → `selected_index` (глобальный 0-based) и при detail — `feed_ordinal` (номер в gutter).

## Сообщения агенту

- Обычный текст в composer → отправка агенту (Enter по настройке send).
- `@` — люди/упоминания; `[ … ]` — артефакты кода и вложения (не markdown-ссылки).
- ЛКМ по телу сообщения **не** меняет выбор (клик по attach-chip → reveal в редакторе).
- На chip справа — приглушённый **`a:…`** (short id для `/anchor peek`).
- **Cockpit Command Line** (полоса над composer): тот же **slash autocomplete**, что в composer (`/`, Tab, ↑↓, клик по списку); preview под строкой; Enter — выполнить, Esc — закрыть. Открыть: **Ctrl+K** затем **/** (handoff), **Ctrl+Q** → `c:ccl` или «Cockpit: Command Line», MCP `cockpit.open_command_line`. Палитра — **Ctrl+Q**, не Ctrl+Shift+P (у Avalonia сочетания с модификаторами ненадёжны).

## Вложения в тексте `[ … ]`

- Оси: `F:` путь · `M:` член · `L:` строки · `S:` scope (напр. `S:for:2` — 2-й `for` в теле `M:…`)
- Примеры: `[M:Run]` · `[Foo.cs M:Bar]` · `[M:Run S:for:1]` · `[F:src/X.cs; M:Y; L:10-20]`
- Autocomplete: набери `[` — подсказки по осям; Tab/Enter — вставить; Esc — закрыть.
- `/intercom attach selection` · `/intercom attach scope` · `/intercom attach file <path> [start] [end]`
- Маркеры `⟦a:…⟧` в черновике — chip; клик в ленте → reveal в редакторе.

## Слэш-команды IDE и Intercom

- `/` — autocomplete (Tab — выбор, Enter — выполнить).
- `/help intercom` — этот текст; `/help <namespace>` — подмножество.
- `/export` — Markdown сессии; `/inspect spine` — JSON spine.

---

Редактируй этот файл: `Intercom/intercom-help.ru.md` (рядом с exe или встроенный ресурс сборки).
