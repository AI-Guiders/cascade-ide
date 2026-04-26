# Editor HUD (inline) — инвентаризация и граница strangler (v1)

**Статус:** рабочий чек-лист  
**Дата:** 2026-04-26  
**Связь:** [ADR 0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md), [0085](../adr/0085-editor-hud-inline-layer-and-hud-banner.md), [ux roadmap editor-forward](../ux/editor-forward-ui-cleanup-roadmap-v1.md), [banner vs inline policy](editor-hud-banner-inline-policy-v1.md)

**Цель:** зафиксировать, **что сейчас** живёт в `Views/DockDocumentView` и **куда** по намерению ADR 0103 / 0085 должны переезжать **модель презентации** (не DAL) и **тонкий рендер** — без дублирования LSP-ввода-вывода в VM.

---

## Уже вынесено (0103, v1)

| Область | Где | Примечание |
|--------|-----|------------|
| Стабилизированный ввод | `EditorStabilizedInputThrottler` + `EditorInputDelta` | Hi-freq не в DataBus; guard активного файла в VM |
| Адаптер поверхности | `IEditorSurfaceAdapter` / `AvaloniaEditSurfaceAdapter` | Координаты, selection, `CaretOffset` |
| File-level HUD banner (данные + текст) | `EditorDocumentHudLayer`, `EditorHudStabilizedContext`, `EditorHudBannerTextComposer` | VM оркестрирует баннер; DAL-полосы через `GetStripsForFile` |
| Семантическая свёртка для баннера | `SemanticProjectionPipeline` / `EditorSemanticSnapshot` | Не путать с полным solution graph (см. 0098) |

---

## `DockDocumentView` — оставшийся inline / hover (по коду)

| Блок | Файл / API | Смысл 0085 | Целевой владелец (strangler) |
|------|------------|------------|------------------------------|
| Squiggles (волны под текстом) | `EditorDiagnosticBackgroundRenderer` + `InstallVisualAdornersOnce` | Editor HUD inline | `Features/Editor` — **фасад установки** рендереров; сам рендерер сейчас `Services/EditorDiagnosticAdorner.cs` (можно позже co-locate с inline-слоем) |
| Tooltip: диагностика hit-test | `WorkspaceDiagnosticsCoordinator.HitTestForToolTip` + `ToolTip` на `TextEditor` | inline hover | `EditorInlineHoverToolTipController` (Presentation) — **логика** debounce/seq; View только подключает pointer |
| Tooltip: Quick Info (Roslyn / async) | `GetEditorQuickInfoAsync` + fallback `CSharpLanguage.GetQuickInfo` | Quick Info = Editor HUD | Те же колбэки в контроллер; **не** тащить VM в `Features/Editor` — только `Func<…>` / интерфейс с оркестрацией снаружи |
| Debug / breakpoints визуально | `BreakpointLineBackgroundRenderer`, `DebugCurrentLineBackgroundRenderer`, `DebugInstructionArrowBackgroundRenderer` в `Services/EditorDocumentDebugLineBackgroundRenderers.cs` | Не LSP-диагностики; документные адорнеры | Регистрация через `EditorDocumentBackgroundVisualsHandle` / `EditorInlineHudLayer.InstallDocumentBackgroundVisuals` (не в `MainWindow` code-behind) |
| ToolTip layout (placement, delay, offsets) | `EditorInlineHoverChrome.ApplyToolTipServiceTo` в `TrySetup` (константы в `EditorInlineHoverChrome`) | Презентация, не [0079](../adr/0079-ide-display-system-ids-overlay-pipeline.md) | Один источник чисел; **IDS** — глобальные оверлеи, это — якорь к `TextEditor`. При появлении **глобального** реестра таймингов тултипов — согласовать цифры, не меняя контур. |
| TextMate, selection chrome | `MainWindow.EnsureTextMate*`, `EditorSelectionChrome` | Подсветка синтаксиса, не HUD | Вне **inline HUD**-миграции (отдельная тема surface) |
| Провайдеры MCP / состояние редактора | `SetEditorStateProvider`, `SetApplyEdit`, … | Интеграция агента, не визуальный HUD | Остаётся в VM + View wiring |

---

## Принципы границы (0103)

1. **DAL** (`Features/*/DataAcquisition`, LSP hosts) **не** перемещать в `DockDocumentView` — только **снимки/полосы** (`EditorDiagnosticStrip`, и т.д.) на границе.
2. **Стабилизированный** канал — для **сводок и баннера**; **hover** — **прореженный** (debounce), не поток `TextInput` в DataBus.
3. **Рендер** AvaloniaEdit (`IBackgroundRenderer`, `ToolTip`) остаётся **тонко** в View; **политика** «что показать» — в `Features/Editor/.../Presentation`.

---

## Следующие шаги (логичный порядок)

1. ~~Вынести тултип-контроллер из code-behind~~ — `EditorInlineHoverToolTipController`.
2. ~~Тонкий фасад для `IBackgroundRenderer` (диагностики + брейкпоинты + отладка)~~ — `EditorDocumentBackgroundVisualsHandle` + `EditorInlineHudLayer.InstallDocumentBackgroundVisuals` (`Services/EditorDocumentDebugLineBackgroundRenderers.cs` переносит бывший `MainWindow.LineRenderers`).
3. **Политика 0085** + токены MFD/Forward — [editor-hud-banner-inline-policy-v1](editor-hud-banner-inline-policy-v1.md) и [roadmap](../ux/editor-forward-ui-cleanup-roadmap-v1.md) §4–6.

---

## Успех

Можно прочитать `DockDocumentView.axaml.cs` и **по этой таблице** понять, что относится к **inline Editor HUD** vs отладка vs баннер — без «всё в одной куче».
