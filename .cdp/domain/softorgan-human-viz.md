# Domain: softorgan-human-viz

## Invariants
- Human-faced SoftOrgan zones = tree / graph / instrument cards — not status/ribbon text dumps.
- SoftOrgan seats (P|F|M) = chrome only; never steal MFD page.
- #CIDE done needs evidence= absolute `.png` of the right window (Read into chat); `shot=true` alone illegal.
- Editor situ WHY·BLAST text ribbon = REJECT; human face = situ card deck (WHY/BLAST/ROLE/DIFF/APPLIES).

## Entry
- Map SSOT: `scratch/softorgan-human-viz-map-2026-08-04.md`
- Editor situ card deck: `GlassGlanceCards.BuildEditorSitu` · `MainWindow.SharedSurface` UniformGrid · PNG `scratch/editor-situ-cards-20260804.png`
- Plan cards: `LatchPaint.Plan.cs` · `MainWindow.LatchEicas.cs` · `PlanWhy/Next/CourseReadout`
- RelatedFiles list+graph: `MainWindow.RelatedFilesSurface.cs` · `RelatedSkia` in `GlassMfdProcessHosts.xaml`
- Problems severity board: `MainWindow.ProblemsSurface.cs` · ERR/WARN/ALL cards + jump list
- FDS card deck: `MainWindow.GlanceCardsSurface.cs` · `GlassGlanceCards.BuildFds` · UniformGrid instrument cards
- HybridIndex map: `MainWindow.HybridIndexSurface.cs` · HCI cards + `HybridSkia` scope map
- Correspondence timeline: `MainWindow.CorrespondenceSurface.cs` · `GlassCorrespondenceFeed.BuildInstrument|BuildTimeline`
- Plan leaf board: `LatchPaint.Plan.cs` · `PlanLeafBoardList` · `CidePlanLatch.Board` (cdp-mcp)
- Arch board on SemanticMap: `GlassArchBoardGlance` · `MainWindow.SemanticMapSurface.cs` · AS_BUILT roles cards+list
- DomainBoard card deck: `GlassDomainBoardGlance` · `MainWindow.GlanceCardsSurface` page DomainBoard · `.cdp/domain/*.md` + latch/learn
- Capture: `tools/Capture-Window.ps1` · title `P/M` or `M · MFD host` or main `(P/F/M)`

## Antipatterns
- Claiming Editor situ text ribbon Done.
- Packing ROLE as `IN-MAP · Nn/Ee · map on MFD` (3 meanings + Trunc = Цикада) — split ROLE/HOPS/LOOK.
- Autoi-in-chat / status-list-as-verify / File.Exists alone = seeming.
- SoftFL / Meta / board-hygiene / inventory mill as work under sealed course.
- **DIG REJECT mill** — agent-invented close when operator asked dig=investigate/think/expand/analyze. DIG REJECT ≠ operator refuse.
- Plan leaf Face painting agent board chrome (`phase mismatch task@… · session=…`) or NEXT Sub with `dig=/domain=` verify args.
- DomainBoard / WorkspaceHealth / Hypotheses `glance · unavailable` with empty WorkspaceRoot = seeming (climb / latch-only must paint).

