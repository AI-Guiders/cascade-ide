# Domain card: AutoIgnition (ignite / CDT)

- id: `ignite`
- organ: `cdp_ignite` / `IdeIgniteArmHost` + `IdeIgniteChannel`
- product: `#CDP` `#CIDE`
- contract: agent-notes `knowledge/domains/agent-operations/playbook-autonomous-continuity-contract-v1.md`

## Invariants

- Composer charge default `minimal` + amnesia postfix; TM body stays in Task Manager.
- **Autonomous Continuity:** empty TM / unknown next ≠ stop. Investigate, seed leaf, build domain/tools/KB, use internet — ~99% without operator. `await_operator` only on explicit operator stop or hard human gate.
- Auto-`LeafPlateau` under overnight/autonomous armed = contract violation — `op=resume`, seed, re-ARM.

## Entry

- Dig: this card · playbook · `harness-checkpoint-automation.mdc`
- Runtime: `cdp_ignite`

## Antipatterns

- Plateau / «жду тебя» while operator away with continue authorization.
- Dual Autoi (cdp + cdp-debug) both mirroring Composer charge → Intercom SA wall + wake_habitat* FDR thrash. Live seat owns Radio Intercom; shared claim for remount/leaf families.

## last_ship

- 2026-08-04: Dual Autoi thrash peel — `IsPrimaryAutoiSeat` + `TryClaimSharedWakeMirror` + `FormatHabitatIntercomRadio` (gap 3.2 SA wall)
- 2026-07-31: Autonomous Continuity Contract stamped
