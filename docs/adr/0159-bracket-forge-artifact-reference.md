# ADR 0159: Bracket `[FRG:…]` — ссылки на артефакты Forge (расширение 0128)

**Статус:** Accepted · Design  
**Дата:** 2026-06-11

## Резюме

- Расширить **bracket-нотацию** ([0128](0128-intercom-attachment-anchors-and-code-references.md) §5) **второй семьёй** токенов **`[FRG:…]`** — ссылки на forge-артефакты (repo, issue, MR), **ортогонально** code-bracket `[F:…; M:…; L:…; S:…]`.
- **Код** по-прежнему → `AttachmentAnchor` / `CodeAnchor`; **forge** → `ForgeArtifactRef` (wire).
- **Prose / markdown / Intercom / slash** — bracket; **браузер / OS** — `forge://` ([FORGE-ADR-0007](../../../agent-forge/design/FORGE-ADR-0007-forge-magic-link-protocol.md)); **MCP** — structured JSON (как у attach).
- Один parse tree, два discriminant: `FRG` vs code (`F`/`M`/`L`/`S`).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | `AttachmentAnchor`, оси `F`/`M`/`L`/`S`, attach/reveal |
| [0131](0131-editor-slash-select-code-by-bracket-reference.md) | `/editor select code [M:…]` — shared code parse |
| [0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md) | Bracket / CodeAnchor в correspondence |
| [0157](0157-cide-magic-link-protocol.md) | `cide://reveal?…&b=…` — bracket в query |
| [0158](0158-forge-lens-crs-overlay.md) | Forge Lens в CRS |
| [FORGE-ADR-0003](../../../agent-forge/design/FORGE-ADR-0003-forge-lens-cide-code-anchors.md) | Lens, storage `anchors[]`, MCP-first write |
| [FORGE-ADR-0007](../../../agent-forge/design/FORGE-ADR-0007-forge-magic-link-protocol.md) | `forge://` deep links |
| [FORGE-ADR-0012](../../../agent-forge/design/FORGE-ADR-0012-bracket-notation-and-lens-anchors.md) | Forge storage, thin web, round-trip |

---

## Контекст

[0128](0128-intercom-attachment-anchors-and-code-references.md) зафиксировал: slash, скобки, chips и MCP сходятся в **один wire-тип** для **кода** (`AttachmentAnchor`). Оси bracket — `F`, `M`, `L`, `S`.

[FORGE-ADR-0003](../../../agent-forge/design/FORGE-ADR-0003-forge-lens-cide-code-anchors.md) добавляет **forge-артефакты** (issue, MR) с тем же `CodeAnchor` в metadata, плюс `forge://` для навигации.

**Проблема v0.3:** forge pilot показывает anchors как `file:line`; агент и оператор мыслят **`[M:…]`** и **`[FRG:…]`**, не JSON и не сырой path. Три параллельных «языка» (Intercom bracket, forge JSON, web `file:line`) ломают принцип §5.1 0128.

**Решение:** не добавлять ось `I:` внутрь code-bracket (смешивает artifact и code), а ввести **отдельный namespace** `FRG:` — симметрично тому, как `cide://` и `forge://` разделены по схеме, но связаны делегированием.

---

## Решение

<a id="adr0159-p1"></a>

### 1. Две семьи bracket

| Семья | Пример | Wire | Эффект клика |
|-------|--------|------|--------------|
| **Code** | `[M:Run]`, `[F:src/Foo.cs; M:Run; S:for:2]` | `AttachmentAnchor` | `intercom.reveal_attachment` / `cide://reveal` / editor select |
| **Forge** | `[FRG:pilot/issues/7]`, `[FRG:pilot/mr/3]` | `ForgeArtifactRef` | Forge Lens panel / `forge://` / HTTPS view |

**Не смешивать** в одной оси: code-bracket **не** содержит номер issue; forge-bracket **не** заменяет `F`/`M`/`L`/`S`.

<a id="adr0159-p2"></a>

### 2. Грамматика `[FRG:…]` (v1)

Форма (BNF-подобно):

```text
forge-bracket ::= "[FRG:" forge-path "]"
forge-path    ::= repo "/" kind "/" number [ ";" code-tail ]
repo          ::= slug (URL-safe, как в forge API)
kind          ::= "issues" | "mr" | "repos"
number        ::= 1..9 digit+
code-tail     ::= тот же L2, что 0128 §5.1 (F/M/L/S), **без** повторного "["
```

Примеры:

```text
[FRG:pilot/issues/1]
[FRG:cad-tools/mr/3]
[FRG:pilot/repos/pilot]
[FRG:pilot/issues/7; F:src/Foo.cs; M:CreateIssue]
```

| Форма | Смысл |
|-------|--------|
| `…/issues/N` | Issue #N в repo |
| `…/mr/N` | Merge request #N |
| `…/repos/{slug}` | Repo profile (редко в prose) |
| `; F:…; M:…` *(optional)* | **Primary code anchor** inline — удобно в одной скобке в body issue; в wire всё равно split: `ForgeArtifactRef` + `AttachmentAnchor` |

**Короткая форма (v1.1, optional):** `[FRG:#7]` при известном default `repo` из `[workspace.forge]` — только CIDE/slash, не в persisted forge body без repo.

<a id="adr0159-p3"></a>

### 3. Wire: `ForgeArtifactRef`

Минимальный контракт (JSON; shared с forge API):

| Поле | Обязательность | Смысл |
|------|----------------|--------|
| `repo` | да | Repo slug |
| `kind` | да | `issue` \| `mr` \| `repo` |
| `number` | для issue/mr | 1-based номер |
| `primaryAnchor` | нет | `AttachmentAnchor` при compound bracket |

