#nullable enable

using AgentNotes.Core;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.CasaField.Application;

/// <summary>Find markdown heading line for a KB section title.</summary>
public static class KbSectionLineResolver
{
    public static int? TryFindSectionLine(string? workspaceRoot, string knowledgeDocPath, string? sectionTitle)
    {
        if (string.IsNullOrWhiteSpace(sectionTitle) || string.IsNullOrWhiteSpace(knowledgeDocPath))
            return null;

        if (!TryReadKnowledgeMarkdown(workspaceRoot, knowledgeDocPath, out var markdown))
            return null;

        return FindSectionLineInMarkdown(markdown, sectionTitle);
    }

    public static int? FindSectionLineInMarkdown(string markdown, string sectionTitle)
    {
        var target = Normalize(sectionTitle);
        if (target.Length == 0)
            return null;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.TrimStart().StartsWith('#'))
                continue;

            var heading = ExtractHeadingText(line);
            var hNorm = Normalize(heading);
            if (hNorm.Length == 0)
                continue;

            if (string.Equals(hNorm, target, StringComparison.OrdinalIgnoreCase)
                || hNorm.Contains(target, StringComparison.OrdinalIgnoreCase)
                || target.Contains(hNorm, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }

        return null;
    }

    internal static string Normalize(string text)
    {
        var s = text.Trim();
        var slash = s.LastIndexOf('/');
        if (slash >= 0 && slash < s.Length - 1)
            s = s[(slash + 1)..].Trim();
        return s;
    }

    private static string ExtractHeadingText(string line)
    {
        var t = line.TrimStart();
        var i = 0;
        while (i < t.Length && t[i] == '#')
            i++;
        return t[i..].Trim();
    }

    private static bool TryReadKnowledgeMarkdown(string? workspaceRoot, string knowledgeDocPath, out string markdown)
    {
        markdown = "";
        var notesWs = McpAgentNotesService.ResolveNotesWorkspacePath(workspaceRoot);
        if (string.IsNullOrEmpty(notesWs))
            return false;

        try
        {
            var storage = new NotesStorage();
            markdown = storage.ReadKnowledgeFile(knowledgePath: null, filePath: knowledgeDocPath);
            return !string.IsNullOrEmpty(markdown);
        }
        catch
        {
            return false;
        }
    }
}
