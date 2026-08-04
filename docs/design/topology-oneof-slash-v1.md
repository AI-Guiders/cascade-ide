# Topology · OneOf via `/` (v1) — Scan Pattern + channel stack

**Status:** act (operator 2026-08-04)  
**Extends:** [topology-oneof-slash-v0.md](topology-oneof-slash-v0.md) · ADR [0193](../adr/0193-agent-attention-channels-ccl.md) · ADR [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) · ADR 0017  

## Hard steer

- **Scan Pattern P | F | M stays.** Attention geography does not go away.
- **Channels are a stack of functions on those anchors** — same idea as Boeing **ND**: one display, several roles (MFD / ECL / …) without inventing a fourth scan seat.
- Wire names **surfaces** (`intercom`, `editor`, `sit`, `world`, `alert`…). Assignment to P/F/M is **where that stack sits in the scan**, not «P/F/M disappeared».

## Wrong swings (do not repeat)

1. «OneOf = any pair of P|F|M as ontology» — treats anchors as the packed *content*.
2. «P/F/M nobody / only channel ids, no scan» — throws away Scan Pattern.
3. Right: **anchors = scan geography**; **channels/surfaces = stack on a geographic slot**; `/` = XOR inside one stack/slot.

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

## DoD

1. Describe packing as: slot → (scan role F|P|M, channel stack[], active channel).
2. Glass remount: active **channel** in stack; scan role picks which TopLevel/zone geography.
3. Chord / auto-switch: cycle or prefer **channel** inside the stack on that scan slot.
4. Dogfood: `(intercom)(sit/world/…)` on 2 windows; three-group full scan on 3 windows.

## Do not

- Replace Scan Pattern with a flat list of channel windows.
- Treat P/F/M as the channel ids.
- Blame habitat when the model was wrong.
