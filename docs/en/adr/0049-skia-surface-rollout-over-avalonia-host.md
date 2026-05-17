<!-- English translation of adr/0049-skia-surface-rollout-over-avalonia-host.md. Canonical Russian: ../../adr/0049-skia-surface-rollout-over-avalonia-host.md -->

# ADR 0049: Phased rollout of Skia-surfaces with Avalonia-host (CIDE-wide)

**Status:** Accepted (partially: chat surface, SkiaKit; other surfaces - by waves)  
**Date:** 2026-04-15

## Related ADRs

| ADR | Role |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | channel -> CDS -> composer -> surface |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | Skia hypothesis in chat |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | instrument/slot |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | P/F/M invariants |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | topology of windows and surfaces |
| [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md) | strict PFD surface |

## Summary

- Phased **rollout of Skia surfaces** with Avalonia as the host (dual-path, waves).
- Not big-bang: migration by instruments/zones with preservation of fallback to Avalonia controls.
- Link with pipeline Intent→Declutter→Layout→Render ([0055](0055-skia-instrument-composition-pipeline.md)).

---
## Context

An experiment with Skia in the chat confirmed the practical usefulness of custom rendering for dense agent-first surfaces. The next step is not a point spike, but controlled migration across CIDE zones without losing the stability of the host layer.

The risk of “rewriting the entire UI in Skia” is that different responsibilities will fall into one basket:

- host/runtime windows (focus, input routing, dialogs, life cycle);
- cabin semantics (CDS, policy, intent, slot composition);
- drawing of individual surfaces.

This ADR captures the rollout strategy: expand Skia where it really simplifies the surface layer, and not blur the boundaries already established by previous ADRs.

## Solution

<a id="adr0049-p1"></a>

1. **The layer model remains unchanged:**  
   - **Avalonia** = host/fuselage (windows, input, focus, lifecycle, system controls);  
   - **CDS/composer** = source of semantics “what and where to show”;  
   - **Skia** = implementation of surface rendering where it is beneficial.

<a id="adr0049-p2"></a>

2. **Rollout is done in waves (strangler), not big bang:**
   - Wave 1: indicator/dense read-mostly surfaces (status bars, cockpit cards, overlays);
   - Wave 2: MFD pages with custom geometry;
   - Wave 3: strictly marked PFD surfaces (in conjunction with [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md)).

<a id="adr0049-p3"></a>

3. **For each zone a dual-path is required:** feature flag + fallback to the current Avalonia view until stability is confirmed. Removal of fallback is allowed only after stabilization and measurements.

<a id="adr0049-p4"></a>

4. **Migration unit = surface-host contract, not “control”:** Each migration relies on an explicit DTO/frame from the CDS/composer (for example slot/instrument descriptors), and not on direct dependencies on an arbitrary control tree.

<a id="adr0049-p5"></a>

5. **Areas that are not migrated to baseline:**
   - host editor and system dialogs;
   - global window chrome;
   - UX, where standard Avalonia controls are already sufficient and do not create bottlenecks.

<a id="adr0049-p6"></a>

6. **Done criteria for each migration:**
   - visual and behavioral parity with the current surface;
   - no regressions in input/focus/navigation;
   - measurable gain (readability/density/supportability, if necessary - perf);
   - preservation of CDS/presentation invariants.

<a id="adr0049-p7"></a>

7. **A separate Roslyn-guardrail is being introduced for Skia-surface (working ID: CASCOPE004):**
   - goal: to prevent host/runtime logic from being dragged into the surface layer;
   - scope: files/types marked as Skia-surface (convention or attribute - by implementation);
   - starting mode: warning/info at the rollout stage;
   - target mode: error after stabilizing the first wave and clearing current violations;
   - minimum starting set of checks:
     - prohibition of direct dependencies on `MainWindowViewModel` and other "fat" VMs;
     - prohibition of direct control of `Window`/`TopLevel`/dialog lifecycle from surface types;
     - allowing only contract DTOs from CDS/composer and render utilities without host-side effects.

<a id="adr0049-p8"></a>
8. **Basic mount-style for production presets (`instrument_id -> slot_id`):**
   - `heavy/workflow` tools (for example Solution Explorer, settings, heavy interactive panels) -> `mfd`;
   - `sa` tools (increasing situational awareness: navigation, status, early signals "what's happening/what's next") -> `pfd`;
   - `hybrid` tools -> main slot `mfd`, in `pfd` only compact `read-mostly` projection (summary/badge) is allowed, without full heavy UX;
   - `forward` is not used by default for these tools: it is the working axis of the editor, deviations are only a separate solution.

