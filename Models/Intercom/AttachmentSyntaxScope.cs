#nullable enable

using System.Text.Json;

namespace CascadeIDE.Models.Intercom;

/// <summary>Синтаксический scope внутри члена (ADR 0128 §3): kind + indexInParent + optional parentMemberKey.</summary>
public sealed record AttachmentSyntaxScope(
    string Kind,
    int IndexInParent,
    string? ParentMemberKey)
{
    public static bool TryParse(JsonElement? element, out AttachmentSyntaxScope? scope)
    {
        scope = null;
        if (element is not { } el || el.ValueKind != JsonValueKind.Object)
            return false;

        var kind = readString(el, "kind");
        if (string.IsNullOrWhiteSpace(kind))
            return false;

        var index = 1;
        if (el.TryGetProperty("indexInParent", out var idxEl) && idxEl.ValueKind == JsonValueKind.Number && idxEl.TryGetInt32(out var i))
            index = i;
        else if (el.TryGetProperty("index_in_parent", out var idx2) && idx2.ValueKind == JsonValueKind.Number && idx2.TryGetInt32(out var i2))
            index = i2;

        if (index < 1)
            index = 1;

        var parent = readString(el, "parentMemberKey") ?? readString(el, "parent_member_key");
        scope = new AttachmentSyntaxScope(kind.Trim(), index, string.IsNullOrWhiteSpace(parent) ? null : parent.Trim());
        return true;
    }

    private static string? readString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
}
