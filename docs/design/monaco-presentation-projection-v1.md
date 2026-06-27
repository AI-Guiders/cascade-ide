# Monaco presentation projection (v1) — companion

**Статус:** companion к канону  
**Дата:** 2026-06-24  
**Канон:** [ADR 0164](../adr/0164-monaco-editor-presentation-projection-and-dock-chrome.md)

Краткая шпаргалка по push-DTO и файлам. Полный контекст, альтернативы и последствия — в ADR.

## Три слоя

| Слой | Владелец |
|------|----------|
| Substrate | `WorkspaceDiagnosticsCoordinator`, LSP hosts |
| Presentation projection (C#) | `MonacoEditorPresentationProjector`, `MonacoEditor*Mapper` |
| Thin host (JS) | `decoration-layer-manager.js`, `cide-editor-bridge.js` |

## Контракт push-DTO

- **Whole-line decorations:** `startLine` / `endLine` (1-based); offset не используется.
- **Token spans:** `startOffset` + `length`.
- **Inlay:** `atEndOfLine: true` → column только в JS (`getLineMaxColumn`).
- **Version:** `expectedModelVersion` на `setDecorations` / `setInlayHints`.

## Политика inline (0085)

`MonacoEditorPresentationProjector.MergeInlayHints`: без var-inlay на строках с диагностикой.

## Файлы

| Файл | Роль |
|------|------|
| `MonacoEditorPresentationProjector.cs` | Единая точка HUD push |
| `MonacoEditorDiagnosticsMapper.cs` | strips → line decorations + EOL inlays |
| `decoration-layer-manager.js` | CECB decoration layers |
| `DocumentsWorkspaceViewModel.cs` | `BindDockChrome` / `UnbindDockChrome` |
