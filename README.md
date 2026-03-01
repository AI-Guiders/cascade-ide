# CascadeIDE

Лёгкая IDE для .NET, которой управляет агент через MCP. Стек: .NET 10, Avalonia, интеграция с локальными моделями **Ollama** (в т.ч. Ollama 4.x).

Концепция — из [IDEAS.md](../../../IDEAS.md) (раздел «IDE, которой рулит агент через MCP»): редактор + дерево решения + сборка/запуск + отладка через debug-mcp, анализ через roslyn-mcp, чат/запросы к модели — через локальный Ollama.

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
- **Отладочный контур:** поддержаны отображение брейкпоинтов/текущей строки/состояния отладки и синхронизация с `dotnet-debug-mcp`.
- **Настройки и данные:** выбранные модели и UI-параметры сохраняются в `%LocalAppData%\\CascadeIDE\\settings.toml`; данные приложения — в WitDatabase (`app.witdb` в том же каталоге).

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
