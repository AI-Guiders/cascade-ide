# ADR 0052: CLI для контракта агента (паритет с MCP) и снапшот-тесты

**Статус:** Accepted · Implemented (`get_ui_modes_diagnostics`, `get_supported_editor_languages`, `get_solution_info`, `get_cockpit_surface`, `get_ide_state`, read-only `git_*` с `--workspace`; CI: `.gitlab-ci.yml` — `dotnet test` + smoke `--agent-contract`; **golden slice** CDS: `CascadeIDE.Tests/TestData/AgentContract/cockpit_surface_contract_slice.approved.json` + `AgentContractCockpitContractSlice`)  
**Дата:** 2026-04-17  
**Принят:** 2026-04-16  

**Реализация:** [`Services/AgentContract/AgentContractRunner.cs`](../../Services/AgentContract/AgentContractRunner.cs), [`Services/AgentContract/AgentContractHeadlessRuntime.cs`](../../Services/AgentContract/AgentContractHeadlessRuntime.cs), вход `CascadeIDE.exe --agent-contract [--workspace <dir>] <command>` в [`Program.cs`](../../Program.cs); тесты [`CascadeIDE.Tests/AgentContractRunnerTests.cs`](../../CascadeIDE.Tests/AgentContractRunnerTests.cs), [`CascadeIDE.Tests/AgentContractRunnerHeadlessTests.cs`](../../CascadeIDE.Tests/AgentContractRunnerHeadlessTests.cs) (в т.ч. снапшот-срез `cockpit_surface`: [`CascadeIDE.Tests/AgentContractCockpitContractSlice.cs`](../../CascadeIDE.Tests/AgentContractCockpitContractSlice.cs), [`CascadeIDE.Tests/TestData/AgentContract/cockpit_surface_contract_slice.approved.json`](../../CascadeIDE.Tests/TestData/AgentContract/cockpit_surface_contract_slice.approved.json)). Паритет с MCP: `ide_get_ui_modes_diagnostics`, `ide_get_supported_editor_languages`, `ide_get_ide_state` (полная сводка — команда CLI `get_ide_state`), `ide_get_cockpit_surface` / только CDS — `get_cockpit_surface` (тот же JSON, что поле `cockpit_surface` в `get_ide_state`), `ide_git_*` (те же JSON-поля, что и в `MainWindowViewModel` / `GitCommandBuilder` + `GitCommandRunner`).

**Связь:** [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) (команды IDE / stdio MCP), [0002](0002-debug-human-agent-parity.md) (паритет «человек ↔ агент»), [0008](0008-mcp-contracts-and-testable-infrastructure.md) (MCP-контракты и тестируемая инфраструктура), [0043](0043-mcp-transport-recovery-human-agent-parity.md) (транспорт MCP и паритет). Цель — **тот же JSON**, что агент получает через тулы, без обязательного GUI и без дублирования сериализации; при том что **релевантные для кабины/раскладки поля** в этом JSON совпадают по смыслу с тем, что питает UI (см. [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md), CDS/снимки поверхности).

---

## Контекст

Сегодня Cascade IDE отдаёт агенту **обогащённый JSON** (layout, состояние workspace, диагностики, проекция кабины/инструментов и т.д.) через **MCP-инструменты** (`ide_*`, см. контракт в [MCP-PROTOCOL.md](../MCP-PROTOCOL.md)). Часть полей — **тот же семантический слой**, на который опирается UI (размещение, видимость слотов, раскладка в смысле CDS/host surface), а не «параллельная выдумка для агента». Это удобно в интерактиве, но:

- **Регрессии** в форме ответа сложно ловить без запуска IDE и сценария MCP.
- **CI** хочется иметь **детерминированные** проверки контракта: «после изменения сборщика снимка ответ не сломался».
- Отдельная реализация «как CLI» рискует **разойтись** с реальным MCP — тесты будут зелёными, агент в Cursor — нет.

## Решение

**Принять** отдельную поставку — **headless CLI** (или подкоманда существующего host’а), которая:

1. **Вызывает те же** публичные/внутренние **сборщики данных и сериализацию JSON**, что и обработчики MCP для выбранных `ide_*` команд — **без второй копии логики** (общий слой: сервисы, `IdeMcpCommandExecutor` хелперы, билдеры снимков и т.д., вынесенные так, чтобы их можно было дернуть и из процесса IDE, и из CLI).
2. Принимает на вход **явный контекст**: как минимум `--workspace` (корень workspace), при необходимости путь к решению, флаги «какой тул эмулировать» и параметры args (по схеме MCP).
3. Пишет **в stdout** тот же JSON (или тот же **нормализованный** вид), что вернул бы соответствующий тул в MCP.

