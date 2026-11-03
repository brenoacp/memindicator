using Microsoft.Win32;

namespace MemIndicator;

// Windows 11 hides new tray icons in the overflow flyout; each icon gets an entry under
// NotifyIconSettings whose IsPromoted value pins it next to the clock.
public static class TrayPromoter
{
    private const string NotifyIconSettingsPath = @"Control Panel\NotifyIconSettings";
    private const string ExecutableName = "MemIndicator.exe";

    // ExecutablePath may be absolute or start with a known-folder GUID, so only the file name is compared
    public static bool IsOwnEntry(string? executablePath) =>
        !string.IsNullOrEmpty(executablePath)
        && string.Equals(Path.GetFileName(executablePath), ExecutableName, StringComparison.OrdinalIgnoreCase);

    // Returns false while Explorer has not created this app's entry yet
    public static bool TryPromote()
    {
        using var settings = Registry.CurrentUser.OpenSubKey(NotifyIconSettingsPath);
        if (settings is null) return false;

        var found = false;
        foreach (var name in settings.GetSubKeyNames())
        {
            using var entry = settings.OpenSubKey(name);
            if (!IsOwnEntry(entry?.GetValue("ExecutablePath") as string)) continue;

            found = true;
            if (entry!.GetValue("IsPromoted") is 1) continue;

            using var writable = settings.OpenSubKey(name, writable: true);
            writable?.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
        }

        return found;
    }
}
