# ADR 0137: Intercom — соответствие диапазона сообщений (gutter) и якоря кода

**Статус:** Accepted · In progress  
**Дата:** 2026-05-20

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0136](0136-intercom-feed-gutter-and-slash-namespace.md) | Gutter 1-based в ветке; `/intercom message select`; MCP `ordinal` |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | `AttachmentAnchor`, reveal, re-resolve; attach ≠ editor line action |
| [0124](0124-slash-parametric-editor-line-commands.md) | Синтаксис `L`, `L R`, `L:R` для строк редактора |
| [0131](0131-editor-slash-select-code-by-bracket-reference.md) | Bracket → select/reveal в буфере |
| [0130](0130-editor-agent-range-reveal-without-selection.md) | Reveal диапазона без selection |
| [0133](0133-commander-cockpit-shared-attention-model-and-instrument-deck.md) | Общая модель внимания Intercom ↔ Editor |
| [0134](0134-intercom-message-prepare-pipeline-v1.md) | Wire `attachments[]` @ commit; проекция ленты |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Event log — source of truth; проекции derived |
| [0135](0135-intercom-attach-symbol-cache-and-hci-sidecar.md) | HCI `index_dir`; colocated SQLite sidecars |
| [0111](0111-editor-linenumber-linerange-value-objects.md) | `LineRange` в домене |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Forward: Intercom или Editor |

## Резюме

**Мост между двумя шкалами номеров** (traceability Intercom ↔ Editor):

| Шкала | Где | Смысл |
|-------|-----|--------|
| **Message ordinal** | Gutter detail-ленты | 1-based в **активной ветке** ([0136](0136-intercom-feed-gutter-and-slash-namespace.md)) |
| **Code ref** | Редактор / attach | `AttachmentAnchor` + `(file, LineRange)` ([0128](0128-intercom-attachment-anchors-and-code-references.md)) |

- **По умолчанию:** авто-infer из `attachments[]` каждого сообщения (экономия внимания IOP-оператора).
- **Явный relate:** только когда **диапазон сообщений** (#3–#5) связывается с кодом, хотя attach есть не в каждом сообщении диапазона.
- **Find:** обратный запрос из редактора → gutter-номера в ветке.
- **Хранение:** explicit relate → **событие event log**; быстрый поиск → **derived index** (SQLite sidecar рядом с HCI, пересборка при re-resolve drift).

**Не цель:** граф всех связей; разрозненные ordinals `#3` + `#7` в MVP; отдельные `c:…` в IntentMelody.

---

## Контекст

`/intercom message select 3:5` — выбор **строк ленты**. Correspondence — «#3–#5 **относятся к** этому коду» и «строки 42–50 в файле → какие сообщения в ветке».

Без модели остаётся только forward navigation (chip → reveal). Commander в кокпите ([0133](0133-commander-cockpit-shared-attention-model-and-instrument-deck.md)) теряет сквозную прослеживаемость миссии ([0045](0045-agent-chat-persistence-event-log-and-projections.md)).

---

## Принятые решения (закрытие Q1–Q5)

| # | Вопрос | Решение |
|---|--------|---------|
| **Q1** | Ручной relate vs авто-infer? | **100% авто-infer по умолчанию** из `attachments[]` @ send ([0134](0134-intercom-message-prepare-pipeline-v1.md)). **Ручной relate** — оверрайд/дополнение для **contiguous message range**, когда контекстный блок (#3 интент, #4 лог, #5 аппрув) относится к одной задаче, а attach только в #4. |
| **Q2** | Contiguous vs разрозненные? | **MVP: только contiguous** (`n`, `n m`, `n:m`) — тот же синтаксис, что [0136](0136-intercom-feed-gutter-and-slash-namespace.md). Разрозненные `#3` + `#7` — не MVP (другой интент или под-тред). |
| **Q3** | Event log vs sidecar vs index? | **Source of truth:** append-only **event log** ([0045](0045-agent-chat-persistence-event-log-and-projections.md)) — событие `message_range_related` с полным **`AttachmentAnchor`** ([0128](0128-intercom-attachment-anchors-and-code-references.md)), не с «голыми» номерами строк. **Derived:** SQLite sidecar @ HCI ([0135](0135-intercom-attach-symbol-cache-and-hci-sidecar.md)) — ускоритель: `(thread_id, file, member_key?)` и опционально line-overlap после **re-resolve**; пересборка при load/reindex/drift. |
| **Q4** | IntentMelody `c:…`? | **Нет.** Только **slash + MCP**; Melody оркеструет, не задаёт синтаксис связей ([0121](0121-intent-oriented-programming-paradigm.md)). |
| **Q5** | Relate на draft? | **Нет в v1** — только committed messages (как wire attach). |
| **Q6** | Cross-file find? | **Да** — по всем attachments + relate-событиям **ветки**. |
| **Q7** | Federated ([0132](0132-intercom-federated-transport-and-multi-client-boundary.md)) | Wire attachments общие; relate-события в логе сессии — общие; derived index — локальный кэш клиента. |

---

## Решение

### 1. Термины

| Термин | Значение |
|--------|----------|
| **Message range** | Contiguous gutter ordinals в активной ветке |
| **Code ref** | `AttachmentAnchor` / `(file, LineRange)` — канон [0128](0128-intercom-attachment-anchors-and-code-references.md) |
| **Inferred** | Автоматически из `attachments[]` сообщения |
| **Explicit** | Событие `message_range_related` в NDJSON |

### 1.1 Идентичность кода (не строки)

**Номера строк — не ID** (тот же принцип, что [0128 §3](0128-intercom-attachment-anchors-and-code-references.md)): drift ветки/редактора ломает привязку «L42 навсегда».

| Слой | Что хранится / сравнивается |
|------|-----------------------------|
| **Relate (explicit)** | `code_ref`: полный `AttachmentAnchor` @ declare (`memberKey`, `syntaxScope`, `attachmentShape`, `excerpt`, снимок `lineStart`/`lineEnd` — **hint**) |
| **Inferred** | Из `attachments[]` каждого сообщения — те же поля якоря |
| **Find / relate (ввод)** | `selection` / `[M:…]` / `anchor:<id>` → resolve → якорь; `L:10-20` — только **M0** positional fallback в открытом файле (как slash editor line), не канон для event log |
| **Match** | 1) тот же `file` + `memberKey` (если есть у обеих сторон); 2) иначе overlap **re-resolved** `LineRange`; 3) `excerpt` — для stale UI, не для ключа индекса |

