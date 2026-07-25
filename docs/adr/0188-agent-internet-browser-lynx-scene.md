# ADR 0188: Agent internet browser — `scene_internet_browser` (lynx)

**Статус:** Accepted · Implemented (CDP 0.5.159)  
**Дата:** 2026-07-25  
**Обновлено:** 2026-07-25 — Chromium UA spoof + `op=search` (DDG default)  
**Tags:** #cdp #browser #lynx #habitat #equal-standing #agent-ide #adr #cascade-ide

## Резюме

- В **agent-IDE (CDP)** появляется свой интернет-браузер: **`cdp_browser`** / **`go=scene_internet_browser`**.
- Движок MVP: **lynx** (`-dump`), не GUI и не Cursor Browser.
- Контроль у агента: tabs, open/dump/links/follow/back/forward/close — как `shell_scene`.
- **UA spoof:** каждый dump шлёт Chromium UA (`-useragent=`; override `CDP_BROWSER_UA`) — многие «обновите браузер» отваливаются.
- **Search:** `op=search q=` → **DuckDuckGo HTML** по умолчанию. Google SERP остаётся JS-gated даже с Chrome UA (enablejs), не суверенный путь.
## Связанные ADR

| ADR | Роль |
|-----|------|
| [0180](0180-agent-shell-habitat-tabs-scene.md) | Scene/tabs habitat twin |
| [0187](0187-cdp-mcp-scene-agent-outlet.md) | Outlet ≠ browser; browser = first-class organ |
| [0002](0002-debug-human-agent-parity.md) | Equal standing: agent eyes on the net |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Agent comfort |

## Проблема

Агенту нужен живой интернет в **своей** IDE. Cursor Browser / human panel — чужой пульт. Shell `lynx` вручную работает, но без scene/history/links как organ.

## Решение

### Метафора

`shell_scene` для терминала → **`scene_internet_browser`** для сети.  
Движок: lynx dump (текст + References → numbered follow).

### Жесты

| Verb | Смысл |
|------|--------|
| `cdp_browser` / `go=scene_internet_browser` | карта tabs + engine |
| `op=which` | путь/version lynx + effective UA |
| `op=open` `url=` | fetch dump → text + links (Chromium UA spoof) |
| `op=search` `q=` | DDG HTML (default); `engine=google|bing` optional |
| `op=dump` | тело текущей страницы (cap) |
| `op=links` | numbered refs |
| `op=follow` `link=N` | открыть ref |
| `op=back` / `forward` | history вкладки |
| `op=close` | закрыть tab |

### Инварианты

1. **Agent habitat**, не host Browser UI.
2. **lynx required** — fail с hint (`scoop install lynx` / `CDP_LYNX=`).
3. JS-SPA may be empty — honest limitation; Chromium later = separate ADR.
4. Caps: tabs ≤ 8, dump body capped, timeout default 45s.

## Отклонённые альтернативы

- **Только Cursor Browser / MCP browser extension** — чужой пульт.
- **Интерактивный curses lynx** — агент плохо «сидит» в TUI.
- **Сразу Playwright** — heavy; dump-first comfort.

## Dogfood

CDP **0.5.158**: `op=open url=https://example.com` → text; `links` → `follow`.
