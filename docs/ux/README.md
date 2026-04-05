# Cascade IDE — UX и макеты

Каталог для макетов, описаний раскладки и контрактов интерфейса Cascade IDE. Эталон реализации — код в `Views/MainWindow.axaml` и темы в `Themes/`.

## Содержимое

| Файл                                    | Назначение                                                                                                       |
| --------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `README.md`                             | Этот файл — оглавление UX-набора.                                                                                |
| `cascade-ide-ui-layout-v1.md`           | Описание макета главного окна: зоны, панели, грид, режимы, ключевые контролы для MCP. |
| `ui-modes-overview-v1.md`               | Краткий обзор режимов: id vs семья, TOML, связь с VM/XAML; детали — ADR 0010. |
| `cascade-ide-main-window-wireframe.png` | Макет-картинка (wireframe) главного окна.                                                                        |
| `concept-screens/`                      | Скрины из чатов: референсы детального хрома UI. См. `concept-screens/README.md` (напр. `power-project-explorer-tree-concept.png`). |
| `concept-generated/`                    | Сгенерированные агентом UI-концепты CascadeIDE (Focus/Balanced/Power “cockpit”).                                 |
| `power-mode-concepts-v1.md`             | Текстовое описание сгенерированных UI-концептов (Focus/Balanced/Power) с чеклистом для имплементации.            |
| `concept-to-implementation-map-v1.md`   | Таблица «концепт → XAML/VM» + **§4.1** визуальный хром Power (дерево / редактор / трасса / телеметрия) vs Fluent по умолчанию. |
| `concept-pfd-mfd-cascade-v1.md`         | Концепт **PFD/MFD** (авионика): первичное внимание vs мультифункциональные панели; связь с Focus/Balanced/Power и агентом; не ADR. |
| `note-acp-cascade-cursor-v1.md`         | **Agent Client Protocol (ACP):** терминология, не путать с другим «ACP»; Cascade как клиент, внешние агенты (в т.ч. Cursor); спека, smoke `samples/AcpSmoke`. |

## Макеты

**Макет-картинка (wireframe):** `cascade-ide-main-window-wireframe.png` — схема главного окна: меню, тулбар, дерево решения слева, редактор в центре, чат справа, терминал снизу. Положи файл в этот каталог (`docs/ux/`); если картинка была сгенерирована в чате — сохрани её из интерфейса Cursor сюда под этим именем.

**Скрины-концепты (из чатов):** см. `concept-screens/` — там лежат исходные картинки, на базе которых собирались Power Mode / раскладка / визуальный стиль.

**Сгенерированные UI-концепты (agent render):** см. `concept-generated/` — `cascadeide-ui-concept-focus/balanced/power.png` и wireframe.

**Источник правды** по разметке — `Views/MainWindow.axaml` и поведение из ViewModel. Документ `cascade-ide-ui-layout-v1.md` фиксирует раскладку и имена панелей/контролов для MCP и онбординга.

**Концепт PNG vs «глянец» в коде:** раскладка и острова Power в целом совпадают с `concept-generated/*.png`, но **строки списков, выделение, плотность** часто остаются на **Avalonia Fluent** (см. **`cascade-ide-ui-layout-v1.md` §10**, **`power-mode-concepts-v1.md` §5**, **`concept-to-implementation-map-v1.md` §4.1**).

## Локализуемые строки (чат и заголовки панелей)

Паттерн как в **IncomeCascade** (`Lang/Resources*.resx`):

- **`Lang/Resources.resx`** — нейтральный набор (русские строки по умолчанию).
- **`Lang/Resources.ru-RU.resx`** / **`Lang/Resources.en-US.resx`** — спутники; SDK кладёт их в подкаталоги **`ru-RU`/`en-US`** рядом с `CascadeIDE.dll` (`CascadeIDE.resources.dll`), чтобы `ResourceManager` подхватывал строки при смене `Resources.Culture`.
- Код: **`CascadeIDE.Lang.Resources`**, при старте — **`UiCulture.ApplyFromSettingsOrSystem()`** из `App.OnFrameworkInitializationCompleted` (сохранённый `UiCultureName` в `settings.toml` или системная локаль через **`ApplyFromSystem`**; далее **`LocViewModel.SetCulture`** если `Loc` уже в ресурсах приложения).
- XAML (чат, заголовки панелей, пункты меню языка): привязка к **`LocViewModel`** из ресурса приложения:
  `{Binding ИмяКлюча, Source={StaticResource Loc}, DataType={x:Type lang:LocViewModel}}` (`xmlns:lang="using:CascadeIDE.Lang"`). Экземпляр объявлен в **`App.axaml`**: `<lang:LocViewModel x:Key="Loc"/>`.
- C#: `Resources.ИмяКлюча` (культура задаётся через `Resources.Culture` / `LocViewModel.SetCulture`).

**Смена языка в рантайме:** меню **«Вид → Язык интерфейса»** (Русский / English; отдельно **«Как в системе»** — сбрасывает `UiCultureName` и вызывает `UiCulture.ApplyFromSystem()`); `SetUiLanguageCommand` → `LocViewModel.SetCulture(ru-RU|en-US)`; строки с привязкой к `Loc` обновляются без перезапуска. Вычисляемые свойства VM, которые читают `Resources` напрямую (например `SafetyLevelDescription`), при смене языка дополнительно уведомляются из команды. Выбор сохраняется в **`UiCultureName`** в `settings.toml` (каталог `%LocalAppData%\CascadeIDE\`); при следующем запуске вызывается **`UiCulture.ApplyFromSettingsOrSystem()`** (если поле пустое — поведение как у системной локали через `ApplyFromSystem`).

## Связь с кодом

- **Режимы UI:** встроенные пресеты Focus / Balanced / Power / Agent Chat / Debug — меню «Вид → Режим интерфейса», комбо в тулбаре, хоткеи цикла `Alt+1` / `Alt+2` / `Alt+3`, `Ctrl+Alt+M`. Поведение и данные: **`ui-modes-overview-v1.md`**, контракт TOML — **[ADR 0010](../adr/0010-ui-modes-toml-configuration.md)**. В Power — task bar, Quick Actions (capability), телеметрия кокпита; в Focus — минимум отвлечений.
- **Темы:** меню «Вид → Тема» (светлая, тёмная, как Cursor, **Power классическая (циан)**); при входе в режим **Power** по умолчанию подставляется **`Themes/power-cockpit-concept-theme.json`** (палитра ближе к `concept-generated/cascadeide-ui-concept-power.png`); прежняя циановая Power — пункт меню. Тул `ide_set_ui_theme` меняет тему из MCP.
- **Панели:** Solution Explorer, Build Output, Chat, Terminal — скрываемые; имена контролов для MCP см. в `cascade-ide-ui-layout-v1.md`.
