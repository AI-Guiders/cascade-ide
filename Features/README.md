# Features — вертикальные срезы UI

Соответствует [docs/architecture-policy.md](../docs/architecture-policy.md): каждая **крупная панель** или самостоятельный блок — отдельный `*ViewModel` (и при необходимости модели) в подпапке `Features/<Имя>/`.

**Примеры:**

- **`Git/`** — вкладка Git нижней панели: `GitPanelViewModel.cs`.
- **`Build/`** — вкладка Build output: `BuildOutputPanelViewModel.cs`.
- **`Terminal/`** — вкладка Terminal: `TerminalPanelViewModel.cs`.
- **`Chat/`** — правая колонка: чат с LLM — `ChatPanelViewModel.cs`.
- **`Instrumentation/`** — трасса агента, таймлайн событий, очередь задач Power, лог тестов, стек/переменные MCP-отладки — `InstrumentationPanelViewModel.cs`.

Корневой неймспейс: `CascadeIDE.Features.<Имя>`.
