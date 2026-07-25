# ADR 0194: Anchor edit `place=before|after` (no silent wipe)

**Статус:** Accepted · Shipped (CDP **0.5.177**)  
**Дата:** 2026-07-25  
**Tags:** #cdp #buffer #anchor #mutate #agent-ide #adr #cascade-ide

## Резюме

- `cdp_buffer edit_op=anchor` historically **always replaced** the resolved locus (`ApplyReplaceRange` over full member span).
- Tool schema advertised `place=` for paste/put; agents reasonably passed `place=before` with `edit_op=anchor` expecting insert.
- **Silent ignore** → member wiped (dogfood: `SceneMap` / `SceneJson` erased while inserting `Pulse`). Ultra-critical mutate footgun.

## Решение

- `edit_op=anchor` honors `place=` / `at_place=`:
  - **replace** (default) — overwrite locus (compat)
  - **before** — insert at locus start (zero-width range)
  - **after** — insert at locus end
- Unknown `place=` → hard error (no silent ignore).
- `place=sniper` on anchor → error (paste/put only).
- XML `+K:Element` insert: `place=` must be omit/replace (own insert axis).
- Result meta includes `place=` so dogfood can verify.
- Schema/Meta text: place= applies to anchor, not only paste/put.

## Related

- Buffer plane: `cdp-mcp/DocumentEditPlane.cs` (`ApplyAnchorEdit`, `ApplyPlacedRange`)
- Paste/put already had `EditorComfort.ApplyPlaced` — same semantics, now shared contract for anchors
- **Regression tests:** `cdp-mcp/CdpMcp.Tests/DocumentEditPlaneAnchorPlaceTests.cs` (place=before keeps locus; after/replace/unknown/sniper)
