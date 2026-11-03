namespace MemIndicator.Tests;

public class TrayPromoterTests
{
    [Theory]
    [InlineData(@"C:\Users\someone\AppData\Local\MemIndicator\MemIndicator.exe")]
    [InlineData(@"{F1B32785-6FBA-4FCF-9D55-7B8E7F157091}\MemIndicator\MemIndicator.exe")]
    [InlineData(@"C:\temp\memindicator\publish\memindicator.EXE")]
    public void Recognizes_own_executable_entries(string executablePath)
    {
        Assert.True(TrayPromoter.IsOwnEntry(executablePath));
    }

    [Theory]
    [InlineData(@"C:\Program Files\Other\NotMemIndicator.exe")]
    [InlineData(@"C:\Windows\explorer.exe")]
    [InlineData("")]
    [InlineData(null)]
    public void Ignores_other_entries(string? executablePath)
    {
        Assert.False(TrayPromoter.IsOwnEntry(executablePath));
    }
}
