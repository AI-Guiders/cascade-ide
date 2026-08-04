# Domain: glass-intercom

## Invariants
- Feed body renders ADR 0129 subset via GlassCore `IntercomMarkdown` + WPF `GlassIntercomMarkdownBody` — not raw `TextBlock`.
- Full CommonMark / Markdig stays Markdown Preview (0069); do not embed Markdig Avalonia renderer in every bubble.
- Fenced code segments never feed attach/`[…]` parse (0128).
- Intercom = Radio (I6): instrument pointers are cards (`delta →` / `look →` / `→ PFD|MFD|Right|…`) — not SA prose wall; generic `→` bullets stay prose.

## Entry
- Parser: `CascadeIDE.GlassCore/Intercom/IntercomMarkdown.cs`
- Radio peel: `CascadeIDE.GlassCore/Intercom/GlassRadioPointer.cs` → feed `ChatBubble.Pointers`
- WPF: `CDP.GlassCockpit.Windows/GlassIntercomMarkdownBody.cs` → `MainWindow.xaml` feed template
- Human send: `GlassIntercomSend` → voice latch + journal + `GlassOperatorShareShelf` (IdeShare operator inbox)
- Tests: `CascadeIDE.Tests/IntercomMarkdownTests.cs` · `GlassOperatorShareShelfTests.cs` · `GlassRadioPointerTests.cs`

## Antipatterns
- Reparenting a still-parented WPF child into `Content` (crash: logical child already set).
- Forking Markdig preview host into Glass feed for "normal MD".
- Silent Cursor Write past PathMutateGate for these files.
- Human Intercom send that only latches PF without writing `.cdp/share` — agent cannot `share from=operator`.
- Claiming Radio Done via SA wall / Autoi dump / File.Exists alone.

## last_ship
- 2026-08-04 · Intercom Radio pointer cards (I6) · `GlassRadioPointer` · PNG `scratch/intercom-radio-pointer-20260804.png` (DELTA · Right:Editor)
- 2026-08-04 · share-to-model human→agent shelf · `GlassOperatorShareShelf` + Intercom send mirror · PNG Intercom/near-black/CFG
