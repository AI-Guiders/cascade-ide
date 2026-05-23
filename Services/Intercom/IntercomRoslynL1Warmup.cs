#nullable enable

namespace CascadeIDE.Services.Intercom;

/// <summary>Прогрев L1 parse/model cache (ADR 0141 P2).</summary>
public static class IntercomRoslynL1Warmup
{
    public static void WarmFile(IntercomAttachResolveCacheContext cacheContext, string absoluteFilePath)
    {
        if (!absoluteFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;

        _ = AttachmentAnchorRoslynResolver.TryGetOrCreateEntry(
            null,
            absoluteFilePath,
            cacheContext,
            out _,
            out _);
    }
}
