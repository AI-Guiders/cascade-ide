# ADR 0106: Hybrid Codebase Index — интеграция в CascadeIDE, свежесть и Semantic Map

**Статус:** Proposed  
**Дата:** 2026-05-07  

**Связь:** базовое решение по ядру и MCP — **[ADR 0105](0105-hybrid-codebase-index-for-csharp-web.md)** (**Accepted · Implemented**; код в репозитории [`hybrid-codebase-index`](https://github.com/KarataevDmitry/hybrid-codebase-index)). Здесь — **контур CascadeIDE**: DAL/CCU/DataBus, UX свежести индекса и подача снимков на graph-backed поверхности.

**Контекст кабины:** [0102](0102-data-acquisition-layer-boundary-and-contract.md), [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), [0099](0099-ide-databus-typed-events-and-projections.md), [0098](0098-semantic-first-document-as-projection.md); навигация и semantic map — [0039](0039-workspace-navigation-affordances.md), [0053](0053-semantic-map-control-flow-pfd.md), [0056](0056-semantic-map-pipeline-adoption.md), [0067](0067-graph-backed-surfaces-contract.md), [0079](0079-ide-display-system-ids-overlay-pipeline.md).

---

## Решение

Инструмент **`hybrid-codebase-index`** оформлен как **общая библиотека** (`HybridCodebaseIndex.Core`) и тонкий **MCP-хост** (`HybridCodebaseIndex.Mcp`) поверх неё — тот же шаблон, что **agent-notes** (`AgentNotes.Core` + exe). **CascadeIDE** в первую очередь подключает **ядро in-proc** (`ProjectReference` на Core из репозитория [`hybrid-codebase-index`](https://github.com/KarataevDmitry/hybrid-codebase-index)): один процесс с редактором, дешёвый вызов `reindex`/`search`, общий `databasePath` с тем, что ожидает внешний MCP при работе из Cursor. Отдельно опубликованный **exe MCP** остаётся для внешних хостов и для сценария изоляции; размещение I/O и жизненного цикла — по слоям кабины ниже, без «божественного» `MainWindowViewModel`.

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

Порядок ориентировочный; первые шаги фиксируют **библиотеку в IDE**, дальше — жизненный цикл и каналы.

1. **Подключить `HybridCodebaseIndex.Core` в решение CascadeIDE**  
   `ProjectReference` на Core (сабмодуль или NuGet при публикации пакета — решение сборки). Один API индекса в процессе IDE; контракт полей ответа совпадает с тем, что описан для tools MCP ([0105 — слой B](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-layer-b), `hit_kind`, версия формата).

2. **Оркестратор + DAL**  
   Проброс `workspace_root` и опционально пути решения — как в [0105 § эскиз области](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-impl-sketch-scope); чтение файлов и вызовы ядра — за границей VM, в духе [0102](0102-data-acquisition-layer-boundary-and-contract.md). Один SQLite на пару *(workspace, solution scope)*, тот же каталог, что использует MCP при той же конфигурации.

3. **Свежесть: сохранения → debounced incremental reindex**  
   Подписка на сохранение документов (или единый watcher, согласованный с политикой IDE), debounce в оркестраторе `Application`, инкрементальный reindex через Core. События прогресса/«индекс обновлён» — в **IDE DataBus** ([0099](0099-ide-databus-typed-events-and-projections.md)).

4. **CCU и каналы**  
   Свёртка результата поиска / статуса индекса в стабильные DTO для **IDE Health** и при необходимости отдельного канала «Index / Orientation» — без дублирования логики индекса в VM ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)).

5. **(Опционально, параллельно или позже)** Запуск опубликованного **`HybridCodebaseIndex.Mcp.exe`** как дочернего процесса — паритет с Cursor, изоляция перезапуска, или пока Core не вшит в sln. Не заменяет шаг 1 для основного UX редактора.

6. **(Опционально)** Связка **`SemanticMapInputSnapshot`** после стабилизации DTO и границы CCU ([0097 § semantic map](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-semantic-map-boundary)).

---

## Последствия

- Дублирования логики индекса в VM нет — CCU только упаковывает; **ядро одно** (Core), MCP — транспорт для внешних вызовов.
- Версия формата индекса (`indexFormatVersion` / status) должна согласовываться между **сборкой Core**, встроенной в CIDE, и **опциональным** exe MCP, если агент использует оба контура к одному workspace — явная проверка при старте или несовместимости.
