#!/usr/bin/env dotnet script
// Сборка всех нумерованных ADR в один документ (как resume: фрагменты через INCLUDE).
// Запуск из каталога docs/adr:
//   dotnet script build-adr.csx
//   dotnet script build-adr.csx --root "D:\path\to\docs\adr"
// Нужно: dotnet tool install -g dotnet-script, Pandoc; PDF без LaTeX — Microsoft Edge.
//
// Директивы в .md (тот же контракт, что resume/build-resume.csx):
//   {{ INCLUDE: путь/от/корня/adr.md }}
//   {{ INCLUDE_MANIFEST: build/order.txt }}
//   {{ INCLUDE_GLOB: snippets/*.md }}
//
// Режимы:
//   • Если есть adr-book.md — он корень (YAML + INCLUDE*), как resume.md.
//   • Иначе — склеиваются все файлы ^\d{4}-.+\.md$ в docs/adr по имени, между ними ---; затем ExpandIncludes.
// Опция: --book adr-book-ui.md — другой корневой файл и префикс артефактов (adr-book-ui.html и т.д.).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

var adrRoot = ResolveAdrRoot();

string bookArg = null;
var argvForBook = Environment.GetCommandLineArgs();
for (var bi = 0; bi < argvForBook.Length - 1; bi++)
    if (string.Equals(argvForBook[bi], "--book", StringComparison.OrdinalIgnoreCase))
    {
        bookArg = argvForBook[bi + 1];
        break;
    }

var bookRel = string.IsNullOrWhiteSpace(bookArg) ? "adr-book.md" : bookArg.Trim();
var baseName = Path.GetFileNameWithoutExtension(bookRel);
if (string.IsNullOrEmpty(baseName)) baseName = "adr-book";
var bookPath = Path.Combine(adrRoot, bookRel.Replace('/', Path.DirectorySeparatorChar));

var buildDir = Path.Combine(adrRoot, "build");
var outDir = Path.Combine(adrRoot, "out");
var outHtmlDir = Path.Combine(outDir, "html");
var outPdfDir = Path.Combine(outDir, "pdf");
var outTxtDir = Path.Combine(outDir, "txt");
var utf8NoBom = new UTF8Encoding(false);

string ResolveAdrRoot()
{
    var argv = Environment.GetCommandLineArgs();
    for (var i = 0; i < argv.Length - 1; i++)
        if (string.Equals(argv[i], "--root", StringComparison.OrdinalIgnoreCase))
            return Path.GetFullPath(argv[i + 1]);
    return Directory.GetCurrentDirectory();
}

string ExpandManifest(string manifestRel, string baseDir)
{
    var manifestPath = Path.Combine(baseDir, manifestRel.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(manifestPath))
    {
        Console.WriteLine($"Warning: INCLUDE_MANIFEST not found: {manifestPath}");
        return "";
    }
    var sb = new StringBuilder();
    foreach (var line in File.ReadAllLines(manifestPath, Encoding.UTF8))
    {
        var t = line.Trim();
        if (t.Length == 0 || t.StartsWith("#", StringComparison.Ordinal)) continue;
        var p = Path.Combine(baseDir, t.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(p))
        {
            Console.WriteLine($"Warning: manifest line file missing: {p}");
            continue;
        }
        if (sb.Length > 0) sb.AppendLine();
        sb.Append(File.ReadAllText(p, Encoding.UTF8));
    }
    return sb.ToString();
}

string ExpandGlob(string globRel, string baseDir)
{
    var g = globRel.Replace('/', Path.DirectorySeparatorChar).Trim();
    var star = g.IndexOf('*');
    if (star < 0)
    {
        Console.WriteLine("Warning: INCLUDE_GLOB needs a * pattern (e.g. snippets/*.md)");
        return "";
    }
    var beforeStar = g.Substring(0, star);
    var lastSep = Math.Max(beforeStar.LastIndexOf(Path.DirectorySeparatorChar), beforeStar.LastIndexOf('/'));
    var relDir = lastSep >= 0 ? beforeStar.Substring(0, lastSep) : "";
    var pattern = g.Substring(star);
    var dir = Path.Combine(baseDir, relDir);
    if (!Directory.Exists(dir))
    {
        Console.WriteLine($"Warning: INCLUDE_GLOB directory not found: {dir}");
        return "";
    }
    var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly)
        .Where(f => !Path.GetFileName(f).StartsWith("_", StringComparison.Ordinal))
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    var sb = new StringBuilder();
    foreach (var f in files)
    {
        if (sb.Length > 0) sb.AppendLine();
        sb.Append(File.ReadAllText(f, Encoding.UTF8));
    }
    return sb.ToString();
}

