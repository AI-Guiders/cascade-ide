using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: документы (см. <c>IdeCommands.PowerDocuments.cs</c>).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterDocumentsPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Документы
        AddPalette(b, "reopen_closed_document", IdeCommands.ReopenClosedDocument, "Открыть закрытую вкладку", "Документы");
    }
}
