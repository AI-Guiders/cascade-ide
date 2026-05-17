<!-- English translation of adr/0111-editor-linenumber-linerange-value-objects.md. Canonical Russian: ../../adr/0111-editor-linenumber-linerange-value-objects.md -->

# ADR 0111: `LineNumber` and `LineRange` as editor domain types

## Related ADRs

| ADR | Role |
|-----|------|
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | Semantics of parametrics by strings; 0111 - VO LR/LN in IDE domain |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Directory, `tail_signature`; `:ln` - directory metadata |
| [0110](0110-roslyn-refactor-intent-melody-bridge.md) | Roslyn by range; future bridge to MCP arguments |

## Summary

- Value objects **`LineNumber`** / **`LineRange`** (1-based, Start ≤ End).
- Border to JSON/commands - `int` in args; `ParsedLineRange` for parsing.


## Rejected alternatives

- **Only `:ln` aliases in TOML** - do not replace API checks and self-documentation in C#.
- **`record` with int without invariants** - duplication of checks in each consumer.
- **Complete transition of commands to typed DTO instead of JSON** - outside scope v1; the boundary remains on serialization.
- **Special tokens “to the end of the file”** (`7:end`, `7:*`, etc.) - not included in v1: separate mini-grammar, interaction with file length in parse vs build, risk of collisions; left to a separate decision when there is a clear need.

## Consequences (current)

- New scripts **only by lines** in the parametric melody branch → preferably via LR/LN.
- The "line is in file" check remains in **`ParametricLineRangeArgsBuilder`**, not in the order of the two numbers in the input and not in LN/LR per se.

---
## Solution

- Enter **value objects** in the `CascadeIDE.Models.Editor` namespace:
  - **LN (`LineNumber`)** — 1-based line number; invariant `Value >= LineNumber.MinimumOneBasedInclusive` (1); creation via `TryCreate(int, out LineNumber)`; comparison operators (including for CA1036 / `IComparable`).
  - **LR (`LineRange`)** - a pair `Start` / `End` of type LN with the invariant `Start <= End` (inclusive range in terms of the editor and IDE commands); creation via `TryCreate(LineNumber, LineNumber, out LineRange)`.
- **`ParametricIntentMelody.ParsedLineRange`** stores the range as **`LineRange Lines`**, rather than two "naked" `ints`.
- Melody tail parser (`TryExtractLineRangeFromRemainder` in `ParametricIntentMelody`):
  - **One whole** without a second slot - **one line** (`<line>` has the same meaning as `<line>:<line>` as LR).
  - **Two integers** — boundaries of one inclusive range; the input order is not important: after parsing, **`min..max`** is applied (for example `7:3` and `3:7` → one LR).
  - **`LineRange.TryCreate`** for **explicit code** still has a "second argument not before the first" contract; "inverted" input is processed **before** the `TryCreate` call, due to normalization in the parser.
- At the border to JSON (**`ParametricLineRangeArgsBuilder`**) **`int`** are still sent to anonymous DTOs via `.Value` in LN - **wire format** `IdeCommands.Select` / `IdeCommands.ApplyEdit` does not change.
- **`IntentMelodyTailSemantics.MinEditorLineNumber`** is consistent with **`LineNumber.MinimumOneBasedInclusive`** (one source constant for "minimal line 1-based").

## Implementation v1 (sources in code)

| Artifact | Destination |
|----------|-----------|
| `Models/Editor/LineNumber.cs` | LN, minimum constant, `TryCreate`, equality and comparison |
| `Models/Editor/LineRange.cs` | LR, `TryCreate` with `End >= Start` |
| `Services/ParametricIntentMelody.cs` | `ParsedLineRange`, `TryParseLineRangeTail`, `TryExtractLineRangeFromRemainder`, delegation to `ParametricLineRangeArgsBuilder` |
| `Services/ParametricLineRangeArgsBuilder.cs` | Assembling JSON-args from `ParsedLineRange.Lines` + checking for file length overshoot |
| `Services/IntentMelodyTailSemantics.cs` | `MinEditorLineNumber` → link to minimum LN |
| `CascadeIDE.Tests/EditorLineNumberRangeTests.cs` | LN/LR Unit Tests |
| `CascadeIDE.Tests/ParametricIntentMelodyTests.cs` | Parsing, one line, `min..max`, build args |
| `IntentMelody/intent-melody-aliases.toml` and copy to `publish-gh-release/IntentMelody/` | Palette tooltips for `els` / `eld` (range and one line) |

