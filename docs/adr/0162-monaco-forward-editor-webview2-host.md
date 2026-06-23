# ADR 0162: Monaco как опциональный хост Forward-редактора (WebView2, не Photino-shell)

**Статус:** Proposed  
**Дата:** 2026-06-20  
**Обновляет:** [0103 §2.3](0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md#adr0103-web-vs-native-forward) — Monaco перестаёт быть «молчаливо отклонённым» baseline; становится **явно разрешённой опциональной линией** при включении в конфиге.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0103](0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) | `IEditorSurfaceAdapter`, Editor HUD substrate, hi-freq bounded-контур |
| [0108](0108-web-ai-portal-host-object-tools-bridge.md) | WebView2 в процессе IDE, Host Object / `WebMessageReceived` — транспортный прецедент |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Граница доверия веб ≠ MCP; **редактор Forward** — controlled origin, не чужой HTTPS |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | Буфер редактора — источник применяемой правды; versioned apply |
| [0085](0085-editor-hud-inline-layer-and-hud-banner.md) | Inline vs HUD banner |
| [0009](0009-strangler-migration-and-exceptions.md) | Dual-path: AvaloniaEdit остаётся до parity |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Forward / primary work surface |
| [0152](0152-editor-control-flow-virtual-spacing.md) | Virtual spacing CF — перенос на Monaco decorations / gutter lane |

### Вне ADR

| Документ | Роль |
|----------|------|
| [editor-surface-candidates-comparison-v1](../design/editor-surface-candidates-comparison-v1.md) | Сравнение хостов (аппендикс) |
| [editor-hud-inline-migration-inventory-v1](../design/editor-hud-inline-migration-inventory-v1.md) | Inventory inline-рендера для strangler |
| [editor-forward-ui-cleanup-roadmap-v1](../ui-ux/editor-forward-ui-cleanup-roadmap-v1.md) | Roadmap полировки Forward |

---

<a id="adr0162-context"></a>

## 1. Контекст

**Продуктовая боль:** Avalonia как **фюзеляж** приемлема (окна, dock, focus, OS integration), но **текстовый UX Forward** на **AvaloniaEdit** систематически слабее зрелых web-редакторов:

- рендеринг текста, caret, selection, scroll;
- IntelliSense / completion / hover / Quick Info — кастомный chrome, визуальный и поведенческий разрыв с VS Code-подобным опытом;
- inline HUD (squiggles, inlays, ghost) — дорогая борьба с `IBackgroundRenderer` и adorners в `DockDocumentView`.

[0103](0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) уже ввёл **`IEditorSurfaceAdapter`** и зафиксировал AvaloniaEdit как baseline; Monaco/WebView2 — «вне спайка v1». Субстрат HUD (`SemanticProjectionPipeline`, `EditorHudEngine`, DAL/CCU) **не привязан** к AvaloniaEdit — второй адаптер архитектурно ожидаем.

**Внешние рекомендации (Photino / «лёгкий Electron»):** часто смешивают три разных тезиса:

1. OS WebView легче упаковки Electron — **верно**;
2. «C# и JS в одном address space», «zero-latency IPC» — **неверно** для WebView2 (renderer out-of-process);
3. «30–40 МБ на IDE» — **нереалистично** для Monaco + нескольких поверхностей; память уходит в процессы Edge/WebView2, а не исчезает.

**Photino-shell** (весь UI в web) **не** требуется, чтобы снять боль редактора: достаточно **Monaco-island** в уже принятом **WebView2** (`Avalonia.Controls.WebView` / `NativeWebView`, см. [0108](0108-web-ai-portal-host-object-tools-bridge.md)).

---

<a id="adr0162-decision"></a>

## 2. Решение

### 2.1 Направление

1. Ввести **`MonacoWebViewSurfaceAdapter`** — реализация **`IEditorSurfaceAdapter`** поверх **Monaco Editor** во **встроенном WebView2** в слоте `DockDocumentView` (Forward), **без** миграции всего CIDE на Photino.

2. **Avalonia остаётся фюзеляжом:** dock, окна, terminal, Skia-острова (Intercom, cockpit), MFD — без изменения [0044](0044-avalonia-host-skia-agent-chat-surface.md) / [0123](0123-intercom-full-skia-surface-evolution.md).

3. **Strangler dual-path** ([0009](0009-strangler-migration-and-exceptions.md)): конфиг **`[editor].forward_host`** = `avalonia_edit` (default) \| `monaco_webview2`. Переключатель на уровне workspace или глобально в `settings.toml`; без silent fallback между хостами на одном документе.

4. **LSP, Roslyn, DAL, agent MCP** — **без изменения контрактов**; только транспорт текста/offsets/decorations через адаптер.

### 2.2 Что **не** делаем

- **Полный Photino-shell** (все панели в HTML) — отдельная линия, не блокер и не замена этого ADR.
- **Electron** — не рассматриваем (bundled Chromium).
- **WebView для forge write path** — по-прежнему [FORGE ADR-0002](https://github.com/AI-Guiders/agent-forge/blob/main/design/FORGE-ADR-0002-mcp-first-web-view-only.md) / CIDE native; Monaco — **редактор кода**, не forge SPA.
- **Monaco как единственный default** — только после явного sign-off parity (фазы ниже).

### 2.3 Инварианты (не ломать)

- [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md): канонический текст для apply — **версионированный буфер**; Monaco не «тихий master» без sync protocol.
- [0085](0085-editor-hud-inline-layer-and-hud-banner.md): inline и banner — разные слои; маппинг на Monaco `deltaDecorations` / content widgets / overlay, не один DOM-хак.
- [0103 §2.2](0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md#adr0103-layering-table): hi-freq caret/pointer — **bounded channel + throttle**, не DataBus на каждую клавишу.
- [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md): origin редактора — **`cide-editor://` / bundled asset**, не произвольный HTTPS; Host Object allowlist **отдельный** от Web AI Portal ([0108](0108-web-ai-portal-host-object-tools-bridge.md)).

---

<a id="adr0162-ipc"></a>

## 3. IPC и память (честные ожидания)

### 3.1 WebView2 — не «zero latency»

| Утверждение | Факт |
|-------------|------|
| C# и страница в одном процессе | **Нет:** renderer WebView2 — **out-of-process** (Edge/Chromium) |
| IPC быстрее localhost HTTP | **Да:** `PostWebMessage` / Host Object |
| IPC быстрее in-process AvaloniaEdit | **Нет:** async marshaling; hi-freq path **coalesce** (16–50 ms throttle уже в 0103) |
| Photino даёт иной IPC, чем Avalonia WebView | **Нет** на Windows: тот же WebView2 под капотом |

### 3.2 Память

- Пустое окно Photino/WebView — десятки МБ; **IDE + Monaco + language services + N webviews** — **сотни МБ** суммарно по процессам Edge — норма.
- Выигрыш vs Electron — **не тащим свой Chromium**; vs AvaloniaEdit-only — **дополнительный** renderer process на Forward-документ.

---

<a id="adr0162-bridge"></a>

## 4. Контракт `cide-editor` (мост C# ↔ Monaco)

**Namespace сообщений:** `cide-editor/*` (ортогонально `executeIdeCommand` из [0108](0108-web-ai-portal-host-object-tools-bridge.md)).

**Транспорт:** `WebMessageReceived` + опционально Host Object для sync read (caret snapshot); JSON camelCase.

**Минимальный набор (v0):**

| Направление | Message | Назначение |
|-------------|---------|------------|
| C# → JS | `editor/setModel` | uri, languageId, text, version |
| C# → JS | `editor/applyEdits` | edits[], expectedVersion |
| C# → JS | `editor/setDecorations` | decoration sets (diagnostics, agent range, CF gutter) |
| C# → JS | `editor/setTheme` | tokens из UI theme pipeline [0086](0086-ui-theme-toml-canonical-json-mcp-wire.md) |
| JS → C# | `editor/didChange` | contentChanges[], version |
| JS → C# | `editor/didChangeCursorSelection` | offsets, reason |
| JS → C# | `editor/ready` | handshake после load |

**Source of truth (0084):** v0 — **C# master**: push `setModel` / `applyEdits` после локальных и agent mutations; JS шлёт deltas, C# мержит и инкрементирует version. Инверсия (Monaco master) — только с отдельным ADR и тестами race.

**Assets:** bundled static (`Assets/editor/` или `wwwroot/cide-editor/`), загрузка через `WebView2` virtual host или `file://` под контролируемым path — детали реализации, origin **не** third-party.

---

<a id="adr0162-phases"></a>

## 5. Фазы внедрения (strangler)

| Фаза | Объём | Критерий |
|------|--------|----------|
| **M0** | Monaco в WebView: open/edit file, caret events | Лог + ручной smoke |
| **M1** | `MonacoWebViewSurfaceAdapter` + `[editor].forward_host` | Переключение host без падения dock |
| **M2** | Diagnostics → decorations из `EditorHudEngine` snapshot | Parity squiggles с AvaloniaEdit на одном файле |
| **M3** | Inline inventory → Monaco (hover, inlay, agent reveal [0130](0130-editor-agent-range-reveal-without-selection.md)) | Item-by-item из migration inventory |
| **M4** | Agent applyEdits + 0084 integration tests | Version mismatch → reject, не silent corrupt |
| **M5** | CF virtual spacing [0152](0152-editor-control-flow-virtual-spacing.md) на Monaco gutter lane | Визуальный sign-off |
| **M6** | Default flip to `monaco_webview2` (optional) | Product sign-off; ADR status → Accepted · Implemented |

**Dual-path до M6:** AvaloniaEdit не удаляем; regression на `avalonia_edit` для CI matrix optional.

---

<a id="adr0162-consequences"></a>

## 6. Последствия

### Положительные

- Текст, selection, completion UI, diff editor — зрелый API Monaco; снимает класс боли AvaloniaEdit без смены фюзеляжа.
- Переиспользование WebView2-инфраструктуры и паттернов [0108](0108-web-ai-portal-host-object-tools-bridge.md).
- `IEditorSurfaceAdapter` и Editor HUD substrate получают **реальную вторую реализацию** — проверка абстракции.

### Отрицательные / trade-offs

- Две темы (Avalonia + Monaco CSS) — генератор из канона [0086](0086-ui-theme-toml-canonical-json-mcp-wire.md), не ручной drift.
- IPC-latency на hi-freq path — дисциплина throttle; возможен micro-jank vs native.
- `DockDocumentView` strangler: AvaloniaEdit-specific renderers отмирают **по inventory**, не одним PR.
- Windows-first (текущий `win-x64` RID); Linux/macOS WebView — отдельный roadmap ([0093](0093-mfd-embedded-browser-for-launch-url.md)), не блокер Proposed.

---

<a id="adr0162-alternatives"></a>

## 7. Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| **Photino-shell** для всего CIDE | Снимает не ту боль; переписывает dock/terminal/Skia; тот же WebView2 IPC |
| **Electron** | Bundled Chromium + Node; против целей lean desktop |
| **Оставить только AvaloniaEdit** | Принято как **default до M6**; не отменяет optional Monaco |
| **Monaco в MFD, не Forward** | Не заменяет primary editor; только preview |
| **CodeMirror 6** вместо Monaco | Допустимый spike; ADR фиксирует Monaco как первый кандидат (LSP ecosystem, diff, VS Code parity) |

---

<a id="adr0162-config"></a>

## 8. Конфиг (sketch)

```toml
[editor]
# avalonia_edit | monaco_webview2
forward_host = "avalonia_edit"
```

Per-workspace override — через существующий stack `settings.toml` / workspace profile (детали при M1).

---

<a id="adr0162-implementation"></a>

## 9. Ownership (sketch)

| Компонент | Расположение |
|-----------|--------------|
| `MonacoWebViewSurfaceAdapter` | `Features/Editor/Application/` |
| `cide-editor` bridge (TS) | `Assets/cide-editor/` или `wwwroot/cide-editor/` |
| WebView host control | `Views/` или `Features/Editor/Presentation/` |
| Theme token export | рядом с [0086](0086-ui-theme-toml-canonical-json-mcp-wire.md) pipeline |

---

<a id="adr0162-history"></a>

## История изменений

| Дата | Изменение |
|------|-----------|
| 2026-06-20 | Proposed: Monaco Forward host, WebView2 island, anti-hype IPC/memory, strangler phases |
| 2026-06-21 | Уточнение: ветка `feature/monaco-forward-editor-m0` убрала Avalonia `TextEditor` из dock; полный перевод и bus — [0163](0163-monaco-native-capability-bus-full-forward-migration.md) |
