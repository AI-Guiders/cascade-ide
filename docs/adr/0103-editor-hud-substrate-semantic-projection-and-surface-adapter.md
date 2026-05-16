# ADR 0103: субстрат Editor HUD — семантическая проекция, адаптер поверхности, границы DAL / CCU / DataBus

**Статус:** Accepted (strangler)  
**Дата:** 2026-04-26

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0006](0006-presentation-layers-and-feature-slices.md) | Слои, вертикальные срезы и роль MainWindowViewModel |
| [0009](0009-strangler-migration-and-exceptions.md) | Strangler-миграция и когда допускаются отклонения от политики |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD / MFD — модель внимания кокпита Cascade IDE |
| [0032](0032-hud-banner-configuration-and-grammar.md) | HUD над редактором — настраиваемое содержимое и грамматика (как у `presentation`) |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Встроенный браузер в MFD, внешние веб-LLM и граница с MCP |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Канал → CDS → композитор поверхности → поверхность (Agent-first отображение) |
| [0039](0039-workspace-navigation-affordances.md) | Навигация по workspace — несколько представлений и «текущий файл + связанные» |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI и слой presentation IDE — раздельные опоры |
| [0067](0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces — общий контракт для семейства графовых экранов |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | Правки агента в редакторе как единственный текстовый источник правды; чат — намерение и статус; слой присутствия (GDocs-like, без обязательного CRDT) |
| [0085](0085-editor-hud-inline-layer-and-hud-banner.md) | Editor HUD — inline-слой в редакторе и отличие от HUD banner |
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | Шина доставки событий в UI (аналогия AFDX) и `System.Threading.Channel<T>` |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | Вычислительные блоки кабины (CCU; аналог LRU *Unit*) — слой между транспортом, смыслом и каналом |
| [0098](0098-semantic-first-document-as-projection.md) | Семантика первична; документ и репозиторий — проекции (Semantic-First) |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE DataBus — типизированные события и проекции состояния |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | Data Acquisition Layer — граница внешних интерфейсов и адаптеров |

### Вне ADR

| Документ | Роль |
|----------|------|
| [чертеж: data-acquisition-layer-boundaries-v1](../design/data-acquisition-layer-boundaries-v1.md) | чертеж: data-acquisition-layer-boundaries-v1 |
| [чертеж: сравнение кандидатов поверхности редактора](../design/editor-surface-candidates-comparison-v1.md) | чертеж: сравнение кандидатов поверхности редактора |
| [чертеж: analyzer-rollout-dal-ccu-v1](../design/analyzer-rollout-dal-ccu-v1.md) | чертеж: analyzer-rollout-dal-ccu-v1 |
| [ux: roadmap полировки Forward](../ux/editor-forward-ui-cleanup-roadmap-v1.md) | ux: roadmap полировки Forward |
**Состояние реализации (v1 слоя «закрыт» в пределах strangler):** `Features/Editor` — `IEditorSurfaceAdapter` / `AvaloniaEditSurfaceAdapter`, hi-freq `EditorStabilizedInputThrottler` (один на окно) + `EditorInputDelta` → guard по `CurrentFilePath` → **`EditorDocumentHudLayer`** (per document: `EditorHudEngine` + `EditorHudStabilizedContext`) → `SemanticProjectionPipeline` / `EditorSemanticSnapshot` из DAL-полосок; **презентация** file-level **HUD banner** в `Application/Presentation/EditorHudBannerTextComposer` (0085: не путать с `EditorInlineHudLayer`-зарезервированным inline); VM только оркестрирует Roslyn-вхождения и присваивает `EditorHudBannerText`, при `DiagnosticsChanged` снимок инвалидируется. **Вне v1:** перенос inline-рендера (squiggles, tooltips) из `DockDocumentView` в тот же слой данных — [editor-hud-inline-migration-inventory-v1](../design/editor-hud-inline-migration-inventory-v1.md), политика баннер/inline — [editor-hud-banner-inline-policy-v1](../design/editor-hud-banner-inline-policy-v1.md), визуальная политика MFD/Forward — [editor-forward-ui-cleanup-roadmap-v1](../ux/editor-forward-ui-cleanup-roadmap-v1.md).

---

<a id="adr0103-context"></a>

## 1. Контекст

`DockDocumentView` и связанные view models уже реализуют фрагменты **Editor HUD** (adorners, LSP, всплывающие подсказки) и **HUD banner** в духе [0085](0085-editor-hud-inline-layer-and-hud-banner.md), но **inline**-опыт **не** оформлен как единый именованный субстрат: контракты Quick Info, inlays, ghost text, gutter разбросаны. Параллельно в стеке закреплены явные слои — **CCU** [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), **DAL** [0102](0102-data-acquisition-layer-boundary-and-contract.md), **IDE DataBus** [0099](0099-ide-databus-typed-events-and-projections.md), **ingestion** [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) — и направление **semantic-first** [0098](0098-semantic-first-document-as-projection.md).

Без единого ADR новая работа по HUD рискует:

- свалить **LSP I/O и чтение файлов** в view models или в CCU (конфликт с [0102](0102-data-acquisition-layer-boundary-and-contract.md) и [analyzer-rollout-dal-ccu-v1](../design/analyzer-rollout-dal-ccu-v1.md));
- публиковать **трафик «на каждую клавишу»** в **DataBus** (конфликт с [0099](0099-ide-databus-typed-events-and-projections.md): типизированные события домена, а не поток high-frequency ввода);
- **смешать** транспорт ([0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md)) с прикладными событиями и ad-hoc свёрткой в UI.

---

<a id="adr0103-decision"></a>

## 2. Решение

<a id="adr0103-three-contours"></a>

### 2.1 Имя субстрата: три согласованных контура

1. **`SemanticProjectionPipeline`** — агрегирует **нормализованные** сущности для зоны Forward-редактора: диагностики, полезная нагрузка hover, контекст символа/связей, флаги присутствия агента, метаданные **версии/актуальности**. **Не** владеет долгосрочно LSP stdio, сырым JSON как «домом» и UX раскладки графа ([0067](0067-graph-backed-surfaces-contract.md) остаётся на graph-backed surfaces).

2. **`EditorHudEngine`** — **политика и композиция**: *что* показывать **inline**, что в **HUD banner**, что отдать PFD/MFD [0036](0036-cds-channel-compositor-surface-pipeline.md) / [0039](0039-workspace-navigation-affordances.md). Потребляет **стабилизованные** или **прореженные (throttle)** входы, а не неограниченный «шум на символ».

3. **`IEditorSurfaceAdapter`** (или эквивалентное имя) — **граница реализации** фактического текстового контрола: **основной baseline** в этом репозитории остаётся **AvaloniaEdit** (см. [concept-to-implementation-map-v1](../ux/concept-to-implementation-map-v1.md), [LANGUAGE-SERVICES-PLAN.md](../LANGUAGE-SERVICES-PLAN.md)). Адаптер отдаёт **координаты документа, каретку, selection** и **affordances** хоста, нужные движку HUD, без размазывания типов редактора по приложению.

Термины [0085](0085-editor-hud-inline-layer-and-hud-banner.md) не меняются: **Editor HUD** = inline + привязка к документу; **HUD banner** = полоса уровня файла; **IDS** = глобальные оверлеи IDE, не Editor HUD [0066](0066-cockpit-ui-vs-ide-presentation-layer.md).

<a id="adr0103-layering-table"></a>

### 2.2 Раскладка: DAL, CCU, DataBus, high-frequency

| Вопрос | Слой | Примечание |
|--------|------|------------|
| LSP stdio, чтение файла/настроек, запуск процессов | **DAL** [0102](0102-data-acquisition-layer-boundary-and-contract.md) в `Features/*/DataAcquisition` | Не CCU, не «толстый» `MainWindowViewModel` [0006](0006-presentation-layers-and-feature-slices.md) |
| Осмысленные **снимки** Editor HUD / Forward (дедуп, приоритет error > warn, `DocumentId` + диапазоны, версия) | **CCU** [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) и/или оркестраторы `Features/.../Application` | кандидаты имён, например `EditorSemanticSnapshot` / `ForwardHudSnapshot` |
| **Сессия** и **после debounce** факты домена (`CSharpLspRestarted`, `ActiveDocumentChanged`, сценарий вроде «диагностики стабилизировались») | **IDE DataBus** [0099](0099-ide-databus-typed-events-and-projections.md) | **Не** на каждое нажатие клавиши |
| **Каретка, указатель, скролл** в масштабе кадра/клавиш | **Отдельный** in-process путь: например `System.Threading.Channels` **ёмкость 1** + `BoundedChannelFullMode.DropOldest` (или SPSC *latest slot*) | **Не** второй глобальный «продуктовый bus»; **не** подмена [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) как сквозного транспорта. Один consumer прореживает и при необходимости публикует **реже** в DataBus / входы CCU. |
| Ingestion / потоки «как лог» в UI | [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | Ортогонально DataBus [0099](0099-ide-databus-typed-events-and-projections.md) |

<a id="adr0103-web-vs-native-forward"></a>

### 2.3 Веб-стек и нативный редактор Forward

- **WebView2** в хосте Avalonia/Win32 **не** равен «втаскиваем **Electron**» (нет оболочки Chromium+Node как приложения). **Иная** ведомость: встроенный рендерер, interop, доверие.
- **Продуктовый baseline** для **редактора кода** во Forward остаётся **нативный** (AvaloniaEdit). **Monaco в WebView2** (и аналоги) **не** молчаливый default: **сравнение** / **отклонён** для Forward по политике продукта; см. [editor-surface-candidates-comparison-v1](../design/editor-surface-candidates-comparison-v1.md).
- Опциональные веб-поверхности **MFD** — по [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) / [0093](0093-mfd-embedded-browser-for-launch-url.md); **вторичные** инструменты, не тезис «редактор = браузер».

<a id="adr0103-invariants"></a>

### 2.4 Инварианты (не ломать)

- [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md): при применении правок агента **буфер редактора** — источник применяемой правды; чат — намерение/статус; канал присутствия отдельно.
- [0085](0085-editor-hud-inline-layer-and-hud-banner.md): не сливать **inline** Editor HUD и **HUD banner** в одном контроле без явных имён.
- [0098](0098-semantic-first-document-as-projection.md): ведущими остаются семантические пути PFD/MFD; подсказки редактора **не** забирают на себя всю навигацию.
- [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) / [0102](0102-data-acquisition-layer-boundary-and-contract.md): **CASCOPE020/021** и rollout [analyzer-rollout-dal-ccu-v1](../design/analyzer-rollout-dal-ccu-v1.md) как «шлагбаум» границы.

<a id="adr0103-editor-core-criteria"></a>

### 2.5 Критерии выбора ядра редактора (для чертежа-аппендикса / будущего спайка)

- Inline hints, ghost text, inlays, gutter; rich hover / Quick Info; производительность на больших буферах; **согласованность темы**; API указателя/каретки/документа; интеграция с **semantic-first** кокпитом [0039](0039-workspace-navigation-affordances.md).

---

<a id="adr0103-strangler"></a>

## 3. Strangler / миграция

1. **Отвязать** презентацию HUD от ad-hoc деталей `DockDocumentView`: ввести три контура выше **без** big-bang замены `AvaloniaEdit`.
2. **Спайк (по умолчанию):** `AvaloniaEditSurfaceAdapter` + **bounded** high-frequency путь + один **срез** `SemanticProjectionPipeline` / `EditorHudEngine` (например диагностики + одна ветка hover). Подробнее — [§5 — объём spike](#adr0103-spike-scope).
3. **Второй** адаптер хоста (например веб) — только при **явном** принятии риска веб-стека во Forward; для первого спайка не обязателен.
4. После стабилизации — при необходимости **graph-backed** surfaces [0067](0067-graph-backed-surfaces-contract.md) для *навигации*, которой не место в текстовом хосте.

---

<a id="adr0103-non-goals"></a>

## 4. Не цели (v1 этого ADR)

- Фиксировать окончательные **имена типов** и расклад папок (strangler: интерфейсы рядом с `Features/Editor` или по согласованию в реализации).
- Подменять **CDS** [0036](0036-cds-channel-compositor-surface-pipeline.md) или переопределять **канал** в смысле кокпита.
- Внешние message broker’ы или сведение всех потоков в один envelope.

---

<a id="adr0103-spike-scope"></a>

## 5. Объём technical spike (по умолчанию)

| Пункт | Объём |
|-------|--------|
| **Адаптер** | `IEditorSurfaceAdapter` для **AvaloniaEdit**; сопоставление каретки/selection/смещений в документе для одного типа документа (например C#) |
| **Hi-freq** | Продюсер(ы) на событиях редактора → `Channel<EditorInputDelta>` (ёмкость 1, drop-oldest) → один consumer с **throttle** (например 16–50 ms) до касаний CCU/DataBus |
| **Пайплайн** | Один вертикальный срез: LSP/диагностики из **DAL** → снимок в духе **CCU** или тонкий оркестратор в `Application` → **EditorHudEngine** → адаптер |
| **Вне спайка** | полный адаптер Monaco/WebView2; полный охват CASCOPE по всем типам HUD |

**Критерии успеха:** нет нового LSP I/O в `Cockpit/ComputingUnits/*`; нет неограниченного per-key `Publish` на `IDataBus`; баннер vs inline [0085](0085-editor-hud-inline-layer-and-hud-banner.md) остаётся явным.

---

<a id="adr0103-related-docs"></a>

## 6. Сопутствующие документы

- **Сравнение хостов (аппендикс):** [editor-surface-candidates-comparison-v1](../design/editor-surface-candidates-comparison-v1.md)
- **Roadmap полировки UI (Forward, баннер, всплывающие подсказки, MFD):** [editor-forward-ui-cleanup-roadmap-v1](../ux/editor-forward-ui-cleanup-roadmap-v1.md)

---

<a id="adr0103-consequences"></a>

## 7. Последствия

- **Плюс:** единая точка расширения Quick Info, inlays, gutter без запутывания DAL, DataBus и сырых событий редактора; согласование с 0097/0102/0099/0094.
- **Минус:** предварительная проработка дизайна и адаптера до каждой косметической правки; дисциплина, чтобы hi-freq не уезжал в DataBus.
- **Риск при игнорировании:** возврат к **god-**`MainWindowViewModel`, **event spaghetti** на DataBus, нарушения CASCOPE между DAL и CCU.

---

<a id="adr0103-rejected-alternatives"></a>

## 8. Отклонённые альтернативы (кратко)

- **Весь поток каретки/диагностик через DataBus** — отклонено: против духа [0099](0099-ide-databus-typed-events-and-projections.md) и лавина подписчиков.
- **CCU читает LSP потоки напрямую** — отклонено: разделение [0102](0102-data-acquisition-layer-boundary-and-contract.md) и [0097](0097-cockpit-compute-units-transport-to-channel-dto.md).
- **Веб-редактор по умолчанию во Forward** без ревью — отклонено для baseline; опциональная линия только с явным продуктовым sign-off.
