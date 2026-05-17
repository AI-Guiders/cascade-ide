<!-- English translation of adr/0023-environment-readiness-glance.md. Canonical Russian: ../../adr/0023-environment-readiness-glance.md -->

# ADR 0023: Glance channel - separate from IDE Health

**Status:** Accepted (decision boundaries and signal selection; specific UI and types in the code - as implemented)  
**Date:** 2026-04-11  
## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD - Cascade IDE Cockpit Attention Model |
| [0022](0022-workspace-health-lexicon.md) | lexicon **IDE Health** - **other** channel |
| [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | renaming the observability channel |

## Summary

- Markdown + **Mermaid/PlantUML** - first-class via LSP and workflow.
- Kroki, export expanded, authoring - **orthogonal** preview ([0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md)).

### Outside ADR

| Document | Role |
|----------|------|
| [`environment-readiness-glance-v1.md`](../../design/environment-readiness-glance-v1.md) | drawing readiness glance |

---
## Context

1. The user needs a **quick check**: is the LSP connected, are the necessary tools available, are **relevant** variables set - **without** opening the settings, if the goal is not editing, but a reminder (peripheral attention, ADR 0021).
2. The **[IDE Health](0022-workspace-health-lexicon.md)** channel is assigned to a **task in the workspace** (build, tests, debugging, git). Confusing “is everything ok with the tools and environment” with it discredits terminology and UX.
3. A complete dump of process environment variables is **unacceptable** (noise, risk of leakage, does not answer the question “is it necessary for the IDE”).

---

## Solution

1. A separate product channel **environment readiness** is being introduced (working name in the UI: as agreed; in the docks - *environment readiness / glance*). It **does not** use `IdeHealth*` prefixes in this channel's contracts and **is not** configured by **`ide_health_*`** keys in TOML modes - these keys are related to [IDE Health](0022-workspace-health-lexicon.md); readiness placement is set separately (preset, ADR 0021).
2. **The channel snapshot** is built only from signals that the IDE **already uses** or explicitly needs to check for its scripts (LSPs, external MCPs, selected chat transport, need for `dotnet`, etc.).
3. **Environment Variables** on this channel: **only** names that Cascade IDE **actually reads** for features to work, or an agreed upon minimum checklist for documented scenarios. Do not print the entire `environ`.
4. **Executable files:** checking **required** tools for the IDE - via an explicit path from the settings or through permission via **PATH** (Windows) and **PATH** / accepted analogues on Linux; not "all programs on disk".
5. **Settings screen** remains the **editing** place; readiness channel - **inspection** and short tips + “Open settings” link. Secrets and API keys at glance **do not** show in clear form (mask or not display).
6. **Presentation layer** (full screen page in the secondary outline of the shell - v1 anchor in the Mfd zone column; PFD badges; palette commands - without TabControl) is specified by the preset and ADR 0021 separately from the IDE Health strip.

Details of signals v1, composition of fields and placement in the UI are in [drawing](../../design/environment-readiness-glance-v1.md).

---

## Consequences

- The new "show environment state for IDE" features expand on **this** channel and document added env/check names in a drawing or in code with a link to the ADR.
- Expansion of **IDE Health** with new segments about LSP/env **is not done** under the guise of the same channel - if in doubt, see §Context and [0022](0022-workspace-health-lexicon.md).

---

## Open

- Name of types in the code (`EnvironmentReadiness*` or other) - during the first implementation.
- Localization of page/card titles.