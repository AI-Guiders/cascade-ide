#nullable enable

using CascadeIDE.Services.Intercom;
using HybridCodebaseIndex.Core.Indexing;

namespace CascadeIDE.Features.HybridIndex.Application;

internal static class HybridIndexIntercomReindexObservers
{
    public static IReadOnlyList<ICodebaseIndexReindexObserver> Create(string? solutionPath, string indexDirectoryRelative) =>
        [new IntercomSymbolLineHciReindexObserver(solutionPath, indexDirectoryRelative)];
}
