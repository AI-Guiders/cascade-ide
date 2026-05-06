using System.IO.Compression;
using System.Security.Cryptography;

namespace CascadeIDE.Services;

/// <summary>Распаковка встроенного <c>kb-base-cide.zip</c> в кэш под <c>%LocalAppData%\CascadeIDE\kb-base-embedded-cache\{{sha256}}</c>.</summary>
internal static class KbBaseEmbeddedBundleProvisioner
{
    internal const string ResourceName = "CascadeIDE.KbBase.kb-base-cide.zip";

    private static readonly object Gate = new();
    private static string? _resolvedCanonRoot;

    /// <returns>Корень канона (родитель каталога <c>knowledge/</c>) или <c>null</c>, если ресурса нет / распаковка невозможна.</returns>
    public static string? TryGetEmbeddedCanonRoot()
    {
        if (_resolvedCanonRoot is not null &&
            Directory.Exists(Path.Combine(_resolvedCanonRoot, "knowledge")))
            return _resolvedCanonRoot;

        lock (Gate)
        {
            if (_resolvedCanonRoot is not null &&
                Directory.Exists(Path.Combine(_resolvedCanonRoot, "knowledge")))
                return _resolvedCanonRoot;

            using var stream = typeof(KbBaseEmbeddedBundleProvisioner).Assembly.GetManifestResourceStream(ResourceName);
            if (stream is null)
            {
                _resolvedCanonRoot = null;
                return null;
            }

            byte[] zipBytes;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                zipBytes = ms.ToArray();
            }

            var hash = Convert.ToHexString(SHA256.HashData(zipBytes)).ToLowerInvariant();
            var extractRoot = Path.Combine(UserSettingsPaths.GetSettingsDirectory(), "kb-base-embedded-cache", hash);
            var markerPath = Path.Combine(extractRoot, ".extracted-marker");

            Directory.CreateDirectory(extractRoot);

            var knowledgeDir = Path.Combine(extractRoot, "knowledge");
            if (!File.Exists(markerPath) || !Directory.Exists(knowledgeDir))
            {
                TryClearExtractFolder(extractRoot);
                Directory.CreateDirectory(extractRoot);

                var tempZip = Path.Combine(extractRoot, "_bundle extracting.tmp.zip");
                try
                {
                    File.WriteAllBytes(tempZip, zipBytes);
                    ZipFile.ExtractToDirectory(tempZip, extractRoot, overwriteFiles: true);
                }
                finally
                {
                    TryDeleteSilently(tempZip);
                }

                if (!Directory.Exists(knowledgeDir))
                {
                    TryClearExtractFolder(extractRoot);
                    _resolvedCanonRoot = null;
                    return null;
                }

                File.WriteAllText(markerPath, hash + "\n", System.Text.Encoding.UTF8);
            }

            _resolvedCanonRoot = extractRoot;
            return extractRoot;
        }
    }

    private static void TryClearExtractFolder(string extractRoot)
    {
        try
        {
            if (!Directory.Exists(extractRoot))
                return;
            foreach (var f in Directory.EnumerateFiles(extractRoot))
                TryDeleteSilently(f);
            foreach (var sub in Directory.EnumerateDirectories(extractRoot))
                Directory.Delete(sub, recursive: true);
        }
        catch
        {
            // повтор попытку на следующем заходе
        }
    }

    private static void TryDeleteSilently(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }
}
