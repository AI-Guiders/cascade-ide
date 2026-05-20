# ADR 0134: Intercom — единый prepare-pipeline сообщений (wire, presentation, commit)

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-20

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | `AttachmentAnchor`, маркеры `⟦a:id⟧`, reveal, degraded outcomes |
| [0129](0129-intercom-message-body-markdown-and-fenced-code.md) | Fenced code в `content` — отдельно от attach |
| [0119](0119-chat-slash-commands-intercom-surface.md) | `IntercomOutboundSendOrchestrator`, фазы send |
| [0123](0123-intercom-full-skia-surface-evolution.md) | Flat feed, Skia-лента |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Event log: `content` + `attachments[]` |
| [0135](0135-intercom-attach-symbol-cache-and-hci-sidecar.md) | L1/L2 кэш member→lines; HCI sidecar после reindex |

## Резюме

Зафиксировать **один пайплайн подготовки** исходящего/входящего в ленту сообщения Intercom:

1. **Prepare** (фон, Roslyn) — parse → resolve по attach → **wire** `content` с `⟦a:id⟧` → **presentation** для Skia.
2. **Commit policy** — единые правила для composer send и `send_chat` (MCP): не коммитить «сырой» текст без маркеров при failed prepare.
3. **Degraded resolve** — один неудачный member не роняет всё сообщение: anchor + marker + `resolveOutcome` (ADR 0128 §9.1).
4. **Тупой рендер** — Skia читает `IntercomFeedPresentation`, hit только на `AttachLink`-сегментах; выделение сообщения не перекрывает ссылки.

**Не цель:** отдельный WPF `BackgroundWorker`; уже достаточно `Task.Run` / `IntercomAttachmentResolveAtSendWorker`.

---

## Проблема (as-is)

| Симптом | Причина |
|---------|---------|
| MCP-сообщение с `[F:… M:…]` без клика | `AppendMessageFromMcpAsync` при `!TryBuild` всё равно пишет **сырой** `content`, `attachments = []` |
| Клик выделяет сообщение | Нет attach-hit: нет маркеров → нет attach-сегмента → только `CreateHit` на всё сообщение |
| Нет подчёркивания | Attach-сегмент не строится; prose идёт через RTK/markdown без `BodyTone.Link` |
| Зависание UI (исправлено ранее) | Синхронный build на UI; вынесено в worker |

Composer send при ошибке build **не** коммитит (orchestrator). MCP — **другая политика**. Skia **повторно** парсит `content`+`attachments` с правилами, отличными от builder.

---

## Решение

### 1. Слои

```text
RawInput
  → Prepare (background)
       Parsed body segments (prose | code)
       Per-attach resolve (strict → degraded)
       Wire: Content + Attachments[]
       Presentation: IntercomFeedPresentation
  → Commit (UI, policy)
  → Project / Render (UI thread, dumb)
  → Reveal @ click (re-resolve получателя, ADR 0128 §8)
```

| Слой | Владелец | Хранится в event log |
|------|----------|----------------------|
| Wire | `ChatMessageViewModel.Content` | да |
| Attachments | `ChatMessageViewModel.Attachments` | да |
| Presentation | вычисляется `IntercomFeedProjector` @ commit/load | нет (детерминирован от wire) |
| Agent envelope | `IntercomAttachmentPromptFormatter` | только prompt, не лента |

### 2. Типы

- `IntercomMessagePrepareStatus`: `Success`, `PartialSuccess`, `Failed`.
- `IntercomMessageCommitPolicy`: `Strict` (composer), `Strict` (MCP inject) — v1 одинаково.
- `PreparedIntercomMessage`: status, warnings, error, `Outbound`, `FeedPresentation`.
- `IntercomFeedSegmentKind`: `Prose`, `Code`, `AttachLink` (+ `AnchorId`, `Stale`).

### 3. Prepare API

```csharp
IntercomOutboundMessagePreparer.PrepareAsync(
    raw, pending, editor, workspace, solution, ct)
  → PreparedIntercomMessage
```

