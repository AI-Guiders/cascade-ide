# Features — вертикальные срезы UI

Соответствует [docs/architecture-policy.md](../docs/architecture-policy.md): каждая **крупная панель** или самостоятельный блок — отдельный `*ViewModel` (и при необходимости модели) в подпапке `Features/<Имя>/`.

**Примеры:**

- **`Git/`** — вкладка Git нижней панели: `GitPanelViewModel.cs`.
- **`Build/`** — вкладка Build output: `BuildOutputPanelViewModel.cs`.
- **`Terminal/`** — вкладка Terminal: `TerminalPanelViewModel.cs`.
- **`Chat/`** — правая колонка: чат с LLM — `ChatPanelViewModel.cs`.
- **`Instrumentation/`** — трасса агента, таймлайн событий, очередь задач Power, лог тестов, стек/переменные MCP-отладки — `InstrumentationPanelViewModel.cs`.

Корневой неймспейс: `CascadeIDE.Features.<Имя>`.

## `UiChrome` и цепочка ADR 0036

Норматив: [ADR 0036](../docs/adr/0036-cds-channel-compositor-surface-pipeline.md) (канал → CDS → композитор → поверхность). Реализация слоёв вынесена в **`Cockpit/`** (неймспейсы `CascadeIDE.Cockpit.*`), чтобы не смешивать с пресетами/режимами (`UiMode*`, capabilities) в `Features/UiChrome/`.

| Слой | Папка / неймспейс | Примеры типов |
|------|-------------------|----------------|
| Канал (п.1) | `Cockpit/Channels/**` | `WorkspaceHealthInputSnapshot`, `WorkspaceHealthProvider`, `IEicasFeed` |
| CDS (п.2) | `Cockpit/Cds/` (`CascadeIDE.Cockpit.Cds`) | `CockpitSurfaceState`, `CockpitSurfaceSnapshotBuilder`, `AttentionLayoutSurfaceKind` |
| Композитор (п.3) | `Cockpit/Composition/**` | `WorkspaceHealthSegmentBuilder`, `EicasMessageSorter` |
| Поверхность (п.4) | `Views/*`, `Cockpit/Surface/UiLayoutSnapshot` | дерево контролов, MCP `ide_get_ui_layout` |
