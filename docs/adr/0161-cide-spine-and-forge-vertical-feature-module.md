# ADR 0161: CIDE spine + vertical Forge feature module

**Status:** Accepted  
**Date:** 2026-06-13  
**Related:** [0005](0005-defer-dynamic-plugins-mef.md), [0006](0006-presentation-layers-and-feature-slices.md), [0024](0024-ide-sdk-and-stable-contracts.md), [0153](0153-slash-catalog-only-resolution.md)–[0160](0160-forge-slash-catalog-runtime-overlay.md), FORGE-ADR-0015 (agent-forge)

## Резюме

- **Spine** CIDE — editor, slash runtime, cockpit/shell, `CascadeIDE.Contracts`, bundled `intent-catalog.toml` (не forge).
- **Vertical slice `Forge`** — один in-solution модуль владеет **всем** клиентским срезом forge: connect, capabilities overlay, execute, CRS Lens, MCP handlers, bracket refs.
- **Не MEF** ([0005](0005-defer-dynamic-plugins-mef.md)): code-first `ICascadeFeatureModule` ([0024](0024-ide-sdk-and-stable-contracts.md)); dynamic DLL-host — позже, те же контракты.
- **Catalog:** bundled TOML + runtime overlay ([0160](0160-forge-slash-catalog-runtime-overlay.md)); forge SSOT = `GET /api/v1/capabilities`.

---

## Контекст

Forge на host уже vertical plugins ([FORGE-ADR-0015](../../../agent-forge/design/FORGE-ADR-0015-vertical-domain-plugins-and-doi-commands.md)): Issue, MR, ViewShell, `capabilities.commands[]`.

CIDE — **второй клиент** того же DOI-каталога, не дублирующий TOML. [0160](0160-forge-slash-catalog-runtime-overlay.md) закрыл Phase D (overlay + execute).

Сейчас forge-код **размазан** по репо (работает, но ownership неочевиден):

| Область | Путь сегодня |
|---------|----------------|
| Connect (OAuth/device) | ~~`Features/WorkspaceNavigation/Application/ForgeLens*Connect*.cs`~~ → `Features/Forge/Lens/` |
| CRS Lens client | ~~`Features/WorkspaceNavigation/Application/ForgeLensCorrespondenceClient.cs`~~ → `Features/Forge/Lens/` |
| Capabilities + overlay + execute | ~~`Services/Forge/Forge*.cs`~~ → `Features/Forge/Infrastructure/` |
| Slash merge | `Features/Chat/SlashLineResolver.cs`, `ChatSlashCommandCatalog.cs` |
| MCP forge handlers | ~~`Features/IdeMcp/...Handlers.Forge.cs`~~ → `Features/Forge/Mcp/ForgeMcpHandlers.cs` (spine dispatches via `ForgeFeatureModule`) |
| Bracket `[FRG:…]` | `Features/Forge/Infrastructure/BracketForgeReferenceParser.cs` |

Без явного **vertical module** следующий zoo-контур (Intercom-style) снова размажет handlers.

---

## Решение

### 1. Spine (MIT, не выкидыается)

**Spine** — то, без чего IDE не IDE и что **не** принадлежит одному backend:

| Spine | Примеры |
|-------|---------|
| Editor substrate | `Features/Editor/` ([0103](0103-editor-surface-adapter-and-stabilized-input.md)) |
| Slash runtime | `IntentMelody/`, `Features/Chat/SlashLineResolver`, bundled catalog |
| Cockpit / shell | `Cockpit/`, `Features/UiChrome/` |
| Contracts | `CascadeIDE.Contracts` ([0024](0024-ide-sdk-and-stable-contracts.md)) |
| MCP host surface | `Features/IdeMcp/` (dispatch), не domain handlers |

**Правило:** spine **не** знает forge REST paths; только **контракты** (`ICascadeFeatureModule`, slash merge hook, MCP dispatch table).

### 2. Vertical slice = `Features/Forge` (ownership)

**Целевой владелец** forge-клиента в CIDE — неймспейс `CascadeIDE.Features.Forge` (папка `Features/Forge/`).

Vertical slice **владеет**:

| Срез | Ответственность |
|------|-----------------|
| Workspace config | чтение `[workspace.forge]` ([0158](0158-forge-lens-crs-overlay.md)) |
| Auth connect | device/OAuth → token in secrets |
| Catalog overlay | [0160](0160-forge-slash-catalog-runtime-overlay.md): fetch, merge, clear |
| Command execute | `POST /api/v1/commands/execute` (кроме local-only `forge.artifact.goto`) |
| CRS Lens | read-only L2 overlay ([0158](0158-forge-lens-crs-overlay.md)) |
| MCP | `forge.*` tool handlers (write path — MCP-first per FORGE-ADR-0003) |
| Brackets | `[FRG:…]` parse/nav ([0159](0159-bracket-forge-artifact-reference.md)) |

