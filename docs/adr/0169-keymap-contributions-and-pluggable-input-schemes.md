# ADR 0169: Keymap contributions — плагинные схемы ввода (VS-style chords, packs)

**Статус:** Proposed  
**Дата:** 2026-06-28

## Резюме

Схемы клавиатурного ввода (плоские жесты, **chord trees** в духе Visual Studio `Ctrl+R` → `R`, корневой **CascadeChord** из [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md)) должны поставляться как **contributions**, а не разрастаться в `if (keymap == …)` внутри shell.

**Канон:**

1. Семантика действия — **`command_id`** ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)); keymap только маршрутизирует жест → `command_id` (+ опциональные args).
2. **Один активный input stack** в host: приоритеты, `when`-контекст, tunnel Avalonia + Monaco ([0163](0163-monaco-native-capability-bus-full-forward-migration.md)).
3. **v1 — code-first packs** в solution (`Keymap.Default`, `Keymap.VSRefactor`); **dynamic DLL** — после [0005](0005-defer-dynamic-plugins-mef.md) / [0024](0024-ide-sdk-and-stable-contracts.md), те же контракты.
4. **Modal editor personalities** (отдельные режимы редактора с собственной машиной ввода) — **вне scope** этого ADR; отдельное решение, если понадобится.

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0005](0005-defer-dynamic-plugins-mef.md) | dynamic plugin host deferred; packs сначала in-solution |
| [0013](0013-command-surface-and-discoverability.md) | палитра, discoverability, keyboard-first |
| [0024](0024-ide-sdk-and-stable-contracts.md) | SDK, capability registry, будущие плагины |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | слои `command_id` / hotkeys TOML / VM bridge |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | CascadeChord (`Ctrl+K` + melody tail); Command Melody `c:` |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | `intent-catalog.toml`, melody slugs |
| [0161](0161-cide-spine-and-forge-vertical-feature-module.md) | vertical feature modules; аналогия с Forge contributions |
| [0163](0163-monaco-native-capability-bus-full-forward-migration.md) | tunnel `host/shortcut`, editor focus |

### Снимок реализации (на момент ADR)

| Элемент | Файл / поведение |
|---------|------------------|
| Плоские жесты | `Hotkeys/hotkeys.toml` + user overlay → `HotkeyTomlLoader` |
| Tunnel + matching | `MainWindowHotkeyService`, `KeyGestureChordMatching` |
| CascadeChord | `CascadeChordIntentSession`, `cascade_chord` в TOML |
| Melody / slash | `IntentMelody/intent-catalog.toml` |
| Исполнение | `IdeMcp.ExecuteCommandAsync(command_id, args)` |

---

## Контекст

Пользователи и команды привыкли к разным **схемам ввода**. Visual Studio — **двухшаговые chords** с удержанием смысла namespace (`Ctrl+R`, `R` = Rename). CascadeIDE — **CascadeChord** + **Intent Melody** ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md), [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md)).

Сегодня жесты размазаны по:

- merged `hotkeys.toml` (плоские `KeyGesture`);
- одной машине `CascadeChordIntentSession` (один корень `cascade_chord`);
- Monaco bridge для фокуса в редакторе.

Добавление «VS refactor pack» или второго корня аккорда **в ядро** ведёт к дублированию tunnel-логики, конфликтам приоритетов и невозможности **включить схему** без пересборки IDE.

Цель — **плагинная (pack) модель**: host знает контракт; схема поставляет таблицы привязок. Примеры стилей: **Visual Studio** (chord tree `Ctrl+R` → `R`), **Cascade** (melody после `Ctrl+K`), будущие **pack’и** без правок shell.

<a id="adr0169-non-goals"></a>

### Вне scope (явно)

- **Modal editor** с отдельной машиной состояний внутри Monaco (условный «Vim-mode» и аналоги) — не keymap pack; требует editor-surface plugin и отдельного ADR.
- Замена палитры (**Ctrl+Q**) или слэша в Intercom — [0013](0013-command-surface-and-discoverability.md), [0119](0119-chat-slash-commands-intercom-surface.md).
- Новые `command_id` ради keymap — команды регистрируют **фичи**; pack только **привязывает** жесты.

