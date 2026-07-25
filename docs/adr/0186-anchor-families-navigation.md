# ADR 0186: Anchor families — `Family:navigation` (не Deep-Link)

**Статус:** Accepted · Implemented (CDP 0.5.156 / ScriptableIde 0.1.55)  
**Дата:** 2026-07-25  
**Обновлено:** 2026-07-25 — nested всегда `Anchor:[Family:…]`; «CodeAnchor» = речь про `Family:code`  
**Tags:** #cdp #anchor #family #navigation #agent-comfort #equal-standing #adr #cascade-ide

## Резюме

- **Якорь** — указатель на место/сущность (resolve / reveal / edit). Слово «Code» в «CodeAnchor» — только домен, не отдельный тип.
- **Один wire** `BracketLocate`; семьи: `Family:code|xml|navigation`.
- Навигация IDE ≠ Deep-Link / URI / ось `Code:`.
- Аттракторы: **open / goto / restore / show / go** → `Command:`; орган → `Go:`; locus → вложенный **`Anchor:[Family:…;…]`** (reuse resolve).
- Канон имён — полные; короткие — **alias**.
- Land: `cdp_land`. `cdp_navigate` / `cdp://` — **удалены**.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | Code-family / attach |
| [0185](0185-life-thread-delayed-self-wake.md) | Wake landing = navigation-family Anchor |
| [0182](0182-restore-previous-desk-dual-instance.md) | `Command:restore` |
| [0039](0039-workspace-navigation-affordances.md) | Навигационные affordances |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom deep links (CIDE) — ортогонально |

## Проблема

URI Deep-Link и отдельный navigate-тип бьют мимо аттракторов агента («open / go to / restore / show»). XML уже показал правильный ход: **те же скобки + новые оси + family dispatch**. Путаница «CodeAnchor vs Anchor vs Code:» — лишние сущности.

## Решение

### Суть: Anchor + Family

Без слова Code якорь — просто **«вот сюда»**. `Family:` выбирает мир. В речи «CodeAnchor» = `Family:code` Anchor; не отдельный wire-тип и не ось `Code:`.

### Family на wire

```text
[Family:code;File:Foo.cs;Member:Bar]
[Family:xml;File:app.csproj;Element:Project/PropertyGroup/OutputType]
[Family:navigation;Command:go;Go:editor_scene;Anchor:[Family:code;File:Foo.cs;Member:Bar]]
[Family:navigation;Command:restore]
[Family:navigation;Command:show;Anchor:[File:out.png]]
[Family:navigation;Command:open;Anchor:[Family:code;File:Foo.cs;Line:10]]
[Family:navigation;Command:goto;Anchor:[Family:code;File:Foo.cs;Member:Bar]]
```

Без `Family:` — эвристика (M/S/L → code; Element/Attribute → xml) для compat.

### Оси (канон → alias)

| Канон | Alias | Семьи |
|-------|-------|--------|
| `Family` | `Fam` | все |
| `File` | `F` | code, xml, nested |
| `Member` | `M` | code |
| `Line` | `L` | code |
| `Scope` | `S` | code |
| `Kind` | `K` | code / xml roles |
| `Element` | `X` | xml |
| `Attribute` | `A` | xml |
| `Command` | `C` | navigation |
| `Go` | `G` | navigation (`Command:go`) |
| `Anchor` | — | navigation — **вложенный якорь любой family** |

`Navigate:true` / `N:true` — **не канон** (compat → `Family:navigation`).

### Navigation `Command`

| Command | Эффект |
|---------|--------|
| `open` | открыть nested `Anchor` как buffer/locus |
| `goto` | land на nested code-family Anchor |
| `restore` | desk restore ([0182](0182-restore-previous-desk-dual-instance.md)) |
| `show` | evidence/preview из nested `File` |
| `go` | cockpit organ; требует `Go:` |

### Агентский API

`Anchor` fluent + `ToWire()`. Emit navigation с каноном; nested code → `Anchor:[Family:code;…]`.

### Отклонено

- `cdp://` Deep-Link как agent surface  
- `NavigationAnchor` / отдельный тип  
- ось **`Code:`** вместо nested `Anchor:` (сужает; png/xml — тоже якоря)  
- `C:go:editor_scene` (второй `:` внутри оси)  
- Однобуквенность как закон  

## Последствия

- Wake / take / CIDE chip: один `[Family:navigation;…;Anchor:[Family:code|…]]` — клик воспроизводит стейт.
- Edit с `Family:navigation` → fail (land ≠ mutate).
- Расширение: новая `Family:` + оси.

## Follow-up

- [ ] Emit navigation Anchor из take / PlantUML / job.done / stop_context.
- [ ] CIDE chip → land (тот же wire).
- [ ] Постепенно Format→канон и для code/xml.
