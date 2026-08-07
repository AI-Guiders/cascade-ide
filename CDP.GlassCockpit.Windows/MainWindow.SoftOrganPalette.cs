#nullable enable

using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Ctrl+Q SoftOrgan EICAS peel (QRH/ECL/alert) — Face now + citizen @intent PlaceOrgan.</summary>
public partial class MainWindow
{
    /// <summary>Equal-hands SoftOrgan open: BringCabin + PreferSurface alert + EICAS tip; habitat runs @intent.</summary>
    void OpenSoftOrganFace(string organId, string intentTail)
    {
        BringCabinAttention();
        _hosts.PreferSurface("alert");

        var label = SoftOrganChromeDensityPolicy.ShortLabel(organId);
        _eicas.Apply(organId, $"EICAS · {label} · opening…");

        // Lived SoftFL: PreferSurface(alert) alone was chrome tip — no Face page (operator: "где QRH?").
        SelectMfdPage("MarkdownPreview", sticky: true);
        ShowSoftOrganHandbookFace(organId, label);

        var sent = GlassCitizenDialogRequest.TryEnqueue(
            $"@intent {intentTail}",
            workspaceRoot: _session.WorkspaceRoot);

        StatusText.Text = sent is null
            ? $"glass · soft · {label} · enqueue fail · {DateTime.Now:HH:mm:ss}"
            : $"glass · soft · {label} · Face+@intent {intentTail} · {sent.Id} · {DateTime.Now:HH:mm:ss}";
    }

    void ShowSoftOrganHandbookFace(string organId, string label)
    {
        if (MarkdownDocumentViewer is null)
            return;

        var body = SoftOrganFaceHandbook.MarkdownFor(organId, label);
        MarkdownDocumentViewer.Document = GlassMarkdownFlowDocumentBuilder.Build(body, MarkdownPipe);
        if (MarkdownStatusLabel is not null)
            MarkdownStatusLabel.Text = $"markdown · SoftOrgan · {label} · Face";
    }
}