// Fenced ``` … ``` → токены, чтобы примеры {{ INCLUDE: … }} в кодовых блоках не раскрывались.
(string Masked, Dictionary<string, string> FenceMap) MaskFencedBlocks(string content)
{
    var map = new Dictionary<string, string>();
    var sb = new StringBuilder();
    var pos = 0;
    var n = 0;
    while (pos < content.Length)
    {
        var start = content.IndexOf("```", pos, StringComparison.Ordinal);
        if (start < 0)
        {
            sb.Append(content, pos, content.Length - pos);
            break;
        }
        sb.Append(content, pos, start - pos);
        var lineEnd = content.IndexOf('\n', start);
        if (lineEnd < 0)
        {
            sb.Append(content, start, content.Length - start);
            break;
        }
        var contentStart = lineEnd + 1;
        var closeLineStart = -1;
        var scan = contentStart;
        while (scan < content.Length)
        {
            var nl = content.IndexOf('\n', scan);
            if (nl < 0)
                break;
            var lineBegin = nl + 1;
            if (lineBegin + 3 <= content.Length && content[lineBegin] == '`' && content[lineBegin + 1] == '`' && content[lineBegin + 2] == '`')
            {
                var afterTicks = lineBegin + 3;
                if (afterTicks >= content.Length || content[afterTicks] == '\r' || content[afterTicks] == '\n')
                {
                    closeLineStart = lineBegin;
                    break;
                }
            }
            scan = nl + 1;
        }
        if (closeLineStart < 0)
        {
            sb.Append(content, start, content.Length - start);
            break;
        }
        var blockEnd = closeLineStart + 3;
        while (blockEnd < content.Length && (content[blockEnd] == '\r' || content[blockEnd] == '\n'))
            blockEnd++;
        var full = content.Substring(start, blockEnd - start);
        var token = $"\uE000FENCE{n++}\uE000";
        map[token] = full;
        sb.Append(token);
        pos = blockEnd;
    }
    return (sb.ToString(), map);
}

string UnmaskFencedBlocks(string content, Dictionary<string, string> map)
{
    foreach (var kv in map)
        content = content.Replace(kv.Key, kv.Value, StringComparison.Ordinal);
    return content;
}

string ExpandIncludes(string content, string baseDir)
{
    var (masked, fenceMap) = MaskFencedBlocks(content);
    content = masked;

    var rxManifest = new Regex(@"\{\{\s*INCLUDE_MANIFEST:\s*([^\s\}]+)\s*\}\}");
    var rxGlob = new Regex(@"\{\{\s*INCLUDE_GLOB:\s*([^\s\}]+)\s*\}\}");
    var rxSingle = new Regex(@"\{\{\s*INCLUDE:\s*([^\s\}]+)\s*\}\}");

    while (true)
    {
        var mMan = rxManifest.Match(content);
        var mGlob = rxGlob.Match(content);
        var mOne = rxSingle.Match(content);

        var candidates = new List<(int Index, int Kind, Match M)>();
        if (mMan.Success) candidates.Add((mMan.Index, 0, mMan));
        if (mGlob.Success) candidates.Add((mGlob.Index, 1, mGlob));
        if (mOne.Success) candidates.Add((mOne.Index, 2, mOne));
        if (candidates.Count == 0) break;

        var pick = candidates.OrderBy(c => c.Index).First();
        var m = pick.M;
        string replacement;
        switch (pick.Kind)
        {
            case 0:
                replacement = ExpandManifest(m.Groups[1].Value.Trim(), baseDir);
                break;
            case 1:
                replacement = ExpandGlob(m.Groups[1].Value.Trim(), baseDir);
                break;
            default:
                var includePath = Path.Combine(baseDir, m.Groups[1].Value.Trim().Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(includePath))
                {
                    Console.WriteLine($"Warning: INCLUDE not found: {includePath}");
                    replacement = "";
                }
                else
                    replacement = File.ReadAllText(includePath, Encoding.UTF8);
                break;
        }

        content = content.Substring(0, m.Index) + replacement + content.Substring(m.Index + m.Length);
    }

    return UnmaskFencedBlocks(content, fenceMap);
}

