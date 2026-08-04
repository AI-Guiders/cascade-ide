# Topology · OneOf via `/` (v0)

**Sealed 2026-08-04** (operator) · extends `()()` grammar of ADR 0017 · does **not** redefine `+`.

## Operators inside one `(…)` group

| Op | Meaning | TopLevel geometry | Example |
|----|---------|-------------------|---------|
| `+` (zone_separator default) | **Split** — anchors simultaneous columns | Zone cut into parts | `(P+M)(F)` / `(xP+yM)(F)` |
| `/` | **OneOf** — XOR role on one full zone | TopLevel stays full; role switches | `(P/M)(F)` |

Verbose alias (docs only, optional parser later): `OneOf(P,M)` ≡ `P/M`. Prefer `/` in the string.

## DoD switch axes (both required)

1. **Chord** — operator toggles active member of the OneOf set (Glass chord / command).
2. **Auto-switch** — habitat switches role when attention/locus needs the other anchor (agent seat / land / soft organ demand).

## Contrast

- `(P+M)(F)` — left TopLevel **split** P‖M (today’s PM host).
- `(P/M)(F)` — left TopLevel **full**; shows either P or M, not both at once; F remains its own TopLevel.

## Out of scope → now shipping

Parser `/` · flags `PmOneOfHostTopology` · Glass `pm_oneof_host` · chord `po` / `toggle_pm_oneof_role` · auto-switch (seats/MFD→M, plan latch→P).

Avalonia CIDE PmOneOf host parity — follow-up if needed.
