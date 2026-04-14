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

---

## 3. Слои (не смешивать)

| Слой | Вопрос | Типичные источники в коде |
|------|--------|---------------------------|
| **CDS** | Какая **кабина** сейчас: пресет, зоны, окна, страница MFD | `presentation`, `PresentationParseResult`, флаги VM колонок/хоста, `CurrentSecondaryShellPage`, `AttentionLayoutSurfaceKind` |
| **Дерево UI** | **Что** нарисовано в виде контролов | `UiLayoutSnapshot`, MCP `ide_get_ui_layout` |
| **Каналы** | **Что** в полосах и списках | `Cockpit/Channels/**` (данные), `Cockpit/Composition/**` (порядок/разметка для VM), readiness и т.д. |
| **Avalonia (хост)** | Окна, фокус, ввод, DPI; хост тяжёлых контролов (редактор и т.д.); **не** канон смысла зон — см. [architecture-policy.md](../architecture-policy.md) (раздел «Avalonia и слой кабины») | `MainWindow`, `MfdHostWindow`, привязки VM; кастомная отрисовка — поверх хоста |


---

## 4. Черновик полей v0 (эволюция)

Поля **не** обязаны совпадать с текущими именами свойств VM — это целевой контракт для стабилизации.

```json
{
  "schema_version": "0.2",
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
  }
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
| **Сейчас** | DTO `CockpitSurfaceState` и вложенные записи в `Cockpit/Cds/CockpitSurfaceState.cs`; сборка — `Cockpit/Cds/CockpitSurfaceSnapshotBuilder.Build(MainWindowViewModel)`; точка входа на VM — `MainWindowViewModel.BuildCockpitSurfaceSnapshot()` (см. также `EffectivePresentationLine`, `IsMfdHostWindowShellOpen`, `IsForwardZoneVisible`). |
| **Дальше** | Проброс в MCP (`ide_get_cockpit_state` или расширение существующего инструмента) и стабилизация полей по обратной связи агента/тестов. |

---

## 7. Связанные файлы кода (ориентиры)

- `Cockpit/Cds/CockpitSurfaceState.cs`, `Cockpit/Cds/CockpitSurfaceSnapshotBuilder.cs` — CDS (семантика кабины); `Cockpit/Cds/AttentionLayoutSurfaceKind.cs` — вид топологии в снимке.
- `Cockpit/Surface/UiLayoutSnapshot.cs` — дерево UI (другой слой, ADR 0036 п.4).
- Каналы и композиторы по ADR 0036 — `Cockpit/Channels/**`, `Cockpit/Composition/**`.
- `Services/Presentation/PresentationParser.cs`, `PresentationLayoutAnalyzer.cs` — презентация.
- `Views/MainWindow.axaml`, `MainWindow.MfdHostWindow.axaml.cs` — геометрия окон.

---

**Версия документа:** 2026-04-12 — v0.1 первичный текст.
