# ADR 0168: Раскладка двух мониторов — `(P+F)(M)` и бюджет PFD / Forward

**Статус:** Proposed  
**Дата:** 2026-06-28

## Резюме

Канон [0017](0017-multi-window-workspace-and-agent-surfaces.md) описывает **три** якоря на **трёх** экранах `(P)(F)(M)`. У оператора часто **два монитора**: `(P+F)(M)` или `(0.25P + 0.75F)(M)` — PFD и Forward **делят один экран**, MFD на втором.

При такой топологии **semantic map на PFD** (дефолт `pfd_primary = "workspace_map"`) и **широкий chrome Monaco** (minimap, будущий outline) на узком Forward дают плохую читаемость. **Принято:**

1. Для **P+F на одном экране** — **PFD по умолчанию схлопнут** (колонка 0 / только полоса cockpit при необходимости). **Не** map и **не** Solution Explorer на PFD — оба отъедают Forward без пользы на ~220 px.
2. **Файловая навигация:** **Ctrl+P** ([0167](0167-solution-explorer-ux-go-to-file-and-compact-tree.md)) + **Solution Explorer на MFD** (страница); Semantic Map — тоже **MFD**.
3. **Forward editor budget** — topology-aware: minimap / outline не съедают кодовую колонку на узком F.

Три монитора `(P)(F)(M)` — отдельный дефолт: на **своём** экране P допустимы map или SE ([0039](0039-workspace-navigation-affordances.md)).

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | `presentation`, topology, host windows |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD / Forward / MFD, внимание |
| [0039](0039-workspace-navigation-affordances.md) | Semantic map vs дерево; map не обязана жить на PFD |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | `CockpitPresentationLayoutPolicy` |
| [0163](0163-monaco-native-capability-bus-full-forward-migration.md)–[0164](0164-monaco-editor-presentation-projection-and-dock-chrome.md) | Monaco chrome, dock |
| [0167](0167-solution-explorer-ux-go-to-file-and-compact-tree.md) | SE MLP, Ctrl+P |

### Вне ADR