**CI:** в корне репозитория — [`.gitlab-ci.yml`](../../.gitlab-ci.yml): `dotnet build` / `dotnet test` и последовательный smoke `dotnet run --project CascadeIDE -- --agent-contract …` (ожидается **Windows** runner с .NET 10 SDK и тег `windows`; при другом окружении — поправить `default.tags`). Дополнительно в репозитории принят **`dotnet script`** (глобальный `dotnet-script`, см. `Financial/finplan/update-finplan-pdf.csx`, `agents-and-humans-book/update-agents-humans-pdf.csx`). Для вызова `--agent-contract` из пайплайна с собранным `CascadeIDE.exe`: [`docs/samples/agent-contract-ci.csx`](../samples/agent-contract-ci.csx) — `ProcessStartInfo`, код выхода, без сюрпризов с `$LASTEXITCODE`. Альтернатива на **PowerShell**: [`docs/samples/agent-contract-ci.ps1`](../samples/agent-contract-ci.ps1) (`pwsh` 7+ или `Start-Process -PassThru.ExitCode` в Windows PowerShell 5.1).

**Тесты:**

- **Интеграционные / контрактные:** запуск CLI (или прямой вызов общего API) на **фикстурном workspace** (минимальный `.sln` / файлы уже есть в тестах) → сравнение с **ожидаемым JSON** или с **снапшотом** с осознанным обновлением.
- **CDS (кабина):** узкий **golden slice** — `schema_version` + `topology` + отсортированный `instruments` (`TestData/AgentContract/cockpit_surface_contract_slice.approved.json`); поля вроде `ui_mode` и `presentation_effective_line` в снапшот не входят (зависят от пользовательского `settings.toml`). При смене дефолтного placement или схемы CDS — обновить approved-файл осознанно.
- **Нормализация для стабильности:** абсолютные пути → относительные к фикстуре; вырезание времени/версий при необходимости; либо assert по **подмножеству полей** (schema-shaped), если полный снапшот слишком шумный.
- **«Отрисовка» на уровне данных:** для сценариев, где MCP отдаёт **состояние кабины / layout / инструменты** (то, что консистентно с тем, что рисует UI), снапшот того же JSON в CI проверяет **регрессию представления** — не пиксели и не Skia/Avalonia-пайплайн, а **что именно должно оказаться в каком слоте и с какими идентификаторами**. Это осмысленный слой проверки «UI vs агент видят одно и то же», без дублирования отдельной модели «только для тестов».

**Негативно:**

- **Пиксели, шрифты, визуальные регрессии Skia/Avalonia** — по-прежнему отдельно (скриншоты, UI-тесты, ручной просмотр). CLI+MCP-снапшоты не заменяют рендер-движок.
- Не дублировать **бизнес-логику** «с нуля» — только тонкая оболочка над общим кодом.

## Последствия

| Плюсы | Минусы / риски |
|--------|----------------|
| Регрессии контракта в CI без GUI | Нужно один раз вынести общий слой и следить, чтобы MCP и CLI шли в одну точку входа |
| Документирование «что именно» отдаёт тул (через тесты) | Фикстуры и нормализация — поддержка при смене ОС/путей |
| Быстрый feedback при рефакторинге сборщиков JSON | Скоуп по тулам расти поэтапно — иначе большой взрыв работы |
| Проверка согласованности **семантики кабины/UI** с тем, что видит агент (один источник в JSON) | Риск смешать слои: держать в снапшотах поля, которые реально shared с UI, а не весь ответ целиком если он шумный |

## Этапы внедрения (рекомендация)

1. Выбрать **один** тул с устойчивым JSON (например снимок layout или один read-only сценарий).
2. Вынести **единую** функцию «построить payload» + «в JSON string».
3. Добавить минимальный **CLI** (`dotnet run --project …` или отдельный `net10.0` tool).
4. Покрыть **одним** снапшот-тестом; затем расширять список команд.

## Принятие и открытые вопросы

**Открытых вопросов нет** — направление зафиксировано для реализации.

Дальше: добавлять команды в `AgentContractRunner.TryGetJson`, вызывая те же сборщики, что и `IIdeMcpActions` / MCP; при существенном расширении — обновить таблицу здесь и в [MCP-PROTOCOL.md](../MCP-PROTOCOL.md).