Вызывают:

- `IntercomOutboundSendOrchestrator` (user send),
- `AppendMessageFromMcpAsync` (`send_chat` assistant),
- `IntercomAttachmentResolveAtSendWorker` (тонкая обёртка над preparer).

### 4. Degraded resolve @ send

При неудаче Roslyn для `member` / `syntaxScope`:

- всё равно вставить `⟦a:id⟧` и `AttachmentAnchor` с `file`, `memberKey`, `displayLabel`, `excerpt` (если есть);
- `resolveOutcome` = `member_not_found` (или аналог);
- `lineStart`/`lineEnd` опционально пусты;
- статус prepare = `PartialSuccess`, предупреждение в UI.

Reveal у получателя делает re-resolve (уже `IntercomAttachmentRevealPlanner`).

### 5. Commit policy

| Prepare status | Composer send | MCP `send_chat` assistant |
|----------------|---------------|---------------------------|
| `Failed` | не коммитить, статус ошибки | вернуть ошибку, **не** добавлять в ленту |
| `PartialSuccess` | коммит + предупреждение | коммит + предупреждение |
| `Success` | коммит | коммит |

### 6. Skia / hit-test

- `IntercomFeedProjector.Project(content, attachments)` — единая логика сегментов.
- **`SkiaChatHitRegistry`** — единственный реестр pointer hit на кадр (`_chatHits.Clear()` в начале `DrawSkiaScene`).
- **Приоритет `FindIndex`:** `RevealAttachment` (attach chip, markdown link) → `SkiaChatPointerAction` (chrome: overview, composer, slash) → z-order.
- **`SkiaChatDrawContext.RegisterHit`** → `RegisterContentRect` (content + `scrollOffset`); chrome — `RegisterControlRect` (`SkiaChatHitGeometry`).
- **Диспетчер:** `SkiaChatSurfaceControl.PointerDispatch.cs` — `TryDispatchPointerPress` / `TryDispatchPointerWheel` (не разрозненные `Contains` по полям).
- **Лента:** `SkiaChatMessageFeedEntity.CreateHit` → **null**; узкие hits только в `Draw` (attach chip, markdown link, row select).
- **Markdown link:** `RegisterFeedMarkdownLinkHits` (markdown-lines) или `RegisterFeedMarkdownLinkHitsFromText` (RTK/plain prose).
- **Composer / overview:** `RegisterComposerPointerHits`, `registerChromePointerHits` — `SkiaChatPointerAction`.
- **`CreateHit` на entity** — только цельные плитки (topic card, slash block), не feed-сообщения.
- `ParseInline`: при `[` без code-ref — продвинуть `i`, не зацикливаться.

### 7. Reveal

Клик из ленты → `IntercomAttachmentNavigator.Apply` (без MCP roundtrip с UI-потока) — ADR 0130 / уже в коде.

---

## Фазы внедрения (этот ADR)

| Шаг | Содержание |
|-----|------------|
| 1 | ADR 0134 + типы + `IntercomFeedProjector` |
| 2 | `TryPrepare` + degraded resolve |
| 3 | `IntercomOutboundMessagePreparer`, единый commit |
| 4 | Orchestrator + MCP append |
| 5 | Skia presentation + hit (chip: рамка/статус, без underline) |

---

## Последствия

- Старые сообщения в логе без маркеров: projector fallback только если есть `attachments[]`; иначе prose как текст (без клика).
- Persistence schema не меняется (presentation не в event log).
- Тесты: builder partial/degraded, projector attach segment, MCP append fail-closed.

## Критерии приёмки

1. `send_chat` с `[F:path M:Member]` при открытом solution → в ленте `[Member]` с подчёркиванием, клик → reveal.
2. При полном fail prepare MCP возвращает ошибку, в ленте нет сырой bracket-строки без маркера.
3. Composer send при fail prepare не добавляет сообщение (как раньше).
4. Shift+клик по attach → select в редакторе.
