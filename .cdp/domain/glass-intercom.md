# Domain: glass-intercom

## Invariants
- Feed body renders ADR 0129 subset via GlassCore `IntercomMarkdown` + WPF `GlassIntercomMarkdownBody` — not raw `TextBlock`.
- Full CommonMark / Markdig stays Markdown Preview (0069); do not embed Markdig Avalonia renderer in every bubble.
- Fenced code segments never feed attach/`[…]` parse (0128).
- Intercom = Radio (I6): instrument pointers are cards (`delta →` / `look →` / `→ PFD|MFD|Right|…`) — not SA prose wall; generic `→` bullets stay prose.
- Sticky Who per seat (`intercom-identity-LATEST.json`): freeform nick, not model id; resolve = explicit name → sticky → bootstrap (guest Кир / operator Operator / citizen Citizen). Света = this machine's sticky, not repo default.
- **Lane × model axes (accepted 2026-08-05):** lane = habitat seat (CIT/HOST/PF) at Send as **XOR Korry strip** (not ComboBox); FM model = HUD ComboBox у HDG/CRS, lit only when lane=CIT; provider key/baseUrl/default in CFG. Design: `docs/design/glass-intercom-lane-model-axes-v0.md`. Do not overload one "Model" ComboBox.
- **NorthStar messenger (accepted 2026-08-05):** Intercom ≠ только чат со вторым пилотом. Ontology: **`#crew`** (люди+агенты вместе) · **DM** · **Radio** (оператор↔агент оператора). Не `#humans`/`#agents` как комнаты (lens=0143). one mind·N seats → мессенджер. Design: `docs/design/glass-intercom-northstar-messenger-v0.md`. I6 Radio pointers ≠ channel kind Radio.

## Entry
- Parser: `CascadeIDE.GlassCore/Intercom/IntercomMarkdown.cs`
- Radio peel: `CascadeIDE.GlassCore/Intercom/GlassRadioPointer.cs` → feed `ChatBubble.Pointers`
- WPF: `CDP.GlassCockpit.Windows/GlassIntercomMarkdownBody.cs` → `MainWindow.xaml` feed template
- Human send: `GlassIntercomSend` → `ResolveIntercomIdentity` (sticky) + voice latch + journal + `GlassOperatorShareShelf`
- Sticky Who: `GlassIntercomIdentity` · path `CdpHabitatPaths.IntercomIdentityLatchFileName` · cdp-mcp `op=identity` / `send name=`
- Tests: `CascadeIDE.Tests/IntercomMarkdownTests.cs` · `GlassOperatorShareShelfTests.cs` · `GlassRadioPointerTests.cs`

## Antipatterns
- Treating Intercom as DM with «second pilot» — NorthStar is team coordination (`#crew` + DM + Radio).
- `#humans` / `#agents` as separate **channels** — discrimination; use lens (0143) inside `#crew`.
- Shipping CIDE session-graph / topic-tree complexity as Glass day-1 (suffering, not work).
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
- 2026-08-05 · **Decision stamped** · NorthStar ontology · `#crew`+DM+Radio · reject `#humans`/`#agents` rooms · `glass-intercom-northstar-messenger-v0.md`
- 2026-08-05 · **Decision stamped** · lane×model axes v0 · Lane=Korry XOR @ Send · Model=HUD Combo @ HDG/CRS · secrets=CFG · design `glass-intercom-lane-model-axes-v0.md` (UI not shipped yet)
- 2026-08-05 · Sticky Intercom Who · `%LocalAppData%/cdp-mcp/intercom-identity-LATEST.json` · Glass `GlassIntercomIdentity` + `ResolveIntercomIdentity` · MCP `cdp_intercom op=identity` · dogfood send without name= → AutoI · pm sticky Света (local claim, not repo default)
- 2026-08-05 · ModelPicker dropdown empty text · Dark Cockpit: override SystemColors Highlight*/Window* on ComboBox + ItemTemplate inherit FG · evidence `tmp-glass-shots/model-picker-fixed-20260805.png` (Citizen · default / Composer · host / PF · habitat)
- 2026-08-05 · Intercom prose residual CLOSED · Citizen/@frame SA walls → Radio collapse (`FormatCitizenWakeIntercom` + Glass `CompactIntercomBody` LooksLikeSaInstrumentWall) · evidence `tmp-glass-shots/intercom-prose-radio-collapsed-20260805.png` (Citizen · SA collapsed · PFD + DELTA cards)
- 2026-08-04 · Autoi Intercom Radio face (Composer charge wall → `→ PFD.NEXT` / Plan delta) · dual-seat claim · cdp-mcp `IdeIgniteArmHost.Fire.Habitat.Radio`
- 2026-08-04 · Intercom Radio pointer cards (I6) · `GlassRadioPointer` · PNG `scratch/intercom-radio-pointer-20260804.png` (DELTA · Right:Editor)
- 2026-08-04 · share-to-model human→agent shelf · `GlassOperatorShareShelf` + Intercom send mirror · PNG Intercom/near-black/CFG
