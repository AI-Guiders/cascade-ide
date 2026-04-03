# CascadeIDE

Лёгкая IDE для .NET, которой управляет агент через MCP. Стек: .NET 10, Avalonia, интеграция с локальными моделями **Ollama** (в т.ч. Ollama 4.x).

Операционная карточка проекта (Purpose, Vision, границы, ссылки) в каноне заметок: `knowledge/work/projects/current-projects/cascade-ide/README.md` в репозитории **agent-notes** (слой **`work/`** — оперативный канон; в публичную KB не входит, см. **`knowledge/PUBLISHING.md`** и `scripts/build-public-kb.ps1` в agent-notes). В двух словах: редактор + дерево решения + сборка/запуск + отладка через debug-mcp, анализ через roslyn-mcp, чат/запросы к модели — через локальный Ollama.

## Требования

- .NET 10 SDK
- Windows / Linux / macOS (Avalonia)
- Для чата с моделью: запущенный [Ollama](https://ollama.com) (localhost:11434)

## Сборка и запуск

```bash
cd cascade-ide
dotnet build
dotnet run
```

При запуске приложение проверяет доступность Ollama и выводит список локальных моделей (или подсказку установить Ollama).

**Текущее состояние:**

- **Редактор:** `AvaloniaEdit` + TextMate-подсветка (включая C#), выделение диапазонов, применение точечных правок из MCP.
- **Чат с моделью:** правая панель — провайдеры Ollama/OpenAI/Anthropic/DeepSeek, выбор модели, история сообщений и стриминг ответа.
- **Дерево решения:** левая панель — открытие `.slnx/.sln`, проекты/файлы в дереве; клик открывает файл в редакторе.
- **Нативная поддержка slnx:** `SolutionParser` загружает `.slnx` (XML) и `.sln` (текстовый формат).
- **MCP сервер IDE:** `IdeMcpServer` с инструментами для редактора, build/test, отладки-представления и UI-автоматизации (включая `ide_get_ui_layout`, `ide_build`, `ide_run_tests`, `ide_set_breakpoint`, `ide_request_confirmation`). Запуск с **`--mcp-stdio`** поднимает MCP на stdin/stdout; Cursor (или другой хост) подключается к процессу и вызывает тулы. См. [docs/MCP-PROTOCOL.md](docs/MCP-PROTOCOL.md).
- **Подтверждения из MCP:** `ide_request_confirmation` показывает модальный `OK/Cancel` диалог в UI и возвращает `ok`/`cancel`.
- **Отладочный контур:** поддержаны отображение брейкпоинтов/текущей строки/состояния отладки и синхронизация с `dotnet-debug-mcp`. Целевое видение — один слой отладки для человека и агента: [docs/debug-human-agent-parity-v1.md](docs/debug-human-agent-parity-v1.md).
- **Git (нативно):** вкладка «Git» в нижней панели (меню **Вид → Git**) — корень репозитория vs каталог решения, статус, diff, `submodule status`, стейджинг, пометка `[sm]` из `.gitmodules`, **submodule update --init** / **sync**, **открыть .sln/.slnx в submodule** (переключение решения), папка submodule в проводнике, коммит (add -A или только staged), push. Подробнее: [docs/git-and-submodules-v1.md](docs/git-and-submodules-v1.md).
- **Режимы UI:** `Focus / Balanced / Power` с переключением из меню/toolbar и хоткеями `Alt+1/Alt+2/Alt+3` (`Ctrl+Alt+M` — циклическое переключение).
- **Настройки и данные:** выбранные модели и UI-параметры сохраняются в `%LocalAppData%\\CascadeIDE\\settings.toml`; данные приложения — в WitDatabase (`app.witdb` в том же каталоге).

## MCP-сервер IDE: откуда список инструментов

Инструменты собираются в коде: [`Services/IdeMcpToolCatalog.cs`](Services/IdeMcpToolCatalog.cs) и [`Services/IdeMcpToolCatalogFull.cs`](Services/IdeMcpToolCatalogFull.cs); для полей `IdeCommands` дополнительно генерируются прокси-тулы. Человекочитаемая таблица команд — в [docs/MCP-PROTOCOL.md](docs/MCP-PROTOCOL.md); блок между маркерами `GENERATED:IdeCommands` обновляется утилитой [`tools/CascadeIDE.ProtocolDocGen`](tools/CascadeIDE.ProtocolDocGen/Program.cs) (см. комментарий в `CascadeIDE.csproj`). В отличие от отдельных консольных MCP в `Financial/software/open` (цикл `ToolCatalog` → `mcp-tools.manifest.json` и `docs/MCP-TOOLS.md` через `ExportMcpManifest`), у встроенного сервера Cascade IDE отдельного манифеста нет — источник правды в перечисленных сервисах и сгенерированных файлах.

## Подключение как submodule (репо open)

Если этот проект вынесен в отдельный репозиторий на GitLab:

1. Создай пустой проект **cascade-ide** на своём GitLab (например `http://193.124.113.7/Krawler/cascade-ide`).
2. В корне репо **open** выполни:

   ```bash
   git submodule add http://193.124.113.7/Krawler/cascade-ide.git cascade-ide
   git add .gitmodules cascade-ide
   git commit -m "Add cascade-ide as submodule"
   ```

3. Если проект пока только локальный (ещё не в отдельном репо): инициализируй git в `cascade-ide`, добавь remote и запушь первый коммит, затем выполни шаги 1–2.

## Стек

- **Avalonia** 11.3 — UI
- **OllamaSharp** + **Microsoft.Extensions.AI** — клиент Ollama API
- **CommunityToolkit.Mvvm** — MVVM

Установка стека на машину — см. [SETUP.md](SETUP.md).

## Архитектура

Слои, вертикальные срезы фич, роль `MainWindowViewModel` и правила миграции: **[docs/architecture-policy.md](docs/architecture-policy.md)**. Карта срезов (Git, Build, Terminal, Chat…) и фазы выноса: **[docs/architecture-migration.md](docs/architecture-migration.md)**. Каталог **`Features/`** — см. [Features/README.md](Features/README.md).

## Git и submodules (roadmap)

Нативная работа с **Git** и **submodules** — целевое требование для использования IDE как основной среды (в т.ч. без боли, типичной для Visual Studio при submodules). Принципы и критерии приёмки: **[docs/git-and-submodules-v1.md](docs/git-and-submodules-v1.md)**.

## Макеты и раскладка UI

Описание макета главного окна (зоны, панели, режимы Focus/Balanced/Power, контролы для MCP) — в **[docs/ux/](docs/ux/)**:
- [docs/ux/README.md](docs/ux/README.md) — оглавление UX-набора;
- [docs/ux/cascade-ide-ui-layout-v1.md](docs/ux/cascade-ide-ui-layout-v1.md) — раскладка по строкам/колонкам и имена панелей для MCP.
Визуальных макетов (Figma/PNG) в репо нет; эталон — `Views/MainWindow.axaml` и этот документ.
