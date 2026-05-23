# Playbook: TCI (Text-Command Interface) v1

**TCI** — семейство Skia-полос ввода со slash-иерархией, popup и отдельным буфером в VM. Не путать с MFD Terminal (OS/process).

## Экземпляры

| Экземпляр | Буфер VM | Enter | Strip |
|-----------|----------|-------|-------|
| Composer | `ChatInput` / `ComposerText` | send агенту | `SkiaComposerStrip` |
| CCL (Cockpit Command Line) | `CockpitCommandLineText` | execute slash | `SkiaCommandLineStrip` |
| Navigator search | `TopicNavigatorSearchQuery` | фильтр тем | однострочный hit-test |
| Chord overlay | короткий ввод | handoff | отдельный UX |

## Общий слой

- **`SkiaPlainTextLayout`** — RichTextKit: measure, caret, hit-test.
- **`SkiaTciTextField`** — selection, horizontal scroll (CCL), caret draw, merge preedit.

Синхронизация VM:

1. Control держит текст и caret (`ComposerText`, `CommandLineText`, …).
2. События `ComposerDraftChanged` / `CommandLineDraftChanged` — **caret до текста**, затем autocomplete.
3. Avalonia binding догоняет VM; не полагаться на порядок `PropertyChanged` от setter VM.

## CCL (ADR 0138 фаза A)

- Mono: `Cascadia Mono` через RichTextKit (`SkiaCommandLineStrip.MonoFontFamily`).
- Click-to-caret, selection (Shift+стрелки), Ctrl+A/C/X/V, Home/End.
- Горизонтальный scroll колёсиком при длинной команде.
- Slash-validation — **pill вокруг команды** (как attach-chip / CodeAnchors): примитив `SkiaStatusChip` + обёртка `SkiaSlashCommandChip`; severity из `SlashCommandPreviewKind`; отдельной строки под полем нет.
- Высота полосы масштабируется с `command_line_pt` (defaults 15pt): строка ввода ≈ `fontSize × 22/12`, preview ≈ `previewPt × 16/10`; не фиксированные 22px (иначе при 15pt режется снизу).
- IME: `IntercomSkiaTextInputClient` в режиме `IsCommandLineInputActive` (без preedit).

### Preview severity (не канал EICAS)

Визуальная валидация slash — **отдельный контур** от W/C/A в [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md): не попадает в `EicasAlertsBar`/CAS, Enter не блокируется (как chip attach, ADR 0128 §9.1). Рисование: `SkiaStatusChip` (примитив) → `SkiaSlashCommandChip` / `SkiaSlashPreviewChrome`. Контракт глифов ✓ / ✕ / **P(n)** — [ADR 0140](../adr/0140-tci-slash-status-glyphs-and-args-counter.md).

**Иконка слева в pill:** рисуется **левее** начала slash-текста на `SkiaStatusChip.IconLeadingOverhang`; clip поля ввода расширяется влево, иначе глиф обрезается.

**Палитру** берём из кокпита (красный / янтарь / зелёный / серый), **семантику** — свою:

| TCI (`SlashCommandPreviewKind`) | Глиф (целевой) | Смысл | Цвет |
|---------------------------------------|----------------|-------|------|
| `Ok` (**Ready**) | ✓ | команда + args готовы | зелёный |
| `Incomplete` | **P** / **P(n)** (сейчас ⚠) | не хватает args, id допечатывается | янтарь |
| `Error` (**Invalid**) | ✕ | нет команды, опечатка, синтаксис | красный |
| `Hint` | ℹ | мягкая подсказка (редко) | серый |

Примеры: `/intercom test` → `Error` (✕); `/intercom message select` → `Incomplete` (P); `… select 5 7` → `Ok` (✓); `/intercom mesage select 5?7` → `Error` (✕).

**Слой:** `SlashCommandPreviewService` → `SlashCommandPreviewRulePipeline` (правила); маппинг severity — **только** `SlashCommandPreviewVisualMapper` → `SkiaSlashPreviewChrome.ToChipSeverity`; chrome — `SkiaStatusChip`. В UI — pill, не вторая строка.

**A11y (P1):** при `Error` / `Incomplete` / `Hint` полный текст — `IntercomSlashPreviewToolTip` на `IntercomSkiaSurface` (`ToolTip.Tip`), плюс поля `CommandLineSlashPreview` / `ComposerSlashPreview` в VM. Сессия CCL: `ICockpitCommandLineSession.PreviewAccessibilityToolTip`.

### SkiaStatusChip (общий примитив)

`Views/SkiaKit/SkiaStatusChip.cs` — скруглённая рамка, заливка, иконка слева; палитра `SkiaStatusChipColors` (Border, Fill, Icon, Accent). Используют: `SkiaSlashCommandChip`, `SkiaIntercomAttachLinkChip` (лента). API: `ComputeRectAroundTextStart`, `DrawFrame`, `DrawChrome`, `ResolveColors(theme, severity)`.

**Composer:** `EvaluateComposerAtCaret` (линия по каретке); VM: `ComposerSlashPreview` / `ComposerSlashPreviewKind`. **CCL:** `Evaluate(buffer)`; VM: `CommandLineSlashPreview` / `CommandLineSlashPreviewKind`. При открытом CCL composer-preview скрывается.

## Composer

См. `playbook-skia-text-surface-v1.md`: многострочный scroll по Y, preedit, send.

## Чеклист при новом TCI-поле

1. Strip на `SkiaPlainTextLayout` + при необходимости `SkiaTciTextField`.
2. `*DraftChanged` с caret-before-text.
3. Hit-test и caret rect для IME (`Get*CaretScreenRect`).
4. Chrome-only invalidate при blink (`InvalidateComposerChrome`).
5. Тесты hit-test/caret в `CascadeIDE.Tests`.

## Связанные ADR

- 0119 — slash commands Intercom
- 0123 — full Skia surface
- 0138 — CCL и parametric ranges (фаза B — preview ranges)
