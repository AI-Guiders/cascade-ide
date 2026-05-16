# ADR 0106: Hybrid Codebase Index — интеграция в CascadeIDE, свежесть и Semantic Map

**Статус:** Accepted · In progress  
**Дата:** 2026-05-07 (обновлено 2026-05-08)  
## Связанные ADR

| ADR | Роль |
|-----|------|
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | Базовое ядро и MCP; этот ADR — контур CascadeIDE (DAL/CCU/DataBus, свежесть, Semantic Map) |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | Data Acquisition Layer — граница внешних интерфейсов и адаптеров |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | Вычислительные блоки кабины (CCU; аналог LRU *Unit*) — слой между транспортом, смыслом и каналом |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE DataBus — типизированные события и проекции состояния |
| [0098](0098-semantic-first-document-as-projection.md) | Семантика первична; документ и репозиторий — проекции (Semantic-First) |
| [0039](0039-workspace-navigation-affordances.md) | Навигация по workspace — несколько представлений и «текущий файл + связанные» |
| [0053](0053-semantic-map-control-flow-pfd.md) | Карта намерений и поток управления на PFD (control flow) |
| [0056](0056-semantic-map-pipeline-adoption.md) | Semantic Map adoption of Skia composition pipeline |
| [0067](0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces — общий контракт для семейства графовых экранов |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS (Ide Display System) — пайплайн оверлеев IDE, ортогонально CDS |

### Вне ADR

| Документ | Роль |
|----------|------|
| [`hybrid-codebase-index`](https://github.com/KarataevDmitry/hybrid-codebase-index) | Репозиторий ядра и MCP-хоста |

### Снимок реализации

| Элемент | Значение |
|---------|----------|
| — | in-proc оркестратор, UI настроек, MFD HIS |

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

В [0097 § P3](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-candidates-p3) зафиксирован кандидат **`SemanticMapInputSnapshot`** — слой B после нормализации через **CCU** ([0097 § граница semantic map](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-semantic-map-boundary)) задаёт содержание такого входа для graph-backed поверхностей. Подробнее, что HCI **даёт и не даёт** карте (ориентация vs граф, UI, non-goals) — **[ADR 0113](0113-hci-semantic-map-orientation-layer.md)**.

### Composition workflow в продукте

Интеграция сценария **«Hybrid search → Roslyn точность»** (п. 5 дорожной карты [ADR 0105](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-rollout-plan)) в UI/оркестрации IDE: подсказки к следующему шагу (`go-to-def`, usages, diagnostics), без смешения `hit_kind` с символьной истиной.

### Persistence и синхронизация с оркестратором

- Модель: `CascadeIdeSettings.HybridIndex` → TOML **`[hybrid_index]`** (общий файл пользовательских настроек CascadeIDE, см. `SettingsService`; образец — `docs/samples/settings.toml`).
- **Clone / Is**: изменения секции HCI участвуют в детекторе «сохранить на диск» (`SaveSettingsIfChanged`).
- После изменения параметров через UI главного окна вызывается **`ApplyHybridCodebaseIndexOrchestrationForCurrentSolution`**: включение watcher, debounce, `scope_mode`, учёт режима **`mcp_only`** при `pause_when_mcp_stdio_host`, смена **`index_dir`** через пересборку in-proc `CodebaseIndexService` и переустановку watcher’ов.
- Сценарий **открытие/смена решения** дополнительно делает **`Poke`** при `auto_reindex_on_solution_open` (как и раньше на `SolutionPath`-изменении).
- Обновление страницы **INDEX / HCI** в MFD: событие `HybridIndexStateChanged` в IDE DataBus; подписка при первом переходе на страницу (см. `MainWindowViewModel.EnvironmentReadiness`).

### Операционные заметки для агента / разработки

Краткий чеклист (поддерживается файлом `docs/agent-hci-cascadeide-notes-v1.md`): где лежит TOML, как не путать in-proc путь БД и внешний MCP к тому же `index_dir`, как подтверждать «живость» индекса через MFD или MCP `codebase_index_status`.

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
