using Microsoft.Win32;

namespace MemIndicator;

internal static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MemIndicator";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string;
        }
    }

    // Also used to repoint an existing entry at the running executable after it moved
    public static void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
