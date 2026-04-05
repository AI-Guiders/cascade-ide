# Режимы интерфейса Cascade IDE (обзор)

Краткое введение для UX, онбординга и ревью макетов. Полный контракт данных, TOML и загрузчика — в **[ADR 0010: UI modes in TOML](../adr/0010-ui-modes-toml-configuration.md)**.

## Что переключает пользователь

- **Строка режима (`UiMode`)** — id пресета: что показывается в комбо и в бейдже тулбара (`Focus`, `Balanced`, `Power`, `AgentChat`, `Debug`, …). Список пунктов меню задаётся **`UiModes/index.toml`** (шипнутый набор рядом с exe).
- **Семья режима (`UiModeFamily`)** — продуктовая ось для кода и XAML: Focus / Balanced / Power / AgentChat / Debug. Нужна, чтобы производный id (например кастомный пресет) вёл себя как «отладка» или «power», не сравнивая строку id по всему приложению.

## Где лежат данные

| Что | Где |
|-----|-----|
| Порядок режимов в меню | `UiModes/index.toml` (`schema_version`, `modes`) |
| Раскладка и capabilities одного режима | `UiModes/<Id>.toml` |
| Общие числа хрома (сплиттеры, базовые ширины чата по правилам семей и т.д.) | `UiModes/workspace.toml` |
| Запасные значения, если файлов нет | код: `UiModeLayoutRegistry`, `UiModeCatalog` |

Наследование между режимами (`inherits`), мердж capabilities и порядок **`family`** — только в ADR 0010.

## Как это попадает в UI

- **ViewModel:** вычисляемое **`UiModeFamily`** из нормализованного `UiMode` (резолвер + каталог TOML).
- **Тонкие флаги «что показать»** (Quick Actions, гипотезы, trace Power и т.д.) — объект **`Capabilities`** на VM (после мержа TOML и дефолтов по семье).
- **XAML:** вместо старых булевых `Is*Mode` — привязки к **`UiModeFamily`** через конвертеры **`UiModeFamilyEq`** / **`UiModeFamilyNe`** и параметр (`Power`, `Focus`, …). Пример: компактная полоса телеметрии под редактором — когда семья **не** Power (`TelemetryStripView`).

## Связанные UX-документы

- Макет окна и имена зон: **`cascade-ide-ui-layout-v1.md`**
- Карта «концепт → код»: **`concept-to-implementation-map-v1.md`**

*Версия: 2026-04.*
