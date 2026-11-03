using System.Globalization;

namespace MemIndicator;

public enum DisplayMode { Percent, Gigabytes }

public enum UsageLevel { Normal, Warning, Critical }

public sealed record MemorySnapshot(ulong TotalBytes, ulong AvailableBytes, int LoadPercent)
{
    public ulong UsedBytes => TotalBytes - AvailableBytes;
}

public static class DisplayFormatter
{
    private const double BytesPerGiB = 1024d * 1024 * 1024;
    private const int WarningPercent = 75;
    private const int CriticalPercent = 90;

    public static string IconText(MemorySnapshot snapshot, DisplayMode mode) => mode switch
    {
        DisplayMode.Percent => snapshot.LoadPercent.ToString(CultureInfo.InvariantCulture),
        DisplayMode.Gigabytes => CompactGigabytes(snapshot.UsedBytes),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    public static string Tooltip(MemorySnapshot snapshot) => string.Format(
        CultureInfo.InvariantCulture,
        "Memória: {0}% — {1:0.0} / {2:0.0} GB",
        snapshot.LoadPercent,
        ToGiB(snapshot.UsedBytes),
        ToGiB(snapshot.TotalBytes));

    public static UsageLevel LevelFor(int percent) => percent switch
    {
        >= CriticalPercent => UsageLevel.Critical,
        >= WarningPercent => UsageLevel.Warning,
        _ => UsageLevel.Normal,
    };

    public static Color ColorFor(UsageLevel level, bool lightTheme) => (level, lightTheme) switch
    {
        (UsageLevel.Warning, true) => Color.FromArgb(0xC2, 0x54, 0x00),
        (UsageLevel.Warning, false) => Color.FromArgb(0xFF, 0xA0, 0x40),
        (UsageLevel.Critical, true) => Color.FromArgb(0xC4, 0x2B, 0x1C),
        (UsageLevel.Critical, false) => Color.FromArgb(0xFF, 0x6B, 0x5E),
        (_, true) => Color.Black,
        (_, false) => Color.White,
    };

    // Fits the 2-3 characters an icon can hold: "9.7" below 10 GB, "12" from there on
    private static string CompactGigabytes(ulong bytes)
    {
        var gib = ToGiB(bytes);
        var tenths = Math.Round(gib, 1, MidpointRounding.AwayFromZero);
        return tenths < 10
            ? tenths.ToString("0.0", CultureInfo.InvariantCulture)
            : Math.Round(gib, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
    }

    private static double ToGiB(ulong bytes) => bytes / BytesPerGiB;
}
