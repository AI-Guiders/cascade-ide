#r "nuget: AIGuiders.HybridCodebaseIndex.Core, 0.1.2"
// Prefer project-built DLL beside Core for 0.1.3 densify:
#r "D:\\Experiments\\Personal Cursor Folder\\Financial\\software\\open\\hybrid-codebase-index-core\\bin\\Release\\net10.0\\HybridCodebaseIndex.Core.dll"
#r "nuget: Microsoft.Data.Sqlite, 10.0.0"
#r "nuget: SQLitePCLRaw.bundle_e_sqlite3, 3.0.3"
using HybridCodebaseIndex.Core;
var root = @"D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide";
var svc = new CodebaseIndexService();
Console.WriteLine("rebuild…");
var sum = await svc.FullRebuildAsync(root);
Console.WriteLine($"files={sum.FilesIndexed} db={sum.DatabasePath}");
var (mid, err) = await svc.SearchAsync(root, "BoardLeaf", topN: 8);
Console.WriteLine($"BoardLeaf err={err} hits={mid.Hits.Count}");
foreach (var h in mid.Hits.Take(5))
  Console.WriteLine($"  {h.Path}:{h.LineStart}");
