using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>Interop с <c>forge auth login</c> → <c>~/.forge/credentials.json</c> (FORGE-ADR-0010).</summary>
public static class ForgeSharedCredentialReader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string? TryGetToken(string baseUrl)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".forge",
                "credentials.json");
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize<ForgeSharedCredentialsFile>(json, JsonOptions);
            if (file?.Hosts is null)
                return null;

            var key = ForgeLensHostUrl.NormalizeKey(baseUrl);
            return file.Hosts.TryGetValue(key, out var entry)
                && !string.IsNullOrWhiteSpace(entry.Token)
                ? entry.Token.Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed class ForgeSharedCredentialsFile
    {
        public Dictionary<string, ForgeSharedHostCredential> Hosts { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ForgeSharedHostCredential
    {
        public string Token { get; set; } = "";
    }
}