**Code anchors на issue** (массив) остаются **`AttachmentAnchor[]`** / `CodeAnchor[]` — не вложены в `ForgeArtifactRef`, кроме optional `primaryAnchor` для inline compound.

<a id="adr0159-p4"></a>

### 4. Parse → resolve → act

```
Prose "[FRG:…]"  →  BracketForgeReferenceParser
                  →  ForgeArtifactRef
                  →  forge_lens.open | forge://issue | HTTPS fallback

Prose "[F:…; M:…]"  →  BracketCodeReferenceParser (0128/0131)
                     →  AttachmentAnchor
                     →  reveal / select / attach
```

| Поверхность | Code bracket | Forge bracket |
|-------------|--------------|---------------|
| Intercom attach | да | **нет** v1 *(forge — не chat attachment)* |
| Issue/MR body (forge) | да | да |
| ADR / docs markdown | да | да |
| `/editor select code` | да | нет |
| `/forge goto [FRG:…]` | — | да |
| MCP | `anchor_json` / поля | `forge_ref_json` или bracket string |

<a id="adr0159-p5"></a>

### 5. Связка с `forge://` и `cide://`

| Bracket | Magic link (canonical build) |
|---------|------------------------------|
| `[FRG:pilot/issues/7]` | `forge://{host}/issue?repo=pilot&n=7` |
| `[FRG:pilot/mr/3]` | `forge://{host}/mr?repo=pilot&n=3` |
| `[FRG:pilot/issues/7; F:src/Foo.cs; M:Run]` | `forge://{host}/lens?repo=pilot&n=7&root=…&b=F%3A…%3B%20M%3A…` → делегирует [0157](0157-cide-magic-link-protocol.md) |

**Thin web:** рендер bracket как `<a href="https://…/view/…" data-forge-uri="forge://…">` — HTTPS первым ([FORGE-0007](../../../agent-forge/design/FORGE-ADR-0007-forge-magic-link-protocol.md)).

**Display label** (UI): `[FRG:pilot/issues/7]` или chip «issue #7 · pilot» — derived from wire, не hand-authored.

<a id="adr0159-p6"></a>

### 6. Code anchor: полный `AttachmentAnchor`

Forge Lens storage и отображение **не** используют урезанный `file:line`:

- wire / DB: те же поля, что [0128 §3](0128-intercom-attachment-anchors-and-code-references.md) (`file`, `memberKey`, `lineStart`, `lineEnd`, `syntaxScope`, …);
- human/agent surface: **`BracketCodeReferenceSerializer`** ↔ `AttachmentAnchor` (symmetric parse/format);
- subset в correspondence index: `CodeAnchor` ([0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md)).

Пример в issue body:

```markdown
Регрессия в [F:src/hello.py; L:4-5; M:main] — см. [FRG:pilot/issues/1].
```

Structured block в metadata (FORGE-0012) дублирует anchors[]; body может содержать те же bracket для читаемости.

<a id="adr0159-p7"></a>

### 7. Парсер: discriminant

1. Если inner начинается с `FRG:` (case-sensitive) → **forge family**.
2. Иначе → **code family** (0128 §5.1).

Общий entry point: `BracketReferenceParser.TryParse(string)` → `BracketReferenceKind.Code | Forge`.

**Fenced code** ([0129](0129-intercom-message-body-markdown-and-fenced-code.md)): обе семьи **не** парсятся внутри fenced — как attach в 0128.

<a id="adr0159-p8"></a>

### 8. Фазы

| Фаза | Содержание | Репо |
|------|------------|------|
| **0** | Этот ADR + [FORGE-0012](../../../agent-forge/design/FORGE-ADR-0012-bracket-notation-and-lens-anchors.md) | cascade-ide, agent-forge |
| **1** | `ForgeArtifactRef` + parse/format unit tests; `ForgeLinkBuilder` bracket ↔ `forge://` | agent-forge |
| **2** | Thin web: anchor list as code-bracket; issue link as `[FRG:…]` | agent-forge |
| **3** | CIDE: `/forge goto`, markdown expander для `[FRG:…]`; CRS chip | cascade-ide |
| **4** | Intercom: опционально «copy as [FRG:…]» из Lens (не attach) | cascade-ide |

---

## Последствия

### Positive

- Один mental model: **код** = `[F/M/L/S]`, **forge** = `[FRG:…]` — как Intercom, без третьего dialect.
- Агент пишет bracket в issue body; MCP может слать JSON — round-trip.
- Symmetry с [0157](0157-cide-magic-link-protocol.md) / [FORGE-0007](../../../agent-forge/design/FORGE-ADR-0007-forge-magic-link-protocol.md).

### Negative / trade-offs

- Два парсера / один facade — нужны тесты на `[FRG:…]` vs `[F:…]` (false positive маловероятен: `FRG:` vs `F:`).
- Compound bracket `[FRG:…; F:…]` — optional; можно отложить до v1.1.
- Intercom attach forge v1 **не** входит — issue refs живут в forge/docs, не в chat event log.

## Alternatives considered

| Alternative | Why not |
|-------------|---------|
| Ось `I:` внутри `[F;M;L;S]` | Смешивает artifact и code; ломает 0128 §5.1 |
| Только `forge://` без bracket | Плохо для агента, Intercom, plain markdown |
| Только `file:line` в web | Нет re-resolve, не AttachmentAnchor |
| `[FRG:]` как attach в Intercom v1 | Scope creep; forge refs — persistence layer |

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-06-11 | Accepted (design): `[FRG:…]` + полный AttachmentAnchor display для Lens. |
