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

Existing concern partials (Intercom*, Session, Threading, Composer*, …) stay as mapped by filename.

Fat siblings next: ComposerAutocomplete (~255).
