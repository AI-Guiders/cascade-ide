#nullable enable

namespace CascadeIDE.Features.MagicLink;

public static class CideMagicLinkArgExtractor
{
    public static string? FromArgs(string[] args)
    {
        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
                continue;

            var trimmed = arg.Trim();
            if (trimmed.StartsWith($"{CideMagicLinkUri.Scheme}://", StringComparison.OrdinalIgnoreCase))
                return trimmed;
        }

        return null;
    }
}
