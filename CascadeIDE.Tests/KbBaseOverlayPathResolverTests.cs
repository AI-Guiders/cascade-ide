using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class KbBaseOverlayPathResolverTests
{
    [Fact]
    public void TryResolveCanonRoot_ReturnsNull_WhenKbDirMissingUnderOverlay()
    {
        var settings = new CascadeIdeSettings
        {
            AgentNotes = new AgentNotesSettings { KbBaseOverlayPath = "not-a-real-canon-xxxx" },
        };

        Assert.Null(KbBaseOverlayPathResolver.TryResolveCanonRoot(settings));
    }

    [Fact]
    public void TryResolveCanonRoot_WithRelativePath_ResolvesFromSettingsDirectoryPrefix()
    {
        var unique = $"kb-overlay-test-{Guid.NewGuid():N}";
        var canon = Path.Combine(UserSettingsPaths.GetSettingsDirectory(), unique);
        var knowledgePath = Path.Combine(canon, "knowledge");

        Directory.CreateDirectory(knowledgePath);

        try
        {
            var settings = new CascadeIdeSettings
            {
                AgentNotes = new AgentNotesSettings { KbBaseOverlayPath = unique },
            };

            Assert.Equal(CanonicalFilePath.Normalize(canon), CanonicalFilePath.Normalize(KbBaseOverlayPathResolver.TryResolveCanonRoot(settings)!));
        }
        finally
        {
            try
            {
                Directory.Delete(canon, recursive: true);
            }
            catch
            {
                // тест только для короткоживущей папки
            }
        }
    }
}
