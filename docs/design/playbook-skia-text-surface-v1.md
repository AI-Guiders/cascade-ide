# Playbook: Skia text surface (composer) v1

**Связь:** [ADR 0123](../adr/0123-intercom-full-skia-surface-evolution.md), `Views/SkiaKit/SkiaPlainTextLayout.cs`, `SkiaComposerStrip.cs`.

## Инварианты (PR-review)

1. **Measure = Draw = Caret = Hit-test** — только `SkiaPlainTextLayout` / `SkiaRichTextKitMarkdown.TryMeasurePlain`; не добавлять отдельный `WrapLines` для UI.
2. **Истина текста в control** — `ComposerText` + `ComposerCaretIndex` (+ `ComposerPreeditText`); VM синхронизируется через `ComposerDraftChanged`, каретка **до** `ChatInput`.
3. **Preedit в measure** — `SkiaIntercomCommandDeckLayout` и `MeasureHeight` получают `composerPreeditText`.
4. **Invalidation** — мигание каретки: `InvalidateComposerChrome()` (кэш ленты); полный `InvalidateVisual()` при смене snapshot/текста ленты.
5. **Pointer** — клик в composer → `TryPlaceComposerCaretAtPoint`; wheel при переполнении → `TryScrollComposer`.

## Чеклист новой фичи ввода

- [ ] Shift+стрелки / выделение / Ctrl+A,C,X,V
- [ ] IME: `SetPreeditText`, высота deck с preedit
- [ ] Headless-тест на caret/hit-test (см. `SkiaPlainTextLayoutTests`)
- [ ] Automation: `SkiaComposerAutomationPeer` Value = `ComposerText`

## Известные границы (v1)

- Clarification batch остаётся на Avalonia `TextBox` (ADR 0123 фаза 4).
- CCL — упрощённый caret (`SkiaCommandLineStrip`), без полного parity composer.
