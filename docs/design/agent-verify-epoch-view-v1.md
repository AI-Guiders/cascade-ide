# UX spec: Agent Verify Epoch View v1

**Статус:** Accepted (W3 target)  
**Дата:** 2026-05-31  
**Норма:** [ADR 0148 §8–§10](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md)  
**Naming:** [naming-layers-v1.md](naming-layers-v1.md) §1  
**Parity:** [0002 debug-human-agent-parity](../adr/0002-debug-human-agent-parity.md)

## Цель

Оператор видит **тот же verify state**, что orchestrator и агент — не «агент сказал green» (Cursor), а **epoch + rung ladder + snapshot + time slices**. Human и agent — **один** `VerificationLadder` API и **одни** DataBus projections.

---

## Surfaces (MLP)

| Surface | Роль | Wave |
|---------|------|------|
| **Chat trace** (collapsible per run) | Primary narrative; `/agent last` | W3 ✓ partial |
| **PFD `AgentEnvironmentInstrument`** | Glance: active task, cancel, stale glyph | W3 |
| **Slash** `/agent verify\|status\|cancel\|last` | Operator parity | W3 ✓ partial |
| **Gutter / editor chrome** | `AgentVerifyEpochStale` → dim files in dirty set | W3+ |
| **Error list / F8** | `diagnose.files` parity (not substitute for epoch) | existing |

---

## Widget: Verify Epoch block

Один блок на **verify epoch** (не на каждый environment task в isolation). Header + rung list + footer.

### Header

```text
Verify · {verify_policy} · epoch {short_id}
snapshot {verify_snapshot_id_short} · {git_head_7}{dirty_suffix}
```

| Field | Source |
|-------|--------|
| `verify_policy` | `minimal` \| `standard` \| `strict` \| `ci_parity` |
| `short_id` | first 4–6 of `run_id` or epoch id |
| `verify_snapshot_id` | HEAD + hash dirty paths @ start |
| `dirty_suffix` | ` · 3 files dirty` if non-empty |

**Stale header overlay:** `⚠ verify устарел — snapshot изменился ({reason})`  
Reasons: `write_in_epoch` \| `superseded` \| `cancel` from `AgentVerifyEpochStale`.

### Rung list (ordered)

Для каждого rung в climb (monotonic до fail или policy cap):

```text
  ✓ diagnose.files      0.8s
  ✓ build.affected      9.1s
  ⟳ test.scoped         running…  [Cancel]
  ✗ compile.project     failed (2 errors)  [Details]
  — test.full           (not required)
```

| Glyph | State |
|-------|-------|
| ✓ | pass |
| ⟳ | running ( spinner ) |
| ✗ | failed (compile/test/diagnostics) |
| ⊘ | cancelled |
| ☠ | host died (`AgentEnvironmentTaskDied`) — **not** failed tests |
| — | skipped / not required by policy |

**Labels:** semantic id + RU tooltip from naming registry; **no** `L0`–`L4` in UI.

**Details** opens structured DTO: diagnostics[], test_results[], log_ref — not raw stdout only.

### Footer (time accounting)

```text
Reasoning: 4.2s · Environment: 16.8s · Blocked: 0.3s
Status: green (standard) · max rung: build.affected
```

**Green rule (MLP):** показывать **green** только if all:

1. `AgentRunCompleted.green == true`
2. `max_rung_reached` ≥ policy minimum rung
3. epoch **not stale** on current workspace snapshot

If agent text says «done» but epoch stale → show **⚠ not verified on current snapshot** (anti Cursor-green).

---

## PFD instrument (compact)

Single line + expand:

```text
⟳ build.affected 9.1s  [Cancel]     ·  standard
```

Expand → same rung list as chat (shared ViewModel from DataBus projection).

Stale: amber border + `⚠ stale`.

Died: red + `☠ environment died` + [Retry verify].

---

## DataBus subscription (ViewModel)

| Event | UI action |
|-------|-----------|
| `AgentRunStarted` | New epoch block; reset rungs |
| `AgentRunPhaseChanged` | Update phase chip (reasoning / environment) |
| `AgentEnvironmentTaskChanged` | Map `task_kind` → rung; progress |
| `AgentEnvironmentTaskCompleted` | Rung ✓/✗; duration |
| `AgentEnvironmentTaskDied` | Rung ☠; alert |
| `AgentVerifyEpochStale` | Header overlay; gutter dim hook |
| `AgentRunCompleted` | Footer green/red; freeze rung list |

Payload field **`max_rung_reached`**: semantic string (`build.affected`), not `L2`.

---

## Human parity checklist

- [ ] `/agent verify standard` renders **identical** block to autonomous run
- [ ] Cancel instrument cancels runner + supervised host token
- [ ] Write to file in `in_verification` set → stale **before** compile finishes
- [ ] Two parallel verify chains **impossible** (UI shows single active epoch)
- [ ] Failed tests (✗) visually distinct from host died (☠)

---

## Non-goals (W3)

- `idle_user` time slice in UI (W3+ analytics)
- Auto-rollback operator tree on failed verify
- L4/ci_parity as default policy

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-31 | v1: MLP surfaces, green rule, semantic rung labels |
