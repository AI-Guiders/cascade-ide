#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Human GUI → agent reverse of <see cref="CdpLandProjector"/>.
/// Publishes %LocalAppData%/cdp-mcp/focus-LATEST.json from stabilized caret (ADR 0103).
/// Does not watch land-LATEST; Melody untouched.
/// </summary>
internal sealed class CdpFocusLatchPublisher : IDisposable
{
    public const string Schema = "navigation_focus_latch/v1";

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    readonly object _gate = new();
    readonly TimeSpan _coalesce = TimeSpan.FromMilliseconds(200);
    DateTimeOffset _suppressUntil = DateTimeOffset.MinValue;
    DateTimeOffset _lastWrite = DateTimeOffset.MinValue;
    string? _lastPath;
    int _lastLine;
    int _lastColumn;
    bool _disposed;

    public static string StateRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "cdp-mcp");

    public static string LatchPath => Path.Combine(StateRoot, "focus-LATEST.json");

    public static CdpFocusLatchPublisher? Instance { get; private set; }

    public static CdpFocusLatchPublisher Start()
    {
        Instance?.Dispose();
        Instance = new CdpFocusLatchPublisher();
        return Instance;
    }

    /// <summary>After agent land apply — ignore caret echo for a short window.</summary>
    public void SuppressEcho(TimeSpan? duration = null)
    {
        lock (_gate)
            _suppressUntil = DateTimeOffset.UtcNow + (duration ?? TimeSpan.FromMilliseconds(400));
    }

    public void OnStabilizedCaret(EditorInputDelta delta, string? editorText)
    {
        if (_disposed || delta.Kind == EditorInputDeltaKind.DocumentText)
            return;
        if (string.IsNullOrWhiteSpace(delta.FilePath))
            return;

        lock (_gate)
        {
            if (DateTimeOffset.UtcNow < _suppressUntil)
                return;
        }

        var text = editorText ?? "";
        if (!EditorTextCoordinateUtilities.TryOffsetToLineColumn(text, delta.CaretOffset, out var line, out var column))
        {
            line = 1;
            column = 1;
        }

        int? endLine = null;
        int? endColumn = null;
        if (delta.SelectionLength > 0)
        {
            var endOff = delta.SelectionStart + delta.SelectionLength;
            if (EditorTextCoordinateUtilities.TryOffsetToLineColumn(text, endOff, out var el, out var ec))
            {
                endLine = el;
                endColumn = ec;
            }
        }

        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (string.Equals(_lastPath, delta.FilePath, StringComparison.OrdinalIgnoreCase)
                && _lastLine == line
                && _lastColumn == column
                && endLine is null
                && now - _lastWrite < _coalesce)
                return;

            _lastPath = delta.FilePath;
            _lastLine = line;
            _lastColumn = column;
            _lastWrite = now;
        }

        try
        {
            Directory.CreateDirectory(StateRoot);
            var doc = new FocusLatchDoc
            {
                Schema = Schema,
                Path = delta.FilePath!,
                Line = line,
                Column = column,
                EndLine = endLine,
                EndColumn = endColumn,
                CaretOffset = delta.CaretOffset,
                SelectionLength = delta.SelectionLength,
                Origin = "human",
                StampedUtc = DateTimeOffset.UtcNow
            };
            var json = JsonSerializer.Serialize(doc, JsonOpts);
            var tmp = LatchPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, LatchPath, overwrite: true);
        }
        catch
        {
            /* best-effort */
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    sealed class FocusLatchDoc
    {
        public string Schema { get; set; } = CdpFocusLatchPublisher.Schema;
        public string Path { get; set; } = "";
        public int Line { get; set; }
        public int Column { get; set; }
        public int? EndLine { get; set; }
        public int? EndColumn { get; set; }
        public int CaretOffset { get; set; }
        public int SelectionLength { get; set; }
        public string Origin { get; set; } = "human";
        public DateTimeOffset StampedUtc { get; set; }
    }
}
