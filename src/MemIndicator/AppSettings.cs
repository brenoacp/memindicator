using Microsoft.Win32;

namespace MemIndicator;

internal static class AppSettings
{
    private const string KeyPath = @"Software\MemIndicator";

    public static DisplayMode DisplayMode
    {
        get => Enum.TryParse<DisplayMode>(Read(nameof(DisplayMode)) as string, out var mode) ? mode : DisplayMode.Percent;
        set => Write(nameof(DisplayMode), value.ToString(), RegistryValueKind.String);
    }

    // Autostart is switched on by default only once, so turning it off in the menu sticks
    public static bool AutoStartInitialized
    {
        get => Read(nameof(AutoStartInitialized)) is 1;
        set => Write(nameof(AutoStartInitialized), value ? 1 : 0, RegistryValueKind.DWord);
    }

    private static object? Read(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(name);
    }

    private static void Write(string name, object value, RegistryValueKind kind)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        key.SetValue(name, value, kind);
    }
}
