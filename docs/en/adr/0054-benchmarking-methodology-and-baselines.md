<!-- English translation of adr/0054-benchmarking-methodology-and-baselines.md. Canonical Russian: ../../adr/0054-benchmarking-methodology-and-baselines.md -->

# ADR 0054: Performance benchmarks and baseline metrics

**Status:** Proposed  
**Date:** 2026-04-17

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD - Cascade IDE Cockpit Attention Model |
| [0049](0049-skia-surface-rollout-over-avalonia-host.md) | Staged rollout of Skia-surfaces at Avalonia-host (CIDE-wide) |
| [0053](0053-semantic-map-control-flow-pfd.md) | Intent map and control flow on PFD (control flow) |

---
## Context

The observed difference in memory consumption between different tools (for example, CIDE vs Cursor) affects product decisions and optimization priorities. Currently, such comparisons are made sporadically and do not have a repeatable protocol.

We need a unified way to measure and discuss performance without the “measured under different conditions” debate.

---

## Solution

1. Introduce a standard set of benchmark scripts for CIDE.
2. Record baseline metrics in the CIDE repository as an engineering artifact.
3. Use benchmarks as a gate for major UI/rendering/navigation changes.

---

## Scripts v1

- `idle`: the application is running, the solution is not loaded, 60 sec stabilization.
- `solution_open`: loaded generic `.sln`, without active build/test/debug.
- `editing`: medium-sized C# file open, normal navigation/scrolling.
- `semantic_map_file`: map in `file` level.
- `semantic_map_controlFlow`: map in the `controlFlow` level.
- `chat_active`: chat page is open, messaging is active (without long background tasks).
- `debug_session`: attach/launch + pause at breakpoint.

---

## Metrics v1

- Process memory:
  - `WorkingSet (MB)`
  - `PrivateBytes (MB)`
- CPU:
  - average load per observation window
  - peak load
- Response time:
  - cold start time until the finished window
  - loading time of the solution to a stable state
- Semantic map:
  - map update time after file/carriage change
  - number of nodes/edges in the resulting subgraph

---

## Measurement protocol

- OS, build (`Debug`/`Release`), target runtime and config are recorded in the report.
- For each scenario, at least 3 runs, report in the form of median + max.
- Compare only scenarios measured on the same machine and in the same profile.
- If an external reference is used (eg Cursor), this is specified as a `reference` rather than a hard SLA.

---

## Artifacts

- In the repository:
  - `docs/benchmarks/README.md` - how to run and read the results.
  - `docs/benchmarks/baselines/*.json` — baseline by date/branch.
  - `docs/benchmarks/reports/*.md` - human-readable reports.
- For CI (later): smoke benchmark on a limited set of scenarios.

---

## Consequences

Pros:
- comparisons become repeatable and transparent;
- it’s easier to justify optimizations and regressions;
- the “fast/slow” discussion is based on facts.

Cons:
- there is an operational burden on running and maintaining baseline;
- without discipline, the protocol quickly becomes outdated.

---

## Open questions

- Is it necessary to separate the baseline for `Debug` and `Release` as two equal contours?
- What scenarios will become mandatory for PR-gate in CI (except smoke)?
- Is it necessary to auto-publish a mini-dashboard of baseline changes between commits?