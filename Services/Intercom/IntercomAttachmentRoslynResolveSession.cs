#nullable enable

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Services.Intercom;

/// <summary>Кэш parse/semantic model на одну отправку сообщения (один файл — одна компиляция).</summary>
public sealed class IntercomAttachmentRoslynResolveSession
{
    private readonly ConcurrentDictionary<string, FileEntry?> _entries = new(StringComparer.OrdinalIgnoreCase);

    internal ConcurrentDictionary<string, FileEntry?> Entries => _entries;

    internal sealed class FileEntry
    {
        public required string Text { get; init; }
        public required SyntaxTree Tree { get; init; }
        public required SemanticModel Model { get; init; }
    }
}
