# Features/Editor

Вертикальный срез **редактора (Forward)**: субстрат ADR 0103, ортогонально `Cockpit/` и толстой логике `ViewModels/`.

- **`Application/`** — `IEditorSurfaceAdapter`, `AvaloniaEditSurfaceAdapter`, `MonacoWebViewSurfaceAdapter` + `Monaco/` bridge (ADR 0162), `EditorStabilizedInputThrottler` (hi-freq, один на окно в `MainWindowViewModel`), `EditorHudEngine`, `SemanticProjectionPipeline` / `EditorSemanticSnapshot`, **`EditorDocumentHudLayer`** (per document), DTO `EditorHudStabilizedContext`.
- **`Application/Presentation/`** — `MonacoEditorHostControl` (WebView2 + Monaco, ADR 0162 M0–M2).
- **`Application/Presentation/`** — `EditorHudBannerTextComposer` (file-level баннер 0085); `EditorInlineHoverToolTipController` (inline hover + `DismissToolTip`/Escape); `EditorForwardDocumentChrome` (padding); **`EditorInlineHoverChrome`** (тайминги/смещения `ToolTip` на `TextEditor`, не IDS/0079); `EditorDocumentBackgroundVisualsHandle` (регистрация `IBackgroundRenderer`: squiggles, inlay, debug из `Services/`); **семантическая хрома** squiggle/MFD — `EditorHudDiagnosticsChroma` в `Services/`. **`EditorInlineHudLayer.InstallDocumentBackgroundVisuals`** — фасад (см. `docs/design/editor-hud-inline-migration-inventory-v1.md`).
- **Презентация (данные) Editor HUD inline:** DTO `EditorDiagnosticStrip` / `EditorTrailingInlayPart` в `Services/`; хрома — `EditorHudDiagnosticsChroma`, рендер squiggle/inlay — `Services/Editor*.cs`.
- **Норматив:** [docs/adr/0103](../../docs/adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md).

Вёрстка и ad-hoc inline (подсветка, tooltips) пока в `Views/DockDocumentView.*` (strangler); данные баннера и проекция — из этого слоя.
