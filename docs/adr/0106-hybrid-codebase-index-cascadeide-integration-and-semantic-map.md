# ADR 0106: Hybrid Codebase Index — интеграция в CascadeIDE, свежесть и Semantic Map

**Статус:** Proposed  
**Дата:** 2026-05-07  

**Связь:** базовое решение по ядру и MCP — **[ADR 0105](0105-hybrid-codebase-index-for-csharp-web.md)** (**Accepted · Implemented**; код в репозитории [`hybrid-codebase-index`](https://github.com/KarataevDmitry/hybrid-codebase-index)). Здесь — **контур CascadeIDE**: DAL/CCU/DataBus, UX свежести индекса и подача снимков на graph-backed поверхности.

**Контекст кабины:** [0102](0102-data-acquisition-layer-boundary-and-contract.md), [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), [0099](0099-ide-databus-typed-events-and-projections.md), [0098](0098-semantic-first-document-as-projection.md); навигация и semantic map — [0039](0039-workspace-navigation-affordances.md), [0053](0053-semantic-map-control-flow-pfd.md), [0056](0056-semantic-map-pipeline-adoption.md), [0067](0067-graph-backed-surfaces-contract.md), [0079](0079-ide-display-system-ids-overlay-pipeline.md).

---

## Решение

Инструмент **`hybrid-codebase-index`** (библиотека + MCP) остаётся **переносимым** и может работать без Avalonia. **CascadeIDE** подключает тот же контракт tools / те же DTO (**или** in-proc ядро с тем же API), но размещает I/O и жизненный цикл по слоям кабины ниже — без «божественного» `MainWindowViewModel`.

### Согласование в контуре CascadeIDE

Встроенная связка (in-proc или дочерний процесс, поднятый IDE) должна вписываться в принятую архитектуру:

- **DAL** ([0102](0102-data-acquisition-layer-boundary-and-contract.md)): обход workspace, чтение файлов под индекс, при необходимости сеть/процессы для embeddings и прочий внешний I/O — в духе `Features/<slice>/DataAcquisition/`, без забрасывания сырого I/O в VM.
- **Оркестраторы `Application`**: сценарии `reindex` / `search`, конфигурация из `settings.toml`, связка watcher ↔ ядро индекса ↔ жизненный цикл SQLite-файлов.
- **CCU** ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)): свёртка результата поиска (FTS + vec, версия индекса, метаданные hit) в **стабильные DTO** для каналов и при необходимости в снимки (топ-N, explain) — без превращения CCU во второй «движок индекса».
- События смены файлов/прогресса индекса и подписки UI — по смыслу через **IDE DataBus** ([0099](0099-ide-databus-typed-events-and-projections.md)).

### Свежесть (freshness) в IDE

При частых сохранениях индекс по `.cs` / `.axaml` и смежным типам файлов должен обновляться **дёшевым инкрементом** (хеш, пересборка затронутых чанков), без UX-лагова «полный reindex на каждый keypress». Семантика: либо сценарный вызов с debounce из оркестратора, либо единый watcher-поток, согласованный с MCP-контрактом там, где агент смотрит тот же `databasePath`.

Подробнее о мотивации см. в [ADR 0105 § watchouts freshness](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-impl-watchouts-freshness) — здесь задаётся **реализация в CIDE**.

### Semantic Map и слой B (граница)

**Semantic Map** — graph-backed поверхность (намерения, control flow, Skia pipeline). Гибридный индекс (**слой B** в терминологии ADR 0105) **не является** каноническим графом Semantic Map и **не заменяет** CFG / Roslyn-символьную истину по C#. Он даёт **ориентацию**: топ попаданий, пути, диапазоны в файлах, версия индекса и при желании вход для declutter карты — с явным `hit_kind` в DTO ([0105 § hit_kind](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-impl-watchouts-hit-kind)).

В [0097 § P3](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-candidates-p3) зафиксирован кандидат **`SemanticMapInputSnapshot`** — слой B после нормализации через **CCU** ([0097 § граница semantic map](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-semantic-map-boundary)) задаёт содержание такого входа для graph-backed поверхностей.

### Composition workflow в продукте

Интеграция сценария **«Hybrid search → Roslyn точность»** (п. 5 дорожной карты [ADR 0105](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-rollout-plan)) в UI/оркестрации IDE: подсказки к следующему шагу (`go-to-def`, usages, diagnostics), без смешения `hit_kind` с символьной истиной.

---

## Rollout (эскиз, только CIDE)

1. Тонкая оболочка: запуск уже опубликованного MCP или общая библиотека — тот же tool id / JSON-контракт.
2. DAL-слой чтения workspace + проброс workspace root / опционально solution path так же, как в ADR 0105 (per-scope SQLite).
3. Подписка на сохранения / debounced incremental reindex через оркестратор + события DataBus.
4. CCU-снимок для канала IDE Health или отдельного «Index / Orientation» канала при необходимости.
5. (Опционально) Связка **`SemanticMapInputSnapshot`** после стабилизации DTO.

---

## Последствия

- Дублирования логики индекса в VM нет — CCU только упаковывает.
- Версионирование MCP и приложения может расходиться: нужны явные проверки `indexFormatVersion`/status при старте сессии.
