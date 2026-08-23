#r "D:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide\CascadeIDE.GlassCore\bin\Release\net10.0\CascadeIDE.GlassCore.dll"
var path = @"D:\Experiments\Personal Cursor Folder\Financial\software\open\cdp-mcp\CdpMcp.csproj";
var root = CascadeIDE.Services.SolutionParser.Load(path);
var ws = System.IO.Path.GetDirectoryName(path)!;
var hits = CascadeIDE.SoftOrgan.GlassGoToFileIndex.Search(root, ws, "Program", 20);
Console.WriteLine($"title={root?.Name} kids={root?.Children.Count} hits={hits.Count}");
foreach (var h in hits.Take(5)) Console.WriteLine($"  {h.Relative}");
