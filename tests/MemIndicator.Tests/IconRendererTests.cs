using System.Drawing;

namespace MemIndicator.Tests;

public class IconRendererTests
{
    [Fact]
    public void Renders_bitmap_at_requested_size()
    {
        using var bitmap = IconRenderer.Render("62", Color.Black, 20);

        Assert.Equal(20, bitmap.Width);
        Assert.Equal(20, bitmap.Height);
    }

    [Theory]
    [InlineData("62")]
    [InlineData("100")]
    [InlineData("9.7")]
    public void Text_ink_stays_inside_the_icon_edges(string text)
    {
        using var bitmap = IconRenderer.Render(text, Color.Black, 20);

        var ink = InkBounds(bitmap);

        Assert.False(ink.IsEmpty, "nothing was drawn");
        Assert.True(ink.Left > 0 && ink.Right < bitmap.Width, $"ink {ink} clipped horizontally");
    }

    [Fact]
    public void Same_length_numbers_render_at_same_height()
    {
        using var narrow = IconRenderer.Render("11", Color.Black, 20);
        using var wide = IconRenderer.Render("88", Color.Black, 20);

        Assert.Equal(InkBounds(wide).Height, InkBounds(narrow).Height);
    }

    [Fact]
    public void Two_digit_numbers_use_most_of_the_icon_width()
    {
        using var bitmap = IconRenderer.Render("88", Color.Black, 20);

        Assert.True(InkBounds(bitmap).Width >= 16, $"ink width {InkBounds(bitmap).Width} too small to read");
    }

    private static Rectangle InkBounds(Bitmap bitmap)
    {
        int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            if (bitmap.GetPixel(x, y).A < 64) continue;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }

        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
