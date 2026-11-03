using System.Drawing;
using System.Globalization;

namespace MemIndicator.Tests;

public class DisplayFormatterTests
{
    private const double BytesPerGiB = 1024d * 1024 * 1024;

    private static MemorySnapshot Snapshot(double totalGiB, double usedGiB, int loadPercent) =>
        new((ulong)(totalGiB * BytesPerGiB), (ulong)((totalGiB - usedGiB) * BytesPerGiB), loadPercent);

    [Fact]
    public void Percent_mode_shows_load_percent()
    {
        var text = DisplayFormatter.IconText(Snapshot(16, 10, 62), DisplayMode.Percent);

        Assert.Equal("62", text);
    }

    [Fact]
    public void Percent_mode_shows_full_hundred()
    {
        var text = DisplayFormatter.IconText(Snapshot(16, 16, 100), DisplayMode.Percent);

        Assert.Equal("100", text);
    }

    [Fact]
    public void Gigabyte_mode_below_ten_shows_one_decimal()
    {
        var text = DisplayFormatter.IconText(Snapshot(16, 9.74, 61), DisplayMode.Gigabytes);

        Assert.Equal("9.7", text);
    }

    [Fact]
    public void Gigabyte_mode_from_ten_shows_integer()
    {
        var text = DisplayFormatter.IconText(Snapshot(16, 12.4, 77), DisplayMode.Gigabytes);

        Assert.Equal("12", text);
    }

    [Fact]
    public void Gigabyte_mode_rounding_up_to_ten_shows_integer()
    {
        var text = DisplayFormatter.IconText(Snapshot(16, 9.96, 62), DisplayMode.Gigabytes);

        Assert.Equal("10", text);
    }

    [Fact]
    public void Gigabyte_mode_uses_dot_even_in_portuguese_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            var text = DisplayFormatter.IconText(Snapshot(16, 9.74, 61), DisplayMode.Gigabytes);

            Assert.Equal("9.7", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Tooltip_shows_percent_and_used_over_total()
    {
        var tooltip = DisplayFormatter.Tooltip(Snapshot(15.69, 9.72, 62));

        Assert.Equal("Memória: 62% — 9.7 / 15.7 GB", tooltip);
    }

    [Theory]
    [InlineData(0, UsageLevel.Normal)]
    [InlineData(74, UsageLevel.Normal)]
    [InlineData(75, UsageLevel.Warning)]
    [InlineData(89, UsageLevel.Warning)]
    [InlineData(90, UsageLevel.Critical)]
    [InlineData(100, UsageLevel.Critical)]
    public void Level_follows_percent_thresholds(int percent, UsageLevel expected)
    {
        Assert.Equal(expected, DisplayFormatter.LevelFor(percent));
    }

    [Fact]
    public void Normal_text_is_black_on_light_taskbar()
    {
        Assert.Equal(Color.Black.ToArgb(), DisplayFormatter.ColorFor(UsageLevel.Normal, lightTheme: true).ToArgb());
    }

    [Fact]
    public void Normal_text_is_white_on_dark_taskbar()
    {
        Assert.Equal(Color.White.ToArgb(), DisplayFormatter.ColorFor(UsageLevel.Normal, lightTheme: false).ToArgb());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Alert_levels_have_distinct_colors(bool lightTheme)
    {
        var colors = new[] { UsageLevel.Normal, UsageLevel.Warning, UsageLevel.Critical }
            .Select(level => DisplayFormatter.ColorFor(level, lightTheme).ToArgb())
            .Distinct();

        Assert.Equal(3, colors.Count());
    }
}
