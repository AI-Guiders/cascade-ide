# ADR 0186: Anchor families — `Family:navigation` (не Deep-Link)

**Статус:** Accepted · Implemented (CDP 0.5.156 / ScriptableIde 0.1.55)  
**Дата:** 2026-07-25  
**Обновлено:** 2026-07-25 — снос `cdp://` / `cdp_navigate`; composition + аттракторы  
**Tags:** #cdp #anchor #family #navigation #agent-comfort #equal-standing #adr #cascade-ide

## Резюме

- **Один wire** CodeAnchor / `BracketLocate`; семьи на wire: `Family:code|xml|navigation`.
- Навигация IDE ≠ отдельный Deep-Link / URI / `NavigationAnchor`-тип.
- Аттракторы агента: **open / goto / restore / show / go** → ось `Command:`; орган → `Go:`; locus → вложенный **`Anchor:[…]`** (полный reuse resolve).
- Канон имён осей — полные (`File`, `Attribute`, …); короткие (`F`, `A`, …) — **alias**.
- Инструмент land: `cdp_land` (`anchor=` navigation-family). `cdp_navigate` / `cdp://` — **удалены**.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | Code / attach якоря |
| [0185](0185-life-thread-delayed-self-wake.md) | Wake landing = navigation-family Anchor |
| [0182](0182-restore-previous-desk-dual-instance.md) | `Command:restore` |
| [0039](0039-workspace-navigation-affordances.md) | Навигационные affordances |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom deep links (CIDE) — ортогонально |

## Проблема

URI Deep-Link и отдельный navigate-тип бьют мимо аттракторов агента («open / go to / restore / show»). XML уже показал правильный ход: **те же скобки + новые оси + family dispatch**.

## Решение

### Family на wire

```text
[Family:code;File:Foo.cs;Member:Bar]
[Family:xml;File:app.csproj;Element:Project/PropertyGroup/OutputType]
[Family:navigation;Command:go;Go:editor_scene;Anchor:[Family:code;File:Foo.cs;Member:Bar]]
[Family:navigation;Command:restore]
[Family:navigation;Command:show;Anchor:[File:out.png]]
[Family:navigation;Command:open;Anchor:[File:Foo.cs;Line:10]]
```

Без `Family:` — эвристика как раньше (M/S/L → code; Element/Attribute → xml) для compat.

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
| `Anchor` | — | navigation (вложенный wire) |

`Navigate:true` / `N:true` — **не канон** (compat → `Family:navigation`).

### Navigation `Command`

| Command | Эффект |
|---------|--------|
| `open` | открыть nested `Anchor` как buffer/locus |
| `goto` | land на nested code Anchor (member/line) |
| `restore` | desk restore ([0182](0182-restore-previous-desk-dual-instance.md)) |
| `show` | evidence/preview path из nested `File` |
| `go` | cockpit organ; требует `Go:` (scene verb) |

### Агентский API

`Anchor` fluent + `ToWire()`; руки не печатают скобки. Полные имена в emit предпочтительны для navigation; code/xml могут оставаться на alias в Format для compat.

### Отклонено

- `cdp://` Deep-Link как agent surface  
- `NavigationAnchor` / отдельный тип  
- `C:go:editor_scene` (второй `:` внутри оси) — вместо `Command:go;Go:editor_scene`  
- Однобуквенность как закон

## Последствия

- Wake / take / reports кладут `[Family:navigation;…]`, не URI.
- Edit с `Family:navigation` → fail (land ≠ mutate).
- Расширение: новая `Family:` + оси, без войны за буквы.

## Follow-up

- [ ] Emit navigation Anchor из take / PlantUML / job.done / stop_context.
- [ ] CIDE chip: тот же wire.
- [ ] Постепенно Format→канон и для code/xml.