int Run(string executable, string args, string workingDirectory = null)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (!string.IsNullOrEmpty(workingDirectory))
            psi.WorkingDirectory = workingDirectory;
        using var p = Process.Start(psi);
        var stdout = p!.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            Console.WriteLine($"Error: command failed (exit={p.ExitCode}): {executable} {args}");
            if (!string.IsNullOrWhiteSpace(stdout))
                Console.WriteLine(stdout.TrimEnd());
            if (!string.IsNullOrWhiteSpace(stderr))
                Console.WriteLine(stderr.TrimEnd());
        }

        return p.ExitCode;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return -1;
    }
}

void TryInjectMermaidScript(string htmlPath)
{
    try
    {
        if (string.IsNullOrEmpty(htmlPath) || !File.Exists(htmlPath)) return;
        var html = File.ReadAllText(htmlPath, Encoding.UTF8);
        if (!html.Contains("class=\"mermaid\"", StringComparison.Ordinal)) return;
        if (html.Contains("mermaid.min.js", StringComparison.OrdinalIgnoreCase)) return;
        if (!html.Contains("</body>", StringComparison.OrdinalIgnoreCase)) return;
        const string snippet = "\n<script src=\"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js\"></script>\n<script>\n  mermaid.initialize({ startOnLoad: true, theme: \"neutral\", securityLevel: \"loose\" });\n</script>\n";
        html = html.Replace("</body>", snippet + "</body>", StringComparison.OrdinalIgnoreCase);
        File.WriteAllText(htmlPath, html, new UTF8Encoding(false));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Mermaid inject skipped: {ex.Message}");
    }
}

string FindEdge()
{
    var x86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    foreach (var p in new[]
             {
                 Path.Combine(x86 ?? "", "Microsoft", "Edge", "Application", "msedge.exe"),
                 Path.Combine(pf, "Microsoft", "Edge", "Application", "msedge.exe")
             })
        if (File.Exists(p)) return p;
    return null;
}

string FindInPath(string name)
{
    if (string.IsNullOrEmpty(Path.GetExtension(name)) && Environment.OSVersion.Platform == PlatformID.Win32NT)
        name += ".exe";
    var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
    foreach (var dir in pathEnv.Split(Path.PathSeparator))
    {
        var full = Path.Combine(dir.Trim(), name);
        if (File.Exists(full)) return full;
    }
    return null;
}

string BuildAutoConcatBody()
{
    var rx = new Regex(@"^\d{4}-.+\.md$", RegexOptions.IgnoreCase);
    var files = Directory.GetFiles(adrRoot, "*.md", SearchOption.TopDirectoryOnly)
        .Where(f => rx.IsMatch(Path.GetFileName(f)))
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (files.Length == 0)
    {
        Console.WriteLine("No numbered ADR files (NNNN-*.md) in " + adrRoot);
        return "";
    }
    Console.WriteLine($"Auto-concat: {files.Length} ADR file(s).");
    var sb = new StringBuilder();
    sb.AppendLine("---");
    sb.AppendLine($"title: \"Cascade IDE — Architecture Decision Records\"");
    sb.AppendLine($"date: {DateTime.UtcNow:yyyy-MM-dd}");
    sb.AppendLine("lang: ru");
    sb.AppendLine("---");
    sb.AppendLine();
    sb.AppendLine("*Сборка: все файлы `NNNN-*.md` в каталоге `docs/adr`, порядок по имени. DOCX не генерируется по политике сборки.*");
    sb.AppendLine();
    for (var i = 0; i < files.Length; i++)
    {
        if (i > 0) sb.AppendLine().AppendLine("---").AppendLine();
        sb.Append(File.ReadAllText(files[i], Encoding.UTF8));
    }
    return sb.ToString();
}