**Запрещено в v1:** событие или sidecar-запись вида «только `(file, line)` без shape/member» как единственная правда связи.

### 2. Команды (ортогональны `select`)

| Режим | Slash | MCP |
|-------|-------|-----|
| Select | `/intercom message select <range>` | `chat_select_message` (`ordinal`) |
| Find | `/intercom message find <code-ref>` | `intercom.messages_for_code` |
| Relate | `/intercom message <range> relate <code-ref>` | `intercom.message_relate` |

**Code-ref tail:** `selection` · `L:10-20` · `[M:…]` / bracket · `anchor:<id>` — как §3 [0128](0128-intercom-attachment-anchors-and-code-references.md).

### 3. Event log (explicit relate)

Новый тип события v1 (расширение [0045](0045-agent-chat-persistence-event-log-and-projections.md)):

```text
message_range_related
```

Payload (schema_version в событии):

| Поле | Тип | Смысл |
|------|-----|--------|
| `thread_id` | string | Ветка |
| `start_ordinal` | int | 1-based начало (gutter) |
| `end_ordinal` | int | 1-based конец |
| `code_ref` | object | Сериализованный `AttachmentAnchor` @ declare time — **канон**; поля `lineStart`/`lineEnd` внутри него не дублировать отдельным top-level `line_range` |
| `source` | string | `slash` \| `mcp` \| `agent` |

Проектор истории **не переписывает** `message_added`; relate — отдельная строка NDJSON (как `message_edited`). При replay explicit relate маппится на ordinals диапазона **по смыслу якоря** (member/syntax), а не по равенству номеров строк в NDJSON.

### 4. Derived index (HCI colocation)

| Аспект | Правило |
|--------|---------|
| Файл | `{hci_index_dir}/intercom-message-code.sqlite` (рядом с symbol sidecar [0135](0135-intercom-attach-symbol-cache-and-hci-sidecar.md)) |
| Построение | Replay event log ветки + текущие `ChatMessages` attachments; explicit — из `message_range_related.code_ref` |
| Ключ запроса (кэш) | `(thread_id, file, member_key?)`; line-bucket — **вторичный** индекс после re-resolve для overlap и editor HUD |
| Значение | `message_id`, `ordinal`, `match_kind`: `inferred` \| `explicit` |
| Drift | Re-resolve якоря → обновление line-bucket; `stale` в find-ответе при `file_missing` / расхождении excerpt |

Inferred строится **без** отдельного события на каждое сообщение — из wire `attachments[]` при проекции.

### 5. Find / overlap

Сообщение попадает в результат, если якорь сообщения **совпадает по смыслу** с запросом: тот же `memberKey` на том же `file`, иначе overlap **текущего** re-resolved `LineRange` (≥1 строка). Explicit relate разворачивается на все ordinals в `[start..end]` с **одним** `code_ref` (якорь), не с отдельной таблицей строк.

### 6. UI

#### 6.1 Intercom gutter (фаза 2+)

Тонкий индикатор на строках R при explicit relate; inferred — опционально слабее.

#### 6.2 Editor gutter — «HUD traceability» (фаза 3)

При открытом файле IDE запрашивает derived index: *какие сообщения этой ветки связаны с файлом?*

