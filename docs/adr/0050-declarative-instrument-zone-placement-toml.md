# ADR 0050: Декларативная карта «инструмент → зона/слот» в TOML

**Статус:** Accepted (реализовано: merge bundle/repo/user, `[instrument_routing]` с alias, приоритет `prefer_repo_instruments_placement`, резолв в `InstrumentPlacementRuntime` + `MainWindowHostSurfaceCompositor`).  
**Дата:** 2026-04-16  

**Связь:** [0017](0017-multi-window-workspace-and-agent-surfaces.md) (строка **`presentation`** — **топология** физических дисплеев и якорей, не наполнение слотов), [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) ( **`CockpitInstrumentDescriptor`**: `instrument_id` + `slot_id`; композитор хоста без дерева контролов), [0028](0028-user-settings-toml-localappdata-and-secrets.md) (личный **`settings.toml`**), [0010](0010-ui-modes-toml-configuration.md) (режимы UI и бандлы `UiModes/`), [0036](0036-cds-channel-compositor-surface-pipeline.md) / [`cds-contract-v0.md`](../design/cds-contract-v0.md) (CDS и список инструментов на поверхности). Аналогия по **слоям данных:** пресеты [`workspace_navigation_context`](../samples/settings.toml) (бандл + репо + пользователь).

---

## Контекст

Сегодня **где какой инструмент кабины** (дерево решения vs Semantic Map vs mount preview и т.д.) в главном окне задаётся **логикой композиторов** (`MainWindowHostSurfaceCompositor`, `DefaultSurfaceSlotInstrumentBindingProvider`, placement rules) и привязками Avalonia. Строка **`presentation`** задаёт **сколько якорей и на каких экранах**, но **не** декларативный выбор «в слоте `pfd` показывать инструмент A или B» для конкретного репозитория или пользователя.

Потребность: **отделить топологию презентации** от **наполнения слотов внимания** и позволить команде или пользователю **переопределять карту инструментов** без правок кода — в духе данных о кабине, уже используемых для CDS ([0047](0047-cockpit-instrument-descriptor-and-slot-composition.md)).

## Решение

**Принять** отдельный **конфигурируемый слой** — **карта размещения инструментов** для основных слотов кабины, хранимый в **TOML** и сливаемый по фиксированным правилам приоритета.

**Инвариант 1 — граница с `presentation`:** строка **`presentation`** / **`[presentation_grammar]`** ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) по-прежнему определяет только **топологию** (сколько групп `(…)`, какие якоря на каком дисплее, опциональные веса колонок). Карта инструментов **не** задаёт число мониторов и **не** заменяет якоря; рантайм сопоставляет **семантические слоты** (`pfd_primary`, `mfd_primary`) с конкретными парами **`surface_id` + `slot_id`** внутри продукта (в т.ч. при смене окна-хоста MFD), без требования писать `surface_id` в пользовательском/репозиторном TOML.

**Инвариант 2 — публичные значения (alias):** в `workspace.toml` и в `[display.instrument_routing]` задаются **человекочитаемые** токены, которые рантайм приводит к каноническим **`instrument_id`** (`CockpitStandardInstrumentIds`):

| Значение в TOML | Канонический `instrument_id` |
|-----------------|-------------------------------|
| `solution_explorer` | `solution_explorer_tree` |
| `workspace_map` | `workspace_navigation_map` |
| `workspace_health` | `workspace_health_status_v1` |

Допустимо также указать **уже канонический** `instrument_id` (как в CDS), если нужен копипаст из диагностик.

**Инвариант 3 — слои merge и конфликт одного ключа:**

Три источника данных (от общего к частному):

1. Встроенный **бандл** IDE (дефолт продуктовой карты).  
2. **Репозиторный** слой — **`.cascade/workspace.toml`** с merge поверх бандла ([0021](0021-pfd-mfd-cockpit-attention-model.md) §2.1).  
3. **Пользовательский** слой — **`[display.instrument_routing]`** в `%LocalAppData%\CascadeIDE\settings.toml` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)).

