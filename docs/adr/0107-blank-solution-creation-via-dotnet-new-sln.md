# ADR 0107: Создание пустого решения через `dotnet new sln` (самодостаточность workspace)

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-10

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0006](0006-presentation-layers-and-feature-slices.md) | связанный ADR |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | связанный ADR |
| [0013](0013-command-surface-and-discoverability.md) | связанный ADR |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | связанный ADR |
| [architecture-migration.md](../architecture-migration.md) | вне нумерованного ADR |


## Резюме

- Пустое решение: **`dotnet new sln`**, меню/MCP, `BlankSolutionCreator`.
- `IDotnetCommandRunner` — общий раннер CLI.


---

## 1. Контекст

CascadeIDE уже умела **открывать** решение, проект или папку как workspace. Для сценария «с нуля» без внешней IDE не хватало минимального шага: **создать новый файл `.sln`**, чтобы сразу загрузить дерево и дальше работать внутри CIDE.

Отдельно существует экосистема **шаблонов .NET** (в т.ч. пакеты NuGet с `packageType: Template`): после `dotnet new install …` те же шаблоны видны и в Visual Studio, и в `dotnet new`. Это **не** дублирование «магии VS» — общий движок шаблонов. В первой итерации CIDE **не** встраивает каталог шаблонов из NuGet и **не** подменяет установку пакетов; достаточно опереться на **встроенный шаблон SDK** для пустого решения.

---

## 2. Решение

1. **Каноничная операция v1:** создать пустое решение командой  
   `dotnet new sln -n <имя> -o <каталог_родитель_файла>`  
   где целевой путь — полный путь к будущему `<имя>.sln`, каталог при необходимости создаётся, файл **не** должен существовать до вызова.

2. **Размещение логики (strangler, ADR 0006 / 0102):**
   - оркестрация без UI — **`Features/Workspace/Application/BlankSolutionCreator`**;
   - запуск CLI — только через порт **`IDotnetCommandRunner`** (DAL, не дублировать `Process` в VM);
   - UI: диалог «Сохранить как» для `.sln`, затем **`MainWindowViewModel.TryCreateBlankSolutionAtPathAsync`** и **`LoadSolution`** — во View / VM-мосте, как для «Открыть решение».

3. **Поверхность команд (ADR 0008 / 0013):**
   - константа **`IdeCommands.CreateNewSolutionDialog`** (`create_new_solution_dialog`);
   - пункт меню **Файл**, палитра (`IdeCommandRegistry`), MCP-хендлер рядом с остальными диалогами «Файл»;
   - описание и контракт — через существующий **ProtocolDocGen** (`IdeCommands` XML-doc).

4. **Тесты:** модульные тесты с подменой **`IDotnetCommandRunner`** (в т.ч. симуляция успешного `dotnet new sln` и проверка аргументов); отказ, если `.sln` уже существует.

---

## 3. Последствия

- На машине пользователя должен быть доступен **`dotnet`** с шаблоном `sln` (типичная установка SDK).
- После создания решение **пустое** (нет проектов): сборка/отладка осмысленны только после добавления проектов — это ожидаемо и документируется поведением продукта, не ошибкой ADR.
- Журнал миграции фиксирует срез (**v1.41m**): новый файл Application + тесты.

---

## 4. Отклонённые / отложенные альтернативы

| Альтернатива | Почему не v1 |
|--------------|--------------|
| Генерировать `.sln` вручную (текстом) без `dotnet` | Дублирование формата, расхождение с SDK и будущими версиями. |
| Встроенный мастер «решение + проект» и выбор шаблона из NuGet | Больше UX и контрактов; делается отдельными ADR/итерациями после стабилизации пустого `sln`. |
| Только MCP без меню | Недостаточно для «самодостаточности» без внешнего агента; меню — каноничный паритет с `open_*_dialog`. |

---

## 5. Расширения (вне scope реализации v1 в этом ADR)

Ниже — **целевой паритет с VS** не как копирование UI, а как **тот же контракт, что и у .NET SDK**: движок шаблонов + граф решения + ссылки. Реализация в CIDE — отдельными итерациями; ориентир по командам зафиксирован в §6.

---

<a id="adr0107-vs-parity-cli"></a>

## 6. Паритет с VS: поверхность `dotnet` (набор для CIDE)

**Идея:** Visual Studio для типового «новый проект / структура решения» опирается на **template engine** и модель **solution + projects** — то же самое даёт CLI. CIDE не обязана повторять каждый диалог VS; паритет — в **возможностях**, выведенных из тех же примитивов.

