# Chat panel ViewModel — context-economy peels

Hub `ChatPanelViewModel.cs` still over quality gate — peel coherent concerns into partials (target ≤~200 LOC / file when practical; project `file_lines` soft fail is 800).

## Recent peels

| Partial | Owns |
| --- | --- |
| `ChatPanelViewModel.Clarification.cs` | Clarification batch show/submit/dismiss + MCP JSON entry points |
| `ChatPanelViewModel.MessageSelection.cs` | Index/offset/thread selection, thinking toggle, assistant edit, readable export |

Existing concern partials (Intercom*, Session, Sedm, Threading, Composer*, …) stay as mapped by filename.

Next hub candidates: CursorACP send/watchdog helpers (`SendChatWithCursorAcpAsync` method_lines warn), MCP append, streaming send paths.
