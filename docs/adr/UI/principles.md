# UI — принципы (карта канона)

**Входной связный текст** — [0076](../0076-ui-ux-principles-hub.md) (канон абзацев в [`../snippets/ui/`](../snippets/ui/README.md)). Здесь — **расширенная карта**: таблицы «идея → ADR» без дублирования полных нормативов. Плоский индекс — [../README.md](../README.md); этот файл — [README.md](README.md).

---

## Модель внимания и зоны (PFD / MFD / Forward / EICAS)

| Идея | Где зафиксировано |
|------|-------------------|
| Вторичный контур **MFD**, лобовой **Forward**, полоска **PFD**; превью и вспомогательные поверхности не конкурируют с набором текста как первичным действием | [0021](../0021-pfd-mfd-cockpit-attention-model.md) |
| **Cockpit UI** (приборы, deck, зоны) отдельно от **presentation IDE** (хром, тема, оверлеи) — не смешивать слои | [0066](../0066-cockpit-ui-vs-ide-presentation-layer.md) |

---

## Полезная нагрузка строки vs проекция (раскладка)

| Идея | Где зафиксировано |
|------|-------------------|
| Один **payload** (порядок строк, идентичность данных); смена **карточки / таблицы / плотности** — проекция во View без смены семантики строк | [0068](../0068-deck-row-payload-and-presentation-projection.md) |
| Декларативные слоты, instrument deck, таксономия примитивов — см. также | [0063](../0063-instrument-deck-named-composition-one-anchor.md), [0064](../0064-deck-primitives-visual-language-render-layer-and-palette.md) |

---

## Поверхность команд, `command_id`, палитра

| Идея | Где зафиксировано |
|------|-------------------|
| Палитра, discoverability, минимальный toolbar как направление | [0013](../0013-command-surface-and-discoverability.md) |
| Слои `command_id`, хоткеев, реестра UI | [0030](../0030-command-ids-hotkeys-and-ui-registry-layers.md) |

---

## Keyboard-first, Command Melody (`c:`), чат

| Идея | Где зафиксировано |
|------|-------------------|
| Аккордный слой, FMS-style, S/T; расширение поверхности команд | [0060](../0060-keyboard-chord-stack-fms-tactical-strategic.md) |
| Чат: topic cards, drill-in; Melody/Chords в **chat-domain** | [0072](../0072-chat-topic-cards-intent-melody-keyboard-contract.md) |
| Агент: текст правок в редакторе; чат не основной дифф; присутствие (курсор, «пишет») | [0084](../0084-agent-edits-editor-source-of-truth-presence-channel.md) |
| IML как язык намерений — вне ADR | [../../intent-melody-language-v1.md](../../intent-melody-language-v1.md) |

---

## Markdown preview, MFD tool surface

| Идея | Где зафиксировано |
|------|-------------------|
| Preview как tool surface, renderer/placement decoupling | [0069](../0069-markdown-preview-tool-surface-and-renderer-decoupling.md) |
| Размещение превью (`workspace.toml`) — исторический [0026](../0026-markdown-preview-surfaces-and-placement.md) (superseded по размещению каноном 0069) |

---

## Настройки, компактность, нехватка места на MFD

| Идея | Где зафиксировано |
|------|-------------------|
| Компактный режим, якорь на MFD, стратегии overflow | [0074](../0074-settings-ui-mfd-compact-layout-overflow.md) |

---

## Указатель UI-ADR и соглашения по страницам MFD

| Идея | Где зафиксировано |
|------|-------------------|
| Папка `UI/`, payload vs проекция, keyboard-first на страницах вторичного контура | [0075](../0075-ui-topic-index-and-mfd-page-conventions.md) |

---

## Сборка «только UI-ADR» в один HTML/PDF

Из `docs/adr`:

```bash
dotnet script build-adr.csx --book adr-book-ui.md
```

Выход: `build/adr-book-ui.md`, `out/html/adr-book-ui.html` и т.д. Подробности — [../build/README.md](../build/README.md).
