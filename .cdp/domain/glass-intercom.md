# Domain: glass-intercom

## Invariants
- Feed body renders ADR 0129 subset via GlassCore `IntercomMarkdown` + WPF `GlassIntercomMarkdownBody` — not raw `TextBlock`.
- Full CommonMark / Markdig stays Markdown Preview (0069); do not embed Markdig Avalonia renderer in every bubble.
- Fenced code segments never feed attach/`[…]` parse (0128).
- Intercom = Radio (I6): instrument pointers are cards (`delta →` / `look →` / `→ PFD|MFD|Right|…`) — not SA prose wall; generic `→` bullets stay prose.
- Sticky Who per seat (`intercom-identity-LATEST.json`): freeform nick, not model id; resolve = explicit name → sticky → bootstrap (guest Кир / operator Operator / citizen Citizen). Света = this machine's sticky, not repo default.
- **Lane × model axes (accepted 2026-08-05):** lane = habitat seat (CIT/HOST/PF) at Send as **XOR Korry strip** (not ComboBox); FM model = HUD ComboBox у HDG/CRS, lit only when lane=CIT; provider key/baseUrl/default in CFG. Design: `docs/design/glass-intercom-lane-model-axes-v0.md`. Do not overload one "Model" ComboBox.
- **NorthStar messenger (accepted 2026-08-05):** Intercom ≠ только чат со вторым пилотом. Ontology: **`#crew`** (люди+агенты вместе) · **DM** · **Radio** (оператор↔агент оператора). Не `#humans`/`#agents` как комнаты (lens=0143). one mind·N seats → мессенджер. Design: `docs/design/glass-intercom-northstar-messenger-v0.md`. I6 Radio pointers ≠ channel kind Radio.
- **PreCondition (operator 2026-08-05):** CIDE EOL → Glass Done requires `All CIDE surfaces adopted` (or named supersede). **Not only Intercom Overview** — full gap table A1–A5 / B1–B3 in cdp-mcp `.cdp/domain/glass.md` PreCondition §. Topics P0b strip ≠ ADR 0072 overview (A1). Failure mode = **half-a instead of A** (PreCondition was stamped; undership) — not «PreCondition missing»; being playbook Seeming-Done.
- **A3 SUPERSEDE (2026-08-05):** Glass does **not** adopt CIDE ADR 0172 `ThreadNode` / worklines / session-graph. Channel index = NorthStar (`#crew` · DM · Radio). Keep 30m quiet-gap clusters as Virtual History only.

## Entry
- Parser: `CascadeIDE.GlassCore/Intercom/IntercomMarkdown.cs`
- Radio peel: `CascadeIDE.GlassCore/Intercom/GlassRadioPointer.cs` → feed `ChatBubble.Pointers`
- WPF: `CDP.GlassCockpit.Windows/GlassIntercomMarkdownBody.cs` → `MainWindow.xaml` feed template
- Human send: `GlassIntercomSend` → `ResolveIntercomIdentity` (sticky) + voice latch + journal + `GlassOperatorShareShelf`
- Sticky Who: `GlassIntercomIdentity` · path `CdpHabitatPaths.IntercomIdentityLatchFileName` · cdp-mcp `op=identity` / `send name=`
- Lane×model: `CascadeIDE.GlassCore/Intercom/GlassIntercomLane.cs` · WPF `MainWindow.xaml` (`LaneCitBtn`/`LaneHostBtn`/`LanePfBtn` + `HudModelPicker`) · `MainWindow.IntercomHud.cs`
- Tests: `CascadeIDE.Tests/GlassIntercomLaneTests.cs` · `IntercomMarkdownTests.cs` · `GlassOperatorShareShelfTests.cs` · `GlassRadioPointerTests.cs`

## Antipatterns
- **Bare topology wipe for dogfood** — never publish `topology=(intercom)` just to shoot Intercom; PreferSurface / surface run without replacing the latch, or restore **`GlassPresentationLayout.OperatorReviewFlightTopology` = `(F/P/M)`** (single OneOf TopLevel — all channels one window). Do **not** invent `(P)(F)(M)`, `(F)(P/M)`, `(intercom)(sit)(world)`, or **`(intercom)(sit/world/alert)`** as "restore" — that last one is 2 windows (F dedicated + satellite host); wrong wire = still regression. Ask if unsure which flight was live.
- **Seeming-Done without topology regression tests** — before Done on presentation/OneOf: write/run tests that lock single-TopLevel no-host vs 2-group host spawn (`GlassPresentationLayoutSurfaceWireTests`); catch host-count regressions in CI, not by operator parrot.
- **Cheap prior long-loss (2026-08-05)** — skip-tests / wrong 2-window wire / HOLD surface parade (ECL·QRH) look cheap now and burn the day in circles; horizon ≠ months (→15.08). Refuse; densest human-faced + locks.
- Treating Intercom as DM with «second pilot» — NorthStar is team coordination (`#crew` + DM + Radio).
- `#humans` / `#agents` as separate **channels** — discrimination; use lens (0143) inside `#crew`.
- Shipping CIDE session-graph / topic-tree complexity as Glass day-1 (suffering, not work).
- Conversational-UI chrome (bubbles, bot cards, suggested-actions default) instead of Slack/MM flat feed — less chrome, more meaning.
- Rebuilding NorthStar feed as Avalonia Skia surface by default — Glass WPF list/virtualization is the lighter face.
- Hardcoding operator nick (Света) as clone default — sticky is local latch, bootstrap is Operator.
- ComboBox for habitat lane (3 fixed seats) — use Korry XOR strip; ComboBox is for FM catalog.
- One control mixing lane + FM model id; two ComboBoxes beside Send; live FM list when lane ≠ CIT.
- Dark ComboBox dropdown: Style Background setters lose to ControlTemplate SystemColors — override HighlightBrushKey / WindowTextBrushKey on the ComboBox; don't trust ItemContainerStyle alone.
- Reparenting a still-parented WPF child into `Content` (crash: logical child already set).
- Forking Markdig preview host into Glass feed for "normal MD".
- Silent Cursor Write past PathMutateGate for these files.
- Human Intercom send that only latches PF without writing `.cdp/share` — agent cannot `share from=operator`.
- Claiming Radio Done via SA wall / Autoi dump / File.Exists alone.
- Hand-wiring Folded AutoI Korry while Review — green paint ≠ consume path; fix ignite-cmd consumer later.

