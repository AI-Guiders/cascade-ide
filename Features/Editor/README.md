# Features/Editor

Вертикальный срез **редактора (Forward)**: субстрат ADR 0103, ортогонально `Cockpit/` и `ViewModels/`.

- **`Application/`** — `IEditorSurfaceAdapter`, `AvaloniaEditSurfaceAdapter`, `EditorStabilizedInputThrottler` (hi-freq bounded, не `IDataBus`; **один экземпляр на главное окно** в `MainWindowViewModel`), `EditorHudEngine` / `SemanticProjectionPipeline` + `EditorSemanticSnapshot`.
- **Норматив:** [docs/adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md](../../docs/adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md).

Пока основная вёрстка остаётся в `Views/DockDocumentView.*`; адаптер и throttle подключаются оттуда (strangler).
