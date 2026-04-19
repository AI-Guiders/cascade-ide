# TOML TextMate grammar (bundled)

- **Grammar file:** `syntaxes/toml.tmLanguage.json` — MIT, originally from [taplo](https://github.com/tamasfe/taplo) (`editors/vscode/toml.tmLanguage.json`), same snapshot as referenced in [flox/flox-vscode](https://github.com/flox/flox-vscode) (see `_source` inside the JSON).
- **Purpose:** `TextMateSharp.RegistryOptions` does not ship a TOML grammar; we load this VS Code-style extension via `LoadFromLocalFile` at runtime (see `TextMateTomlGrammar.cs`).

To refresh the grammar, fetch the latest `toml.tmLanguage.json` from taplo or flox-vscode and preserve licensing headers.
