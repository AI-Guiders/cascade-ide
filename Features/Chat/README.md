# Chat panel ViewModel — context-economy peels

Hub `ChatPanelViewModel.cs` still over quality gate — peel coherent concerns into partials (target ≤~200 LOC / file when practical; project `file_lines` soft fail is 800).

## Recent peels

| Partial | Owns |
| --- | --- |
| `ChatPanelViewModel.Clarification.cs` | Clarification batch show/submit/dismiss + MCP JSON entry points |
| `ChatPanelViewModel.MessageSelection.cs` | Index/offset/thread selection, thinking toggle, assistant edit, readable export |
| `ChatPanelViewModel.CursorAcp.cs` | Dispose, model-pick reaction, PromptAsync send path |
| `ChatPanelViewModel.CursorAcp.Watchdog.cs` | Loading stage, wait watchdog, session model list, error mapping |
| `ChatPanelViewModel.StreamingMaf.cs` | Streaming provider + MAF IDE agent send path |
| `ChatPanelViewModel.McpAppend.cs` | External MCP `send_chat` append (bracket prepare + UI commit) |
| `ChatPanelViewModel.ThinkingMessages.cs` | Thinking/tool bubble helpers (ACP shared) |
| `ChatPanelViewModel.Sedm.cs` | SEDM scope strip, session-event cache, workline resolve |
| `ChatPanelViewModel.Sedm.Mcp.cs` | Intent/decision MCP recording + cross-workline stale |
| `ChatPanelViewModel.Sedm.Materialize.cs` | Context-card materialization + outbound agent-context prefixes |
| `ChatPanelViewModel.IntercomAttach.cs` | Pending drafts, slash handlers, composer insert, workspace resolve |
| `ChatPanelViewModel.IntercomAttach.Affordance.cs` | Selection/scope/diagnostic/problem + drag-drop affordances |
| `ChatPanelViewModel.IntercomAttach.Reveal.cs` | Reveal attachment from feed into IDE |
| `ChatPanelViewModel.IntercomCorrespondence.cs` | Slash find/relate message↔code + in-memory explicit relates |
| `ChatPanelViewModel.IntercomCorrespondence.Mcp.cs` | MCP JSON messages-for-code / message-relate |
| `ChatPanelViewModel.IntercomCorrespondence.Mcp.Parse.cs` | MCP ordinal/range_expr segment parse helpers |
| `ChatPanelViewModel.ComposerAutocomplete.cs` | Popup facade, caret refresh, slash/bracket routing, commit/move |
| `ChatPanelViewModel.ComposerAutocomplete.Bracket.cs` | Bracket suggestions, debounce, move/commit/dismiss |
| `ChatPanelViewModel.Surface.cs` | Collections, observable surface props, send gate, message-change refresh |
| `ChatSlashCommandRunner.cs` | Slash runner hub (ctor + TryRunAsync dispatch) |
| `ChatSlashCommandRunner.Args.cs` | Path/args build, validation, success detail formatting |
| `ChatSlashCommandRunner.Local.cs` | Local help/report/intercom/agent execution |
| `ChatSlashCommandRunner.Forge.cs` | Forge lens / artifact.goto execution |
| `ChatSlashCommandRunner.Ide.cs` | IDE bridge command execution |

Existing concern partials (Intercom*, Session, Threading, Composer*, …) stay as mapped by filename.

Hub `ChatPanelViewModel.cs` ≈193 (fields+ctor; Surface peeled). ChatPanel VM + ChatSlashCommandRunner under epic ~200. IntercomComposer hub ≈632; SkiaChatSurfaceControl hub ≈512 (Scene peeled; soft-fail cleared). Fat next: `DocumentsWorkspaceViewModel.cs` (~827).
