# Нотация записи аккордов и последовательностей (рекомендация для CascadeIDE)

Документ задаёт **единый способ писать** комбинации клавиш в текстах проекта: ADR, комментарии к `hotkeys.toml`, подсказки в UI, MCP-описания команд. Стиль близок к **Vim** (`:help key-notation`), но подстроен под **FMS-мышление**, **CascadeChord** и фактический формат жестов в конфиге ([ADR 0060](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md), [ADR 0030](adr/0030-command-ids-hotkeys-and-ui-registry-layers.md)).

**Реализация разбора:** `Services/ChordNotation/` — см. слои ниже; Vim-EBNF в `ChordNotationGrammar.cs` ([Eto.Parse](https://www.nuget.org/packages/Eto.Parse)).

**Философия читаемости:** нотация — для людей; где уместно, рядом с цепочкой шагов даётся **мнемоника намерений** (см. [ADR 0060 §10](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md#adr0060-p10) — невидимый инструмент, overlay как суфлёр; **три входа** команд: палитра, аккорд, слэш в Intercom — [philosophy §7](design/cascadeide-philosophy-v1.md)). Конкретные буквы и количество шагов зависят от версии грамматики и `hotkeys.toml`; смысл §10 не привязан к одной таблице.

**Command Melody в палитре:** префикс строки поиска **`c:`** — отдельный текстовый namespace ([ADR 0060 §11](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md#adr0060-p11)), не путать с Vim-нотацией `<C-…>`: `c:` не описывает физические клавиши, а задаёт **alias** к тому же `command_id`, что и аккорд (`Ctrl+K` + буквы семейства/действия). Формальное описание языка и раздел для обсуждения эволюции — [intent-melody-language-v1.md](intent-melody-language-v1.md).

---

## Слои: синтаксис, семантика, рендер

1. **Синтаксис (несколько поверхностей)** — разные **текстовые формы** для людей и доков, без прошивки платформы в одну грамматику:
   - **Vim-стиль** с угловыми скобками: `<C-k> s p` — разбор `ChordNotationParser` + EBNF ниже.
   - **UI / hotkeys-стиль**: `Ctrl+K`, `Ctrl + Shift + P`, `⌘K`, `Ctrl+K Ctrl+C` — разбор `KeyGestureChordSyntax` (шаги разделяются пробелами; внутри шага — `+` между частями; **пробелы вокруг `+` схлопываются**, чтобы `Ctrl + Shift + P` был одним аккордом, а не пятью токенами).

2. **Семантика (единая модель)** — `NormalizedKeySequence`: список шагов `NormalizedChordStep(ChordModifierKeys, KeySymbol)` и `NormalizedPlainKeyStep(KeySymbol)`. Строится через `ChordSemanticNormalizer.FromVimSteps(…)` или напрямую из KeyGesture-синтаксиса.

3. **Рендер (отображение)** — `IChordNotationRenderer`: **`ChordNotationRenderer.Windows`** (`Ctrl+…+Win+K`) и **`ChordNotationRenderer.MacSymbols`** (глифы ⌃⌥⇧⌘ + ключ); один аккорд без последовательности — `ChordNotationRenderer.FormatChord(…)`.

4. **Рендер «клавишами» (кэпы)** — третий вид: **`ChordKeycapLayoutBuilder.Build`** / **`ChordNotationRenderer.BuildKeycapLayout`** → `ChordKeycapSequence` (шаги → сегменты с подписью). В UI — **`ChordKeycapStrip`**: отдельные скруглённые плашки на модификатор и на клавишу, шаги в ряд с отступом (как на подсказках в IDE/ОС). Подписи: `ChordKeycapLabelFlavor.WindowsWords` или `MacGlyphs`. Платформенный вид не смешивается с парсером и с `hotkeys.toml`.

Соответствие `hotkeys.toml` / `KeyGesture.Parse` остаётся **отдельным шагом** (строка вида `Ctrl+K` из модели или из конфига), чтобы не смешивать Avalonia с документной нотацией.

---

## EBNF (канон для кода и доков; синтаксис Vim-стиля)

Нотация в духе W3C/ISO: `=` правило, `|` альтернатива, `{` `}` ноль или более повторов, `"…"` литерал, `(* … *)` комментарий.

```ebnf
(* корневая последовательность: шаги через пробельные разделители *)
chord_sequence ::= ws_opt step { step_sep step } ws_opt ;

ws_opt           ::= { whitespace } ;
step_sep         ::= whitespace { whitespace } ;
whitespace       ::= (* любой пробельный символ по правилам Eto.Parse Terminals.WhiteSpace *)

step             ::= bracket_chord | plain_token ;

bracket_chord    ::= '<' bracket_inner '>' ;
bracket_inner    ::= { modifier_prefix } key ;

modifier_prefix  ::= "Alt-" | "C-" | "M-" | "A-" | "S-" | "D-" ;

(* ключ: непустая цепочка букв/цифр — односимвольная k или имя F1, Esc, Tab, … *)
plain_token      ::= key ;
key              ::= letter_or_digit { letter_or_digit } ;
letter_or_digit  ::= (* Eto.Parse.Terminals.LetterOrDigit *)
```

**Ограничения v1:** между шагами нужен **пробел** (например `<C-k> m`, не `<C-k>m`). Внутри `<…>` пробелы не допускаются. Токены FMS без угловых скобок (`L1`, `EXEC`) попадают в то же правило `key`, что и plain-шаги.

---

## Таблица нотации

| Что записываем | Нотация | Пример |
|----------------|---------|--------|
| Одновременное нажатие (modifier + key) | `<C-x>`, `<M-x>`, `<S-x>`, `<D-x>` | `<C-k>` = Ctrl+K, `<M-s>` = Alt+S (если нужна буква в разборе) |
| Одновременное нажатие с несколькими модификаторами | `<C-M-x>`, `<C-A-x>` или явно `<C-Alt-x>` | `<C-Alt-n>` = Ctrl+Alt+N (то же, что `<C-M-n>`) |
| Последовательность шагов (не одновременно) | шаги через **пробел** | `<C-k> <C-s>` — сначала Ctrl+K, отпустить, затем Ctrl+S |
| Клавиша без модификаторов в роли шага | как символ или имя | `m`, `s`, `F1`, `Enter`, `Esc` — **без** угловых скобок |
| Имена клавиш, которые иначе неоднозначны | `Space`, `Tab`, `Up`, `Down` | по аналогии с Vim |
| FMS / «кнопки на MFD» (сценарный слой, не Win32-VK) | именованные линии/кнопки | `L1`, `R2`, `EXEC` — в доках про аппаратный/сюжетный FMS, не смешивать с `<C-…>` без пояснения |

**Модификаторы в угловых скобках (кратко):**

| Префикс в `<…>` | Значение |
|-----------------|----------|
| `C-` | Control |
| `M-` | Meta / Alt |
| `S-` | Shift |
| `D-` | Super / Win |

---

## Два разных смысла «аккорда»

1. **В конфиге и коде IDE** термин **CascadeChord** ([ADR 0060](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md)) — это **последовательность**: корневой жест из `hotkeys.toml` (по умолчанию `Ctrl+K`), затем одна или несколько **обычных** клавиш **без** Ctrl/Alt в текущей реализации. В нотации документации это удобно писать так:  
   `<C-k> m m` — префикс, затем `m` (вход в зоны кокпита), затем `m` (MFD).  
   Пробел между шагами обязателен, если хотя бы один шаг — это `<…>` или именованная клавиша из нескольких букв.

2. **Угловые скобки `<C-k>`** здесь означают именно **одновременное** удержание модификаторов с клавишей, а не «имя команды Vim».

3. **Палитра, melody (`c:`)** — строка в поле command palette: `c:` + mnemonic (например `c: gs`). Это **не** шаг клавиатурной последовательности и **не** подмножество EBNF выше; см. [ADR 0060 §11](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md#adr0060-p11). Тот же смысл может дублироваться аккордом вида `<C-k> g s` при intent-first грамматике.

---

## Соответствие `hotkeys.toml`

В файлах конфигурации по-прежнему используется разбор жестов в стиле **KeyGesture** (как в поставке: `Ctrl+K`, `Ctrl+Q`). Табличная нотация `<C-k>` — **для человекочитаемых текстов**; при переносе в TOML запись остаётся `Ctrl+K`, не `<C-k>`.

---

## Примеры для текущей машины CascadeChord

| Действие (смысл) | В документации |
|------------------|----------------|
| Semantic Map: сменить вид | `<C-k> s p` |
| Semantic Map: уровень / детализация | `<C-k> s f`, `<C-k> s d` |
| Зоны кокпита: MFD / PFD / Forward | `<C-k> m m`, `<C-k> m p`, `<C-k> m f` |
| Отмена ожидания шага | `Esc` |

---

## FMS-стиль (MFD)

Обозначения `L1`, `R2`, `EXEC` оставляем для сценариев, где важна **аналогия с реальным CDU/FMS** (левый/правый ряд, исполнение). Их **не** подставляют вместо `<C-…>` в описании хоткеев IDE, если только явно не делается связка «экранный тренажёр ↔ жест в CascadeIDE».

---

## Ссылки

- [ADR 0060 — аккордный слой, таймаут, overlay](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md)
- [ADR 0030 — command_id, hotkeys, реестр](adr/0030-command-ids-hotkeys-and-ui-registry-layers.md)
- [Eto.Parse](https://github.com/picoe/Eto.Parse) — парсер, fluent API и EBNF-грамматики
- Vim: `:help key-notation` (ориентир по угловым скобкам и модификаторам)
