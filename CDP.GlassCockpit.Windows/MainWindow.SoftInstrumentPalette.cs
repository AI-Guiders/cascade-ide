#nullable enable

using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>Ctrl+Q SoftInstrument EICAS peel (QRH/ECL/alert) — Face now + citizen @intent PlaceOrgan.</summary>
public partial class MainWindow
{
    /// <summary>Equal-hands SoftInstrument open: BringCabin + PreferSurface alert + EICAS tip; habitat runs @intent.</summary>
    void OpenSoftInstrumentFace(string organId, string intentTail)
    {
        BringCabinAttention();
        _hosts.PreferSurface("alert");

        var label = SoftInstrumentChromeDensityPolicy.ShortLabel(organId);
        _eicas.Apply(organId, $"EICAS · {label} · opening…");

        // Lived SoftFL (operator eyes): markdown wall ≠ QRH/ECL — Plan-like glance cards + find.
        var page = SoftInstrumentFaceHandbook.MfdPageFor(organId);
        if (SoftInstrumentFindBox is not null)
            SoftInstrumentFindBox.Text = "";
        SelectMfdPage(page, sticky: true);
        RefreshGlanceCardsBody();

        var sent = GlassCitizenDialogRequest.TryEnqueue(
            $"@intent {intentTail}",
            workspaceRoot: _session.WorkspaceRoot);

        StatusText.Text = sent is null
            ? $"glass · soft · {label} · enqueue fail · {DateTime.Now:HH:mm:ss}"
            : $"glass · soft · {label} · Face cards+@intent {intentTail} · {sent.Id} · {DateTime.Now:HH:mm:ss}";
    }
}
