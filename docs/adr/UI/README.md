# UI — тематический указатель ADR

Здесь **нет отдельной нумерации**: нормативные решения по-прежнему лежат в родительском каталоге [`docs/adr/`](../README.md) как `NNNN-краткий-kebab-title.md`.

Эта папка — **оглавление по теме UI** (MFD, кокпит, палитра, keyboard-first): быстрые ссылки, чтобы не терять контекст между обсуждением в чате и файлом в репо.

**С чего начать (один связный текст):** [0076 — центр UI/UX-принципов](../0076-ui-ux-principles-hub.md) (канон формулировок в [`../snippets/ui/`](../snippets/ui/README.md)). **Карта ссылок** по темам (без длинного текста): [principles.md](principles.md). Сборка UI-ADR в один HTML/PDF: `dotnet script build-adr.csx --book adr-book-ui.md` — [../build/README.md](../build/README.md).

**Канонический индекс** всего набора ADR — в [../README.md](../README.md). Политика и таблица «решение → ADR» — в [../../architecture-policy.md](../../architecture-policy.md).

---

## Указатель по UI

| Тема | ADR |
|------|-----|
| **Центр UI/UX** — вводные принципы (кокпит, философия продукта); текст в [`snippets/ui/`](../snippets/ui/README.md) | [0076](../0076-ui-ux-principles-hub.md) |
| Модель внимания кокпита, якоря **PFD / Forward / MFD** | [0021](../0021-pfd-mfd-cockpit-attention-model.md) |
| **Cockpit UI** vs presentation IDE (хром, тема) | [0066](../0066-cockpit-ui-vs-ide-presentation-layer.md) |
| Полезная нагрузка строки vs **проекция представления** (таблица/список ≠ смена payload) | [0068](../0068-deck-row-payload-and-presentation-projection.md) |
| Палитра, **Command Melody (`c:`)**, аккорды — keyboard-first | [0060](../0060-keyboard-chord-stack-fms-tactical-strategic.md) |
| Чат: topic navigation, Melody/Chords **в chat-domain** | [0072](../0072-chat-topic-cards-intent-melody-keyboard-contract.md) |
| **Настройки:** компактность, якорь MFD, нехватка места | [0074](../0074-settings-ui-mfd-compact-layout-overflow.md) |
| **Тема «UI-ADR»:** указатель + соглашения по MFD-страницам | [0075](../0075-ui-topic-index-and-mfd-page-conventions.md) |
| **Принципы UI — карта канона** (таблицы «идея → ADR») | [principles.md](principles.md) |

---

## Связанные документы (не ADR)

- [../../intent-melody-language-v1.md](../../intent-melody-language-v1.md) — IML v1 (`c:`).