---

## Решение

<a id="adr0169-p1"></a>

### 1. Атом действия — `command_id`

Любая привязка keymap **обязана** ссылаться на существующий `command_id` ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)) или на зарезервированный UI-only ключ с документированной семантикой (как `debug_start_or_continue`).

Pack **не** исполняет бизнес-логику; host вызывает тот же путь, что палитра и MCP:

`IInputDispatchHost.ExecuteCommandAsync(command_id, args?, cancellationToken)`.

<a id="adr0169-p2"></a>

### 2. Три класса привязок (единый реестр)

| Класс | Пример | Источник сегодня |
|-------|--------|------------------|
| **Flat** | `Ctrl+Shift+U` → `intercom.attach_selection` | `hotkeys.toml` |
| **Chord tree** | `Ctrl+R` → `R` → `roslyn.rename` | *новое* (VS-style) |
| **Melody chord** | `Ctrl+K` → `rn` → `…` | CascadeChord + `intent-catalog` |

**Правило:** все три класса регистрируются через **`IKeymapContribution`** в одном **input stack**, а не через независимые `KeyDown`-обработчики в VM.

Melody chord **не дублируется**: реализация [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) становится **встроенным contribution** `Keymap.Cascade` (default pack).

<a id="adr0169-p3"></a>

### 3. Контракт contribution (SDK / `CascadeIDE.Contracts`)

Минимальный v1 (имена ориентировочные):

```csharp
/// <summary>Одна поставляемая схема ввода (pack).</summary>
public interface IKeymapContribution
{
    string Id { get; }                    // "cascade", "vs-refactor", …
    int Priority { get; }                  // выше — раньше в stack
    IReadOnlyList<IKeyBinding> Bindings { get; }
}

public interface IKeyBinding
{
    string When { get; }                   // см. §4
    KeyBindingKind Kind { get; }           // Flat | ChordTree | MelodyRoot
    // Flat: KeyGesture
    // ChordTree: root gesture + ordered steps (без Ctrl на шагах 2..N)
    // MelodyRoot: root gesture → делегат в IMelodyChordEngine (CascadeChord)
    string CommandId { get; }
    string? ArgsJson { get; }
}
```

**Регистрация:** code-first при старте ([0024](0024-ide-sdk-and-stable-contracts.md)); позже — manifest в DLL pack ([0161](0161-cide-spine-and-forge-vertical-feature-module.md)).

**Experimental** до стабилизации tunnel + тестов; breaking changes без SemVer.

<a id="adr0169-p4"></a>

### 4. Контекст `when` (ограниченный v1)

Выражение **строковое**, без произвольного C# в pack:

| Предикат | Смысл |
|----------|--------|
| `always` | глобально (как сейчас window tunnel) |
| `editor.focus` | фокус в Forward / Monaco |
| `composer.focus` | фокус в Intercom composer |
| `palette.open` | открыта палитра |
| `chord.armed` | активна фаза chord-wait |
| `editor.lang:csharp` | язык текущего файла |
| `editor.selection` | ненулевое выделение |
| `editor.diagnostic_at_caret` | squiggle под кареткой |

Комбинации: `&&`, `!` (без скобочной адской вложенности в v1).

Host предоставляет **`IInputContextSnapshot`** на каждый `KeyDown` (фокус, файл, selection, armed chord id).

<a id="adr0169-p5"></a>

### 5. Input dispatch stack

Единая точка: **`IInputDispatchService.TryConsumeKeyDown(KeyEventArgs)`** (имя в коде может отличаться).

Порядок:

1. **Modal overlays** (палитра Esc, chord overlay) — как сейчас в tunnel.
2. **Active chord session** (если armed) — шаги 2..N chord tree / melody tail.
3. **Flat bindings** по приоритету contribution + специфичности `when`.
4. **Chord roots** (начало нового дерева).
5. **PassThrough** → редактор / composer.

**Monaco:** при `editor.focus` host по-прежнему может получать `host/shortcut` из bridge; pack может регистрировать **mirror** flat/chord roots для паритета с tunnel ([0163](0163-monaco-native-capability-bus-full-forward-migration.md)).

