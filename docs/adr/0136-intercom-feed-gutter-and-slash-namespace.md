# ADR 0136: Intercom — gutter номеров, явный select, namespace `/intercom`

**Статус:** Accepted · In progress  
**Дата:** 2026-05-20

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0119](0119-chat-slash-commands-intercom-surface.md) | Слэши как CLI в composer |
| [0137](0137-intercom-message-code-correspondence.md) | Соответствие gutter ↔ code; infer + find / relate (Accepted) |
| [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md) | topic/spine inspect |
| [0134](0134-intercom-message-prepare-pipeline-v1.md) | Attach chip, reveal |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Overview / detail |

## Резюме

1. **Номера сообщений** — gutter слева в detail-ленте (1-based в активной ветке), как line numbers в редакторе.
2. **Выделение** — только **ПКМ → «Выбрать сообщение»** или `/intercom message select <n>`; **ЛКМ** не меняет `SelectedMessageIndex` (клик по attach → reveal).
3. **Без фиолетовой полоски** `DrawFeedAccent` для selection; выбранная строка подсвечивается в gutter + лёгкий фон строки.
4. **Слэши Intercom** — единый префикс `/intercom …`; старые `/topic`, `/spine`, `/overview`, `/attach`, `/card`, `/thread` **удаляются** (миграция не нужна).

## Gutter

| Элемент | Поведение |
|---------|-----------|
| Ширина | ~36px слева от ленты |
| Номер | Порядковый **в активной ветке** detail-режима: 1, 2, 3… |
| Выбор | Подсветка ячейки gutter + фон строки при `MessageIndex == SelectedMessageIndex` |
| ЛКМ по gutter | Не меняет selection (как по телу) |

## Select

| Способ | Действие |
|--------|----------|
| ПКМ по сообщению / gutter | Контекстное меню «Выбрать сообщение #n» |
| `/intercom message select <n>` | `n` или диапазон `n m` / `n:m` (как `/editor line select`); активно сообщение **m** (конец диапазона) |
| `/intercom message next\|prev` | Сдвиг выбора в ленте (бывш. melody / без отдельного `/thread`) |
| Double-click thinking | Без изменений (toggle details) |

Attach chip: ЛКМ → только `RevealAttachment`.

## Namespace `/intercom`

Примеры (канон в `intent-catalog.toml`):

```
/intercom overview
/intercom show
/intercom topic list | list text | tree | tree text | create | open | cards
/intercom spine list | tree | set | show | toggle | open
/intercom message select | next | prev
/intercom attach selection | scope | file
```

Парсер: `head=intercom`, хвост делегируется во внутренний verb; `ChatSlashCommandCatalog.TryResolve` строит полный путь через `IntercomSlashPathBuilder`.

## MCP и координаты выбора

| Канал | Параметр | Смысл |
|-------|----------|--------|
| Gutter, slash, MCP `ordinal` | 1-based | Порядок в **активной detail-ветке** (как в UI) |
| MCP `index` (legacy) | 0-based | Позиция в полном `ChatMessages` сессии |
| `chat_get_selected_message` | `selected_index` + `feed_ordinal?` | Глобальный индекс и номер gutter, если ветка открыта |

Предпочтение для агента: `ordinal` (или `message_id` из ответа), не путать с `index`. Диапазон: `end_ordinal` (как `n:m` в slash).

## Последствия

- Тесты и подсказки autocomplete — `/intercom *`.
- Справка: `Intercom/intercom-help.ru.md`, `MCP-PROTOCOL.md`.
