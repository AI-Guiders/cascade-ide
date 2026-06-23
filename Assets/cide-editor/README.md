# CIDE Forward Editor (Monaco, ADR 0162 / 0163)

Bundled host page for WebView2 Forward editor.

- **Monaco Editor** — MIT, Copyright Microsoft Corporation ([monaco-editor](https://github.com/microsoft/monaco-editor))
- **Offline bundle:** `monaco/min/vs` (refresh via `tools/vendor-monaco-editor.ps1`)
- **Architecture:** bridge v1 → **CIDE Editor Capability Bus** ([ADR 0163](../../docs/adr/0163-monaco-native-capability-bus-full-forward-migration.md))

```toml
[editor]
forward_host = "monaco_webview2"
```

Bridge (`cide-editor-bridge.js`) — transport + Monaco providers; intelligence from C# (Roslyn / future LSP via capability router). Syntax: built-in Monaco languages + Monarch for custom grammars (see ADR 0163 §2.4).