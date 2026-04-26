# Сравнение кандидатов на поверхность редактора (v1)

**Статус:** чертеж-компаньон к [ADR 0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md)  
**Дата:** 2026-04-26

**Связь:** [0085](../adr/0085-editor-hud-inline-layer-and-hud-banner.md), [0098](../adr/0098-semantic-first-document-as-projection.md), [0035](../adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md), [concept-to-implementation-map-v1](../ux/concept-to-implementation-map-v1.md), [LANGUAGE-SERVICES-PLAN.md](../LANGUAGE-SERVICES-PLAN.md)

Документ — **сравнительный аппендикс** к хосту документного редактора в зоне **Forward**. Он **не** меняет продуктовый baseline: **AvaloniaEdit** остаётся стеком по умолчанию в репозитории.

---

## 1. Текущий стек (baseline)

| Измерение | AvaloniaEdit (сейчас) |
|-----------|------------------------|
| **Интеграция** | Нативный Avalonia; хост `DockDocumentView`; TextMate через AvaloniaEdit.TextMate |
| **LSP / семантика** | Склейка в коде приложения (направление DAL — [0102](../adr/0102-data-acquisition-layer-boundary-and-contract.md)); «клей» не исчезает сам |
| **Inline HUD** | Adorners, свои renderers, tooltips — реализуемо; полноценные inlays/ghost уровня VS — предмет доработок |
| **Производительность** | Для типичных файлов хорошо; для очень больших — от способа использования хоста |
| **Тема** | Может следовать теме приложения; паритет с MFD/кабиной — отдельная работа ([0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md)) |
| **High-frequency** | События напрямую; всё равно вести через **bounded**-контур [0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md), а не в DataBus |
| **Риск** | Меньше разброса платформы на Windows; нет встроенного браузера во Forward |

---

## 2. Веб-редактор в WebView2 (например Monaco)

| Измерение | Monaco (или аналог) в WebView2 |
|-----------|----------------------------------|
| **Что это** | Встроенный **Edge** webview в **нативном** процессе — **не** оболочка Electron-приложения. Иные компромиссы (interop, C++/WinRT, два кучи) vs связка Chromium+Node. |
| **Inline HUD** | Богатая экосистема (у Monaco: decorations, codelens-паттерны); остаётся работа по **C#-специфичной** стыковке LSP и **IPC** к DAL/IDE |
| **Тема** | Две системы: веб-CSS и Avalonia `PrimitivesKit` / кабина — риск **дублирования** |
| **Производительность** | Может быть сильной; large-doc и **alloc** — от интеграции |
| **Платформа** | WebView2 ориентирован на Windows; кроссплатформенный MFD уже ссылается на [0093](../adr/0093-mfd-embedded-browser-for-launch-url.md) для **вторичных** поверхностей, без обязанности для **кодового** редактора |
| **Политика** | [ADR 0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md): **не** baseline Forward; только опциональная исследовательская линия. WebView2 как инструмент **MFD** — [0035](../adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md). |

---

## 3. Другие нативные или гибридные варианты (strawman)

| Вариант | Когда смотреть |
|---------|----------------|
| **Более тяжёлая кастомизация Avalonia** (больше adorners, Skia, своя вёрстка текста) | Оставаться нативным; вкладываться, если пределы AvaloniaEdit **измеримы** в целевых сценариях (большие файлы, плотность hint’ов) |
| **Редактор в отдельном процессе** (IPC) | Вне v1; растёт операционка и синхронизация с «истиной буфера» [0084](../adr/0084-agent-edits-editor-source-of-truth-presence-channel.md) |
| **Стиль Roslyn/VS platform** | Долгий горизонт; не вытекает из спайка 0103 |

---

## 4. Как это питает `IEditorSurfaceAdapter`

Контракт адаптера — **независимый от хоста**: тот же `SemanticProjectionPipeline` / `EditorHudEngine` сначала крутит AvaloniaEdit; веб-хост реализует **тот же** порт координат, каретки и **семантического отображения**, без дублирования DAL или CCU.

**Минимальная поверхность порта (концептуально):**

- `DocumentId` + снимок текста или диапазоны изменений для LSP
- Каретка/selection в смещениях документа; опционально визуальная строка/колонка
- API запроса **стабилизированной** презентации hover/диагностик от **движка** (а не сырой LSP во view)

---

## 5. Сводка решения

| Сценарий | Направление |
|----------|-------------|
| **Редактор кода Forward по умолчанию** | **AvaloniaEdit** + слои [0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) |
| **MFD / URL запуска / внешний LLM** | WebView2 по **0035 / 0093** — не замена ядра редактора без отдельного ADR |
| **Исследование «replacement-first»** | Опциональный спайк **после** первого вертикального среза AvaloniaEdit; явный sign-off рисков |
