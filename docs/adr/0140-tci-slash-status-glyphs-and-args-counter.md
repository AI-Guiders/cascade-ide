# ADR 0140: TCI — глифы статуса slash (✓ / ✕ / P(n)) и счётчик аргументов

**Статус:** Accepted (контракт глифов и отрисовка) · Proposed (счётчик `P(n)` в preview pipeline)  
**Дата:** 2026-05-23

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0138](0138-cockpit-command-line-and-parametric-ranges.md) | CCL, debounced slash-preview, Enter не блокируется |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Каталог slash, иерархический autocomplete |
| [0124](0124-slash-parametric-editor-line-commands.md) | Параметрические хвосты (`/editor line select …`) |
| [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) | Динамическое дополнение путей / тем |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | Attach-style chip: preview без блокировки |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | EICAS W/C/A — **отдельный** канал от TCI preview |

Playbook: [`playbook-tci-v1.md`](../design/playbook-tci-v1.md).

## Резюме

В полосах TCI (CCL, composer) slash-команда оборачивается в **pill** (`SkiaStatusChip` / `SkiaSlashCommandChip`). **Слева в pill** — компактный **глиф статуса** (не отдельный чекбокс снаружи поля). Семантика:

| Глиф | Смысл для оператора |
|------|---------------------|
| **✓** | Команда распознана, обязательные аргументы на месте — Enter без сюрприза |
| **✕** | Команды нет в каталоге, опечатка namespace/action, синтаксис parametric |
| **P** или **P(n)** | Команда известна, **ожидаются аргументы**; `n` — сколько ещё нужно ввести (опционально) |
| **A** | Синоним «args» в подсказке/tooltip (не обязателен в v1 UI; см. § Глифы) |

Preview **не блокирует** Enter ([0138](0138-cockpit-command-line-and-parametric-ranges.md), [0128](0128-intercom-attachment-anchors-and-code-references.md) §9.1). Полный текст — tooltip (`IntercomSlashPreviewToolTip` / `CommandLineSlashPreviewToolTip`).

## Контекст

### Что уже было (до ADR)

- `SlashCommandPreviewKind`: `Ok`, `Incomplete`, `Error`, `Hint`, `None`.
- Маппинг в chip: `Ok` → ✓, `Error` → ✕, `Incomplete` → ⚠, `Hint` → ℹ (`SkiaStatusChip.GlyphFor`).
- Pill вокруг **текста буфера** (строка с `/`), иконка **левее** начала slash-токена (`IconLeadingOverhang`).

### Проблемы

1. **Иконка не была видна:** pill рисовался с иконкой слева от `/`, но `ClipRect` обрезал область **левее** `textBounds` — глиф оказывался за клипом. **Исправлено:** clip расширяется на `SkiaStatusChip.IconLeadingOverhang` в `SkiaCommandLineStrip` и `SkiaComposerStrip`.
2. **⚠ для «ждём args»** плохо читается при пошаговом autocomplete: оператор ожидает **P** / **P(3)**, как в REPL.
3. **На полупути** (`/intercom mes`) preview часто даёт **✕** («нет команды»), хотя autocomplete ещё ведёт по ступеням — нужны правила «prefix match → Incomplete + P(n)», не Error.

## Решение

### 1. Контракт глифа (отдельно от severity)

Ввести **отображаемый глиф** как производное от preview-результата, не смешивая с EICAS:

```csharp
// Features/Chat/SlashCommandPreviewGlyph.cs (при имплементации)
public readonly record struct SlashCommandPreviewGlyph(
    string IconText,           // "✓", "✕", "P", "P(3)", "ℹ"
    SlashCommandPreviewKind Kind,
    int? ArgsRemaining = null);
```

`SlashCommandPreviewResult` расширить опциональным `Glyph` или вычислять через `SlashCommandPreviewGlyphResolver` из `(Kind, ArgsRemaining, CatalogDescriptor)`.

**Правило v1 (до счётчика):**

| `SlashCommandPreviewKind` | Глиф по умолчанию |
|---------------------------|-------------------|
| `Ok` | ✓ |
| `Error` | ✕ |
| `Incomplete` | **P** (заменить ⚠; tooltip сохраняет пояснение) |
| `Hint` | ℹ |
| `None` | (pill не рисуется) |

