# Cascade IDE — макет главного окна (v1)

Описание раскладки главного окна для согласования с MCP и онбординга. **Эталон —** `Views/MainWindow.axaml` и связанные view (в т.ч. `DocumentsDockView`, `MfdShellView`).

**Режим UI:** в поставке один id в **`UiModes/index.toml`** — **Flight** (полигон PFD · Forward · MFD). Отдельных продуктовых пресетов **Focus / Balanced / Power** и переключателя «режим интерфейса» в меню **нет**. Семья **`UiModeFamily.Flight`** и capabilities — в TOML режима; контракт — **[ADR 0010](../adr/0010-ui-modes-toml-configuration.md)**, обзор — **[ui-modes-overview-v1.md](ui-modes-overview-v1.md)**, внимание и зоны — **[ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md)**.

---

## 1. Общая структура

Окно: **Grid** (корень) → внутри **DockPanel** — сверху **Menu**, ниже **`MainGrid`** (одна сетка рабочей области).

**Размер по умолчанию:** 1000×600. Ресайз по границе окна и **сплиттерам** между колонками PFD / Forward / MFD.

Поверх — **CommandPalette** (`ZIndex` 5000), при необходимости оверлеи зон/подсветки.

---

## 2. Меню (`Menu`, DockPanel.Top)

Актуальный состав (см. код):

- **Файл:** открыть решение / папку / файл, экспорт Markdown, выход.
- **Отладка:** шаг F5/старт, стартовый проект, attach, остановка, шаги (over/into/out).
- **Вид:** палитра команд; чекбоксы видимости **PFD** (обозреватель / карта — по раскладке), **вывод сборки** (страница MFD), **MFD** (колонка), **терминал** (страница MFD), **Git** (страница MFD), **док инструментирования**; подменю **Тема**; язык интерфейса; Markdown preview в MFD; превью в отдельном окне.
- **Настройки:** параметры AI и чата.
- **Справка:** о программе.

**MCP-баннер:** `McpBannerView` в строке 0 `MainGrid` (не в меню), если IDE в режиме MCP-сервера.

---

## 3. `MainGrid` — сетка

**Строки (RowDefinitions):** три строки — `Auto`, `Auto`, `*`.

| Row | Содержимое |
| --- | --- |
| 0 | `McpBannerView` (при `IsMcpServerMode`) |
| 1 | **`TaskCockpitView`** — полоса задач, CascadeChord, быстрые действия (по capabilities) |
| 2 | **Три зоны внимания:** PFD · Forward · MFD (см. ниже) |

**Колонки (ColumnDefinitions):** `220, 4, *, 4, 340` (базовые ширины; PFD и MFD могут схлопываться по привязкам).

| Col | Содержимое |
| --- | --- |
| 0 | **PFD** — `AttentionZoneContainer Zone="Pfd"`: обозреватель решения и/или карта навигации workspace (по runtime/placement), опциональный mount инструмента. |
| 1 | `GridSplitter` (видимость/блокировка с VM) |
| 2 | **Forward** — `AttentionZoneContainer Zone="Forward"`: **`DocumentsDockView`** (Avalonia Dock — вкладки документов, редактор). |
| 3 | `GridSplitter` |
| 4 | **MFD** — `AttentionZoneContainer Zone="Mfd"`: **`MfdShellView`** (вторичный контур: полоса сверху + стек **страниц**). |

Оверлей геометрии зон (отладка раскладки): `SkiaZoneGeometryOverlayPfd` / `Forward` / `Mfd`. Подсветка агента: `AgentHighlightLayer` на весь `MainGrid`.

`UiModeBloomOverlay` — декоративный bloom по настройкам хрома (семья/тема из TOML).

---

## 4. Зона Forward (`DocumentsDockView`)

- **HUD** (стр. [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) §9): при непустом баннере — полоса над доком.
- **Док-менеджер:** `DockControl` — factory/layout из ViewModel, группы редакторов, **без** привязки «журнал сборки внизу колонки» к старому макету: длинные логи — на **страницах MFD** (сборка, тесты, терминал, …).

Имена для MCP: по возможности совпадают с `Name` в XAML док-контролов (см. `DocumentsDockView` и фабрику доков).

---

## 5. Зона MFD (`MfdShellView` + `MfdShellPageStack`)

Сверху вниз внутри колонки MFD:

1. **`WorkspaceChromeBandView`** — полоса в духе EICAS / IDE Health (видимость и содержимое — по TOML и VM).
2. **`MfdContourStackHost`** — `Border`-хост со **`MfdShellPageStack`**: одна **активная страница** (`CurrentMfdShellPage`), например:
   - Workspace Health (выделенная страница), обозреватель в MFD, related files, превью Markdown, чат, настройки AI, **терминал**, **журнал сборки**, Problems, **Git**, события, тесты, гипотезы, стек отладки, …

Переключение страниц — из VM/команд/меню (не отдельный `TabControl` нижней панели **главного** окна: его в текущем `MainWindow` **нет**).

**Терминал** на странице MFD — заглушка «одна команда → вывод»; не Integrated Shell — **[mfd-terminal-stub-vs-integrated-shell-v1.md](mfd-terminal-stub-vs-integrated-shell-v1.md)**.

---

## 6. Ключевые контролы и MCP

| Зона / смысл | Имя / примечание |
| --- | --- |
| Корень окна | `RootWindow` |
| Сетка | `MainGrid` |
| Чат (страница MFD) | внутри `ChatMfdPageView` / стека |
| Ввод в чат | `ChatInputBox` (на странице чата) |
| Терминал (ввод) | `TerminalInputBox` (`TerminalMfdPageView`) |
| Подсветка агента | `AgentHighlightOverlay` на `AgentHighlightLayer` |

`ide_set_panel_size` и аналоги — по актуальному контракту MCP; геометрия сейчас опирается на **сплиттеры трёх колонок** и настройки из `workspace.toml` / capabilities.

---

## 7. Оверлей подсветки

`AgentHighlightLayer` (Canvas, `ZIndex` 1000) поверх сетки; `AgentHighlightOverlay` — рамка вокруг целевого контрола (`ide_highlight_control`). `IsHitTestVisible=false`, чтобы не перехватывать клики.

---

## 8. Исторический контекст (не эталон раскладки)

Ранние макеты с **нижней панелью** главного окна (вкладки Terminal / Build / … в одном `BottomPanelView`) и пресетами **Focus / Balanced / Power** относятся к **старой** топологии. Текущий **Flight** — **PFD | Forward | MFD** в одной сетке, длинные потоки — **страницы MFD**. Уточнения по старым PNG — `concept-to-implementation-map-v1.md`, `concept-generated/`; визуал концептов **не** обязан совпадать с кодом.

---

*Версия документа: 2.0. Соответствует `MainWindow.axaml` и `UiModes/index.toml` (только **Flight**). При смене разметки — обновить этот файл.*
