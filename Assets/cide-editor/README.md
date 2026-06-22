# CIDE Forward Editor (Monaco, ADR 0162)

Bundled host page for WebView2 Forward editor.

- **Monaco Editor** — MIT, Copyright Microsoft Corporation ([monaco-editor](https://github.com/microsoft/monaco-editor))
- **Offline bundle:** `monaco/min/vs` (refresh via `tools/vendor-monaco-editor.ps1`)

Switch host in `settings.toml`:

```toml
[editor]
forward_host = "monaco_webview2"   # or avalonia_edit
```

Bridge (`cide-editor-bridge.js`) wires Roslyn intelligence from the host: completion, hover, signature help, reference highlights, CF gutter glyphs, sticky-scroll label (Avalonia chrome + host push).