**Не владеет:** issue/MR **server** schema, WitDB, web ViewShell — **forge-host** (agent-forge).

### 3. Регистрация (0024, без MEF)

Каждый vertical module реализует **`ICascadeFeatureModule`** (когда [0024](0024-ide-sdk-and-stable-contracts.md) wire завершён; до wire — **явный** `ForgeFeatureRegistration` в composition root):

```csharp
// Sketch — CascadeIDE.Contracts.Experimental
public interface ICascadeFeatureModule
{
    string Id { get; }  // "forge"
    void Register(ICapabilityRegistry registry);
}
```

**Forge module регистрирует:**

- `CommandCapability` — bundled paths **не** дублируют `/forge *` (0160)
- `ServiceCapability` — `IForgeConnectionService`, overlay refresh
- hooks: `ISlashCatalogContributor` (overlay), `IMcpHandlerContributor`

**Dynamic plugins ([0005](0005-defer-dynamic-plugins-mef.md)):** тот же `ICascadeFeatureModule`, загрузка DLL — отдельный ADR после стабилизации cockpit slots ([0021](0021-pfd-mfd-cockpit-attention-model.md), [0025](0025-sdk-attention-zones-and-capabilities.md)).

### 4. Параллель Forge ↔ CIDE

| Forge (host) | CIDE (client) |
|--------------|---------------|
| `Plugin.Issue` vertical DLL | `Features/Forge` — issue **client** (slash, MCP, nav) |
| `capabilities.commands[]` SSOT | overlay merge ([0160](0160-forge-slash-catalog-runtime-overlay.md)) |
| `/issue open` (web) | `/forge issue open` (full DOI path) |
| MCP `forge.issue.open` | same `commandId` / execute |

### 5. Миграция (incremental, не big-bang)

| Phase | Deliverable |
|-------|-------------|
| **F0** | ADR 0160 + wire overlay — **done** (`Features/Forge/Infrastructure`, Chat merge) |
| **F1** | `Features/Forge/README.md` ownership map; ADR 0161 (this) — **done** |
| **F2** | Move `Services/Forge/*` → `Features/Forge/Infrastructure/` — **done** |
| **F3** | Move `ForgeLens*` from `WorkspaceNavigation` → `Features/Forge/Lens/` — **done** |
| **F4** | `ForgeFeatureModule : ICascadeFeatureModule` — single registration site — **done** |
| **F5** | Architecture analyzer **CASCOPE043**: forge client namespaces only from allowlisted hooks — **done** |

**Не блокер:** F2–F5 можно параллелить с cockpit; overlay уже работает в F0.

### 6. Guardrails (CASCOPE parity)

По духу FORGE-ADR-0015 §8 и `CascadeIDE.ArchitectureAnalyzers`:

- `/forge *` paths **only** from overlay when connected — **не** в bundled `intent-catalog.toml`
- новые forge MCP tools — только в `Handlers.Forge` / `Features/Forge`
- **FORGE drift (dev):** optional export capabilities → CI diff ([0155](0155-documentation-code-correspondence-and-architectural-drift.md))

---

## Последствия

### Positive

- Один ownership для forge-клиента; проще Phase D maintenance и zoo.
- Spine остаётся lean; forge = opt-in via `[workspace.forge]`.
- Готовность к plugin-host без смены mental model.

### Negative / trade-offs

- F2–F3 — churn namespaces/imports (**done** 2026-06-13).
- До полного wire capability-map forge MCP уже централизован в `ForgeFeatureModule`; descriptor registration — по мере 0025.

---

## Отклонённые альтернативы

| Alternative | Why not |
|-------------|---------|
| MEF plugin host сейчас | [0005](0005-defer-dynamic-plugins-mef.md); нет стабильных slots |
| Весь forge в `Services/` | ломает [0006](0006-presentation-layers-and-feature-slices.md) vertical slices |
| Дублировать forge commands в TOML | [0160](0160-forge-slash-catalog-runtime-overlay.md); dual maintenance |
| Forge logic в `MainWindowViewModel` | God VM anti-pattern ([0006](0006-presentation-layers-and-feature-slices.md)) |

---

## Связь с agent-forge

| Doc | Role |
|-----|------|
| FORGE-ADR-0015 §3.3 Phase D | server contract; CIDE = this ADR + [0160](0160-forge-slash-catalog-runtime-overlay.md) |
| FORGE-ADR-0003 | Lens, anchors, MCP write |
| FORGE-ADR-0014 | `capabilities` endpoint |