### 2. Счётчик `P(n)` (фаза B — Proposed)

Когда каталог / parser знает **число обязательных хвостовых аргументов**:

| Состояние | Глиф | Пример |
|-----------|------|--------|
| Команда без хвоста, всё введено | ✓ | `/intercom message anchors list` |
| Нужен 1 хвост, пусто | P(1) | `/intercom message select` |
| Нужно 3 сегмента range, введён 1 | P(2) | `/editor line select [3;5]` + ещё сегменты |
| Неизвестная команда | ✕ | `/intercom mesage` (нет в каталоге) |
| Префикс совпал с catalog, action не допечатан | P | `/intercom mes` + autocomplete |

Источники `n`:

- Статические дескрипторы `ChatSlashCommandCatalog` / `IntentSlashCatalog` (поля `MinArgCount`, `ParametricKind`).
- `ParametricSegmentListParser` — для `[L;R]` ожидаемый минимум сегментов.
- Dynamic completion ([0125](0125-slash-workspace-file-commands-and-dynamic-completion.md)) — `n = 1` пока путь/тема не выбраны.

**Не цель v1:** точный подсчёт optional args; только **обязательный** минимум для «зелёного ✓».

### 3. Отрисовка (Skia)

- Примитив: `SkiaStatusChip.DrawIconGlyph` — уже поддерживает произвольную строку.
- `SkiaSlashCommandChip.Draw` принимает `SlashCommandPreviewGlyph` (или `iconText`) вместо только `SlashCommandPreviewKind`.
- **Инвариант клипа:** при `ShouldDrawChip` clip-rect слева расширяется на `IconLeadingOverhang`; при смене глифа на `P(3)` учитывать **ширину** глифа (моно, 2–4 символа) — при необходимости увеличить `IconBox` или рисовать глиф без отдельного квадрата иконки.

### 4. A11y

- Tooltip: `SlashCommandPreviewAccessibility.FormatToolTip` дополняется: «Ожидается ещё 2 аргумента» при `P(2)`.
- Severity для цвета pill **без изменений**: `Incomplete` → Warning (янтарь), `Ok` → Success, `Error` → Error.

### 5. Preview rules (фаза B)

Добавить / уточнить в `SlashCommandPreviewRulePipeline`:

- **Prefix catalog match** → `Incomplete` + `P`, не `Error`, пока `ChatSlashAutocomplete` ещё предлагает ступени.
- **Unknown only** когда нет ни одного подходящего route и нет suggestions.

## Последствия

| Область | Действие |
|---------|----------|
| `SkiaStatusChip` | `GlyphFor(Incomplete)` → `P`; опционально `GlyphFor(SlashCommandPreviewGlyph)` |
| `SlashCommandPreviewRuleHelpers` | Возвращать `ArgsRemaining` для parametric / catalog |
| `ChatSlashCommandCatalog` | Метаданные min args (постепенно) |
| Тесты | `SlashCommandPreviewGlyphResolverTests`, snapshot glyph strings |
| Доки | `playbook-tci-v1.md` § Preview severity |

## Не делаем

- Блокировка Enter при ✕ или P(n).
- Дублирование статуса в EICAS / `EicasAlertsBar`.
- Отдельная строка под полем (pill-only, [playbook-tci-v1](../design/playbook-tci-v1.md)).

## Состояние имплементации

| Элемент | Статус |
|---------|--------|
| Pill + ✓/✕/⚠/ℹ по `SlashCommandPreviewKind` | **Implemented** |
| Clip для иконки слева (`IconLeadingOverhang`) | **Implemented** (2026-05-23) |
| Глиф **P** вместо ⚠ для `Incomplete` | **Proposed** |
| Счётчик **P(n)** + prefix→Incomplete | **Proposed** |
| Метаданные args в каталоге | **Proposed** |

## Критерии приёмки (фаза B)

1. `/intercom message select` без хвоста → pill **P(1)** (или P), янтарь, tooltip с пояснением.
2. Полный валидный хвост → **✓**.
3. `/nope` → **✕**.
4. `/intercom mes` при открытом autocomplete по ступеням → **P**, не ✕.
5. CCL и composer: глиф **виден слева** в pill при горизонтальном scroll.

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-23 | Accepted: контракт глифов; clip fix; P(n) — Proposed |
