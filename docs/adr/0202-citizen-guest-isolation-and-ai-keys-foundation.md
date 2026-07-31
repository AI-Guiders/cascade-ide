# ADR 0202: Citizen/guest isolation + AI keys foundation (pointers)

**Status:** Accepted  
**Date:** 2026-07-31  
**Tags:** #cdp #citizen #guest #secrets

**SSOT:**

- Isolation: [CDP-ADR-0025](../../cdp-mcp/docs/adr/CDP-ADR-0025-citizen-guest-isolation.md)
- Keys: [CDP-ADR-0026](../../cdp-mcp/docs/adr/CDP-ADR-0026-citizen-ai-keys-foundation.md) · CIDE [0028](0028-user-settings-toml-localappdata-and-secrets.md) (`ai-keys.toml`)
- Wire draft: [citizen-agent-wire-v0.md](../../cdp-mcp/docs/design/citizen-agent-wire-v0.md)

## Product note

Guest (Cursor MCP) and future citizen (in-habitat host) must not thrash each other's peer/Autoi. Keys stay in `ai-keys.toml` (not `api-keys.toml`). Citizen consume blocked on host ship; CIDE keys line already live.
