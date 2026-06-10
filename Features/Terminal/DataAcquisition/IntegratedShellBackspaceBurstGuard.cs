using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Terminal.DataAcquisition;

/// <summary>
/// Не шлёт подряд много Backspace без ответа shell (PSReadLine рисует строку после каждого удаления).
/// На пустой строке stdout молчит — лимит защищает от FailFast legacy ConsoleHost pwsh.
/// </summary>
[IoBoundary]
internal sealed class IntegratedShellBackspaceBurstGuard
{
    private const int MaxConsecutiveBackspaceWithoutOutput = 48;

    private int _consecutiveBackspaceWithoutOutput;

    public void Reset() => _consecutiveBackspaceWithoutOutput = 0;

    public void NotifyShellOutput() => _consecutiveBackspaceWithoutOutput = 0;

    public byte[] FilterUserInput(ReadOnlySpan<byte> input)
    {
        if (input.IsEmpty)
            return [];

        input = IntegratedShellStreamSanitizer.StripLeadingUtf8Bom(input);
        if (input.IsEmpty)
            return [];

        if (!ContainsBackspace(input))
        {
            _consecutiveBackspaceWithoutOutput = 0;
            return input.ToArray();
        }

        if (IntegratedShellLaunch.IsPureBackspaceInput(input))
        {
            if (_consecutiveBackspaceWithoutOutput >= MaxConsecutiveBackspaceWithoutOutput)
                return [];

            _consecutiveBackspaceWithoutOutput++;
            return input.ToArray();
        }

        var output = new List<byte>(input.Length);
        var index = 0;
        while (index < input.Length)
        {
            var current = input[index];
            if (current is 0x7F or 0x08)
            {
                if (_consecutiveBackspaceWithoutOutput >= MaxConsecutiveBackspaceWithoutOutput)
                {
                    index++;
                    continue;
                }

                output.Add(current);
                _consecutiveBackspaceWithoutOutput++;
                index++;
                continue;
            }

            _consecutiveBackspaceWithoutOutput = 0;
            output.Add(current);
            index++;
        }

        return [.. output];
    }

    private static bool ContainsBackspace(ReadOnlySpan<byte> input)
    {
        foreach (var b in input)
        {
            if (b is 0x7F or 0x08)
                return true;
        }

        return false;
    }
}