## last_ship
- **2026-08-08 Glass Done PreCondition STAMPED** — dig=`glass.md` PreCondition + plan ALL SURFACES + SoftOrgan Face Done · PrintWindow `CDP GlassCockpit · Windows` Soft:QRH cards live · evidence `tmp-glass-shots/glass-done-precondition-20260808.png` + `cdp_see`. SoftOrgan Meta CLOSED.
- **2026-08-08 SoftOrgan Face Done axis4** — Soft:QRH cards VERIFY (M·QRH situations+find) · dig=`tmp-glass-shots/softorgan-qrh-face-cards-20260808.png` + `cdp_see` · operator in-thread (chat open ≠ Soft:QRH fail; nested axb steer) · SoftOrgan Meta CLOSED.
- **2026-08-08 SoftFL Soft:QRH Face cards VERIFY** — Soft:QRH → `M · QRH` · situations cards (cabin-start/open-project/where-am-i/hung-glass) · find · not markdown wall. Evidence `tmp-glass-shots/softorgan-qrh-face-cards-20260808.png` (title=`CDP GlassCockpit · Windows`). SoftOrgan Meta CLOSED. Face Done axis4 = Soft:QRH VERIFY + operator steer.
- **2026-08-08 Glass Open/SE = CIDE SolutionParser SSOT** — linked `SolutionParser`+`ProjectFileTreeBuilder`+`FolderWorkspaceTreeBuilder`+`SolutionItem` into GlassCore; `GlassSession.SetSolutionOrProjectPath` = CIDE LoadSolution; WPF SE paints `SolutionItem` children. Evidence path: Ctrl+O→P→CdpMcp.csproj → tree. SoftOrgan Meta CLOSED.
- **2026-08-08 W0 Open/Load SoftFL** — Ctrl+O → F/P/D/R overlay · open file/sln/folder · MRU recent · `SetWorkspaceRoot` + SE refresh. Catalogs palette/chord `os/od/or/oo` · QRH «открыть проект» steps. Evidence `tmp-glass-shots/open-family-chord-20260808.png`. Plan `.cdp/plans/glass-cide-port-glance.md`. SoftOrgan Meta CLOSED · invent REJECT.
- **2026-08-08 SoftFL HERE/NEXT + Soft:QRH situations→steps (nested axb)** — operator: label deck ≠ «что дальше»; want aircraft QRH + always HERE. Ship: `OperatorSituationCatalog` (situations+steps) · SoftOrgan Face cards = situations (click→шаг N/M · дальше/назад/сделать/к списку) · MFD `HereNext` + palette `HERE/NEXT` + chord `hn` (GlassChordCatalog fallback) · HERE line locus. Build Release 0 err · Add-Type ChipsFor ok · evidence `tmp-glass-shots/softorgan-qrh-situations-20260808.png` + `softorgan-here-next-20260808.png` (CDP GlassCockpit · Windows · M·HereNext · шаг 1/4). dig=`ADR 0014`+`softorgan-human-viz` · SoftOrgan Meta CLOSED.
- **2026-08-08 SoftFL SoftOrgan Face cards+find** — operator eyes: QRH/ECL markdown wall ≠ one glance (Plan is the bar). Ship: `OpenSoftOrganFace` → MFD `QRH|ECL|Alert` glance deck (`SoftOrganFaceHandbook.ChipsFor` + `SoftOrganFindBox` filter) · not MarkdownPreview. Build Release ok · Add-Type ChipsFor ok · live PNG owed (Capture-Window hung; operator glance). dig=`softorgan-human-viz` · SoftOrgan Meta CLOSED.
- **2026-08-07 SoftFL Glass C-q QRH/ECL/alert Face path** — Ctrl+Q discover+run SoftOrgan EICAS family without SoftOrgan Meta reopen. `OpenSoftOrganFace` → citizen `@intent qrh|ecl|alert` PlaceOrgan · evidence `tmp-glass-shots/softfl-cq-palette-qrh-20260807.png` (palette Soft: QRH) · dig=`GlassCommandPaletteCatalog.cs`.
- **2026-08-07 Glass human-viz nested SoftFL CLOSED** — operator ask finish SoftOrgan human viz via nested[axb]. Lived residual: ProductSpineStrip `Glass PreCondition · A6 ADOPTED denser…` + MarkdownPreview pressure-LATEST dump. SoftFL: `ChatProductSpinePresentation.FormatFaceStrip` · MD drop pressure fallback · EICAS `FormatEicasFace` SoftFL jargon strip · latch spine humanized. Unit Add-Type: `Glass Done · message select ready`. Live PNG `tmp-glass-shots/softorgan-nested-after-softfl-20260807.png` (spine=`Glass Done · SoftOrgan human viz · instruments people can fly`) + MFD sweep `tmp-glass-shots/softorgan-sweep-{problems,flightdatastorage,domainboard,editor,markdownpreview}-20260807.png`. Map ok zones = verify not invent. SoftOrgan human-viz epic DONE.
- **2026-08-07 Plan Face agent-chrome SoftFL** — lived dig: OPEN card painted `phase mismatch task@act · session=explore` + NEXT Sub `dig=…` · Ship: `PlanBoardLeaf.IsAgentBoardChrome` skip · `StripAgentVerifyArgs` · FormatGlanceNext Sub=cleaned · evidence `tmp-glass-shots/plan-face-phase-chrome-softfl-20260807.png` (CDP GlassCockpit · Windows · PLAN) · OPEN 4→3 · phase mismatch gone
- **2026-08-07 Plan leaf-board SoftFL** — Consolas TM wall → FLY/OPEN/DONE strip + Face cards (`PlanBoardLeaf` + `MainWindow.PlanLeafBoard`) · default hide DONE wall · evidence `tmp-glass-shots/plan-leaf-board-instrument-20260807.png` (CDP GlassCockpit · Windows) · dig=operator ask expand not DIG REJECT theater
- **2026-08-07 Face live dogfood WHY+NEXT** — stash SEALED-marker → WhyLine=Glass Done (human flight); latch task/feature humanized; Glass FormatGlanceNext SoftFL/nested/agent-refuse fallback · evidence `.cdp/evidence/face-why-next-human-20260807.png` (CDP GlassCockpit · Windows · PLAN) · cdp-mcp `c5c6d1d` · cascade `8bc3e98a`
- **2026-08-07 Face theatre strip across LatchPaint peels** — Plan WHY/NEXT/board · SoftOrgan chrome · Seats tip · Intercom compact · IgniteWake tip · slash SoftFL jargon out · paired cdp-mcp IdeHumanFacePlan Why/Next/Pulse/Board on PublishGlass · no operator-eyes theatre on Face
- **2026-08-06 share-glass-axb** — IdeShare `with=operator` → FDS SHARE chip (share/v1 LATEST, project-first via `GlassIdeShareGlance`) · not co-presence latch · tests GlassGlanceCards 9/9 · live PNG `tmp-glass-shots/fds-share-ideShare-20260806.png` (M·MFD · SHARE=`share-20260806-0…`) · cdp-mcp mirror habitat+project (sibling hard deploy)
- **2026-08-06 glance-pages-ac CLOSED** — DIG ACCEPT SA glance Ready-to-Interact: Events READY · Env READY · Hyp paints MISSING · WH climb paints (was unavailable) · `GlassWorkspaceClimb` + always-build WH/Hyp · tests TryProbe_null_root 2/2 · evidence `tmp-glass-shots/glance-{events,wh-climb,env,hyp}-ac-20260806.png` (M·MFD). Extra hands beyond refresh DIG REJECT.
- **2026-08-06 climb score** — Prefer .git/.sln/workspace.toml over thin .cascade-ide/.cdp (was GIT no / SLN none on project folder) · Score unit 1/1 · live WH READY · GIT yes · CascadeIDE.sln · `tmp-glass-shots/glance-wh-score-20260806.png`. Polish empty space = later.
- 2026-08-05 · cabin glass_scene ROLE human-face parity · `IdeGlassSurfaceChannel.BuildRoleInGraph` → ROLE=`сирота`/`в карте` + hops/look (not `ORPHAN·IN-MAP·map on MFD`) · GlassSurfaceIpcTests 2/2 · dig=domain antipattern Packing ROLE
- 2026-08-05 · file-situ Applies on locus CLOSED · diags Roslyn+build scoped · tests T-scoped wire (RefreshTestParse→situ) · unit 8/8 · live PNG `tmp-glass-shots/window-20260804-applies-semantic.png` (M·MFD APPLIES E1 W0 + tint L6)
- 2026-08-05 · gap 3.3 NEXT glance · `FormatGlanceNext` (Dig densest…CLOSED — residual) · Sub=full leaf · dark/scale DIG REJECT (CFG+Dark already LIVE) · evidence `tmp-glass-shots/gap33-next-glance-20260805.png`
- 2026-08-05 · Intercom prose residual · Citizen SA wall → Radio collapse · evidence `tmp-glass-shots/intercom-prose-radio-collapsed-20260805.png`
- 2026-08-05 · Topology 3-window dedicated dogfood `(intercom)(sit)(world)` · OneOf→Triple Sync fix (close hosts before EnsurePfd) · P Plan WHY/NEXT live · evidence `tmp-glass-shots/topology-3win-{forward,pfd,mfd}-fixed-20260805.png`
- 2026-08-05 · presentation latch merge live dogfood · mfd_page=Events alone kept topology `(intercom)(sit/world/alert)` · title `sit/world/alert · alert active · OneOf host` · evidence `tmp-glass-shots/latch-merge-alert-oneof-20260805.png` · cdp-mcp `67d03f0` hard-self 21:40Z
- 2026-08-05 · Topology OneOf v1 dogfood 2-window channel switch sit→world · titles `sit/world/alert · sit|world active · OneOf host` · evidence `tmp-glass-shots/topology-oneof-sit-active-20260805.png` + `topology-oneof-world-active-20260805.png` · latch merge fix cdp-mcp (mfd_page alone must not wipe topology)
- 2026-08-04 · Dig WHY+NEXT Glass face LIVE · WHY=sealed course · NEXT=TM leaf · COURSE=Shared-SSOT · evidence `tmp-glass-shots/window-20260804-plan-why-next-live.png` (title `P · PFD host`) · dig note `scratch/dig-why-next-glass-face-20260804.md`
- 2026-08-04 · Applies-on-locus live dogfood · semantic Roslyn Collect + CLEAN tone fix · APPLIES=`E1 W0 · problems on MFD` · tint L6 · evidence `tmp-glass-shots/window-20260804-applies-semantic.png` (title `M · MFD host`)
- 2026-08-04 · Surface wire `(intercom)(sit/world/alert)` · Scan anchors + ND channel stack · OneOf PreferSurface · evidence `tmp-glass-shots/window-20260804-surface-wire-0.png` (title `sit/world/alert · world active · OneOf host`) · cascade-ide `e253954e`/`f1a77c32`/`237bade2`
- 2026-08-04 · ROLE situ live PNG dogfood after Glass rebuild · ROLE=`в карте` · HOPS=`25 узлов · 96 связей` · LOOK=`карта → MFD` · evidence `scratch/role-situ-live-20260804.png` (P/M · OneOf · PrintWindow)
- 2026-08-04 · Editor situ ROLE split human labels · ROLE=`в карте`/`сирота` · HOPS=`N узлов · E связей` · LOOK=`карта → MFD` · tests 3/0
- 2026-08-04 · Editor situ WHY-file instrument cards · PNG `scratch/editor-situ-cards-20260804.png` (M·Editor · LEVEL SITU)
- 2026-08-04 · DomainBoard instrument cards · DOM LIVE · PNG `scratch/domain-board-cards-20260804.png`
- 2026-08-04 · Arch board on SemanticMap · cards+roles · PNG `scratch/arch-board-semanticmap-20260804.png`
- 2026-08-04 · Plan leaf board WHY/NEXT/COURSE + stage tree · PNG `scratch/plan-leaf-board-20260804.png`
- 2026-08-04 · Correspondence CRS cards + thread timeline · PNG `scratch/correspondence-timeline-20260804.png`
- 2026-08-04 · HybridIndex HCI cards + Skia scope map · PNG `scratch/hybridindex-map-20260804.png`
- 2026-08-04 · FDS card deck PLAN/SHARE/PRESSURE/WAKE · PNG `scratch/fds-card-deck-20260804.png`
- 2026-08-04 · Problems severity board ERR/WARN/ALL · PNG `scratch/problems-severity-board-20260804.png`
- 2026-08-04 · RelatedFiles companions Skia+list · `75eb3c95` · PNG `scratch/relatedfiles-list-graph-20260804.png`
