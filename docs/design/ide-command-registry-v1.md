# Реестр команд IDE для палитры и MCP (чертёж v1)

**Статус:** чертёж (полный `IdeCommandUiMeta` и т.д. — по плану); **реализация v1 (частично) — Implemented в коде:** partial `Services/IdeCommandRegistry*.cs` (палитра, `CommandAccessibleFrom`, хоткеи окна); жесты только в `hotkeys.toml`. Согласуется с [ADR 0013](../adr/0013-command-surface-and-discoverability.md), [ADR 0010](../adr/0010-ui-modes-toml-configuration.md), [ADR 0030](../adr/0030-command-ids-hotkeys-and-ui-registry-layers.md) (**Accepted · Implemented**).  
**Задача:** один **идентификатор команды** (`command_id`) для MCP, `ide_execute_command`, меню и палитры; отдельно — **метаданные для человека** (имя, группа, фильтр по режиму) и **хоткеи** из файлов.

---

## 1. Что уже есть (не переписываем)

| Слой | Роль |
|------|------|
| `IdeCommands` | Стабильные строковые **`command_id`** + XML-summary для MCP/доков. Контракт summary/линта для ProtocolDocGen: [ide-commands-protocol-docgen-contract.md](../dev/ide-commands-protocol-docgen-contract.md). |
| `IdeMcpCommandExecutor` | Регистрация **`command_id` → обработчик**. |
| `IdeCommandsArgs` (генерация) | Схема аргументов по id. |
| `MCP-PROTOCOL.md` | Таблица команд, генерируемая из XML. |

Это уже **единый реестр исполнения** и контракт для агента. Палитре не хватает **UI-метаданных** и явного «показывать / не показывать».

---

## 2. Что добавляем: каталог метаданных (не второй список id)

Новый тип — например **`IdeCommandUiMeta`** (или строка в одном **`IdeCommandCatalog`**), **по одному на команду, которая может попасть в палитру** (и опционально — строка «только для валидации» на все id из `IdeCommands` — см. §8).

Минимальные поля:

| Поле | Назначение |
|------|------------|
| **`CommandId`** | Строка = `IdeCommands.*` (один источник констант). |
| **`Category`** | Группа в списке палитры: `File`, `View`, `Build`, `Git`, `Debug`, `Agent`, `Documents`, `Settings`, … (enum или строковые константы в одном месте). |
| **`TitleKey`** | Ключ локализованной строки (`IdeCmd.open_file`) **или** литерал v1; для поиска в палитре используется **разрешённый** заголовок. |
| **`PaletteVisibility`** | `Normal` \| `Hidden` — скрыть низкоуровневые команды (`click_control`, `set_control_layout`, …) из палитры; в MCP они остаются. |
| **`ModeRule`** | Правило доступности в текущем UI-режиме (см. §4). |

Не дублируем здесь: **args** (уже в `IdeCommandsArgs`), **описание для MCP** (уже в XML-summary), **хоткей** (берётся из мерджа `Hotkeys/*.toml`, ключ = `command_id` в snake_case как в [UX §9](../ux/command-palette-ux-concept-v1.md)).

---

## 3. Хоткеи

- Разрешённая строка жеста: **`IHotkeyMap.TryGetGesture(command_id)`** после мерджа шип + AppData.
- В **`IdeCommandUiMeta` хоткей не храним** — иначе два источника правды; в VM палитры: `meta + resolvedGesture`.

---

## 4. Доступность в режиме (`ModeRule`)

Варианты от простого к точному (выбрать один стиль на v1):

1. **`AllModes`** — команда всегда в списке (может быть серая, если исполнение всё равно отклонит — по желанию).
2. **`AllowedFamilies(UiModeFamily[])`** — например только `Focus` для `focus_checkpoint`, только `Power` для части автономки; соответствие с продуктовыми семьями из [0010](../adr/0010-ui-modes-toml-configuration.md).
3. **`AllowedModeIds(string[])`** — если удобнее оперировать id из `UiModes/index.toml`, а не семьёй.

Палитра при смене режима: фильтрует или помечает пункт **серым** + «недоступно в режиме …» ([UX](../ux/command-palette-ux-concept-v1.md)).

Тонкая связка с **`UiModeCapabilities`** (показывать quick action только если capability включена) — **опционально v2**: отдельное поле или подмножество команд с `WhenCapability(...)`.

---

## 5. Локализация

- **`TitleKey`** → ресурсы `.resx` / существующий механизм Cascade; fallback: ключ или английская строка.
- Категории для подзаголовка в списке — тоже ключи (`IdeCmdCategory.View`).

---

## 6. Поток данных (палитра)

1. Загрузить **`IReadOnlyList<IdeCommandUiMeta>`** где `PaletteVisibility == Normal`.
2. Сопоставить с **`IHotkeyMap`** по `CommandId`.
3. Текущий **`ResolvedMode`** / семья из VM → вычислить `IsAvailable` по `ModeRule`.
4. Поиск по **Title** (+ опционально `command_id`, категория).
5. Выполнение: **`ide_execute_command`** с `command_id` и пустыми/дефолтными `args` (для команд без обязательных аргументов) или открыть мастер (если когда-нибудь понадобится).

---

## 7. Валидация и дрейф

- **Тест:** каждый **`IdeCommandUiMeta.CommandId`** существует в регистрации `IdeMcpCommandExecutor` (рефлексия или общий список констант `IdeCommands`).
- **Тест (мягче):** каждый **публичный** `IdeCommands` константа либо имеет meta с `Hidden`, либо `Normal` — чтобы новая команда не забылась в палитре/скрытии осознанно.
- Ключи в **`hotkeys.toml`**: опционально проверка «неизвестный `command_id`» в CI (предупреждение).

---

## 8. Почему не один гигантский enum «все команды»

Сейчас **сотни** констант в `IdeCommands`, часть — чисто для автоматизации UI. Каталог метаданных:

- либо **только для команд с `Normal`** (короткий список),
- либо полный перечень с явным **`Hidden`** для каждой «не-палитровой» команды.

Рекомендация v1: **явный список палитровых** + тест «новый id без meta не ломает сборку, но падает предупреждающий тест optional» — на выбор команды.

---

## 9. Связь с ADR 0013

Формулировка «потребуется единый реестр» в [0013 § Последствия](../adr/0013-command-surface-and-discoverability.md) при этом означает:

- **id + исполнение** — уже есть (`IdeCommands` + executor);
- **единый реестр для UX палитры** — этот документ: **`IdeCommandUiMeta` + `Category` + `ModeRule` + локализация**, хоткеи снаружи.

После реализации палитры можно добавить одну строку-отсылку из ADR 0013 на этот файл.

---

*Версия: 2026-04.*
