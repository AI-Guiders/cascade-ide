# ADR 0150: Slash line — канонический путь и единый резолв (autocomplete · Enter · execute)

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-26  

> **Дополнение (2026-05-28):** источник идентичности пути — **только каталог**; parser shape и `IntercomSlashPathBuilder` удалены — [0153](0153-slash-catalog-only-resolution.md).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0119](0119-chat-slash-commands-intercom-surface.md) | Грамматика slash, autocomplete, local execution |
| [0136](0136-intercom-feed-gutter-and-slash-namespace.md) | `/intercom <group> <verb>` — пути глубже двух уровней |
| [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) | Динамический completion после пути |
| [0140](0140-tci-slash-status-glyphs-and-args-counter.md) | Счётчик args в TCI |

## Проблема

Один ввод в composer описывался **тремя моделями**:

1. **Каталог** — полный путь (`/intercom server start`).
2. **Парсер** — `head` + `action` + `ArgsTail` (вложенный `start` оказывается в хвосте).
3. **Autocomplete** — по сегментам; **Enter** — отдельная эвристика popup / `auto_run`.

Симптомы повторялись: Enter коммитит подсказку вместо send; аргумент **склеивается** с командой без пробела; опциональный `base_url` не отличался от «команда завершена».

## Решение

### 1. Единый резолв строки

`SlashLineResolver` по тексту slash-строки возвращает:

| Поле | Смысл |
|------|--------|
| `CanonicalPath` | Путь из `intent-catalog` (`/intercom server start`) |
| `ArgTail` | Текст **после** пути (URL, заголовок темы, …) |
| `ArgTailKind` | `none` \| `optional` \| `required` |
| Флаги UI | exact path, пробел после пути, есть ли текст хвоста |

**Autocomplete**, **Enter (send)**, **Runner (args)** читают только этот резолв, не дублируют эвристики.

### 2. Явный `arg_tail` в каталоге

В `[[command.form.slash]]`:

```toml
arg_tail = "none"      # по умолчанию после полного пути — send на Enter
arg_tail = "optional"  # после commit — пробел; можно дописать args
arg_tail = "required"  # без хвоста — не runnable; dynamic completion
```

Legacy `requires_arg_tail = true|false` остаётся; при отсутствии `arg_tail` маппится в `required` / `none`. Эвристики (completion, дочерние пути, `open`) — только если `arg_tail` не задан.

### 3. Правила UI (норматив)

| `arg_tail` | Popup сегментов после полного пути | Insert после Tab/Enter на сегменте | Enter → send |
|------------|-----------------------------------|-------------------------------------|--------------|
| `none` | скрыть | без лишнего пробела | да |
| `optional` | скрыть; режим ввода хвоста | **пробел** после пути | да (хвост может быть пустым) |
| `required` | до хвоста — по completion / сегментам | пробел если есть следующий сегмент каталога | только если `ArgTail` непустой |

Резолв строит канонический путь по токенам каталога; runner берёт `ArgTail` из резолва. *(Историческая v1 оставляла `ChatSlashCommandParser.TryParse` — снято в [0153](0153-slash-catalog-only-resolution.md).)*

## Последствия

- Новые slash с опциональным хвостом: **`arg_tail = "optional"`** в TOML, без кода в `IntercomSlashArgsTail`.
- Тест-матрица: `SlashLineResolverTests` (путь × ввод × runnable × popup).
- ~~`IntercomSlashPathBuilder` / `TryResolve(parse)`~~ — удалено ([0153](0153-slash-catalog-only-resolution.md)).

## Non-goals

- Переписать парсер на три уровня `SubAction` для всех namespace.
- Автогенерация `arg_tail` из JSON schema `IdeCommands` без явной записи в TOML (допустимо позже как hint, не как источник правды).

## Альтернативы (отклонены)

| Вариант | Почему нет |
|---------|------------|
| Только точечные фиксы в autocomplete | Регрессии на следующей команде |
| Всегда trailing space после commit | Ломает `none` и auto_run |
| Один `requires_arg_tail: bool` | Не различает optional и required |

---

## Статус реализации (2026-05-27)

| Элемент | Артефакт |
|---------|----------|
| `SlashLineResolver` | `Features/Chat/SlashLineResolver.cs` |
| Autocomplete / Enter | `ChatSlashAutocomplete`, `ChatPanelViewModel.IntercomComposerKeys` |
| Execute args | `ChatSlashCommandRunner` → `ArgTail` из резолва |
| `arg_tail` в каталоге | `IntentMelody/intent-catalog.toml` — без `requires_arg_tail` |
| Тесты | `SlashLineResolverTests`, `SlashRouteCatalogIndexTests`, `IntentCatalogArgTailPolicyTests` |

Legacy `requires_arg_tail` в TOML не используется; `SlashRouteCatalogIndex` оставляет эвристики только для оверлеев без `arg_tail`.
