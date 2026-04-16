# CDS — контракт «кабины» в CascadeIDE (чертёж v0)

**Статус:** живой чертёж (не ADR). **Назначение:** зафиксировать **контрактный смысл CDS** до разрастания кода и MCP, чтобы новые фичи расширяли **один** семантический слой, а не плодили параллельные «снимки внимания».

**Связь:** [ADR 0036 — цепочка канал → CDS → композитор → поверхность](../adr/0036-cds-channel-compositor-surface-pipeline.md) (нормативная ось). [ADR 0021 §1.1 — CDS / `UiLayoutSnapshot`](../adr/0021-pfd-mfd-cockpit-attention-model.md#glossary-cds-contract), [0017](../adr/0017-multi-window-workspace-and-agent-surfaces.md) (мультиоконность, `presentation`), [workspace-health-implementation-map-v1](workspace-health-implementation-map-v1.md) (канал Workspace Health — ортогонален CDS).

---

## 1. Зачем отдельный документ

- **ADR 0021** задаёт **принципы** зон и внимания; этот файл — **черновик полей** агрегированного «состояния кабины» для API, тестов и агента.
- **`UiLayoutSnapshot`** остаётся слоем **дерева контролов**; CDS — слой **семантики** (что за кабина, без обхода визуального дерева).

---

## 2. Термины

| Термин | Смысл |
|--------|--------|
| **CDS (контракт)** | Снимок **семантики** отображения: зоны, топология, окна, активный вторичный контур. Не реализация ARINC 661 и не один God-class в репозитории. |
| **Cockpit surface state** (рабочее имя DTO) | Условное имя будущей структуры/JSON, агрегирующей поля ниже; версионируется (`schema_version`). |
| **Канал** | Поток данных внутри слота (Workspace Health, EICAS, readiness) — **не** входит в CDS целиком; в CDS только **какие слоты/страницы активны**, без полного текста сегментов. |
| **Instrument (кабинный)** | Именованный выбор **представления в слоте внимания** (результат композитора); не `Control` Avalonia. Термин сознательно в духе PFD/MFD; в бытовом английском *instrument* многозначно — в глоссарии закреплено значение «логическая единица кабины», см. [ADR 0047](../adr/0047-cockpit-instrument-descriptor-and-slot-composition.md). Дескриптор в коде — `CockpitInstrumentDescriptor` (`Cockpit/Composition/CockpitInstrumentDescriptor.cs`). Пример слота PFD: дерево решения vs Semantic Map — два разных `instrument_id`. |

---

## 3. Слои (не смешивать)

| Слой | Вопрос | Типичные источники в коде |
|------|--------|---------------------------|
| **CDS** | Какая **кабина** сейчас: пресет, зоны, окна, страница MFD | `presentation`, `PresentationParseResult`, флаги VM колонок/хоста, `CurrentSecondaryShellPage`, `AttentionLayoutSurfaceKind` |
| **Дерево UI** | **Что** нарисовано в виде контролов | `UiLayoutSnapshot`, MCP `ide_get_ui_layout` |
| **Каналы** | **Что** в полосах и списках | `Cockpit/Channels/**` (данные), `Cockpit/Composition/**` (порядок/разметка для VM): `Composition/Shell/MainWindowShellSurfaceCompositor` — только **колонки** PFD/MFD в main grid; `Composition/HostSurface/MainWindowHostSurfaceCompositor` — **кадр хоста** (shell + список `CockpitInstrumentDescriptor`, ADR 0047) без деревьев контролов — удобная граница перед Skia в слотах. |
| **Avalonia (хост)** | Окна, фокус, ввод, DPI; **фюзеляж** для тяжёлых контролов (редактор и т.д.); **не** канон смысла зон — см. [architecture-policy.md](../architecture-policy.md) (раздел «Avalonia и слой кабины») | `MainWindow`, `MfdHostWindow`, привязки VM; отрисовка слотов кабины — поверх хоста (в т.ч. Skia) по кадру композитора |

### Источник правды сегодня и как сблизить с CDS

**Сейчас** каноническое **состояние кабины** в рантайме по-прежнему живёт в **свойствах `MainWindowViewModel`** (видимость колонок, страница вторичного контура, хост Mfd и т.д.); **CDS** — **проекция** в `CockpitSurfaceState` через [`CockpitSurfaceSnapshotBuilder`](../../Cockpit/Cds/CockpitSurfaceSnapshotBuilder.cs) — это уже **язык для агента, тестов и наблюдаемости** ([ADR 0036 п. 2](../adr/0036-cds-channel-compositor-surface-pipeline.md#adr0036-p2)), не дубль «второй источник правды», если снимок **детерминирован** от VM.

**Улучшать** имеет смысл инкрементально:

1. **Читать семантику кабины** в тестах и инструментации прежде всего из **`Build(vm)`** / готового DTO, а не из россыпи полей VM.
2. **Сужать мутации**: страница shell, топология, хост — через **узкий набор методов**, чтобы после каждого шага снимок CDS оставался согласованным.
3. **Позже** — выделить явный агрегат «состояние навигации кабины» (один тип), из которого и биндинги, и билдер CDS читают одни и те же поля; полный перенос истины в отдельный сервис с подпиской VM — только при явной потребности (дорого).

---

## 4. Черновик полей v0 (эволюция)

Поля **не** обязаны совпадать с текущими именами свойств VM — это целевой контракт для стабилизации.

```json
{
  "schema_version": "0.3",
  "ui_mode": "string",
  "presentation_effective_line": "string",
  "presentation_parse_success": true,
  "topology": {
    "surface_kind": "main_window_docked_grid | …",
    "mfd_host_window_open": false,
    "mfd_column_visible_in_main": true
  },
  "secondary_shell": {
    "current_page": "WorkspaceHealth | Chat | Terminal | …"
  },
  "zones": {
    "pfd_visible": true,
    "forward_visible": true,
    "mfd_visible": true,
    "pfd_required_by_presentation": true,
    "forward_required_by_presentation": true,
    "mfd_required_by_presentation": true
  },
  "instruments": [
    {
      "instrument_id": "solution_explorer_tree",
      "slot_id": "pfd",
      "schema_version": "0.2"
    }
  ]
}
```

**Намеренно узко v0:** без дублирования каналов данных, без геометрии в пикселях (bounds — при необходимости отдельное расширение или слой [0017](../adr/0017-multi-window-workspace-and-agent-surfaces.md)).

---

## 5. Версионирование

- **`schema_version`** — семантическое версионирование JSON (например `0.1`, `0.2` при добавлении полей).
- Обратная совместимость для потребителей MCP: явно документировать в [MCP-PROTOCOL.md](../MCP-PROTOCOL.md), когда появится отдельный инструмент или расширение существующего.

---

## 6. Реализация (статус)

| Этап | Содержание |
|------|------------|
| **Сейчас** | DTO `CockpitSurfaceState` и вложенные записи в `Cockpit/Cds/CockpitSurfaceState.cs`; сборка — `Cockpit/Cds/CockpitSurfaceSnapshotBuilder.Build(MainWindowViewModel)`; точка входа на VM — `MainWindowViewModel.BuildCockpitSurfaceSnapshot()` (см. также `EffectivePresentationLine`, `IsMfdHostWindowShellOpen`, `IsForwardZoneVisible`). В `schema_version=0.3` добавлен список `instruments` (`instrument_id`, `slot_id`, `schema_version`) как проекция HostSurface-кадра для MCP/наблюдаемости. **MCP:** тот же объект вложен в `ide_get_workspace_state` как поле `cockpit_surface` (паритет со Skia/UI). |
| **Дальше** | При необходимости — отдельный тул `ide_get_cockpit_state` (если понадобится без полной сводки); стабилизация полей по обратной связи агента/тестов. |

---

## 7. Связанные файлы кода (ориентиры)

- `Cockpit/Cds/CockpitSurfaceState.cs`, `Cockpit/Cds/CockpitSurfaceSnapshotBuilder.cs` — CDS (семантика кабины); `Cockpit/Cds/AttentionLayoutSurfaceKind.cs` — вид топологии в снимке.
- `Cockpit/Composition/CockpitInstrumentDescriptor.cs` — дескриптор инструмента слота (ADR 0047).
- `Cockpit/Composition/Shell/MainWindowShellSurfaceCompositor.cs` — метрики колонок shell.
- `Cockpit/Composition/HostSurface/MainWindowHostSurfaceFrame.cs`, `MainWindowHostSurfaceCompositor.cs`, `CockpitHostSurfaceIds.cs` — кадр для хоста (shell + инструменты); стабильные `instrument_id` / `slot_id`.
- `Cockpit/Surface/MainWindowInstrumentMountRegistry.cs` — реестр монтирования `instrument_id → mount` (хост-слой, вне композитора; Avalonia/Skia backend).
- `Cockpit/Surface/UiLayoutSnapshot.cs` — дерево UI (другой слой, ADR 0036 п.4).
- Каналы и композиторы по ADR 0036 — `Cockpit/Channels/**`, `Cockpit/Composition/**`.
- `Services/Presentation/PresentationParser.cs`, `PresentationLayoutAnalyzer.cs` — презентация.
- `Views/MainWindow.axaml`, `MainWindow.MfdHostWindow.axaml.cs` — геометрия окон.

---

**Версия документа:** 2026-04-16 — подраздел «Источник правды сегодня и как сблизить с CDS» (проекция из VM, инкрементальное улучшение). Ранее: 2026-04-15 — v0.3: список `instruments` в CDS-снимке + реестр монтирования в Surface-слое.
