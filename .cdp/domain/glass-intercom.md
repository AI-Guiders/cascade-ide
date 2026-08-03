# Domain: glass-intercom

## Invariants
- Feed body renders ADR 0129 subset via GlassCore `IntercomMarkdown` + WPF `GlassIntercomMarkdownBody` — not raw `TextBlock`.
- Full CommonMark / Markdig stays Markdown Preview (0069); do not embed Markdig Avalonia renderer in every bubble.
- Fenced code segments never feed attach/`[…]` parse (0128).

## Entry
- Parser: `CascadeIDE.GlassCore/Intercom/IntercomMarkdown.cs`
- WPF: `CDP.GlassCockpit.Windows/GlassIntercomMarkdownBody.cs` → `MainWindow.xaml` feed template
- Tests: `CascadeIDE.Tests/IntercomMarkdownTests.cs`

## Antipatterns
- Reparenting a still-parented WPF child into `Content` (crash: logical child already set).
- Forking Markdig preview host into Glass feed for "normal MD".
- Silent Cursor Write past PathMutateGate for these files.

## last_ship
- 2026-08-03 · Glass Intercom MD subset live dogfood · commits `02425a61` + follow-up Glass UI
