# Topology · OneOf via `/` (v1) — Scan Pattern + channel stack

**Status:** act (operator 2026-08-04)  
**Extends:** [topology-oneof-slash-v0.md](topology-oneof-slash-v0.md) · ADR [0193](../adr/0193-agent-attention-channels-ccl.md) · ADR [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) · ADR 0017  

## Hard steer

- **Scan Pattern P | F | M stays** as convenient **attention labels** (geography of glance / intent).
- **«Meta» means:** P/F/M name the *scan seat*, not a physical monitor. That naming **detaches Scan Pattern from physical topology** (2 vs 3 windows, OneOf packing). Physical packing is separate — `/` and window count.
- **Channels are a stack of functions on those scan seats** — Boeing **ND** analogy: one display geography, several roles, without inventing a fourth scan seat.
- Wire may name **surfaces** (`intercom`, `editor`, `sit`, `world`, `alert`…). Assignment to P/F/M is which **scan label** that stack rides — not «which HDMI».

## Wrong swings (do not repeat)

1. «OneOf = any pair of P|F|M as ontology» — treats anchors as the packed *content*.
2. «P/F/M nobody / only channel ids, no scan» — throws away Scan Pattern.
3. «Meta = P/F/M are paint hints only / not scan» — wrong sense of meta; meta = scan labels unbound from physical screens.
4. Right: **P/F/M = Scan Pattern labels (abstract)**; **physical topology** = 2|3 windows + `/`; **channels** = function stacks on scan seats.

## Wire → Scan

| Topology (surfaces) | Scan assignment |
|---------------------|-----------------|
| `(intercom)(sit/world/alert/…)` | **F** = intercom stack · **P/M** = OneOf stack of the rest |
| `(editor)(…)` | **F** = editor · **P/M** = remaining channel stack |
| `(intercom)(alert/sit/…)(…)` three groups | Full scan **F, P, M** — each group a stack (dedicated or `/` OneOf) |

So: who is F? The surface named in the Forward slot (intercom / editor / …). Who is P/M? The other slot(s) — often one physical window with `/` channel stack (like ND).

## Grammar

| Op | Meaning |
|----|---------|
| `+` | Split simultaneous faces in one TopLevel (rare for channel stacks) |
| `/` | OneOf — XOR active **channel** inside one scan slot’s stack |

Compat legacy `(F)(P/M)` / `(P/M)(F)` = old **meta glyphs** for the same geography; prefer surface names in new wire.

## Channel stack (ADR 0193)

sit · work · probe · report · world · alert — functions that **mount on** P/F/M slots.  
Example: alert may ride chrome/EICAS *and* appear in a P/M OneOf stack when operator packs it there — scan seat unchanged, function expanded (ND analogy).

## Surface wire (shipped)

`PresentationSurfaceWire.Parse("(intercom)(sit/world/alert)")` → slots:
- window0 **F** stack=`intercom`
- window1 **PmOneOf** stack=`sit/world/alert` (P/M geography, channel OneOf)

Three groups `(intercom)(sit)(world)` → full Scan **F,P,M**. Legacy `(F)(P/M)` via `FromLegacyMetaWire`.

**Single TopLevel OneOf** `(P/F/M)` / `(sit/world/alert)` → one `PmOneOf` slot, **no** satellite hosts; Glass XOR-paints main columns (`*,4,0,4,0` / `0,4,*,4,0` / `0,4,0,4,*`). Chord `po` cycles active. Spatial `(P+F+M)` stays legacy Split (surface wire refuses single-group `+`).

1. Describe packing as: slot → (scan role F|P|M, channel stack[], active channel). **shipped**
2. Glass remount: active **channel** in stack; scan role picks which TopLevel/zone geography. **shipped** (`f1a77c32`)
3. Chord / auto-switch: `po` cycles PreferSurface; MFD page → ResolveStackSurface → PreferSurface (else PreferPmOneOf). **shipped** (`e253954e`) · dogfood title `sit/world/alert · world active · OneOf host`
4. Dogfood: `(intercom)(sit/world/…)` on 2 windows; channel switch sit→world (mfd_page=Browser with topology held). **shipped** 2026-08-05 · evidence `tmp-glass-shots/topology-oneof-sit-active-20260805.png` + `topology-oneof-world-active-20260805.png`. Three-group full scan `(intercom)(sit)(world)` on 3 windows (F Intercom · P Plan · M Events) after Sync tear-down-before-remount fix — **shipped** 2026-08-05 · evidence `tmp-glass-shots/topology-3win-forward-fixed-20260805.png` + `topology-3win-pfd-fixed-20260805.png` + `topology-3win-mfd-fixed-20260805.png`.
5. Single-TopLevel `(P/F/M)` OneOf (was painting as `(P+F+M)` because `groups=1` failed surface wire). **shipped** 2026-08-05 — wire + main-column XOR; dogfood evidence pending PNG.

## Do not

- Replace Scan Pattern with a flat list of channel windows.
- Treat P/F/M as the channel ids.
- Blame habitat when the model was wrong.