var pandocPath = FindInPath("pandoc");
if (string.IsNullOrEmpty(pandocPath))
{
    Console.WriteLine("Pandoc not found. Install: winget install JohnMacFarlane.Pandoc");
    return 1;
}

string mdContent;
if (!string.IsNullOrWhiteSpace(bookArg) && !File.Exists(bookPath))
{
    Console.WriteLine($"--book file not found: {bookPath}");
    return 1;
}
if (File.Exists(bookPath))
{
    Console.WriteLine($"Source: {bookRel}");
    mdContent = File.ReadAllText(bookPath, Encoding.UTF8);
}
else
{
    Console.WriteLine("Source: auto-concat of NNNN-*.md (no adr-book.md)");
    mdContent = BuildAutoConcatBody();
    if (string.IsNullOrEmpty(mdContent)) return 1;
}

mdContent = ExpandIncludes(mdContent, adrRoot);

Directory.CreateDirectory(buildDir);
Directory.CreateDirectory(outHtmlDir);
Directory.CreateDirectory(outPdfDir);
Directory.CreateDirectory(outTxtDir);

var expandedMdPath = Path.Combine(buildDir, $"{baseName}.md");
File.WriteAllText(expandedMdPath, mdContent, utf8NoBom);
Console.WriteLine($"OK: {expandedMdPath}");

var cssPath = Path.Combine(adrRoot, "adr.css");
if (File.Exists(cssPath))
    File.Copy(cssPath, Path.Combine(outHtmlDir, "adr.css"), true);

var htmlPath = Path.Combine(outHtmlDir, $"{baseName}.html");
var cssArg = File.Exists(cssPath) ? " -c \"adr.css\"" : "";
var expandedFull = Path.GetFullPath(expandedMdPath);
var htmlArgs = $"\"{expandedFull}\" -o \"{baseName}.html\" --standalone --metadata lang=ru --toc --toc-depth=3{cssArg}";
if (Run(pandocPath, htmlArgs, outHtmlDir) != 0)
{
    Console.WriteLine("Pandoc → HTML failed.");
    return 1;
}
Console.WriteLine($"OK: {htmlPath}");
TryInjectMermaidScript(htmlPath);

var txtPath = Path.Combine(outTxtDir, $"{baseName}.txt");
var txtArgs = $"\"{expandedFull}\" -t plain -o \"{txtPath}\"";
if (Run(pandocPath, txtArgs) == 0)
    Console.WriteLine($"OK: {txtPath}");
else
    Console.WriteLine("Warning: plain text export failed.");

var pdfPath = Path.Combine(outPdfDir, $"{baseName}.pdf");
var pdfPandocPath = Path.Combine(outPdfDir, $"{baseName}.pandoc.pdf");

var pdfDirect = $"\"{expandedFull}\" -o \"{pdfPandocPath}\" --metadata lang=ru --pdf-engine=xelatex -V mainfont=\"Times New Roman\" -V monofont=\"Consolas\"";
var okPdf = Run(pandocPath, pdfDirect) == 0;

if (okPdf && File.Exists(pdfPandocPath))
{
    try
    {
        File.Copy(pdfPandocPath, pdfPath, true);
        Console.WriteLine($"OK: {pdfPath} (Pandoc PDF engine)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Pandoc PDF: {ex.Message}");
        Console.WriteLine($"OK: {pdfPandocPath}");
    }
    return 0;
}

var edgePath = FindEdge();
if (string.IsNullOrEmpty(edgePath))
{
    Console.WriteLine("PDF: нет LaTeX и не найден Edge — пропуск PDF. HTML и TXT в out/.");
    return 0;
}

var edgeArgs = $"--headless --disable-gpu --no-pdf-header-footer --print-to-pdf=\"{pdfPath}\" \"{htmlPath}\"";
if (Run(edgePath, edgeArgs) != 0)
{
    Console.WriteLine("Edge PDF failed.");
    return 1;
}
System.Threading.Thread.Sleep(1500);
if (File.Exists(pdfPath))
    Console.WriteLine($"OK: {pdfPath} (Edge print)");
else
    Console.WriteLine("PDF file not created after Edge print.");

return 0;
