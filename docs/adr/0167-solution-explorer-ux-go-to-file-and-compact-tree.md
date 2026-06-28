# ADR 0167: Solution Explorer UX — Go to File, компактное дерево и единый индекс путей

**Статус:** Proposed  
**Дата:** 2026-06-28

## Резюме

Обозреватель решения (`SolutionExplorerView`) остаётся **вторичным** каналом навигации ([0039](0039-workspace-navigation-affordances.md)), но его текущая реализация создаёт высокий когнитивный налог: **все узлы раскрыты**, нет **быстрого поиска файлов**, неудобные **отступы** и плотность в Power mode.

**Принято направление:**

1. **Visual Studio** — **UX baseline** для механики Solution Explorer (поиск в панели, collapse, sync с документом, плотность, жесты) — см. [§2.0](#adr0167-vs-baseline).
2. **Go to File** (глобально, Ctrl+P) — быстрый вход по имени/пути; единый `WorkspaceFileIndex` для палитры, slash и фильтра дерева.
3. **Дерево + иконки** — свёрнуто по умолчанию, компактные отступы, **читаемый набор иконок** (не текущий «vivid» mix на 16–20 px).
4. **Не дублировать** три независимых fuzzy-поиска: обобщить `WorkspaceFileSlashCompletionProvider` → `WorkspaceFileIndex`.

Целевой релизный срез — **MLP** (Minimum **Lovable** Product): не «минимум работает», а «приятно пользоваться каждый день» — см. [§3](#adr0167-phases).

Semantic Map и «вокруг текущего файла» ([0039](0039-workspace-navigation-affordances.md)) **дополняют** дерево; этот ADR не отменяет их.

---

## Связанные ADR

| ADR                                                                           | Роль                                                                |
| ----------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| [0039](0039-workspace-navigation-affordances.md)                              | Дерево — не единственная навигация; PFD vs MFD; «шкаф vs карта боя» |
| [0013](0013-command-surface-and-discoverability.md)                           | Палитра команд, discoverability, жесты в TOML                       |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)                    | `command_id`, `IdeCommands`, hotkeys                                |
| [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md)          | `/file open` + динамические подсказки по путям решения              |
| [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md)              | После выбора файла — открытие в Forward                             |
| [0021](0021-pfd-mfd-cockpit-attention-model.md)                               | PFD: дерево или Semantic Map; MFD: полный обозреватель              |
| [0010](0010-ui-modes-toml-configuration.md)                                   | Power vs Standard — плотность дерева, пресеты                       |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | Semantic map — другой контур; не смешивать с file index             |
| [0161](0161-cide-spine-and-forge-vertical-feature-module.md)                  | Размещение фичи в `Features/Workspace`                              |

### Снимок реализации (на момент ADR)

| Элемент              | Файл / поведение                                                                                                            |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Дерево UI            | `Views/SolutionExplorerView.axaml` — `TreeView` на `Workspace.SolutionRoots`                                                |
| Раскрытие            | `TreeViewItem` → `IsExpanded = True` **на всех узлах**                                                                      |
| Модель               | `Models/SolutionItem.cs`, загрузка — `SolutionParser`, `SolutionWorkspaceViewModel`                                         |
| Slash-пути           | `WorkspaceFileSlashCompletionProvider` (ADR 0125)                                                                           |
| Иконки               | `SolutionItemIconConverter` → `Assets/Icons/*.svg` (file-icon-vectors **vivid**); см. [ASSETS-ICONS.md](../ASSETS-ICONS.md) |
| Синхронизация выбора | `DocumentsWorkspaceViewModel.SyncSelectedSolutionItemToCurrentFile`                                                         |
| MFD-страница         | `SolutionExplorerMfdPageView` — хост того же `SolutionExplorerView`                                                         |

---

<a id="adr0167-context"></a>

## 1. Контекст

### 1.1 Симптомы (operator feedback)

- Глубокое дерево на крупном `.sln` — **длинный скролл** без быстрого фильтра.
- **Отступы** Avalonia `TreeView` + Power styles (`MinHeight` 32, padding) — мало файлов на экран.
- **Все папки раскрыты** при открытии solution — шум вместо обзора.
- **Нет Ctrl+P / Go to File** в глобальной палитре; поиск файлов доступен только через **slash в чате** ([0125](0125-slash-workspace-file-commands-and-dynamic-completion.md)).
- **Иконки** — визуально слабые: смешение **vivid** SVG на 16–20 px, нет согласованности с Power/Standard темой; solution/project/folder плохо отличимы от «ещё одной картинки» (operator feedback).

### 1.2 Visual Studio как эталон механики

**Visual Studio Solution Explorer** — признанный хороший UX для **взаимодействия с деревом** (не для единственной навигации по всему IDE). CIDE **копирует механику VS**, а не изобретает альтернативное дерево:

| VS (эталон)                         | Цель в CIDE                                        |
| ----------------------------------- | -------------------------------------------------- |
| Search Solution Explorer (Ctrl+`;`) | In-panel filter + общий `WorkspaceFileIndex`       |
| Quick Open / Go to File             | Ctrl+P (глобально)                                 |
| Collapse / expand, память состояния | P1 collapse; P2 persist                            |
| **Track Active Item**               | Sync selection ↔ активный документ (toggle в меню) |
| Double-click open, контекстное меню | P1–P2                                              |
| Плотность строк и indent            | Compact tree                                       |

**Не копируем в MLP:** Solution Folders editor, References UI, Show All Files, drag-drop между проектами — отдельные ADR/фазы.

### 1.3 Согласование с 0039

[0039](0039-workspace-navigation-affordances.md) уже фиксирует: классическое дерево **полезно**, но не должно быть **единственным** способом навигации. Этот ADR **доводит SE до VS-grade mechanics** и добавляет Go to File; Semantic Map остаётся слоем C.

### 1.4 Существующий актив

Логика сбора и ранжирования путей для slash **уже реализована** (`WorkspaceFileSlashCompletionProvider`, `McpSolutionTree.CollectFileEntries`). Дублировать её в Go to File и фильтре дерева — технический долг; ADR вводит **единый** `WorkspaceFileIndex`.

---

<a id="adr0167-decision"></a>

## 2. Решение

<a id="adr0167-vs-baseline"></a>

### 2.0 Visual Studio baseline (нормативно)

**Правило:** при споре о поведении SE по умолчанию сравнивать с **Visual Studio 2022 Solution Explorer** (механика, не весь shell). «Lovable» = оператор не ощущает регрессию относительно VS при ежедневном открытии файлов из дерева.

### 2.1 Три слоя навигации (нормативно)

| Слой                | UX                               | Реализация (целевая)                                                                                                            |
| ------------------- | -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| **A. Быстрый вход** | Go to File, Ctrl+P               | `WorkspaceFileIndex` + модальная палитра                                                                                        |
| **B. Дерево**       | Структура solution (**VS-like**) | `SolutionExplorerView` + expand + **иконки**                                                                                    |
| **C. Контекст**     | Semantic Map, related files      | [0039](0039-workspace-navigation-affordances.md), [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) |

Слои **A** и **VS-grade B** (включая иконки) — обязательная часть **MLP**; слой C без изменений.

### 2.2 `WorkspaceFileIndex` (единый индекс)

**Контракт (предложение):**

```csharp
// Features/Workspace/Application/WorkspaceFileIndex.cs
public sealed class WorkspaceFileIndex
{
    void Invalidate(ObservableCollection<SolutionItem> roots, string? solutionPath, string workspaceRoot);
    IReadOnlyList<WorkspaceFileMatch> Search(string query, int limit);
}
```

- **Источник:** обход `SolutionItem` / `McpSolutionTree.CollectFileEntries` (как в [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md)).
- **Потребители:** `WorkspaceFileSlashCompletionProvider` (тонкая обёртка), **Go to File palette**, in-panel filter дерева (фаза 2).
- **Ранжирование:** сохранить существующую эвристику `Rank` из slash provider; при необходимости добавить fuzzy (subsequence / prefix segments) **в одном месте**.

Инвалидация при `LoadSolution` / смене `SolutionRoots`.

### 2.3 Go to File

| Поле            | Значение                                                              |
| --------------- | --------------------------------------------------------------------- |
| `command_id`    | `workspace.go_to_file` (новая константа в `IdeCommands`)              |
| Жест            | `Ctrl+P` в `hotkeys.toml` (паритет Cursor/VS Code)                    |
| UI              | Модальное окно / overlay палитры (тот же chrome, что command palette) |
| Действие        | Enter → `open_file` / существующий путь открытия документа            |
| Побочный эффект | Раскрыть путь в дереве + `SelectedSolutionItem`                       |

**Не заменяет** полную command palette ([0013](0013-command-surface-and-discoverability.md)) — только файлы solution/workspace.

Паритет MCP: опционально `ide_execute_command` с тем же `command_id` (фаза 1 или 2).

### 2.4 Дерево: раскрытие и синхронизация

**Убрать** глобальный стиль `IsExpanded = True` на всех `TreeViewItem`.

**По умолчанию после load solution:**

- Раскрыты: корень solution, узлы **проектов** (`.csproj`).
- Свёрнуты: папки и файлы внутри проектов.

**Track Active Item** (как в VS, toggle в overflow / контекстном меню панели):

- **On:** при смене активного документа — раскрыть путь и выделить узел (`SyncSelectedSolutionItemToCurrentFile`).
- **Off:** дерево не прыгает при переключении табов (поведение VS по умолчанию можно сделать **On**).

**Сохранение состояния** (фаза 2): per-workspace в `%LocalAppData%\CascadeIDE\` или `workspace.toml` — не блокер MLP.

### 2.5 Компактность и отступы

Ввести стиль `solutionExplorerTreeCompact` (Standard и Power):

| Параметр              | Сейчас (Power)  | Целевое                                                                          |
| --------------------- | --------------- | -------------------------------------------------------------------------------- |
| `MinHeight` узла      | 32              | 22–24                                                                            |
| `Padding`             | 4,6             | 2,2–4,2                                                                          |
| Горизонтальный indent | каскад Avalonia | **фиксированный шаг** ~16–18 px на уровень глубины (attached `Depth` + `Margin`) |

Переключатель **«Компактное дерево»** в overflow заголовка панели; default **on** для Power, **on** для Standard после фазы 1.

### 2.6 In-panel filter (Search Solution Explorer)

Под заголовком «Решение · дерево проекта» — поле поиска (**паритет VS Search Solution Explorer**):

- Жест: **Ctrl+`;`** когда фокус в панели SE (или глобально с routing в SE — как в VS);
- substring / fuzzy по `Title` и относительному пути через `WorkspaceFileIndex`;
- при непустом фильтре: **prune tree** (показать match + предков);
- очистка фильтра — Esc.

### 2.7 Поведение мыши и контекстное меню (фаза 2)

| Жест                 | Действие                                                                                       |
| -------------------- | ---------------------------------------------------------------------------------------------- |
| Один клик            | Выделить (`SelectedSolutionItem`)                                                              |
| Двойной клик / Enter | Открыть файл, если `FullPath` — файл                                                           |
| Контекстное меню     | Открыть, показать в проводнике, копировать путь, collapse/expand project; отладка — как сейчас |

### 2.8 Lazy children (фаза 3, опционально)

При очень глубоких `ProjectFileTreeBuilder` — placeholder-папки до первого expand. Не блокер MLP; метрика: время построения дерева на `CascadeIDE.sln` &lt; 500 ms UI freeze.

### 2.9 Размещение PFD vs MFD

Без смены [0021](0021-pfd-mfd-cockpit-attention-model.md):

- **PFD** (`IsDockedPfdSolutionExplorerTree`): компактное дерево **или** Semantic Map (переключение пресетом).
- **MFD** (`SolutionExplorerMfdPageView`): тот же контрол SE + **предпочтительный дом** для Semantic Map на **2 мониторах** — см. [0168](0168-presentation-two-screen-pf-m-layout-policy.md).

**Два монитора `(P+F)(M)`:** по [0168](0168-presentation-two-screen-pf-m-layout-policy.md) PFD **скрыт** по умолчанию — не map, не SE; дерево и карта на **MFD**, файлы — **Ctrl+P**.

Не поддерживать два **разных** дерева с расходящейся логикой — один `SolutionExplorerView` + bindable options (`ShowFilter`, `CompactMode`, `TrackActiveItem`).

### 2.10 Иконки дерева (MLP — обязательно)

<a id="adr0167-icons"></a>

**Проблема:** текущий набор **file-icon-vectors (vivid)** на 16–20 px в `SolutionExplorerView` воспринимается как визуальный шум: разный стиль glyph, плохая читаемость в Power theme, слабое различие **solution / project / folder**, `.csproj` часто уходит в generic `xml.svg`.

**Цель (VS-grade readability):** за один взгляд отличать тип узла и основные расширения; иконки не «ломают» lovable-ощущение панели.

**Решение:**

| Аспект           | Норма                                                                                                                                                          |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Источник**     | Единый согласованный набор: предпочтительно **subset vscode-icons / Codicons** (MIT) или курируемый **CIDE icon set** под SE — не смешивать vivid + ad-hoc SVG |
| **Размер**       | **16×16** logical px в дереве (Standard и Power); один размер, без 16 vs 20 рассинхрона                                                                        |
| **Тема**         | Standard: цветные file icons как в VS; Power: **tinted/monochrome** вариант через `CascadeTheme.*` (не сырые многоцветные SVG на неон-фоне)                    |
| **Узлы**         | Отдельные glyph: **solution**, **project** (C#/F#), **folder** / **folder-open**, **file** fallback                                                            |
| **Расширения**   | Маппинг `SolutionItem.IconKey` → asset; `.csproj`/`.fsproj` → **project**, не xml; partial / dependentUpon — без спец-иконки в MLP                             |
| **Сервис**       | `ISolutionExplorerIconProvider` (или расширение `SolutionItemIconConverter`) — единая точка; Go to File palette **переиспользует** те же иконки                |
| **Документация** | Обновить [ASSETS-ICONS.md](../ASSETS-ICONS.md): «SE MLP set» vs legacy vivid                                                                                   |

**MLP checklist (иконки):**

- [ ] Solution / project / folder узнаются без чтения текста
- [ ] `.cs`, `.axaml`, `.json`, `.md` различимы на 16 px
- [ ] Power mode: контраст ≥ WCAG для фона `PowerSolutionTreePanelBackground`
- [ ] Нет fallback на `file.svg` для типичных расширений cascade-ide solution

**Не в MLP:** git status overlays (modified/added), nested project badges, custom per-solution icons — фаза 3+.

---

<a id="adr0167-phases"></a>

## 3. MLP (Minimum Lovable Product)

**MLP** — один релизный срез, после которого SE **не стыдно** использовать ежедневно (паритет **механики VS** + приятные иконки). Не «минимум фич», а **минимум качества**.

| Блок         | Deliverable                                             | Критерий «lovable»                                    |
| ------------ | ------------------------------------------------------- | ----------------------------------------------------- |
| **P0**       | `WorkspaceFileIndex` + рефактор slash                   | Тесты parity slash autocomplete                       |
| **MLP**      | Go to File + Ctrl+P                                     | Файл из `CascadeIDE.sln` &lt; 3 с                     |
| **MLP**      | Collapse default, expand-to-file, **Track Active Item** | Как VS при открытии/переключении таба                 |
| **MLP**      | Compact indent + in-panel search (Ctrl+`;`)             | ≥ 2× файлов на экран; фильтр без скролла всего дерева |
| **MLP**      | Double-click open, базовое контекстное меню             | Ожидания VS выполняются                               |
| **MLP**      | **Icon set v2** ([§2.10](#adr0167-icons))               | Operator sign-off: «не отстой» на Power + Standard    |
| **Post-MLP** | Persist expand; lazy folders; git overlays на иконках   | Отдельные PR                                          |
| **Post-MLP** | Режим «вокруг текущего файла» в заголовке               | [0039](0039-workspace-navigation-affordances.md)      |

---

<a id="adr0167-non-goals"></a>

## 4. Не цели

- **Весь** Visual Studio Solution Explorer (Solution Folders editor, References UI, nested project drag-drop, Show All Files) — **механика** VS в MLP, не полный feature parity.
- Замена Semantic Map или HCI navigation graph.
- Fuzzy по **всему диску** workspace без привязки к solution tree.
- Три разных алгоритма file search.
- **1170 иконок** file-icon-vectors в репо — только курируемый MLP-набор + fallback.

---

<a id="adr0167-consequences"></a>

## 5. Последствия

### Положительные

- Оператор и агент (через `workspace.go_to_file` / MCP) открывают файлы **быстрее**, чем через скролл дерева.
- Меньше визуального шума при открытии solution.
- SE визуально **не хуже VS** на первом впечатлении (иконки + плотность).

### Отрицательные / риски

- Рефактор slash provider — регрессия autocomplete в чате; **обязательны** тесты на `WorkspaceFileSlashCompletionProvider`.
- Avalonia `TreeView` indent — может потребовать custom item template; риск платформенных багов.
- Ctrl+P конфликтует с браузерными привычками в WebView host — разрешение через `hotkeys.toml` overlay.

### Тесты

| Область                        | Тип                                                 |
| ------------------------------ | --------------------------------------------------- |
| `WorkspaceFileIndex.Search`    | Unit                                                |
| Slash provider после рефактора | Unit (существующие + parity)                        |
| Expand path to file            | Unit на `SolutionTreePath`                          |
| Go to File E2E                 | Manual / `Category=Shell` по мере появления harness |

---

## 6. Отклонённые альтернативы

| Альтернатива                           | Почему нет                                        |
| -------------------------------------- | ------------------------------------------------- |
| Только улучшить дерево, без Go to File | Не снимает главную боль на больших solution       |
| Go to File только через slash в чате   | Скрыто; не паритет Cursor/VS; лишний шаг          |
| Сразу virtualizing tree + lazy load    | Отложить post-MLP; MLP даёт 80% без этого         |
| Оставить vivid SVG «как есть»          | Ломает lovable; иконки — часть MLP                |
| Один flat list вместо дерева           | Теряется структура проектов; не заменяет Explorer |

---

<a id="adr0167-history"></a>

## История

| Дата       | Событие                                                       |
| ---------- | ------------------------------------------------------------- |
| 2026-06-28 | Proposed: co-design (отступы, поиск, иконки)                  |
| 2026-06-28 | Amend: VS baseline, **MLP** (не MVP), icon set v2 в scope MLP |