| Элемент | Поведение |
|---------|-----------|
| Маркер у строк L | Неброская точка / мини-ordinal `[3]` на полях редактора |
| Клик по маркеру | Scroll Intercom к сообщению #3 + рамка выбора ([0136](0136-intercom-feed-gutter-and-slash-namespace.md)) |
| Shift+клик | Опционально reveal range в редакторе |

ЛКМ по телу сообщения в ленте — без изменения selection ([0136](0136-intercom-feed-gutter-and-slash-namespace.md)).

---

## Фазы внедрения

### Фаза 1 — Inferred index + Find (MVP)

**Интент:** обратный индекс без event log relate.

| Шаг | Артефакт |
|-----|----------|
| 1 | `IntercomMessageCodeCorrespondenceProjector` — из ветки: ordinal ↔ message ↔ `AttachmentAnchor` (memberKey + line hint) |
| 2 | `IntercomMessageCodeIndex` — SQLite sidecar @ HCI `index_dir`; `Rebuild(threadId)`, `Query(file, LineRange)` |
| 3 | `ChatPanelViewModel.FindMessagesForCodeRef(codeRef)` → ordinals + select лучший |
| 4 | Slash `/intercom message find` + `IntercomSlashPathBuilder` + handler |
| 5 | MCP `intercom.messages_for_code` |
| 6 | Unit-тесты: overlap, пустой selection, несколько hits |

**Критерий:** `find selection` в detail-ветке с attach на текущий файл возвращает корректные `#n`.

### Фаза 2 — Explicit relate (event log)

| Шаг | Артефакт |
|-----|----------|
| 1 | `ChatHistoryEventKind.MessageRangeRelated` + payload record |
| 2 | `PersistEventAsync` из slash/MCP relate |
| 3 | Проектор лога → дополнение derived index (`match_kind: explicit`) |
| 4 | Slash `message <range> relate …` + MCP `intercom.message_relate` |
| 5 | Gutter Intercom: индикатор explicit |

### Фаза 3 — Editor gutter markers

Маркеры на line gutter редактора + click → scroll Intercom (§6.2).

---

## Следующий интент в кодовой базе (фаза 1)

**Один вертикальный срез** — без relate-событий, без editor gutter:

```
Services/Intercom/IntercomMessageCodeIndex.cs          # SQLite + Query/Rebuild
Services/Intercom/IntercomMessageCodeIndexCoordinator.cs  # hook после message_completed / thread open
Features/Chat/IntercomMessageCodeCorrespondenceProjector.cs  # pure: messages → entries
Features/Chat/ChatPanelViewModel.IntercomCorrespondence.cs    # FindMessagesForCodeRef
Features/Chat/ChatSlashIntercomHandlers.cs                   # message_find
Features/Chat/IntercomSlashPathBuilder.cs                    # path /intercom message find
IntentMelody/intent-catalog.toml                             # entry + help
Services/IdeCommands.Intercom.cs                             # intercom.messages_for_code
```

**Порядок работ:** projector + unit-тесты overlap → in-memory index (без SQLite) → find slash → SQLite colocation → MCP.

Это минимальный «обратный индекс», на который ляжет и HUD редактора (фаза 3).

---

## Последствия

- [0136](0136-intercom-feed-gutter-and-slash-namespace.md): help — раздел find/relate; координаты ordinal.
- [0045](0045-agent-chat-persistence-event-log-and-projections.md): новый kind `message_range_related` (фаза 2).
- [0135](0135-intercom-attach-symbol-cache-and-hci-sidecar.md): ещё один sidecar в `index_dir`, не путать с symbol L2.
- Тесты: парсер `find selection`; index; MCP JSON schema.

## Критерии приёмки

### Фаза 1

1. Attach на `Foo.cs` L10–20 → `find L:10-20` (файл открыт) → ordinals включают сообщение с attach.
2. `find selection` без selection — явная ошибка.
3. Документация: `Intercom/intercom-help.ru.md`, `MCP-PROTOCOL.md`.

### Фаза 2

4. `message 3:5 relate selection` → строка в `*.events.ndjson`; find учитывает #3–#5.

### Фаза 3

5. Маркер у строки редактора → scroll + select сообщения в Intercom.

---

## Статус реализации

| Компонент | Состояние |
|-----------|-----------|
| ADR | Accepted · In progress |
| `IntercomMessageCodeCorrespondenceProjector` | In-memory; match по `memberKey`, fallback line overlap |
| `IntercomMessageCodeIndex` (SQLite) | — (фаза 1 без SQLite) |
| Slash `message find` | Implemented |
| MCP `intercom.messages_for_code` | Implemented |
| Event `message_range_related` | Implemented |
| Slash `message relate` | Implemented |
| MCP `intercom.message_relate` | Implemented |
| Editor gutter HUD | — (фаза 3) |
