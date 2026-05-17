# ADR 0105: Hybrid Codebase Index (core + MCP) for C# stacks with Roslyn truth

**Status:** Accepted · Implemented  
**Date:** 2026-05-06  

## Related ADRs

| ADR | Role |
|-----|------|
| [0039](0039-workspace-navigation-affordances.md) | Workspace navigation — multiple views and “current file + related” |
| [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) | LSP (C# / Markdown) — command line in `settings.toml`: presets, optional keys, environment override |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | Agent contract CLI (MCP parity) and snapshot tests |
| [0053](0053-semantic-map-control-flow-pfd.md) | Intent map and control flow on PFD |
| [0056](0056-semantic-map-pipeline-adoption.md) | Semantic map adoption of Skia composition pipeline |
| [0067](0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces — shared contract for graph screen family |
| [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | Markdown Preview — MFD instrument, renderer-first decoupling, no inline preview in document |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS vs CDS; AXAML index — not IDS |
| [0095](0095-workspace-solution-ide-health-stratification.md) | Three Health levels — Workspace, Solution, IDE (channel taxonomy) |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | Cockpit compute units (CCU; LRU *Unit* analog) — layer between transport, meaning, and channel |
| [0098](0098-semantic-first-document-as-projection.md) | Semantics first; document and repository as projections (Semantic-First) |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE DataBus — typed events and state projections |
| [0100](0100-project-constitution.md) | Project constitution |
| [0101](0101-licensing-and-commercialization-strategy.md) | Licensing and commercialization strategy |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | Data Acquisition Layer — external interfaces and adapters boundary |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | Hybrid Codebase Index — CascadeIDE integration, freshness, Semantic Map |

## Summary

- **Hybrid codebase index:** portable core + MCP; **Roslyn is truth for C#**.
- SQLite **FTS5** (keyword) + optional **vec** (semantic); fusion α/β.
- Scope: C#, Razor, AXAML, web stacks in one workspace; ADR 0106 — CIDE integration.

---

<a id="adr0105-glossary"></a>

## Terms and abbreviations

Working definitions **within this ADR**; algorithm details — per SQLite / chosen embedding provider documentation.

| Term | Meaning here |
| --- | --- |
| **FTS** (*full-text search*) | Full-text search: index and queries over **tokens/words** inside document texts (file or chunk), not only exact field match or filename search. |
| **FTS5** | Fifth **SQLite** full-text module: FTS5 virtual tables, inverted index “term → document occurrences”, relevance-aware queries. In this ADR — primary **keyword** backend of layer B. |
| **Inverted index** | Structure “word/term → list of documents (and positions)” backing fast FTS; not to be confused with Roslyn **symbol graph**. |
| **BM25** (*Best Matching 25*, Okapi BM25 family) | Class of statistical **ranking functions** for full-text hits: balance “term frequent in this document” vs “term rare in corpus”. In SQLite FTS5 relevance uses auxiliary rank functions (including **`bm25()`**); in this ADR “keyword / BM25” means **full-text with such ranking**, not a separate engine outside SQLite. |
| **Keyword search** | Search by **word/phrase match** (via FTS), without required “understanding” of the query in different phrasings. |
| **Embedding** | Fixed-dimension vector from a model over text (code fragment, paragraph, query). Semantically similar texts ideally get **close** vectors in the chosen metric. |
| **Semantic / vector search** | Select fragments by **embedding proximity** of query and chunks (cosine similarity, etc.), not keyword match. In this ADR also **vec** (vector channel). |
| **Vector store** | Storage for vectors and metadata (chunk id, path, line range), with nearest-neighbor operations (ANN / full scan at small scale). |
| **sqlite-vec** | **SQLite** extension for vector storage and query; in this ADR — optional local vector store **beside** FTS, not replacing the keyword layer. |
| **Fusion** | Merge hit lists from **two channels** (here FTS and vec): score normalization, weighted sum or equivalent, final top‑N. See [§ fusion sketch](#adr0105-impl-sketch-fusion). |
| **Chunk** | Continuous file fragment indexed as one FTS/vec unit (line window, logical block, etc.); see [§ chunking](#adr0105-impl-sketch-chunking). |
| **MCP** | *Model Context Protocol* — transport and tool contract for agents/IDE; separate index MCP service in [§ deployment](#adr0105-deployment). |
| **DAL** | *Data Acquisition Layer* — layer for data from workspace and external world per [0102](0102-data-acquisition-layer-boundary-and-contract.md). |
| **CCU** | *Cockpit Compute Unit(s)* — packaging compute results into stable channel DTOs per [0097](0097-cockpit-compute-units-transport-to-channel-dto.md). |

---

<a id="adr0105-context"></a>

## Context

CascadeIDE is an MCP-first IDE: the agent must orient quickly in the codebase and assemble context in a small model window (or under a limited step/call budget).

For **any** .NET/C# solution we already have a “source of truth” for precise semantic operations:

- Roslyn (via roslyn-mcp and IDE wiring) for: diagnostics, go-to-definition, find-usages, rename, symbol-level navigation.

But Roslyn does not fully solve:

- fast “sense overview” and a “first map” of the solution without reading dozens of files;
- full-text and orientation over **Markdown**, configs, `.csproj` / `.sln` / `.slnx`, YAML/TOML, the **web layer** (**Razor/Blazor `.razor`**, HTML/CSS), **Avalonia (`.axaml`)** markup, and other artifacts **without** a Roslyn semantic model for those formats;
- for a **plain** C# project (including **CascadeIDE** itself) the same hybrid layer gives fast keyword/optional semantic over the **whole repository** — including **`.cs` as text** ([layer B](#adr0105-layer-b): FTS only, not symbols), while rename/impact stay on Roslyn;
- persistence across sessions: the “map” should live beside the project/IDE profile and not require re-training the agent every time.

External solutions exist (e.g. SocratiCode) with hybrid search + graph + impact, but they add infrastructure load (Docker/Qdrant/Ollama) and license risk (AGPL) for product integration.

Additionally: CascadeIDE is cross-platform (Avalonia). We do not want the critical navigation layer tied to Windows-only/drivers/Docker, but on Linux we may allow heavier backend options.

---

<a id="adr0105-decision-summary"></a>

## Decision in one sentence

Introduce a **two-layer navigation model**: **Roslyn is truth for C# semantics**, beside a **light hybrid index** over the **solution contour**: web artifacts (`.razor`, MD, HTML/CSS), **Avalonia `.axaml`** (and pairing heuristic with code-behind `.cs` when needed), configuration and companions (**including optional full-text on `.cs` as text**, without replacing symbol-level operations); keyword + optional semantics; minimal ops cost and cross-platform support.

---

<a id="adr0105-goals"></a>

## Goals

1. **Reduce agent step count**: 1–2 calls → enough relevant context to decide.
2. Provide a “first map” without “read 20 files”: top files/nodes/flows, entry points — for **Blazor/Web**, **Avalonia (AXAML + bindings/control names)**, and **plain C#**, including **developing CascadeIDE itself** on the same tool stack.
3. Preserve **semantic correctness**: C# refactor-impact is Roslyn-based, not heuristic.
4. Work **without mandatory Docker** (especially on Windows), with predictable local install/update.
5. Be cross-platform (Windows/Linux/macOS), with optional backend accelerators on Linux.

---

<a id="adr0105-non-goals"></a>

## Non-goals (first phase)

- Full “polyglot dependency graph” across 18+ languages.
- Replacing Roslyn MCP: Roslyn remains the truth layer for C#.
- Mandatory vector DB/containers for baseline scenarios.
- “One graph that is always right”: graph/impact outside C# allows heuristics and needs verification.

---

<a id="adr0105-architecture"></a>

## Architecture (by layer)

<a id="adr0105-layer-a"></a>

### Layer A: Roslyn truth (C#)

Use Roslyn for:

- diagnostics / code actions;
- find usages / rename;
- symbol navigation;
- (where possible) call graph / entrypoints within a C# project.

This layer is **precise** but “expensive” in workflow: the agent still needs to know *what to search for*.

<a id="adr0105-layer-b"></a>

### Layer B: Hybrid index (artifacts around C#, web layer, Avalonia AXAML, optional `.cs` text)

Index for files and fragments **outside** Roslyn symbolism or **as text** (not as a type graph):

- `.razor`, `.razor.cs` (including partial / file pairing);
- `.md` / `.mdx`;
- `.html`, `.css`, `.scss` (including `@import`, classes/selectors);
- basic configs (`appsettings*.json`, `.editorconfig`, `*.props`, `*.targets`, `*.csproj`, `*.slnx`, pipeline YAML, `*.yml`, `*.toml`, etc.);
- **`.axaml`** (and typical code-behind `*.axaml.cs` if present): markup and attributes — **as text for FTS** and light heuristics (`x:Name`, `{Binding …}`, `Classes=`, `avares:` paths); **not** a substitute for an Avalonia XAML parser, **not** CDS/IDS semantics (see [0079 — CDS vs IDS](0079-ide-display-system-ids-overlay-pipeline.md#adr0079-cds-vs-ids));
- **`*.cs` (index option):** **full-text/keyword only** (identifiers and strings match as text in the file); **rename/find-usages/impact** remain Roslyn-only. Tool responses must mark `.cs` hits as **text-ranked** so they are not mixed with symbol truth.

The index provides:

- **keyword / BM25**: config strings, CSS, Razor routes, `.cs`/`.axaml`/doc fragments;
- **optional semantic**: “by meaning” search (embeddings), without mandatory Docker.

Index data:

- stored locally (IDE profile or beside the project);
- updated incrementally (watcher + hash);
- explicit format versioning (so migration does not break UX).

<a id="adr0105-storage"></a>

#### Storage / backend (baseline)

Recommended default (no Docker, cross-platform):

- **Keyword/BM25**: SQLite **FTS5** (on-disk local DB) as fast full-text index.
- **Semantic vectors (optional)**: SQLite + **`sqlite-vec`** as local vector store (enabled only when semantics are on).

The engine here is **classic SQLite** (e.g. `Microsoft.Data.Sqlite` or another provider to the same SQLite library), **not** [WitDatabase](https://github.com/dmitrat/WitDatabase) (`*.witdb`): Wit stays for CascadeIDE application data; the index file is a separate on-disk SQLite.

Important: hybrid = **FTS (keyword)** + **vec (semantic)** as two independent sub-indexes merged at the service layer (ranking/fusion), not “one DB magic”.

<a id="adr0105-layer-c"></a>

### Layer C: Composition (agent workflow, portable)

Default agent scenario (outside a specific IDE):

1. Hybrid search (fast, cheap) → top-N fragments and map.
2. Roslyn navigation for precise C# check/refactor.
3. Point reads of files/fragments only after search.

**Embedding this scenario in CascadeIDE** (buttons, channels, debounced reindex, CCU/DataBus, Semantic Map) — **[ADR 0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)**.

<a id="adr0105-deployment"></a>

### Deployment: library + separate MCP

Package the index as a **shared library** (core: indexing, SQLite, request/response formats) and a **separate MCP server** (thin stdio layer + tool registration) so that:

- search can be used **outside CascadeIDE** (other MCP IDE/agents, CLI, automation);
- the heavy process (watcher, SQLite files, optional embeddings) is isolated: restarts and updates do not mix with Avalonia/UI.

CascadeIDE may use **the same core in-proc** or launch **the same MCP binary** as a child process — **tool ids and contracts** stay shared for both (cockpit placement details — [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)).

---

<a id="adr0105-config-ux"></a>

## Configuration and UX invariants

- **Off-by-default for infrastructure**: if semantic embeddings need an external provider, that must be opt-in.
- **Cross-platform**: same tool ids/contracts in MCP; difference only in backend provider.
- **Small-window operation**: tool responses should be “compact by default” (top-N, with path/range/score), with a separate command to expand.

---

<a id="adr0105-impl-watchouts"></a>

## Implementation watchouts

Operational points without which dogfood and production disappoint quickly:

<a id="adr0105-impl-watchouts-volume"></a>

1. **Volume and noise.** FTS over all `*.cs` inflates the index and can **pollute top-N** with raw string hits. Need explicit **defaults and filters** in `settings.toml` (or equivalent): ignores/`gitignore` alignment, path masks, **ranking** (e.g. prioritize docs/configs over “raw” `.cs`, or the opposite — “code first” mode), ability to temporarily exclude `*.cs` from FTS without disabling the rest of the index.

<a id="adr0105-impl-watchouts-freshness"></a>

2. **Freshness** on saves from **CascadeIDE**. Cheap increment and lag-free UX — **[ADR 0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)**. MCP/core may use a watcher and incremental reindex; product tie-in with the editor session — in the IDE.

<a id="adr0105-impl-watchouts-hit-kind"></a>

3. **MCP contract from the first prototype.** Search response structure needs a **stable hit-type field** (e.g. `hit_kind`: `text_fts` / `text_vector` / `symbol_followup_roslyn` or equivalent) so agent and human do not guess from free text. Changing field semantics later costs more than baking it in v0.

---

<a id="adr0105-alternatives"></a>

## Alternatives and why not (for now)

<a id="adr0105-alt-roslyn-grep"></a>

### A) “Roslyn + grep only”

Pros: minimal infrastructure, high C# accuracy.  
Cons: too many steps and file reads for agent scenarios; poor coverage of docs/config/web and **global** “where mentioned” across the repo without a heavy Roslyn-only sweep.

<a id="adr0105-alt-socraticode"></a>

### B) Embed SocratiCode wholesale

Pros: ready hybrid+graph+impact layer, fast “orientation” on a large repo.  
Cons:
- ops: Docker/Qdrant/Ollama in baseline;
- graph correctness outside C# depends on heuristics;
- **AGPL license** — undesirable for product embedding (see [0101](0101-licensing-and-commercialization-strategy.md)).

<a id="adr0105-alt-lsp-all"></a>

### C) LSP for everything (full polyglot)

Pros: potential semantic accuracy per language.  
Cons: too large operational and integration cost; does not solve “small window/few calls” without a separate index/ranking layer.

---

<a id="adr0105-consequences"></a>

## Consequences

<a id="adr0105-consequences-positive"></a>

### Positive

- The agent gets a fast “first pass” over the solution **and** can **dogfood** the same index while developing **CascadeIDE** and other C# repos, not limited to “Blazor only”.
- Roslyn remains “truth” for dangerous operations (rename/impact/diagnostics).
- Docker becomes optional: Windows-friendly baseline; Linux may get extended modes.

<a id="adr0105-consequences-risks"></a>

### Negative / risks

- A new data layer (index) → versions, migrations, observability needed.
- Risk of false links in `.razor`/CSS/HTML heuristics → need “confidence” and explicit “hint” labeling.
- Indexing `.cs`/`.axaml` as text may **look like** “semantic find” → see [§ implementation watchouts](#adr0105-impl-watchouts) ([`hit_kind`](#adr0105-impl-watchouts-hit-kind), [ranking](#adr0105-impl-watchouts-volume)).
- Tools must stay compact or the hybrid index may spam context and hurt UX.

---

<a id="adr0105-rollout-plan"></a>

## Rollout plan (portable core + MCP): status

| Step | Content | ADR 0105 scope (implemented) |
| --- | --- | --- |
| 1 | MCP contracts (`search`, `status`, `reindex`, `explain-result`, version/`hit_kind`); core in library | ✅ **`hybrid-codebase-index`** repo |
| 2 | Keyword index, increment, ignores; optional FTS on `*.cs`; watcher tool | ✅ |
| 3 | Razor / AXAML: `.razor`↔`.razor.cs`, `.axaml`↔`.axaml.cs` pairs; heuristic headers `__hci_*` (directives, resources, bindings, tags) | ✅ (`HybridCodebaseIndex.Core` augment) |
| 4 | Embeddings opt-in (`settings.toml`), sqlite-vec optional | ✅ |
| 5 | IDE workflow + freshness on save | → **[ADR 0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)** |
| 6 | Scope defaults, FTS chunking, FTS+vec fusion | ✅ `settings.toml` + hybrid search |

---

<a id="adr0105-impl-sketch-scope-chunk-fusion"></a>

## Sketch: index scope, chunks, fusion (FTS + vec)

Addendum to [rollout plan](#adr0105-rollout-plan): reasonable defaults at spike, without changing top-level architecture ([layer B](#adr0105-layer-b), [storage](#adr0105-storage)).

<a id="adr0105-impl-sketch-scope"></a>

### Index scope

- **Primary anchor:** active `.sln` / main `.csproj` of the CascadeIDE profile — same workspace contour as the Roslyn session.
- **Default extension:** paths under **workspace root**, minus aligned **`.gitignore`** (and if needed `.cursorignore` against agent noise) and a **hard denylist**: `bin/`, `obj/`, `node_modules/`, `.git/`, typical tool cache dirs.
- **Monorepo:** one index DB per **(workspace_root, solution_path)** pair; another solution in the same tree — separate index contour (switch by profile). Field **`extra_include_roots`** in `settings.toml` for sibling dirs (docs, external KB, etc.) — **opt-in**.

<a id="adr0105-impl-sketch-chunking"></a>

### Chunking for FTS

| Type | Strategy |
| --- | --- |
| Compact configs, small `.md`, `.razor` within size limit | One FTS document per file; upper document size limit (e.g. 256–512 KiB) — configurable. |
| Long `.md`, `.cs`, `.axaml` | Sliding **line windows** (guide: 80–120 lines, overlap 10–15); stable `chunk_id`: path + range (`start_line` / offset). |
| `.razor` | Prefer **logical boundaries** (`@code`, large markup blocks); if not cheap — same line windows. |

**Freshness:** on edit rebuild only affected chunks; for small files whole-document rebuild is allowed. Tool response always gives **path and line range** (or offset) so agent and human open the point without guessing.

<a id="adr0105-impl-sketch-fusion"></a>

### Fusion of keyword (FTS) and semantic (vec), v0

1. Independently get **top‑K** from FTS and vec (internal K, guide 20–40; outward after merge — compact top‑N).
2. **Normalize** scores within each channel (min-max or rank-based, e.g. `1/(rank+R)`).
3. Merge unique chunks: final score **`S = α·S_fts + β·S_vec`**; if a chunk is missing in a channel — that channel’s contribution is **0**.
4. **Default with vec on:** `α ≈ 0.65`, `β ≈ 0.35`; with vec off — FTS only.
5. **Short query (1–2 tokens)** or low max `S_vec`: boost FTS or do not mix vec (keyword-dominant mode).

In the DTO, keep **both contributions** (`fts_score`, `vec_score` when present) with `hit_kind` and final rank — so explainability (“why in top”) is preserved. Thresholds and weights in `settings.toml` without breaking response format on later iterations.