<a id="adr0049-p9"></a>

9. **The scope of CASCOPE004 is fixed as hybrid:**
   - primary scope: by namespace/folder (for example `Cockpit/Surface/**`, `*.Skia*`) for automatic coverage of the base zone;
   - explicit include: `SkiaSurface` attribute for types outside the primary scope;
   - explicit escape: `SkiaSurfaceHostEscape` attribute (rare, documented case) for a controlled exception;
   - analyzer rule: triggered when `(in primary scope OR [SkiaSurface]) AND NOT [SkiaSurfaceHostEscape]`.

<a id="adr0049-p10"></a>

10. **`instrument_id -> instrument_class` is entered as incremental registry v1 (source of truth):**
   - format: separate registry (for example `instrument-classification.toml`) in the cockpit policy layer;
   - scope v1: mandatory entry only for new and significantly changed `instrument_id`, without an instant “total freeze” for the entire legacy;
   - enforcement: start with warning (missing classification), then switch to stricter mode according to rollout waves;
   - goal: a single classification (`heavy` / `sa` / `hybrid`) for mount-style and guardrail checks without spreading over verbal agreements.

<a id="adr0049-p11"></a>

11. **For `slot_id=pfd`, strict marking is not overridden in this ADR:**
   - criteria and mode `PfdStrict` are set by ADR [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md);
   - the composition of instruments and the layout `instrument_id -> slot_id` are set by the composer layer/registry according to ADR [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md);
   - ADR 0049 does not duplicate these rules, but uses them as the regulatory basis for Skia’s rollout policy.

<a id="adr0049-p12"></a>

12. **Minimum perf-gate v1 for removing fallback (without performance analyzers):**
   - there are no functional regressions in key zone scenarios (opening, switching, entering, focus);
   - there are no consistently reproducible visual artifacts with standard resize/theme switches;
   - there are no consistently reproducible crashes/freezes in smoke scenarios;
   - according to the team’s working assessment, the new surface does not feel slower than the baseline on the target hardware;
   - the surface has passed the agreed minimum of real sessions without falling back to fallback (the threshold is set in task/iteration).
   - performance analyzers/profiling metrics are considered deferred until the rollout stabilization stage.

<a id="adr0049-p13"></a>

13. **Minimum policy `SkiaSurfaceHostEscape`:**
   - the attribute is allowed only as a temporary exception for the unblock task, if the zone does not start without host access;
   - each exception must contain:
     - `reason` (in short: what exactly is blocking the clean surface path);
     - link to task/issue/ADR note;
     - target withdrawal period (`remove_by` - iteration or date);
   - exceptions without these fields are considered a violation of policy and must be deleted before merge;
   - extension of the period is allowed only by a separate review solution with an update of the `reason` and links.

<a id="adr0049-p14"></a>
14. **Design is considered as a measurable SA-hypothesis (L1/L2/L3), and not just as an engineering rollout:**
   - each `mount_style` must have target scripts and an SA profile by level:
     - **L1 (Perception):** what signals the operator should notice;
     - **L2 (Comprehension):** what connections/meaning should be correctly collected;
     - **L3 (Projection):** what immediate developments of the situation should be predicted.
   - acceptance/removal of fallback for policy is allowed only after checking against a battery of metrics:
     - SA metrics (objective probes, freeze/query if necessary);
     - workload (separate from SA);
     - performance (separate from SA/workload).
   - single-score policy is prohibited: aggregation into one number without a breakdown by L1/L2/L3 and scenarios is considered insufficient.
   - if a policy improves one layer locally, but degrades the global SA picture (for example, attention tunneling between P/F/M), the policy is not considered ready for production preset.

## Consequences

- Skia becomes a standard tool for the CIDE surface layer, but not a replacement for the entire UI platform.
- Technical debt is controlled through wave rollout and dual-path.
- Architecture invariants (0036/0046/0047) are not blurred for the sake of migration speed.
- Rollout gets explicit SA-gates: design solutions are tested as hypotheses with measurable effect, and not just against engineering/visual criteria.

## Open questions

- At the time of this ADR there are no open questions; further clarifications are recorded in separate ADR updates.