# Agent notes: freshness W1

## Context
KB full-a freshness MLP. Operator steer: ship KB freshness as MLP (not MVP).

## W1 Scope
1. Scene — what "fresh" means for this repo
2. Watchlist — which KB paths matter
3. Scan — how to detect stale/changed
4. Digest — summary format
5. Cache — aliases + explain (not raw)
6. Explain stub — how to read digest

## Repo KB paths
- `docs/adr/` — ADR registry
- `docs/architecture/` — architecture maps
- `.cdp/domain/` — domain cards
- `.cdp/plans/` — plans
- `agent-contributions.md` — agent registry

## Scan method
- git status for tracked changes
- file mtime for untracked
- cross-ref with watchlist

## Next
- W2: digest deltas + actionable stale list
