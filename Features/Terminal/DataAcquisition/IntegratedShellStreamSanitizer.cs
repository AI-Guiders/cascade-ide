using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Terminal.DataAcquisition;

[IoBoundary]
internal static class IntegratedShellStreamSanitizer
{
    public static byte[] SanitizeShellOutput(ReadOnlySpan<byte> data, ref bool leadingBomStripped)
    {
        if (data.IsEmpty)
            return [];

        if (!leadingBomStripped)
        {
            data = StripLeadingUtf8Bom(data);
            leadingBomStripped = true;
        }

        return data.ToArray();
    }

    public static ReadOnlySpan<byte> StripLeadingUtf8Bom(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            return data[3..];

        return data;
    }
}
