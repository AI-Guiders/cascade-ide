# Cascade IDE — UX и макеты

Каталог для макетов, описаний раскладки и контрактов интерфейса Cascade IDE. Эталон реализации — код в `Views/MainWindow.axaml` и темы в `Themes/`.

## Содержимое

| Файл                                    | Назначение                                                                                                       |
| --------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `README.md`                             | Этот файл — оглавление UX-набора.                                                                                |
| `cascade-ide-ui-layout-v1.md`           | Описание макета главного окна: зоны, панели, грид, режимы (Focus / Balanced / Power), ключевые контролы для MCP. |
| `cascade-ide-main-window-wireframe.png` | Макет-картинка (wireframe) главного окна.                                                                        |
| `concept-screens/`                      | Сохранённые изображения из чатов (референсы и UI-концепты, включая Power Mode).                                  |
| `concept-generated/`                    | Сгенерированные агентом UI-концепты CascadeIDE (Focus/Balanced/Power “cockpit”).                                 |
| `power-mode-concepts-v1.md`             | Текстовое описание сгенерированных UI-концептов (Focus/Balanced/Power) с чеклистом для имплементации.            |
| `concept-to-implementation-map-v1.md`   | Таблица соответствия “концепт → текущий XAML/VM” + список минимальных инкрементов для выравнивания UI 1:1.       |

## Макеты

**Макет-картинка (wireframe):** `cascade-ide-main-window-wireframe.png` — схема главного окна: меню, тулбар, дерево решения слева, редактор в центре, чат справа, терминал снизу. Положи файл в этот каталог (`docs/ux/`); если картинка была сгенерирована в чате — сохрани её из интерфейса Cursor сюда под этим именем.

**Скрины-концепты (из чатов):** см. `concept-screens/` — там лежат исходные картинки, на базе которых собирались Power Mode / раскладка / визуальный стиль.

**Сгенерированные UI-концепты (agent render):** см. `concept-generated/` — `cascadeide-ui-concept-focus/balanced/power.png` и wireframe.

**Источник правды** по разметке — `Views/MainWindow.axaml` и поведение из ViewModel. Документ `cascade-ide-ui-layout-v1.md` фиксирует раскладку и имена панелей/контролов для MCP и онбординга.

## Связь с кодом

- **Режимы UI:** Focus / Balanced / Power — переключение из меню «Вид → Режим интерфейса» и хоткеи `Alt+1` / `Alt+2` / `Alt+3`, `Ctrl+Alt+M`. В Power-режиме видимы Task Bar, Quick Actions, Telemetry; в Focus — минимум отвлечений.
- **Темы:** меню «Вид → Тема» (светлая, тёмная, как Cursor); тул `ide_set_ui_theme` меняет тему из MCP.
- **Панели:** Solution Explorer, Build Output, Chat, Terminal — скрываемые; имена контролов для MCP см. в `cascade-ide-ui-layout-v1.md`.
