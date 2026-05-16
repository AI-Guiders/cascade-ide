# ADR 0051: Intent-based attention routing (TOML)

**Статус:** Accepted · Implemented (секция `[attention_routing]` в bundle `UiModes/workspace.toml` и оверлее `.cascade/workspace.toml`; intent-id — `AttentionRoutingIntentIds`)  
**Дата:** 2026-04-16  

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | модель зон внимания и их канонические id |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | топология `presentation` |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | декларативный placement `instrument_id` по `surface_id+slot_id` |
---

## Контекст

В `UiModes/workspace.toml` и `.cascade/workspace.toml` сегодня существует карта размещения UI-панелей по зонам внимания: секция `[attention_zone_panels]` задаёт `panel_id -> attention_zone`.

Проблема DX:

- Пользователь должен знать **внутренние идентификаторы** (какие ключи допустимы).
- Названия в стиле `attention_zones` / `attention_zone_panels` легко спутать с другой осью конфигурации: **размещением “какого инструмента кабины”** в слотах (см. ADR [0050](0050-declarative-instrument-zone-placement-toml.md)).

Нужно чётко развести два независимых слоя:

1. **Attention routing**: *куда на сцене направить UI-намерение* (пример: чат — в `mfd`, редактор — в `forward`).  
   Это про **поток внимания и географию** (`pfd/mfd/forward/hud/eicas`), но **не** про “какой инструмент в слоте”.
2. **Instrument placement**: *какой `instrument_id` смонтирован в `surface_id+slot_id`* (ADR 0050).  
   Это про **контент слотов кабины**, но **не** про маршрутизацию панелей/интентов.

---

## Решение

Принять intent-based конфигурацию routing’а внимания и **явно назвать** секцию так, чтобы она не пересекалась с instrument placement.

### 1) Новая секция

Заменить `[attention_zone_panels]` на:

- **`[attention_routing]`**

Причины:

- слово *routing* прямо указывает на “маршрутизацию/куда направлять”, а не на “что находится в слоте”;
- не пересекается по смыслу с `[instrument_routing]` (ADR 0050).

### 2) Ключи: “интенты”, а не внутренние panel id

Ключи — человеко-читаемые intent id (v1 совпадают 1:1 с текущими панелями, но трактуются как intent):

- `solution_explorer`
- `chat`
- `git`
- `terminal`
- `editor`

Значения — канонические id зон из ADR 0021:

- `forward`, `pfd`, `mfd`, `hud`, `eicas`

Пример:

```toml
[attention_routing]
solution_explorer = "pfd"
chat = "mfd"
git = "mfd"
terminal = "mfd"
editor = "forward"
```

### 3) Связь с реализацией

Внутри рантайма routing остаётся реализован через карту `panel_id -> AttentionZone`, но TOML задаёт **intent**:

- intent id нормализуется и транслируется в соответствующий `panel_id` (внутренний ключ).
- Если intent неизвестен — выдаётся явная диагностика (лог/статус).

### 4) Слои merge (bundle/repo)

Как и для текущего `workspace.toml`:

- bundle: `UiModes/workspace.toml`
- repo overlay: `.cascade/workspace.toml`

Правило приоритета: repo поверх bundle для совпадающих ключей.

---

## Последствия

- Конфиг становится понятнее: пользователь видит **интенты**, а не “магические id”.
- Терминология не конфликтует с ADR 0050 (instrument placement).
- Кодовая модель допускает дальнейшую эволюцию: появление новых интентов без раскрытия внутренних panel id.
- `editor_hud` фиксирован как инвариант слоя HUD внутри `forward` и не является настраиваемым intent в TOML.

---

## Миграция

Решение “replace”:

- `[attention_zone_panels]` считается устаревшим и удаляется после внедрения `[attention_routing]`.
- Переходный период может поддерживать оба, но в v1 реализации предпочтительно не держать два параллельных источника истины.

---

## Открытые вопросы

- Отдельная пользовательская секция routing в `%LocalAppData%\\CascadeIDE\\settings.toml`: нужна ли в v1, или достаточно bundle+repo.
