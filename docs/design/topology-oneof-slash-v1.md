# Topology · OneOf via `/` (v1) — beyond P/M · channels

**Status:** draft → act (operator 2026-08-04)  
**Extends:** [topology-oneof-slash-v0.md](topology-oneof-slash-v0.md) · ADR [0193](../adr/0193-agent-attention-channels-ccl.md) · ADR 0017  
**Does not** redefine `+` (Split).

## Intent

- Agents ship **human-faced instruments per attention channel** (sit / work / probe / report / world / alert) — not HDMI copies of seats.
- Operator calmly runs **2 or 3 physical windows**; `/` packs channels so they are **not locked** to «one P + one F + one M monitor».
- Chord + auto-switch move the **active channel face** inside a OneOf TopLevel.

## Grammar (unchanged join)

| Op | Meaning | Example |
|----|---------|---------|
| `+` | Split simultaneous | `(P+M)(F)` |
| `/` | OneOf XOR full zone | `(P/M)(F)` · `(F/M)(P)` · `(P/F)(M)` |

Parser already accepts any anchor set with `/`. **v0 host/analyzer hardcodes P/M only** — that is the gap.

## Packing matrix (v1 DoD)

| Windows | Topology examples | Behavior |
|--------:|-------------------|----------|
| 3 | `(P)(F)(M)` | Dedicated TopLevels; no OneOf required |
| 2 | `(F)(P/M)` · `(P)(F/M)` · `(M)(P/F)` (+ sym) | One dedicated + OneOf of the other two |
| 1 | out of scope v1 | compact tier stays ADR 0171 |

Three-way OneOf `(P/F/M)` on one TopLevel — **defer** (v1.1) unless dogfood demands.

## Channel → anchor (projection, not identity)

ADR 0193 channels are **not** window labels. Default projection (layout `agent` compatible):

| Channel | Default face anchor |
|---------|---------------------|
| sit / report | P (plan / report board) |
| work | F (editor / Intercom forward) |
| probe / world | M (script / git / shell / browser) |
| alert | EICAS chrome (not OneOf steal) |

When topology OneOf-hides an anchor, **auto-switch** PreferOneOf(that anchor) on channel demand (land / MFD page / locus). SoftOrgan seats still **never** steal page/OneOf (PresentationPmOneOfPolicy.SeatsMaySelectMfd = false — keep).

## DoD switch axes (same as v0, generalized)

1. **Chord** — toggle active member of the OneOf set (not only P↔M).
2. **Auto-switch** — channel / land / MFD intent → show needed member.

## Ship slices

1. **Analyzer + flags** — `IsOneOfPlusDedicatedTwoScreen` for any distinct pair + single dedicated; retire Pm-only as sole path (compat alias ok).
2. **Glass OneOf host** — generic member set `{A,B}` + dedicated host for the third; titles `A/B · X active`.
3. **Policy** — `PresentationOneOfPolicy` (rename/generalize from Pm); channel→Prefer map.
4. **Chord** — `po` cycles OneOf set; optional `po P` / `po M` land.
5. **Live dogfood** — PNG 2-window `(F)(P/M)` + `(P)(F/M)` toggle; 3-window `(P)(F)(M)` unchanged.

## Do not

- Mirror agent SoftOrgan seats as extra HDMI windows.
- Force human-faced work to invent chrome only on Editor when channel belongs on M/P.
- SoftFL invent / Meta mill under citizen hold — this feature is operator-authorized densest Glass gap.

## Compat

- v0 `(P/M)(F)` keeps working.
- Avalonia CIDE parity — follow-up after Glass Windows dogfood.
