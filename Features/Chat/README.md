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

Existing concern partials (Intercom*, Session, Threading, Composer*, …) stay as mapped by filename.

Hub ~333 LOC. Fat siblings next: IntercomAttach (~326) / ComposerAutocomplete (~255) / IntercomCorrespondence (~297).
