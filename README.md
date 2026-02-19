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
- **Редактор:** центральная панель с текстом (пока TextBox; планируется AvaloniaEdit + подсветка C#).
- **Чат с моделью:** правая панель — выбор модели Ollama, история сообщений, стриминг ответов.
- **Дерево решения:** левая панель — открытие .slnx/.sln (кнопка «Открыть решение»), проекты в виде дерева; клик по узлу открывает файл в редакторе.
- **Нативная поддержка slnx:** парсер `SolutionParser` загружает .slnx (XML) и .sln (текстовый формат).
- **MCP для модели:** сервис `McpClientService` (пока заглушка) — вызовы debug-mcp и RoslynMCP для отладки и рефакторинга; модель сможет использовать их при интеграции tool calling.
- **MCP сервер IDE:** `IdeMcpServer` — тулы `ide_open_file`, `ide_set_breakpoint`, `ide_show_preview`, `ide_request_confirmation`; агент/модель может управлять IDE через этот MCP (подключение: stdio при запуске в режиме MCP или будущий TCP).

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