## last_ship
- 2026-08-05 · **share-3.8 Send×lane** · HOST→`host-composer-request-LATEST.json` + ShareShelf `what=intercom-host` · CIT→`GlassCitizenDialogRequest`+`intercom-cit` · PF→`GlassIntercomSend`+`intercom` · evidence `tmp-glass-shots/share-3.8-host-send-20260805.png` · live latch pending id=026c0dd46d52
- 2026-08-05 · **Review flight lock** · `OperatorReviewFlightTopology=(F/P/M)` · tests: single-TopLevel no satellite vs `(intercom)(sit/world/alert)` spawns host · antipattern stamp glass-intercom · parrot tax fix
- 2026-08-05 · **ONE-WAVE PreCondition Intercom** · A2 ADOPTED (summary+spine strip) · A3 SUPERSEDE NorthStar (no ThreadNode) · B1–B3 DIG REJECT/defer · evidence `tmp-glass-shots/topic-overview-a2-20260805.png` · residual A4 denser only
- 2026-08-05 · **A1+A5 ADOPTED** · ADR 0072 Glass overview ↔ detail ↔ back · `ChatTopicOverviewPolicy`→GlassCore · `topic_overview`/`topic_enter` (`ato`/`atb`) · evidence `tmp-glass-shots/topic-overview-a1-20260805.png` · PreCondition residual → A2 spine/summary, A3 dig/supersede, B denser
- 2026-08-05 · **Decision stamped** · Face = Slack/MM light (not Conversational UI chrome) · northstar-messenger-v0 Face §
- 2026-08-05 · **Decision stamped** · NorthStar ontology · `#crew`+DM+Radio · reject `#humans`/`#agents` rooms · `glass-intercom-northstar-messenger-v0.md`
- 2026-08-05 · **lane×model UI shipped** · XOR Korry CIT|HOST|PF @ composer · HudModelPicker CIT-lit only · latch `glass-intercom-lane.json` + legacy model migrate · tests `GlassIntercomLaneTests` 6/6 · evidence `tmp-glass-shots/lane-model-axes-20260805.png` (+ hud/composer crops) · design `glass-intercom-lane-model-axes-v0.md`
- 2026-08-05 · **Decision stamped** · lane×model axes v0 · Lane=Korry XOR @ Send · Model=HUD Combo @ HDG/CRS · secrets=CFG · design `glass-intercom-lane-model-axes-v0.md`
- 2026-08-05 · Sticky Intercom Who · `%LocalAppData%/cdp-mcp/intercom-identity-LATEST.json` · Glass `GlassIntercomIdentity` + `ResolveIntercomIdentity` · MCP `cdp_intercom op=identity` · dogfood send without name= → AutoI · pm sticky Света (local claim, not repo default)
- 2026-08-05 · ModelPicker dropdown empty text · Dark Cockpit: override SystemColors Highlight*/Window* on ComboBox + ItemTemplate inherit FG · evidence `tmp-glass-shots/model-picker-fixed-20260805.png` (Citizen · default / Composer · host / PF · habitat)
- 2026-08-05 · Intercom prose residual CLOSED · Citizen/@frame SA walls → Radio collapse (`FormatCitizenWakeIntercom` + Glass `CompactIntercomBody` LooksLikeSaInstrumentWall) · evidence `tmp-glass-shots/intercom-prose-radio-collapsed-20260805.png` (Citizen · SA collapsed · PFD + DELTA cards)
- 2026-08-04 · Autoi Intercom Radio face (Composer charge wall → `→ PFD.NEXT` / Plan delta) · dual-seat claim · cdp-mcp `IdeIgniteArmHost.Fire.Habitat.Radio`
- 2026-08-04 · Intercom Radio pointer cards (I6) · `GlassRadioPointer` · PNG `scratch/intercom-radio-pointer-20260804.png` (DELTA · Right:Editor)
- 2026-08-04 · share-to-model human→agent shelf · `GlassOperatorShareShelf` + Intercom send mirror · PNG Intercom/near-black/CFG
