# Features/Editor

Вертикальный срез **редактора (Forward)**: субстрат ADR 0103, ортогонально `Cockpit/` и толстой логике `ViewModels/`.

- **`Application/`** — `IEditorSurfaceAdapter`, `AvaloniaEditSurfaceAdapter`, `EditorStabilizedInputThrottler` (hi-freq, один на окно в `MainWindowViewModel`), `EditorHudEngine`, `SemanticProjectionPipeline` / `EditorSemanticSnapshot`, **`EditorDocumentHudLayer`** (per document), DTO `EditorHudStabilizedContext`.
- **`Application/Presentation/`** — `EditorHudBannerTextComposer` (file-level баннер 0085); `EditorInlineHoverToolTipController` (inline hover: диагностика + Quick Info); **`EditorInlineHudLayer`** — фасад/индекс (см. `docs/design/editor-hud-inline-migration-inventory-v1.md`).
- **Норматив:** [docs/adr/0103](../../docs/adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md).

Вёрстка и ad-hoc inline (подсветка, tooltips) пока в `Views/DockDocumentView.*` (strangler); данные баннера и проекция — из этого слоя.