The directory still describes **two** numeric slots in `tail_signature` (`<start:ln>:<end:ln>`); the "single number" abbreviation is **application parser convention**, not a separate directory string.

## Implementation v2 (roadmap after v1 - closed in code)
| Artifact | Destination |
|----------|-----------|
| `Models/Editor/ColumnNumber.cs` | CN - 1-based column (`TryCreate`, compare) |
| `Models/Editor/EditorDocumentPath.cs` | Document path wrapper: `CanonicalFilePath.TryNormalize`, case-insensitive comparison |
| `Models/Editor/EditorMcpSpans.cs` | `EditorTextSpan.TryParse`, `EditorContentLineRangeMcpArgs.TryParse`, `EditorGoToPositionMcpArgs.TryParse` |
| `Services/RoslynLinePositionMapper.cs` | `Microsoft.CodeAnalysis.Text.LinePosition` (0-based) → `(LineNumber, ColumnNumber)` for UI/MCP |
| `Services/ContextMinimizer.cs`, `Services/WorkspaceDiagnosticsCoordinator.cs` | Use mapper instead of duplicating `+1` |
| `ViewModels/IdeMcpCommandExecutor.Handlers.Editor/*.cs` | `Select` / `ApplyEdit` / `GoToPosition` / `GetEditorContentRange` via general VO parsing |
| `Services/ParametricLineRangeArgsBuilder.cs` | Column boundaries via `ColumnNumber` + canonical `EditorDocumentPath` |
| `Features/WebAiPortal/Application/WebAiPortalChatMixInFormatter.cs` | Editor range snapshot: `LineRange` instead of "naked" int lines |
| `CascadeIDE.Tests/EditorMcpSpansTests.cs`, extension `EditorLineNumberRangeTests` | MCP Parser Failures and Limits |

## Related ADRs

| ADR | Role |
|-----|------|
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | Semantics of parametrics by strings; 0111 - VO LR/LN in IDE domain |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Directory, `tail_signature`; `:ln` - directory metadata |
| [0110](0110-roslyn-refactor-intent-melody-bridge.md) | Roslyn by range; future bridge to MCP arguments |

## Rejected alternatives

- **Only `:ln` aliases in TOML** - do not replace API checks and self-documentation in C#.
- **`record` with int without invariants** - duplication of checks in each consumer.
- **Complete transition of commands to typed DTO instead of JSON** - outside scope v1; the boundary remains on serialization.
- **Special tokens “to the end of the file”** (`7:end`, `7:*`, etc.) - not included in v1: separate mini-grammar, interaction with file length in parse vs build, risk of collisions; left to a separate decision when there is a clear need.

## Consequences (current)

- New scripts **only by lines** in the parametric melody branch → preferably via LR/LN.
- The "line is in file" check remains in **`ParametricLineRangeArgsBuilder`**, not in the order of the two numbers in the input and not in LN/LR per se.

---

## Roadmap (after v1) - Done (v2)

The goal of the v2 iterations is **the same pattern**: invariants in types up to the JSON/MCP boundary, without changing the `IdeCommands` wire contracts.

### Priority 1 - MCP boundary → editor - **done**

- **`EditorTextSpan`** + **`ColumnNumber`**, **`TryParse`** from `IReadOnlyDictionary<string, JsonElement>` for `Select` / `ApplyEdit`; **`EditorGoToPositionMcpArgs`** for `go_to_position`; **`EditorContentLineRangeMcpArgs`** for `get_editor_content_range` (if there are no keys - 1..1 as before; inverted explicit range - failure with message).

### Priority 2 - web portal - **done**

- **`WebAiPortalChatMixInFormatter`**: `EditorRangeSnap` stores **`LineRange`**; JSON parsing normalizes boundaries through `min/max` when ints are “reversed” in the wire response.

### Priority 3 - parametric args - **done**

- **`ParametricLineRangeArgsBuilder`**: columns via **`ColumnNumber.TryCreate`**, path via **`EditorDocumentPath`**.

### Priority 4 - Roslyn - **done**

- **`RoslynLinePositionMapper`**: `LinePosition` (0-based) → `(LineNumber, ColumnNumber)`; **`ContextMinimizer`**, **`WorkspaceDiagnosticsCoordinator`**.

### File path - **done**

- **`EditorDocumentPath`** over **`CanonicalFilePath.TryNormalize`**.

### Readiness criterion

- Two call sites with one set of JSON fields (`Select` + `ApplyEdit`) → **`EditorTextSpan`**; tests in **`EditorMcpSpansTests`**.