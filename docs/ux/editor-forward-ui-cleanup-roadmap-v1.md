# Forward-редактор — roadmap полировки UI (v1)

**Статус:** чертеж / roadmap-компаньон к [ADR 0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md)  
**Дата:** 2026-04-26

**Связь:** [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) §9, [0085](../adr/0085-editor-hud-inline-layer-and-hud-banner.md), [0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md), [architecture-migration.md](../architecture-migration.md), [editor-surface-candidates-comparison-v1](../design/editor-surface-candidates-comparison-v1.md), [inline HUD inventory](../design/editor-hud-inline-migration-inventory-v1.md), [banner vs inline policy](../design/editor-hud-banner-inline-policy-v1.md)

**Цель:** визуально **согласованная** зона Forward (хром документа, **HUD banner**, **inline** Editor HUD, всплывающие подсказки) и **согласованные** с ней поверхности MFD/Problems и т.п., не смешивая язык **deck** кокпита с **presentation**-оболочкой IDE [0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md).

---

## Roadmap (в порядке strangler)

**Чекпойнт (2026-04-26):** инвентаризация `DockDocumentView` + граница DAL/презентации — [editor-hud-inline-migration-inventory-v1](../design/editor-hud-inline-migration-inventory-v1.md); политика дублирования баннер/inline — [editor-hud-banner-inline-policy-v1](../design/editor-hud-banner-inline-policy-v1.md); hover tooltip вынесен в `EditorInlineHoverToolTipController` (`Features/Editor/.../Presentation`).

1. **Согласовать с [architecture-migration.md](../architecture-migration.md):** при выносе editor HUD **не** раздувать DAL внутри view models. LSP/файлы/настройки → DAL; снимки для UI → CCU или `Features/*/Application` + тонкий оркестратор.

2. **Общие семантические презентеры** из `DockDocumentView` и связанных VM: единый паттерн для `hover`, модели презентации `diagnostics`, `inline` hints, присутствия `agent` (вход **стабилизованный** из пайплайна 0103, не сырые события).

3. **Разделить данные и рендеринг:**
   - **Данные:** `WorkspaceDiagnostics`, полезные нагрузки LSP/Roslyn, граф навигации, будущие code actions.
   - **Презентация:** **banner** HUD, inline hint, gutter, tooltip/popover, семантические **chips** (маппинг только на границе, не в DAL).

4. **Иерархия на поверхности Forward:**
   - Унифицировать хром **документа**, зону **вкладок** header, полосу **banner** и отступы **редактора**.
   - Задать **приоритет** и **вес** для сигналов `error / warn / info / agent / semantic`, чтобы зона Forward не выглядела набором несвязанных оверлеев.

5. **MFD и Problems (и аналоги):** тот же **визуальный язык** (плотность, токены цвета, иконки), что и Forward, где показывается **тот же класс** диагностик — избегать «технодемо»-несогласованности между squiggles в редакторе и строками списка на MFD, **когда** оба на экране.

6. **Tooltip и popover:** единая модель взаимодействия: задержка, **указатель vs клавиатура** (доступность), снятие, без наложения на глобальные IDS [0079](../adr/0079-ide-display-system-ids-overlay-pipeline.md), кроме **намеренной** иерархии.

7. **После** вертикального среза [0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md): пройтись **токенами** отступов/типографики **одним** проходом (без дрейфа по фичам).

---

## Вне объёма (этот roadmap-док)

- Полный редизайн PFD **instrument deck** (см. ADR deck) — только **согласованность**, где Forward и MFD показывают **общие** семантические сигналы.
- Аудит строк i18n — отдельно [0033](../adr/0033-internationalization-resx-avalonia.md).

---

## Успех (UX)

- Пользователь читает проблемы **одного** файла без **конкурирующего** громкого хрома; inline и banner — **намеренные** дубли или дополнения по [0085](../adr/0085-editor-hud-inline-layer-and-hud-banner.md), а не случайность.
- **Semantic-first** [0098](../adr/0098-semantic-first-document-as-projection.md): MFD/PFD остаются убедительным домом **навигации**; редактор не становится единственной «занятой» поверхностью.
