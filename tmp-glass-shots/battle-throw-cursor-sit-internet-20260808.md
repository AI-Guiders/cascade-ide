# Battle-test: throw-Cursor · sit-internet (Glass) — CORRECTED 2026-08-08

Source checklist from Auto subagent was **incomplete**: Face/wire/shield only.
Lived miss: Radio Dialog body-map omitted `@intent browser` → Sierra «нет браузера» while wire Face existed.
Anti-seeming Done-shield alone ≠ peer self-model honesty.

Do everything in Glass / Citizen. Done stays **REOPENED** vs max DoD.

0. **Radio body-map (HARD gate before Face dogfood)** — **PASS 2026-08-08 ~09:44**
   Ask Sierra to open Hacker News / sit on the web.
   Pass: she emits `@intent browser open|search` (or says she will) — **does not** claim «нет браузера / HTTP / не оборудована».
   Lived: Dialog «Сажусь в интернет… `@intent browser open url="https://news.ycombinator.com"`» · host ack lynx HN.

1. **Open WebAi via `@intent browser` open|search** (not hand URL bar) — **PASS**
   Pass: `web_ai_url` → `RunWebAiPortal` navigates WebView2.
   Lived: seats `m=browser` `mfd_page=WebAiPortal` `web_ai_url=https://news.ycombinator.com/` · Face shot HN.

2. **Default without explicit URL** — **PASS (search path)**
   Pass: duckduckgo.com. Fail: chatgpt.com / empty / old default.
   Lived: `cdp_browser op=search` → `html.duckduckgo.com/html/?q=…` (engine=ddg).

3. **Search/open → look at WebView2** — **PASS**
   Pass: target page visible. Fail: lynx updated, WebView2 frozen.
   Lived: evidence `battle-webai-hn-face-20260808-0945.png` — **M · WebAiPortal** + HN list (Voyager / Nixpkgs…).

4. **Lynx dump parallel** — **PASS**
   Pass: peer text alive; does not replace Face. Fail: dump dead or Face substitute.
   Lived: Dialog peer pulse lynx HN + Face WebView2 same turn.

5. **Citizen/Glass Done without `webai` evidence path** — shield unit tests green (SeemingDone filter); no epic Done stamp attempted.
6. **Done with `…webai….png` evidence** — evidence path lived; stamp still **REOPENED** (not max DoD).
7. **Canon vs stamp** — **PASS** — last_ship REOPENED; no «всё Done».
8. **Full loop without Cursor host** — **PASS** — Dialog→`@intent browser`→Face WebView2→webai PNG in Glass (agent Face latch + Sierra intent).

## Evidence

- `cascade-ide/tmp-glass-shots/battle-webai-hn-face-20260808-0945.png` + `cdp_see`

## Not Done (honest)

- Throw-Cursor max DoD closed
- SoftOrgan Meta invent (REJECT under sealed course)
- Treating battle VERIFY as Glass/Citizen Done stamp