| Документ | Роль |
|----------|------|
| [playbook-layout-presentation-intercom-troubleshooting-v1](https://github.com/KarataevDmitry/agent-notes/blob/main/knowledge/work/projects/door-to-singularity/cascade-ide/playbook-layout-presentation-intercom-troubleshooting-v1.md) | Симптомы `(P+F)(M)` |
| [cascade-ide-ui-layout-v1.md](../ui-ux/cascade-ide-ui-layout-v1.md) | `MainGrid` 220 · * · 340 |

### Снимок (на момент ADR)

| Параметр | Значение |
|----------|----------|
| Дефолт topology | `(0.25P + 0.75F) (M)` — [defaults-settings.toml](../../Settings/defaults-settings.toml) |
| Дефолт PFD primary | `pfd_primary = "workspace_map"` |
| PFD в main | `SolutionExplorerView` **или** `WorkspaceNavigationMapView` (взаимоисключающие) |
| Forward | Monaco: `minimap: enabled`, inlay hints on |
| Детектор 2-screen P+F | `PresentationLayoutAnalyzer.IsPmPlusForwardTwoScreenPreset` |

---

<a id="adr0168-context"></a>

## 1. Контекст

### 1.1 Два монитора vs канон трёх

| Topology | Экран 1 | Экран 2 | Типично |
|----------|---------|---------|---------|
| `(P)(F)(M)` | P | F | M | 3 монитора, north-star [0017](0017-multi-window-workspace-and-agent-surfaces.md) |
| `(P+F)(M)` / `(0.25P + 0.75F)(M)` | **P + F** | M | **2 монитора** (operator default) |
| `(F)(xP+yM)` | F only | P+M host | узкий main |

На **2 мониторах** PFD — не «боковая панель на всём экране F», а **~25% ширины** монитора (или фиксированные **220 px** в `MainGrid`). Semantic map с графом/подписями в **220 px** нечитаема; оператор feedback: «плохо читается».

### 1.2 Forward: Monaco отъедает место

На том же экране Forward делит ширину с PFD. Дополнительно:

- **Minimap** (включён в [cide-editor-bridge.js](../../Assets/cide-editor/cide-editor-bridge.js));
- **Inlay hints**;
- планируемый **outline / breadcrumbs** в dock chrome ([0164](0164-monaco-editor-presentation-projection-and-dock-chrome.md));
- HUD / diagnostic strips.

Итог: **эффективная** ширина кода &lt; ожиданий VS/Rider на одном мониторе.

### 1.3 Согласование с 0039

[0039](0039-workspace-navigation-affordances.md) уже допускает: **semantic map на PFD**, **дерево на MFD**. Для **2 экранов** приоритет: **не дублировать «шкаф» рядом с редактором** — файлы через **Ctrl+P** и страницы **MFD**; PFD не обязан показывать ни map, ни SE.

### 1.4 Зачем не SE на PFD при 2 мониторах

| Аргумент | Смысл |
|----------|--------|
| Узкая колонка | SE в 180–220 px — тот же скролл-ад, что и map, только другой вид |
| [0167](0167-solution-explorer-ux-go-to-file-and-compact-tree.md) | Ежедневное «открыть файл» = **Ctrl+P**, не дерево |
| MFD уже есть | `SolutionExplorerMfdPageView` — полноценная страница SE |
| [0039](0039-workspace-navigation-affordances.md) | Дерево — **вторичный** инструмент, не якорь PFD на каждой топологии |
| Forward | Каждый px PFD — минус у кода на том же мониторе |

**Вывод:** на `(P+F)(M)` PFD default = **hidden/collapsed**; SE и map — **opt-in** (меню «Вид») или **MFD**, не дефолт на PFD.

---

<a id="adr0168-decision"></a>

## 2. Решение

### 2.1 Topology-aware PFD (2-screen: **не** map, **не** SE)

**Правило:** при `IsPmPlusForwardTwoScreenPreset` (P+F на одном physical screen) effective default:

| Состояние PFD | Когда |
|---------------|--------|
| **`collapsed`** (колонка скрыта, `IsPfdColumnVisible = false`) | **Default** — Forward на всю ширину экрана 1 |
| `cockpit_strip` (optional) | Только TaskCockpit / health strip без дерева и map |
| `solution_tree` / `workspace_map` | **Только opt-in** — меню «Вид» или `settings.toml` |

Реализация: `CockpitPresentationLayoutPolicy` + существующие `IsPfdColumnVisible` / `IsPfdRegionExpanded`.

**Три монитора** `(P)(F)(M)`: на **отдельном** экране P — `workspace_map` или `solution_tree` по вкусу (широкая колонка).

### 2.2 MFD — дом для «шкафа» и карты

| Инструмент | Дом на 2-screen |
|------------|-----------------|
| **Solution Explorer** | Страница MFD (`SolutionExplorerMfdPageView`) — когда нужно дерево |
| **Semantic Map** | Страница MFD (`WorkspaceNavigationMapView`) |
| **Ctrl+P** | Повседневное открытие файла без панели |

Переключение страниц MFD — как сейчас; не требовать дублирования SE на PFD.

### 2.3 Бюджет ширины на 2-screen

| Мера | Цель |
|------|------|
| PFD default **hidden** | Forward = почти 100% ширины монитора 1 |
| Opt-in PFD | Оператор сам включает SE/map, если нужен «шкаф» сбоку |
| Не дублировать map **и** tree на PFD | Как сейчас (взаимоисключение), при opt-in |

### 2.4 Forward editor chrome (topology-aware)

**`ForwardEditorChromePolicy`** (новый маленький сервис или флаги в settings):

| Chrome | 3-screen F (широкий) | 2-screen F (узкий, P сосед) |
|--------|----------------------|-----------------------------|
| Minimap | on (default) | **off** или `fit` minimap |
| Inlay hints | on | on (мало по ширине) |
| Outline / symbol panel | dock tab / toggle | **не** постоянная колонка; overlay / Ctrl+Shift+O |
| Breadcrumb | compact one line | optional off |

Порог: `forwardColumnWidth < 900` (настраиваемо) → apply narrow preset.

Monaco options push через `editor/setIntelligence` или dedicated `editor/setLayoutChrome`.

### 2.5 Рекомендуемые пресеты settings (документация)

```toml
[display.screens]
topology = "(0.25P + 0.75F) (M)"

[display.instruments]
# 2-screen policy: PFD collapsed unless user sets pfd_primary explicitly
# pfd_primary = "collapsed"   # future explicit key; until then IsPfdColumnVisible default false

[display.forward]
# narrow_chrome_auto = true
# minimap = "auto"
```

### 2.6 MLP вместе с [0167](0167-solution-explorer-ux-go-to-file-and-compact-tree.md)

**MLP layout (2 монитора):**

- [ ] PFD **скрыт** по умолчанию при P+F shared
- [ ] Ctrl+P — основной путь к файлам
- [ ] SE / map — страницы **MFD**, не дефолт на PFD
- [ ] Minimap off при narrow Forward (или user toggle)

---

<a id="adr0168-non-goals"></a>

## 3. Не цели

- Отмена `(P)(F)(M)` для 3 мониторов.
- Новый четвёртый якорь в EBNF presentation.
- Автоматическое определение числа мониторов без `topology` (оператор задаёт строку явно).

---

<a id="adr0168-consequences"></a>

## 4. Последствия

- `defaults-settings.toml`: для 2-screen — policy **скрывает PFD** (`IsPfdColumnVisible` default false), не меняет `pfd_primary` на SE; map/SE — MFD + Ctrl+P.
- Playbook layout: симптом «map/SE бесполезны на PFD» → topology `(P+F)(M)` → скрыть PFD, открыть MFD.
- Тесты: `IsPmPlusForwardTwoScreenPreset` → `IsPfdColumnVisible == false` при старте (без user override).

---

<a id="adr0168-history"></a>

## 5. История

| Дата | Событие |
|------|---------|
| 2026-06-28 | Proposed: operator feedback — `(P+F)(M)`, map на PFD + outline/minimap на F |
| 2026-06-28 | Amend: 2-screen — PFD **collapsed**, не SE на PFD; SE/map на MFD + Ctrl+P |
