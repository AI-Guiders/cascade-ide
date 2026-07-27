# ADR 0196: Architecture staging board (`arch_desk` / `cdp_arch`)

**Статус:** Accepted  
**Дата:** 2026-07-27  
**Tags:** #cdp #cide #architecture #board #codeanchor #wire #kneeboard #adr #cascade-ide

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | CDS / Channel / Compositor / Surface pipeline |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | Compute units / transport → channel |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Instrument descriptor / composition |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD attention — соседний cockpit model |
| [0191](0191-scan-pattern-seats-desk-repl.md) | Desk / go= REPL — соседний organ |
| [0193](0193-agent-attention-channels-ccl.md) | Soft organs / attention |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Board = cheap staging; не жечь FM на remap essays |

## Резюме

- Не Miro whiteboard и не free-form sketch: **ontological staging board** — роли (CCU|Channel|CDS|Compositor|Surface|…) + кандидаты как **CodeAnchor** wires `[F:;M:;K:]`.
- Soft organ: `go=arch_desk` / Meta `cdp_arch` (cdp-mcp **0.5.239**).
- Board ≠ code: `promote` v0 — **plan-only** (next go), без мутации исходников.
- Цель: та же архитектура, что CIDE → будущая интеграция = **wire**, не remap.

---

## Контекст

IdeCockpit и соседние органы в cdp-mcp выросли peel'ами. North star: **parity по швам CIDE** (CCU → Channel → CDS → Compositor → Surface), а не «ещё один refactor monster». Нужен стол, где агент и оператор **объявляют роли**, кладут кандидатов-якорей, выбирают/отклоняют, рисуют рёбра — до того как трогать код.

## Решение

### Контракт v0

| op | смысл |
|----|--------|
| `scene` | pulse доски |
| `add_role` | slot: ccu\|channel\|cds\|compositor\|surface\|instrument\|dal\|transport |
| `add_candidates` | `anchors=` CodeAnchor wires (не bare path как SSOT) |
| `elect` / `reject` | выбор / отсев кандидата на роли |
| `edge` | from→to kind=feeds\|mounts\|projects\|wires |
| `promote` | plan-only: статус promoted + next go (не rewrite) |
| `clear` / `roles` | сброс / lexicon |

SSOT: `.cdp/arch-board/LATEST.json` (+ stamped).

### CodeAnchor = кандидат

Канон: `[F:File.cs;M:Member]`. Shorthand `File::Member` нормализуется в wire. Bare symbol = label-only до настоящего wire.

### Не делать

- Не подменять architecture desk произвольным canvas / sticky notes без ролей.
- Не считать board выполненным рефакторингом.
- Не принимать bare path как elected wire без нормализации.

## Последствия

- Dogfood: remap `IdeCockpit.BuildAsync` идёт через роли на `arch_desk`, затем implement по elected anchors.
- Будущее: promote → real wire в CIDE packs (за пределами v0).
