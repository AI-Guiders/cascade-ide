# ADR 0190: Agent IDE Settings organ

**Статус:** Accepted · Implemented (CDP 0.5.163)  
**Дата:** 2026-07-25  
**Tags:** #cdp #settings #options #lsp #habitat #equal-standing #agent-ide #adr #cascade-ide

## Резюме

- Настройки IDE — **Tools → Options** внутри CDP (`cdp_settings` / `go=options`), не Cursor `settings.json`.
- UX: дерево страниц → страница → `set` / **Languages: `lsp_ensure`** (probe → shell install → hot pool).
- Слои: **process** (`cdp-mcp.toml`) + **user** (`ide-settings.json`).

## Languages / LSP (0.5.163)

| Op | Смысл |
|----|--------|
| `page=languages` | servers + recipes + missing next[] |
| `lsp_probe` | PATH resolve |
| `lsp_install id= via=` | IDE shell (`npm`/`pip`/`go`/…) |
| `lsp_ensure id=` | missing → install → probe |
| `lsp_add` | register custom preset in user store |

Recipes: python (basedpyright), go, rust, yaml, json, markdown.

Flow: Options → Languages → (browser search) → shell install → **есть**.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | CIDE human twin: LocalAppData settings |
| [0180](0180-agent-shell-habitat-tabs-scene.md) | Shell organ — `shell.*` Options |
| [0187](0187-cdp-mcp-scene-agent-outlet.md) | MCP presets — `mcp.default_preset` |
| [0188](0188-agent-internet-browser-lynx-scene.md) | Browser — `browser.search_engine` etc. |
| [0189](0189-cockpit-tile-manager.md) | `desk.default_layout` / MFD |

## Проблема

У человека Settings — внутри IDE (Tools → Options). У агента prefs жили в host `mcp.json` / toml без page tree — «гость без Options».

## Решение

### Wire

| Surface | Смысл |
|---------|--------|
| `cdp_settings op=options` | Tools → Options root (`tree[]`) |
| `op=page page=` | Одна страница controls (choices, dirty) |
| `op=set key= value=` | Apply + persist user layer |
| `go=options` / `go=options_page` | cockpit |

### Pages

| Page | Knobs |
|------|--------|
| `environment` | session.default_phase / object |
| `internet` | search_engine, UA, width, timeout, dump_chars |
| `desk` | default_layout, default_mfd |
| `shell` | timeout_seconds, codepage |
| `mcp` | default_preset (memory\|serena\|…) |
| `process` | toml backends (read-only) |

Env `CDP_BROWSER_UA` still wins over user UA.

### Инварианты

1. Не писать в Cursor settings — только CDP user store + read process toml.
2. Process backend toggles не мутируются через set.
3. Options = organ; не отдельный `cdp_options` noun.

## Отклонённые альтернативы

- **Плоский catalog без pages** — не похоже на Tools→Options (исправлено в 0.5.162).
- **Дублировать Cursor UI settings** — чужой продукт.

## Dogfood

`cdp_settings` → `page=internet` → `set key=browser.search_engine value=ddg` → `page=desk` → `set desk.default_layout=code+net` → cockpit `go=options`.
