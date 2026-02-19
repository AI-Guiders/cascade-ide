# AgentIde

Лёгкая IDE для .NET, которой управляет агент через MCP. Стек: .NET 10, Avalonia, интеграция с локальными моделями **Ollama** (в т.ч. Ollama 4.x).

Концепция — из [IDEAS.md](../../../IDEAS.md) (раздел «IDE, которой рулит агент через MCP»): редактор + дерево решения + сборка/запуск + отладка через debug-mcp, анализ через roslyn-mcp, чат/запросы к модели — через локальный Ollama.

## Требования

- .NET 10 SDK
- Windows / Linux / macOS (Avalonia)
- Для чата с моделью: запущенный [Ollama](https://ollama.com) (localhost:11434)

## Сборка и запуск

```bash
cd agent-ide
dotnet build
dotnet run
```

При запуске приложение проверяет доступность Ollama и выводит список локальных моделей (или подсказку установить Ollama).

**Текущее состояние:** запуск, проверка Ollama, список моделей работают; главное окно пока без оформления (дальше: панели, чат, дерево решения).

## Подключение как submodule (репо open)

Если этот проект вынесен в отдельный репозиторий на GitLab:

1. Создай пустой проект **agent-ide** на своём GitLab (например `http://193.124.113.7/Krawler/agent-ide`).
2. В корне репо **open** выполни:
   ```bash
   git submodule add http://193.124.113.7/Krawler/agent-ide.git agent-ide
   git add .gitmodules agent-ide
   git commit -m "Add agent-ide as submodule"
   ```
3. Если проект пока только локальный (ещё не в отдельном репо): инициализируй git в `agent-ide`, добавь remote и запушь первый коммит, затем выполни шаги 1–2.

## Стек

- **Avalonia** 11.3 — UI
- **OllamaSharp** + **Microsoft.Extensions.AI** — клиент Ollama API
- **CommunityToolkit.Mvvm** — MVVM

Установка стека на машину — см. [SETUP.md](SETUP.md).
