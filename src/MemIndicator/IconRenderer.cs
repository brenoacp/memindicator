using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MemIndicator;

public static class IconRenderer
{
    private const int Margin = 1;
    private const float LayoutEmSize = 100f;
    private static readonly FontFamily Family = new("Segoe UI");

    public static Bitmap Render(string text, Color color, int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        // Pixel i spans [i, i+1], so whole-number path edges land exactly on pixel edges
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using var path = TextPath(text);
        var bounds = LayoutBounds(text);

        var available = size - 2 * Margin;
        var scale = Math.Min(available / bounds.Width, available / bounds.Height);
        // Whole-pixel offsets keep horizontal strokes crisp
        var left = MathF.Round((size - bounds.Width * scale) / 2);
        var top = MathF.Round((size - bounds.Height * scale) / 2);

        using var transform = new Matrix();
        transform.Translate(-bounds.X, -bounds.Y, MatrixOrder.Append);
        transform.Scale(scale, scale, MatrixOrder.Append);
        transform.Translate(left, top, MatrixOrder.Append);
        path.Transform(transform);

        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
        return bitmap;
    }

    // Covers the ink of any digit at each position, so "11" and "88" share glyph size and
    // position and no digit gets clipped at the icon edge
    private static RectangleF LayoutBounds(string text)
    {
        var bounds = RectangleF.Empty;
        foreach (var digit in "0123456789")
        {
            using var variant = TextPath(new string(text.Select(c => char.IsDigit(c) ? digit : c).ToArray()));
            var variantBounds = variant.GetBounds();
            bounds = bounds.IsEmpty ? variantBounds : RectangleF.Union(bounds, variantBounds);
        }

        return bounds;
    }

    private static GraphicsPath TextPath(string text)
    {
        var path = new GraphicsPath();
        path.AddString(text, Family, (int)FontStyle.Bold, LayoutEmSize, PointF.Empty, StringFormat.GenericTypographic);
        return path;
    }
}