**Конфликты:** при равном приоритете — **более узкий `when`** побеждает; иначе — детерминированный порядок регистрации + запись в hotkey-log (как сегодня).

<a id="adr0169-p6"></a>

### 6. Данные pack (TOML) vs код

**Фаза 1:** bindings в C# (in-solution pack).  
**Фаза 2:** опциональный файл рядом с pack:

```toml
[keymap]
id = "vs-refactor"
priority = 100

[[chord]]
root = "Ctrl+R"
when = "editor.focus && editor.lang:csharp"

  [[chord.step]]
  keys = "R"
  command_id = "roslyn.rename"

  [[chord.step]]
  keys = "M"
  command_id = "roslyn.extract_method"
```

Пользовательский **`hotkeys.toml`** остаётся **оверлеем flat-жестов** ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)); **не** заменяет целый pack, но может переопределить конкретный `command_id` → gesture (как сейчас).

Настройка **`[input] active_keymap = "cascade"`** (или список enabled packs) — в `settings.toml` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)).

<a id="adr0169-p7"></a>

### 7. Discoverability

Pack **обязан** дублировать привязки в discoverability-слой:

- подсказки в overlay chord (как CascadeChord dropdown);
- опционально строки в `intent-catalog` / help (`/help keymap`);
- паритет с палитрой: каждая привязанная команда уже в `IdeCommandPaletteCatalog`, если команда discoverable.

Скрытые power-user жесты допустимы, но **документируются** в pack README.

<a id="adr0169-phases"></a>

### 8. Фазы внедрения

| Фаза | Содержание | Критерий готовности |
|------|------------|---------------------|
| **P0** | `IInputDispatchService` + вынести `CascadeChordIntentSession` за `IMelodyChordEngine`; flat — за `IKeymapResolver` | Существующие hotkeys + Ctrl+K без регрессий; headless-тесты stack |
| **P1** | In-solution `Keymap.Cascade` (default) + `Keymap.VSRefactor` (пример chord tree) | `Ctrl+R`,`R` вызывает stub/real `roslyn.rename` при `editor.focus` |
| **P2** | TOML в pack + `active_keymap` в settings | Переключение схемы без пересборки |
| **P3** | Dynamic plugin host загружает `IKeymapContribution` | Тот же контракт, что P1 ([0005](0005-defer-dynamic-plugins-mef.md)) |

**Не блокирует:** attach affordances ([0128](0128-intercom-attachment-anchors-and-code-references.md) §H0) — остаются flat bindings в default pack.

---

## Последствия

- Новый жест: добавить **`command_id`** и handler; в pack — binding; **не** писать отдельный `KeyDown` в `MainWindowViewModel`.
- [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) **уточняется**, не отменяется: CascadeChord = default melody contribution.
- [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md): `hotkeys.toml` = user-overridable **flat** слой; chord trees — в pack TOML/C#.
- Тесты: unit на stack + `when`; integration на tunnel + Monaco shortcut mirror.

## Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| Отдельный chord engine на каждый стиль (VS, Cascade, …) в VM | Дублирование tunnel, конфликты, нет переключения |
| Только расширить `hotkeys.toml` chord-деревьями | Нет `when`, нет приоритетов, смешение с user overlay |
| Modal editor в этом ADR | Другая поверхность и lifecycle ([§Вне scope](#adr0169-non-goals)) |
| MEF plugin host сейчас | [0005](0005-defer-dynamic-plugins-mef.md); packs in-solution достаточно для P0–P2 |

## Открытые вопросы

1. **Имя настройки:** один `active_keymap` vs stack нескольких packs (default + user overlay pack).
2. **Chord timeout:** глобальный (как 8 с у CascadeChord) vs per-tree в TOML.
3. **Discoverability VS chords:** буквы на пунктах меню Refactor (как VS) — UI forge или только overlay.
4. **Генерация** фрагментов `hotkeys.toml` из pack для подсказок палитры — в scope [ide-command-registry-v1](../design/ide-command-registry-v1.md) или отдельно.

---

## История изменений

| Дата | Изменение |
|------|-----------|
| 2026-06-28 | Proposed: keymap contributions, контракт, фазы P0–P3, вне scope modal editor |
