# Glass Intercom · NorthStar messenger (v0 steer)

**Status:** accepted direction 2026-08-05 (operator).
**Do not:** wire Folded AutoI Korry by hand in this leaf — separate residual.

## Thesis

Glass **Intercom** is **not** «чат со вторым пилотом».

**NorthStar** = координационный центр команды: люди + агенты + агенты агентов.
Product face → **мессенджер с каналами** (Slack/Mattermost grammar), не linear agent-chat и не CIDE session-graph страдание.

## Already canon (reuse, don’t reinvent)

- [ADR 0080](../adr/0080-intercom-naming-and-multi-party-channel-model.md) — Intercom = multi-party channel, not bot window.
- [intercom-ux-reference-slack-mattermost-v1](intercom-ux-reference-slack-mattermost-v1.md) — composer, roles, flat feed, topic≈channel list patterns; **do not** fork full Slack server into IDE.
- [ADR 0172](../adr/0172-conversation-first-habitat.md) — session-graph habitat was CIDE north-star; **Glass must not copy the suffering** (heavy tree/overview chrome as default).

## Glass stance (this steer)

| Prefer | Avoid |
|--------|--------|
| Channels as first-class (who’s on the wire) | One DM with «the second pilot» |
| Light channel switcher + flat feed | CIDE topic-card / session-tree complexity as day-1 |
| Human / agent / system / nested-agent voices | LLM-only bubbles |
| Workspace+tools unique in IDE; heavy team ops outside if needed ([0080 §5](../adr/0080-intercom-naming-and-multi-party-channel-model.md)) | Rebuilding Mattermost inside Glass |

## Relation to lane × model axes

[glass-intercom-lane-model-axes-v0](glass-intercom-lane-model-axes-v0.md) (CIT/HOST/PF Korry + HUD model) stays as **near-term chrome**.

Longer arc: lane strip → **channel strip / channel rail** (messenger). CIT/HOST/PF may become named channels or filters, not the final ontology. Model Combo stays on Citizen/MAF path when that channel needs an FM brain.

## Folded AutoI Korry (note only — no hand fix here)

Green Korry = latch **paint** (state). Click writes `glass_ignite_cmd` pending. If Autoi halted/folded and no consumer → «кликабельно, никуда не ведёт». Fix later as ignite-cmd consume path, not Review manual wiring.

## Open (preference later)

First channel set for Glass v0 (examples, not decided): `#ops` / seat channels / per-citizen / per-human — pick when shipping channel rail, not in this stamp.