Каноничная документация Microsoft: [.NET CLI overview](https://learn.microsoft.com/dotnet/core/tools/), [`dotnet new`](https://learn.microsoft.com/dotnet/core/tools/dotnet-new), [`dotnet sln`](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln).

### 6.1. Шаблоны и «типы» проектов (как в VS после установки пакета шаблонов)

| Задача | CLI | Заметка для паритета |
|--------|-----|----------------------|
| Список доступных шаблонов (установленные + встроенные в SDK) | `dotnet new list` | С .NET 7 — подкоманды `list` / `search` / `install` / `uninstall` / `update`. |
| Поиск шаблонов в NuGet | `dotnet new search <строка>` | Аналог «полистать галерею» до установки. |
| Установить пакет шаблонов (в т.ч. с NuGet или `.nupkg`) | `dotnet new install <PACKAGE_ID_or_path>` | После этого шаблоны видны и в VS, и в `dotnet new` — один источник. |
| Снять пакет | `dotnet new uninstall <PACKAGE_ID_or_path>` | Без аргумента — список установленных пакетов и команд uninstall. |
| Обновить установленные пакеты | `dotnet new update` | В т.ч. `--check-only` для проверки. |
| Создать проект/элемент по шаблону | `dotnet new <shortName> -o <путь> …` | Параметры (`-f`, `--lang`, кастомные из `template.json`) — как в VS «Additional info». |
| Шаблоны элементов (файлы в проекте) | `dotnet new <itemTemplate>` в каталоге проекта | VS «Add → Class» и т.п. — тот же механизм, другой short name. |

См. также: [Custom templates for `dotnet new`](https://learn.microsoft.com/dotnet/core/tools/custom-templates).

### 6.2. Граф решения (Solution Explorer в терминах CLI)

| Задача | CLI |
|--------|-----|
| Список проектов в `.sln` / `.slnx` / `.slnf` | `dotnet sln [<файл>] list` |
| Добавить проект(ы) | `dotnet sln [<файл>] add <пути.csproj…>`; опции `--in-root`, `-s\|--solution-folder` (папки решения, как в VS) |
| Убрать проект(ы) | `dotnet sln [<файл>] remove <path_or_project_name>` |
| Миграция `.sln` → `.slnx` | `dotnet sln [<файл>] migrate` |

Источник: [`dotnet sln` (Microsoft Learn)](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln).

### 6.3. Ссылки между проектами (Project → Project)

| Задача | CLI |
|--------|-----|
| Добавить ссылку на другой проект | `dotnet reference add <путь.csproj>` |
| Список ссылок | `dotnet reference list` |
| Удалить ссылку | `dotnet reference remove` |

### 6.4. Ссылки на NuGet-пакеты (не путать с шаблонами)

| Задача | CLI (актуальный стиль — см. доки для твоей версии SDK) |
|--------|--------------------------------------------------------|
| Добавить / удалить / перечислить пакет | `dotnet package add` / `dotnet package remove` / `dotnet package list` (+ `search`, `update`, …) |

Это ближе к **NuGet / зависимости**, чем к «новому типу проекта»; в VS — отдельный менеджер пакетов.

### 6.5. Что в паритет VS через CLI обычно **не** входит (или второй эшелон)

- **COM / Reference Manager / произвольные сборки** — специфика .NET Framework и UI VS; для кроссплатформенного CIDE — за скобами или отдельный режим.
- **`dotnet workload`** (MAUI, wasm-tools, …) — если понадобятся шаблоны под workload, это отдельный слой «готовность SDK» (см. IDE Health / environment).
- **`dotnet msbuild` / произвольные цели** — мощно, но не замена осмысленному UX.

### 6.6. Рекомендуемый порядок внедрения в CIDE (к дорожной карте)

1. **Уже есть:** пустое решение (`dotnet new sln`) — §2.
2. **Высокий выигрыш:** мастер или два шага «новый проект в текущем решении»: `dotnet new <template> -o …` + `dotnet sln add` (+ опционально `solution-folder`).
3. **Обслуживание шаблонов:** обёртки над `dotnet new list/search/install/uninstall/update` (лог в UI, без своего NuGet-клиента в v1).
4. **Ссылки:** `reference` и `package` — из контекста дерева решения / проекта.

Все вызовы — через тот же **`IDotnetCommandRunner`** (и при необходимости разбор stdout для ошибок), без дублирования `Process` в VM.
