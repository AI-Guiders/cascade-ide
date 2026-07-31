# Chat panel ViewModel — context-economy peels

Hub `ChatPanelViewModel.cs` still over quality gate — peel coherent concerns into partials (target ≤~200 LOC / file when practical; project `file_lines` soft fail is 800).

## Recent peels

| Partial | Owns |
| --- | --- |
| `ChatPanelViewModel.Clarification.cs` | Clarification batch show/submit/dismiss + MCP JSON entry points |
| `ChatPanelViewModel.MessageSelection.cs` | Index/offset/thread selection, thinking toggle, assistant edit, readable export |
| `ChatPanelViewModel.CursorAcp.cs` | Dispose, model-pick reaction, PromptAsync send path |
| `ChatPanelViewModel.CursorAcp.Watchdog.cs` | Loading stage, wait watchdog, session model list, error mapping |

Existing concern partials (Intercom*, Session, Sedm, Threading, Composer*, …) stay as mapped by filename.

Next hub candidates: AppendMessageFromMcp, streaming/MAF send paths (hub still ~617 LOC / warn 400).
