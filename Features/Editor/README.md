# Features/Editor

Вертикальный срез **редактора (Forward)**: субстрат ADR 0103, ортогонально `Cockpit/` и толстой логике `ViewModels/`.

- **`Application/`** — `IEditorSurfaceAdapter`, `AvaloniaEditSurfaceAdapter`, `EditorStabilizedInputThrottler` (hi-freq, один на окно в `MainWindowViewModel`), `EditorHudEngine`, `SemanticProjectionPipeline` / `EditorSemanticSnapshot`, **`EditorDocumentHudLayer`** (per document), DTO `EditorHudStabilizedContext`.
- **`Application/Presentation/`** — `EditorHudBannerTextComposer` (текст **HUD banner** 0085, не inline); **`EditorInlineHudLayer`** — пустой якорь под будущий inline Editor HUD.
- **Норматив:** [docs/adr/0103](../../docs/adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md).

Вёрстка и ad-hoc inline (подсветка, tooltips) пока в `Views/DockDocumentView.*` (strangler); данные баннера и проекция — из этого слоя.
