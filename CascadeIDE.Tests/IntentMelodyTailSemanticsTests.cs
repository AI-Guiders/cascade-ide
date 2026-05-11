using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntentMelodyTailSemanticsTests
{
    [Theory]
    [InlineData("<start:ln>:<end:ln>", 0, 2, 2)]
    [InlineData("<start:LINENUMBER>:<end:ln>", 0, 2, 2)]
    [InlineData("<start:int>:<end:int>", 2, 0, 2)]
    [InlineData("<a:int>:<b:ln>", 1, 1, 2)]
    [InlineData("<start:ln>", 0, 1, 1)]
    [InlineData("", 0, 0, 0)]
    public void Counts_int_ln_and_combined_delimited_numeric_slots(string sig, int intCount, int lnCount, int combined)
    {
        Assert.Equal(intCount, IntentMelodyTailSemantics.CountIntSlots(sig));
        Assert.Equal(lnCount, IntentMelodyTailSemantics.CountLineNumberSlots(sig));
        Assert.Equal(combined, IntentMelodyTailSemantics.CountDelimitedNumericSlots(sig));
    }
}