**Внутри одного слоя-таблицы** `[instrument_routing]` ключи уникальны (`pfd_primary`, `mfd_primary`).

**Между источниками** по умолчанию: **пользовательский слой сильнее репозиторного, репозиторный сильше бандла** (`user > repo > bundle`) для одной и той же семантики слота.

**Флаг в пользовательском `settings.toml`:** **`prefer_repo_instruments_placement`** (bool). При **`true`** для совпадающих ключей побеждает **репозиторный/bundle** слой; при **`false`** или если ключ не задан — **`user > repo`**.

**Инвариант 4 — валидация:** неизвестный ключ слота, неизвестный alias/`instrument_id` — **явная диагностика** при загрузке настроек (валидация `[display]`); для workspace — отладочный лог при игнорировании строки.

**Инвариант 5 — без `surface_id` в публичном TOML:** пользователь и репозиторий задают только **`[instrument_routing]`** / **`[display.instrument_routing]`**; соответствие поверхностям `main_window_docked_grid` / `main_window_plus_mfd_host_top_level` выполняется в `InstrumentPlacementRuntime`.

### Публичный v1-контракт

**Бандл / репо** (`UiModes/workspace.toml`, `.cascade/workspace.toml`):

```toml
[instrument_routing]
pfd_primary = "solution_explorer"
mfd_primary = "workspace_map"
```

**Пользователь** (`settings.toml`):

```toml
[display.instrument_routing]
pfd_primary = "workspace_map"
```

- `pfd_primary` и `mfd_primary` — ключи уровня продукта.  
- Значения — **alias** из таблицы выше или канонический `instrument_id`.

Низкоуровневый массив `[[instrument_placement_rules]]` с полями `surface_id` / `slot_id` **не используется** в публичном контракте v1 (внешних потребителей нет; DX — только таблица выше).

## Почему не только код

- **Репозитории** различаются по тому, что считается «главным» в PFD (карта vs дерево vs оба в разных пресетах).  
- **Эксперименты Flight** и продуктовые режимы должны переключать карту **данными**, а не ветками с разными композиторами.  
- **Агенты и наблюдаемость:** CDS уже проецирует список инструментов ([0036](0036-cds-channel-compositor-surface-pipeline.md)); единый источник в TOML упрощает согласование «что заявлено» и «что в снимке».

## Альтернативы (отклонённые как основной путь v1)

| Альтернатива | Почему не базовый выбор |
|--------------|-------------------------|
| Только правки в `MainWindowHostSurfaceCompositor` | Нет пользовательского и репо-слоя без форка. |
| Расширить только `presentation` литералами инструментов | Смешение топологии дисплеев и выбора виджетов; строка станет нечитаемой и сломает парсер по смыслу. |
| Только JSON в настройках | TOML уже канон для IDE и workspace; один стиль предпочтительнее. |

## Последствия

- Композитор хоста читает **эффективную карту** после merge.  
- Тесты: unit на merge словаря, alias-резолвер, сценарий «другой инструмент в PFD при том же `presentation`».  
- Документация: samples рядом с `settings.toml`.

## Статус реализации

**Реализовано**:
- bundle/repo: `UiModes/workspace.toml` + `.cascade/workspace.toml` через `UiWorkspaceTomlMerger` и `UiModeCatalog.ApplyRepositoryWorkspaceOverlay`;
- user: `[display.instrument_routing]` и `prefer_repo_instruments_placement` в `%LocalAppData%\CascadeIDE\settings.toml`;
- единый runtime: `InstrumentRoutingAliasResolver`, `InstrumentPlacementRuntime`, `MainWindowHostSurfaceCompositor`.

## Открытые вопросы

- **Числовой приоритет** у строки не требуется в v1: достаточно **слоя merge** между бандл / репо / user; при необходимости тонкой настройки в одном файле — позже, отдельным полем.  
- Нужно ли версионирование схемы секции (`schema_version` внутри блока) для миграций.  
- Связь с **mount-слоем** (`InstrumentMountPolicyRules`, `use_skia_instrument_mount`): **ортогонально** — style остаётся про визуал, карта из этого ADR — про *какой* `instrument_id` в слоте.
