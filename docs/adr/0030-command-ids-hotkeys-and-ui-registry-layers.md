# ADR 0030: Слои идентификаторов команд, хоткеев и UI (без одной таблицы «всё в одном» пока)

**Статус:** Accepted · Implemented (реестр команд v1 в коде)  
**Дата:** 2026-04-08  
## Связанные ADR

| ADR | Роль |
|-----|------|
| [0013](0013-command-surface-and-discoverability.md) | поверхность команд, палитра, `hotkeys.toml` |
| [0018](0018-ide-commands-canonical-xml-documentation.md) | канон XML для `IdeCommands` |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | путь `%LocalAppData%\CascadeIDE\`, в т.ч. пользовательский `hotkeys.toml` |

### Вне ADR

| Документ | Роль |
|----------|------|
| [ide-command-registry-v1.md](../design/ide-command-registry-v1.md) | ide command registry v1 |
**Реализация (текущая):** partial **`IdeCommandRegistry*.cs`** (палитра + метаданные глобальных хоткеев окна), **`IdeCommandPaletteCatalog`** (проекция), **`HotkeyTomlLoader`**, **`MainWindowHotkeyService`**, **`Hotkeys/hotkeys.toml`**; тесты согласованности — **`CascadeIDE.Tests/IdeCommandRegistryTests.cs`**. Полный чертёж `IdeCommandUiMeta` из чертежа — по-прежнему отдельные итерации (§6 ниже).

---

## Контекст

[0013](0013-command-surface-and-discoverability.md) уже зафиксировал намерение: **палитра + паритет с MCP**, **жесты в data-файлах** (`Hotkeys/hotkeys.toml` + пользовательский оверлей), а в последствиях — **«единый реестр команд»** с отсылкой на чертёж [ide-command-registry-v1.md](../design/ide-command-registry-v1.md).

На практике сейчас **несколько согласованных, но разделённых слоёв**: нет одной структуры в коде вида «id → ICommand → жест → пункт меню → строка в палитре». Это вызывает вопросы при разработке («где правда?») и риск рассинхрона при добавлении команд.

Нужно **явно описать**, что считается каноном на каком уровне и что остаётся **намеренным мостом в коде**, пока не реализован полный каталог UI-метаданных из чертежа.

---

## Решение

### 1. Канон `command_id` для агента и исполнения MCP

- **`IdeCommands`** (частичный класс, `Services/IdeCommands*.cs`) — **единственный источник строковых констант** для `ide_execute_command` / MCP, документации ([MCP-PROTOCOL.md](../MCP-PROTOCOL.md), генерация ProtocolDocGen) и линта контракта ([0018](0018-ide-commands-canonical-xml-documentation.md)).
- **`IdeMcpCommandExecutor`** — реестр **исполнения**: `command_id` → обработчик.

### 2. Палитра команд (подмножество + метаданные для человека)

- **`IdeCommandPaletteCatalog`** — статический список **не всех** `IdeCommands`, а команд, которые **должны быть discoverable через палитру** (заголовок, категория, опционально args, правила по `UiModeFamily`).
- Идентификатор в строке палитры совпадает с **`command_id`** там, где команда идёт в MCP; отдельные `PaletteId` допустимы как ключ строки, но **паритет имён** с `IdeCommands` — целевое правило ([0013](0013-command-surface-and-discoverability.md)).

### 3. Жесты (строки клавиш), не логика команд

- **`Hotkeys/hotkeys.toml`** (шип) и **`%LocalAppData%\CascadeIDE\hotkeys.toml`** (оверлей) — мердж через **`HotkeyTomlLoader`**; результат:
  - подсказки рядом с командами в палитре (**`HotkeyGestureMap`**, ключ = `command_id` где применимо);
  - **`Window.KeyBindings`**, пункты меню (**`HotKey`**) и **tunnel** для фокуса в редакторе — **`MainWindowHotkeyService`**.
- **Источник правды по строке жеста** — TOML, не литералы в AXAML/C# (политика [0013 § Решения по реализации](0013-command-surface-and-discoverability.md)).

### 4. Ключи без `IdeCommands` (только UI)

Часть жестов привязана к **RelayCommand во `MainWindowViewModel`**, для которых **нет** отдельной константы в `IdeCommands` (пример: **`debug_start_or_continue`** — «Начать или продолжить» в меню отладки). Такие id **зарезервированы в `hotkeys.toml`** и в **`MainWindowHotkeyService`** как стабильные строковые идентификаторы UI; в MCP они **не** обязаны существовать.

Аналогично **`set_ui_mode_by_index_0` … `_8`** — параметризованная семья жестов для режимов UI, не одна команда `IdeCommands`.

### 5. Мост VM ↔ TOML

- **`IdeCommandRegistry`** задаёт, какие `command_id` / hotkeys-ключи требуют **глобального** жеста на главном окне; разрешение в `ICommand` — в **`MainWindowHotkeyService`** (`ResolveWindowCommand` по `MainWindowHotkeyVmBinding`). Жесты по-прежнему только в TOML.

### 6. Целевое состояние (не блокирует текущую реализацию)

Полная **склейка** «id → UI-метаданные → исполнение → жест» описана в чертеже **[ide-command-registry-v1.md](../design/ide-command-registry-v1.md)** (`IdeCommandUiMeta` / единый каталог). Переход к ней — **отдельные итерации** (рефакторинг палитры, валидация покрытия, при желании генерация фрагментов из одного источника). Этот ADR **не отменяет** чертёж и **не требует** немедленной реализации всего списка из §2 чертежа.

---

## Последствия

- Новая команда **для MCP**: добавить константу в **`IdeCommands`**, обработчик в **`IdeMcpCommandExecutor`**, при необходимости строку в **`IdeCommandPaletteCatalog`**, ключ в **`hotkeys.toml`** (если нужен жест), и запись в **`MainWindowHotkeyService`**, если команда вешается на главное окно / tunnel / меню с тем же id.
- Рецензенты и авторы фич ориентируются на этот ADR + [0013](0013-command-surface-and-discoverability.md) + чертёж [ide-command-registry-v1.md](../design/ide-command-registry-v1.md), чтобы не ожидать **одного класса «реестр всего»** до его появления.
- При внедрении **`IdeCommandUiMeta`** (или эквивалента) этот ADR можно уточнить ссылкой на PR/миграцию или пометить **Superseded** для §5–§6, не меняя смысл [0013](0013-command-surface-and-discoverability.md).

## Отклонённые альтернативы (как немедленное требование)

- **Объявить существующий `IdeCommands` полным «реестром UI»** — отклонено: там нет заголовков палитры, категорий, правил режима и жестов; это контракт автоматизации.
- **Считать `hotkeys.toml` единственным реестром команд** — отклонено: в нём нет исполнения и не все ключи — MCP `command_id`.
