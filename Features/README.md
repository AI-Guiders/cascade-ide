# Features — вертикальные срезы UI

Соответствует [docs/architecture-policy.md](../docs/architecture-policy.md): каждая **крупная панель** или самостоятельный блок — отдельный `*ViewModel` (и при необходимости модели) в подпапке `Features/<Имя>/`.

**Примеры:**

- **`Git/`** — вкладка Git нижней панели: `GitPanelViewModel.cs`.
- `Build/`, `Terminal/`, `Chat/` — см. [docs/architecture-migration.md](../docs/architecture-migration.md).

Корневой неймспейс: `CascadeIDE.Features.<Имя>`.
