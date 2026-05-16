# ADR 0070: Command Palette как прямой overlay surface, маршрутизируемый в активный TopLevel

**Статус:** Accepted  
**Дата:** 2026-04-19  

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0013](0013-command-surface-and-discoverability.md) | палитра и discoverability |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | несколько `TopLevel` и фокус |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | модель внимания |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | keyboard-first и overlay-подсказки |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | shell chrome vs cockpit UI |
---

## Контекст

Командная палитра уже была канонической surface в [0013](0013-command-surface-and-discoverability.md), но её конкретный render-host оставался незафиксированным. Практика показала, что композиция через общий `ModalOverlay` оказалась хрупкой для multi-window topology:

1. overlay мог не материализоваться в нужном `TopLevel`, хотя хоткей и состояние VM уже сработали;
2. при мультиоконности палитра могла визуально появляться не в том хосте или дублироваться;
3. keyboard-first UX ломался именно в критическом месте discoverability: у пользователя создавалось ощущение, что `Ctrl+Q` ничего не делает.

Проблема оказалась не в самой палитре как продуктовой идее, а в фундаменте surface: общий модальный compose-path был слишком неявным для keyboard-first overlay, который обязан быть привязан к активному окну и фокусу.

---

## Решение

<a id="adr0070-p1"></a>

### 1. Базовый surface палитры

Command Palette рендерится как **прямой overlay surface** внутри конкретного host view, а не как обязательная композиция через общий `ModalOverlay`.

Это означает:

- корневой визуал палитры сам содержит dimmer/panel;
- палитра сама управляет видимостью, фокусом и keyboard routing;
- `ModalOverlay` остаётся общей UI-инфраструктурой, но больше **не считается каноническим фундаментом** для палитры.

<a id="adr0070-p2"></a>

### 2. Маршрутизация в активный TopLevel

При открытии палитра должна быть видима **ровно в одном активном host window**:

- `MainWindow`
- `PfdHostWindow`
- `MfdHostWindow`

Источник истины для выбора host — состояние shell/VM, отражающее активный `TopLevel` и источник последнего keyboard entry. Overlay не должен “угадывать” по глобальному статическому состоянию вне текущего окна.

<a id="adr0070-p3"></a>

### 3. Keyboard-first инварианты

Для палитры обязательны следующие инварианты:

- открытие по хоткею должно работать из дочерних контролов и редактора;
- при открытии фокус переходит в строку поиска;
- при закрытии фокус возвращается в предыдущий элемент текущего host;
- `Esc`, `Enter`, `Up/Down`, `PageUp/PageDown` обрабатываются на уровне самой палитры;
- overlay не должен зависеть от того, есть ли в конкретной раскладке дополнительные chrome/composition wrappers.

<a id="adr0070-p4"></a>

### 4. Граница решения

Этот ADR фиксирует только baseline surface для **Command Palette**.

Он **не** означает, что:

- любой modal/dimmer в IDE должен быть переписан под ту же схему;
- `ModalOverlay` запрещён;
- overlay-подсказки chord-системы из [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) обязаны использовать тот же visual tree.

Но для палитры правило по умолчанию теперь однозначно: **прямой overlay в нужном host**.

---

## Последствия

- Поведение палитры становится предсказуемым в multi-window topology.
- Тесты должны страховать не только hotkey routing, но и host routing: палитра видна только в ожидаемом `TopLevel`.
- Документация по keyboard-first теперь может опираться на конкретный baseline, а не на “историческую” реализацию через `ModalOverlay`.

---

## Отклонённые альтернативы

1. **Оставить `ModalOverlay` каноном и чинить частные баги вокруг него.**  
   Отклонено: причина поломки не локальная, а архитектурная для multi-window keyboard-first surface.

2. **Глобальный singleton-overlay поверх всех окон.**  
   Отклонено: нарушает модель фокуса и внимания из [0017](0017-multi-window-workspace-and-agent-surfaces.md) и [0021](0021-pfd-mfd-cockpit-attention-model.md).

3. **Привязывать палитру только к `MainWindow`, а host-окна заставлять проксировать ввод назад.**  
   Отклонено: пользователь ожидает, что discoverability surface появится именно там, где сейчас работает.
