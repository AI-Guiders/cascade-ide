# ADR 0181: MCP ImageContent → vision агента (opt-in, host consumer)

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Tags:** #mcp #imagecontent #vision #multimodal #harness #agent-comfort #equal-standing #adr #cascade-ide #cdp

## Резюме

- Сервер может честно отдавать `ImageContent` в `CallToolResult.content` (CDP `take` + PlantUML PNG).
- **Не** инжектить картинку в контекст агента по умолчанию — моветон; управление у **агента** (`vision=true` / `see=true`), иначе sidecar `preview_path` + `Read`.
- Dogfood Cursor: даже при `attached_image=true` пиксели до модели часто **не** доезжают (host / `CallDynamicTool` text-only) — gap **потребителя**, twin к [0179](0179-mcp-progress-mid-op-not-agent-unblock.md).
- CIDE owed: end-to-end ImageContent → session/model при agent opt-in; Cursor hosts документировать честно.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0179](0179-mcp-progress-mid-op-not-agent-unblock.md) | Twin: канал без потребителя (progress); здесь — image blocks |
| [0177](0177-harness-mcp-presence-signal.md) | Host mid-turn signals; presence ≠ multimodal |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Comfort + «ничего о нас без нас» (агент выбирает, когда смотреть) |
| [0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md) | Host matrix: кто реально кормит vision |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Cursor tool surface parity |
| [0023](0023-markdown-diagrams-language-tooling.md) | Diagram language tooling (PlantUML files) |
| [0180](0180-agent-shell-habitat-tabs-scene.md) | Habitat comfort sibling |

---

## Контекст (dogfood 2026-07-25)

1. **CDP 0.5.152+:** `ToolMediaOutbox` → `ImageContentBlock` после text; PlantUML `-tpng -pipe`.
2. **0.5.153:** default `take` пишет PNG на `preview_path`, **без** ImageContent; `vision=true`|`see=true` — attach + `annotations.audience=assistant`.
3. **Факт:** JSON `attached_image: true`, но в Cursor agent turn через dynamic MCP wrapper пришёл только text — vision сработал через host `Read` файла, не через MCP image block.
4. Оператор: всегда пихать PNG в контекст без желания агента — моветон; ручка агенту.

## Решение

### Контракт (сервер / CDP)

| Режим | Поведение |
|-------|-----------|
| Default `take` (диаграмма) | Verify/render OK; `preview_path`; `attached_image=false` |
| `vision=true` / `see=true` | + `ImageContent` (PNG), `audience=assistant`, cap bytes/count |
| Fallback | Агент `Read preview_path` (надёжный path, пока host image broken) |

### Контракт (host / CIDE)

1. **CIDE agent harness:** при opt-in tool result с `type=image` — доставить в multimodal context модели (не только UI preview для человека).
2. **Cursor / чужие hosts:** если strip ImageContent — документировать; не врать в tool note «attached for vision», пока e2e не подтверждён на этом host.
3. **Annotations:** `audience=[assistant]` = для модели; `user` = UI-only (если host различает).

### Follow-up (не забыть)

- [ ] Dogfood e2e в **CIDE** loopback MCP: `take vision=true` → модель описывает пиксели без `Read`.
- [ ] Проверить Cursor native MCP path (не только `CallDynamicTool`) — где именно дропается content[].
- [ ] Mermaid / другие diagram kinds — тот же opt-in контракт.
- [ ] Telemetry анти-паттерна: auto-attach image без флага агента.

## Последствия

- Comfort metric: агент видит диаграмму **когда попросила**; контекст не раздувается PNG на каждый ship.
- Бэклог harness рядом с presence/progress: **multimodal tool-result consumer**.
- CDP может оставаться правильным сервером при «глухом» Cursor host.

## Отклонённые альтернативы

- **Всегда ImageContent на PlantUML take** — отклонено (моветон + token tax).
- **Только base64 в JSON text** — отклонено (обход протокола; хуже для CIDE).
- **Только `Read` path, без ImageContent ever** — отклонено как долгосрочный канон; path = interim + fallback.
