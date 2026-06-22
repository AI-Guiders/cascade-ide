# CIDE Forward Editor (Monaco spike, ADR 0162)

Bundled host page for WebView2 Forward editor.

- **Monaco Editor** — MIT, Copyright Microsoft Corporation ([monaco-editor](https://github.com/microsoft/monaco-editor))
- Loaded at runtime from jsDelivr (`monaco-editor@0.52.2`) until vendored min bundle lands (M1+)

Switch host in `settings.toml`:

```toml
[editor]
forward_host = "monaco_webview2"   # or avalonia_edit
```
