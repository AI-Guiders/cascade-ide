# Topology · OneOf via `/` (v1) — **channels first** · P|F|M = meta

**Status:** act (operator 2026-08-04 correction)  
**Extends:** [topology-oneof-slash-v0.md](topology-oneof-slash-v0.md) · ADR [0193](../adr/0193-agent-attention-channels-ccl.md) · ADR 0017  
**Does not** redefine `+` (Split).

## Hard steer (do not regress)

- **Primary:** attention **channels** — sit · work · probe · report · world · alert (ADR 0193).
- **Secondary meta only:** P · F · M — legacy zone tags for *which human face / Glass zone paints* when a channel is active. **Not** window identity. **Not** OneOf member identity.
- Agent SoftOrgan ≠ HDMI. Operator windows = **2 or 3 slots** that host channel faces (dedicated or `/` OneOf).
- Wrong leaf: «generalize any P|F|M pair». Right leaf: pack **channels** onto slots; keep P|F|M as projection meta.

## Intent

1. Agents ship **human-faced instruments per channel** calmly (no monitor-count anxiety).
2. Operator calmly uses **2 or 3 physical windows**; `/` XOR-switches **channels** inside a slot.
3. Chord + auto-switch = active **channel**, not «toggle P↔M as ontology».

## Grammar (wire still uses P|F|M tokens — as meta)

Join ops unchanged:

| Op | Meaning |
|----|---------|
| `+` | Split simultaneous faces in one TopLevel |
| `/` | OneOf — XOR **channel faces** on one full TopLevel |

String examples `(F)(P/M)` remain valid **compat wire**: each token is a **meta tag** mapped to a default channel pack, not «the seat is the channel».

### Meta → default channel (projection table)

| Meta token | Default channel face | Typical human face |
|------------|----------------------|--------------------|
| `P` | sit (report shares sit face) | Plan / report board |
| `F` | work | Editor / Intercom forward |
| `M` | world (probe shares world face) | MFD instruments / shell / git / browser |
| _(none)_ | alert | EICAS chrome — never a OneOf steal |

One meta token may cover multiple channels that share a face (sit+report → P face). Switching **report** vs **sit** is organ/page within sit-face, not a new window.

## Packing matrix (channel language)

| Windows | Meaning | Compat wire (meta) |
|--------:|---------|-------------------|
| 3 | three dedicated channel-faces | `(P)(F)(M)` → sit \| work \| world |
| 2 | one dedicated + OneOf{two faces} | `(F)(P/M)` → work \| OneOf{sit, world} |
| 1 | out of scope v1 | compact ADR 0171 |

Symmetries of meta strings are just different labels for the same channel packing.

## Switch axes

1. **Chord** — cycle active **channel** in the OneOf slot (UI may still show meta glyph P/M as badge).
2. **Auto-switch** — channel demand (land / locus / agent intent) → show that channel’s face if it lives in a OneOf slot.

SoftOrgan seats never steal page/OneOf (keep v0 policy).

## Model types (code)

- `PresentationChannelId` — sit|work|probe|report|world|alert.
- `PresentationZoneMeta` — optional P|F|M tag on a face (paint/remount hint only).
- Slot describe: dedicated channel-face **or** OneOf set of channel-faces (+ meta for Glass remount).
- **Do not** treat `PresentationAnchorKind` as the OneOf member id in new APIs; map meta↔channel at the boundary.

## Ship slices (corrected)

1. ~~Any P|F|M pair analyzer as ontology~~ → **channel packing describe** + meta map (this correction).
2. Glass OneOf host remount by **active channel** (meta only selects which zone UIElement).
3. Chord `po` / Prefer by **channel id**.
4. Live dogfood: 2-window channel switch + 3-window dedicated; badges may show meta.

## Do not

- Invent «P/F/M are the channels».
- Mirror agent seats as extra HDMI windows.
- Blame habitat when the model was wrong.

## Compat

- v0 `(P/M)(F)` wire keeps parsing.
- Prior analyzer `IsOneOfPlusDedicatedTwoScreen` = **meta-level** helper for Glass remount until hosts speak channels; call sites must go through channel describe.
