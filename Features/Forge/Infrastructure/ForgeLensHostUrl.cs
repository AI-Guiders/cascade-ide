namespace CascadeIDE.Features.Forge.Infrastructure;

internal static class ForgeLensHostUrl
{
    internal static string NormalizeKey(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
            throw new ArgumentException("Forge base URL must be absolute.", nameof(baseUrl));

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/').ToLowerInvariant();
    }
}
