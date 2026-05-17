# Feature archetype v1 — как добавлять возможность в Cascade IDE

**Статус:** действующий чертёж (v1).  
**Связь:** [architecture-policy.md](../architecture-policy.md), [architecture-migration.md](../architecture-migration.md), [ADR 0006](../adr/0006-presentation-layers-and-feature-slices.md), [ADR 0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md), [ADR 0076](../adr/0076-ui-ux-principles-hub.md), [iop-manifest-v1.md](../iop-manifest-v1.md).

Цель — чтобы новая работа **сама ложилась** в каркас CDS · CCU · DAL · IDS, как после внедрения pipeline кабины, а не обрастала исключениями в `MainWindowViewModel`.

---

## 1. Чеклист Definition of Done

Перед merge фичи проверь:

| # | Вопрос | Ожидаемый ответ |
|---|--------|-----------------|
| 1 | Где I/O (файл, процесс, сеть)? | `Features/<Domain>/DataAcquisition/` — не в CCU и не в VM |
| 2 | Где бизнес-смысл и snapshot? | `Features/<Domain>/Application/` или CCU в `Cockpit/` |
| 3 | Куда попадает на экране? | **CDS** (зона PFD / Forward / MFD / слот прибора) **или** **IDS** (палитра, модалка) — явно в ADR/описании PR |
| 4 | Где UI state и команды? | `Features/<Domain>/*ViewModel` + view; **не** новые сотни строк в MWVM |
| 5 | Skia-surface? | Intent → … → Layout → Render; примитивы из `Views/SkiaKit/`; CASCOPE008 |
| 6 | Avalonia MFD/page? | `CascadeTheme.*` + `Views/UiKit/`; без сырых `#RRGGBB` в разметке |
| 7 | Intent для агента? | Тот же `command_id` в MCP / палитре / Intercom ([0119](../adr/0119-chat-slash-commands-intercom-surface.md)) |
| 8 | Тесты | Application/CCU/оркестратор; UI — по необходимости headless contract |

**MWVM** — композитор: bind, `TryNavigate…`, мост `IIdeMcpActions`, вызов оркестратора. Логика «как собрать JSON / что значит статус» — вне MWVM.

---

## 2. Каркас слоёв (кратко)

```
DAL          →  сырьё с диска/процесса/wire
Application  →  use-cases, оркестраторы, политики
CCU + CDS    →  кабина: канал → композитор → surface snapshot
IDS          →  overlay shell (палитра, …) поверх workspace
Features/*   →  VM + Views + регистрация в shell
MWVM         →  composition root окна
```

Подробнее: [architecture-migration.md § Стратегия](../architecture-migration.md#стратегия-опора-на-целевой-каркас-cds--ccu--dal--ids).

---

## 3. Шаблон каталогов (новая фича `Foo`)

```
Features/Foo/
  DataAcquisition/     # опционально
  Application/         # оркестраторы, проекции
  FooPanelViewModel.cs
Views/
  FooMfdPageView.axaml   # если страница MFD
  Foo*.axaml             # или Skia surface в Views/Foo/Skia/
CascadeIDE.Tests/
  Foo*Tests.cs
```

Не добавлять доменную логику в `IdeMcpCommandExecutor` — только диспетчер; handler вызывает Application или `IIdeMcpActions`.

---

## 4. UI: IDE chrome vs cockpit

| Слой | Путь | Когда |
|------|------|--------|
| **IDE chrome** | `CascadeTheme`, `Views/UiKit/`, `Themes/*.json` | меню, MFD-страницы, настройки, чат-оболочка AXAML |
| **SkiaKit** | `Views/SkiaKit/` | плотные surfaces (чат, карты) |
| **PrimitivesKit** | `Cockpit/PrimitivesKit/` | приборы, лампы, deck — **не** для обычных панелей |

Токены chrome v1: [ide-chrome-tokens-v1.md](ide-chrome-tokens-v1.md).  
UX-принципы: [ADR 0076](../adr/0076-ui-ux-principles-hub.md).

---

## 5. IOP / Intercom

Если фича — способ договориться о работе:

- один смысл → `IdeCommands` + опционально slash / Melody;
- верификация остаётся в Forward (код, diff, тесты);
- продуктовая линия — [iop-manifest-v1.md](../iop-manifest-v1.md), [ADR 0121](../adr/0121-intent-oriented-programming-paradigm.md).

---

## 6. Исключения

Отклонение от чеклиста — только с явной пометкой в PR + ссылкой на [ADR 0009](../adr/0009-strangler-migration-and-exceptions.md). Временный код в MWVM помечай `// strangler:` и заводи задачу на вынос.
