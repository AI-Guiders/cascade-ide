# GlassCore · inventory CIDE config SSOT (v0)

Для общего слоя (Avalonia CIDE + WPF Glass + habitat peels): **не второй конфиг**, те же файлы и merge.

## Два главных файла (да — по сути они)

| Файл | Где | Слой | Роль для glass / presentation |
|---|---|---|---|
| **`settings.toml`** | `%LocalAppData%/CascadeIDE/settings.toml` | **личный** (машина/оператор) | Topology / grammar / tier / `primary_work_surface` / display instruments / AI / LSP / theme tokens… Канон [ADR 0028](../adr/0028-user-settings-toml-localappdata-and-secrets.md), presentation [0017](../adr/0017-multi-window-workspace-and-agent-surfaces.md). |
| **`workspace.toml`** | `<repo>/.cascade/workspace.toml` | **командный** (репо) | Overlay раскладки/режимов/instrument routing / code_navigation presets. **Не** дублирует всю личную машину. Latch presentation **не** пишет сюда ([ShellConstruction](../../ViewModels/MainWindowViewModel.ShellConstruction.cs): glass patch → settings only). |

**Merge (типичный):** бандл IDE → `workspace.toml` (репо) → `settings.toml` (user). Точные оси — по секции (UI modes [0010](../adr/0010-ui-modes-toml-configuration.md), nav [0039](../adr/0039-workspace-navigation-affordances.md)).

## Соседние файлы в `%LocalAppData%/CascadeIDE/` (не «третий SSOT presentation», но DAL тот же)

| Файл | Нужен glass host? | Заметка |
|---|---|---|
| `hotkeys.toml` | позже (chrome) | User overlay; бандл `Hotkeys/hotkeys.toml` |
| `editor-languages.toml` | позже (editor projector) | User overlay; бандл `Settings/editor-languages.toml` |
| `ai-keys.toml` | нет для layout | Секреты; [0028](../adr/0028-user-settings-toml-localappdata-and-secrets.md) — **не** тащить в GlassCore surface |

Пути: `Features/Settings/DataAcquisition/UserSettingsPaths.cs`.

## Бандл рядом с exe (заводской слой)

| Артефакт | Роль |
|---|---|
| `Settings/defaults-settings.toml` | Шипнутый дефолт; merge *под* user settings.toml |
| UI mode / workspace бандлы (см. 0010) | База до repo overlay |
| `CodeNavigation/presets.toml` и т.п. | Не layout P\|F\|M; semantic map / MCP |

## Live desk (не TOML, но SSOT кадра в runtime)

| Latch | Где | vs settings |
|---|---|---|
| `presentation-LATEST.json` | `%LocalAppData%/cdp-mcp/` | Live patch topology/tier/mfd → обычно **persists into settings.toml**, не в workspace.toml |
| `intercom-LATEST.json`, seats, alert/qrh/ecl… | тот же корень | Каналы / EICAS; не файл настроек |

## Что выносить в GlassCore (настройки)

**Сейчас (peel):** Tomlyn-срез topology / tier / primary_work_surface / grammar.

**Цель:**

1. `UserSettingsPaths` + load/save **`settings.toml`** (= `SettingsService` / `CascadeIdeSettings` без Avalonia).
2. Loader **`workspace.toml`** + merge policy (как `UiModeCatalog` / `UiWorkspaceToml`) — когда открыт scm root.
3. `defaults-settings.toml` как baseline merge.
4. Presentation stack уже linked (`Services/Presentation/*`).

**Не в первый клин:** hotkeys, editor-languages, ai-keys, Themes JSON strangler, полный AI/LSP graph — подключать по мере projector’ов.

## Инвариант

Один оператор / одна машина → один `settings.toml`. Один репо → один `.cascade/workspace.toml`. WPF и Avalonia **читают одни и те же**; Surface только проецирует.
