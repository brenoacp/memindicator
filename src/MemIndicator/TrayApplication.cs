using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MemIndicator;

internal sealed class TrayApplication : ApplicationContext
{
    // install.ps1 signals this to close the running copy cleanly, so no ghost icon stays in the tray
    private const string ExitEventName = @"Local\MemIndicator-Exit";
    private const int RefreshIntervalMs = 1000;
    private const int PromotionIntervalMs = 2000;
    private const int PromotionAttempts = 15;
    private const int ErrorCancelled = 1223;

    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = RefreshIntervalMs };
    private readonly System.Windows.Forms.Timer _promotionTimer = new() { Interval = PromotionIntervalMs };
    private readonly ToolStripMenuItem _percentItem;
    private readonly ToolStripMenuItem _gigabytesItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly EventWaitHandle _exitRequested = new(false, EventResetMode.AutoReset, ExitEventName);
    private readonly RegisteredWaitHandle _exitWait;

    private DisplayMode _mode = AppSettings.DisplayMode;
    private Icon? _currentIcon;
    private string? _currentIconKey;
    private int _promotionAttemptsLeft = PromotionAttempts;

    public TrayApplication()
    {
        InitializeAutoStart();

        _percentItem = new ToolStripMenuItem("Mostrar porcentagem (%)", null, (_, _) => SetMode(DisplayMode.Percent));
        _gigabytesItem = new ToolStripMenuItem("Mostrar GB usados", null, (_, _) => SetMode(DisplayMode.Gigabytes));
        _autoStartItem = new ToolStripMenuItem("Iniciar com o Windows", null, OnAutoStartClick) { Checked = AutoStart.IsEnabled };
        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _percentItem,
            _gigabytesItem,
            new ToolStripSeparator(),
            _autoStartItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Sair", null, (_, _) => ExitThread()),
        });
        UpdateModeChecks();

        _notifyIcon = new NotifyIcon { ContextMenuStrip = menu };
        _notifyIcon.MouseClick += OnMouseClick;
        RefreshIcon();
        _notifyIcon.Visible = true;

        _refreshTimer.Tick += (_, _) => RefreshIcon();
        _refreshTimer.Start();
        _promotionTimer.Tick += OnPromotionTick;
        _promotionTimer.Start();

        // A signal left over from before this instance started is not meant for it
        _exitRequested.Reset();
        var uiContext = SynchronizationContext.Current!;
        _exitWait = ThreadPool.RegisterWaitForSingleObject(
            _exitRequested, (_, _) => uiContext.Post(_ => ExitThread(), null), null, Timeout.Infinite, executeOnlyOnce: true);
    }

    protected override void ExitThreadCore()
    {
        _exitWait.Unregister(null);
        _exitRequested.Dispose();
        _refreshTimer.Dispose();
        _promotionTimer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        ReleaseIcon(_currentIcon);
        _currentIcon = null;
        base.ExitThreadCore();
    }

    private static void InitializeAutoStart()
    {
        if (!AppSettings.AutoStartInitialized)
        {
            AutoStart.Enable();
            AppSettings.AutoStartInitialized = true;
        }
        else if (AutoStart.IsEnabled)
        {
            AutoStart.Enable();
        }
    }

    private void RefreshIcon()
    {
        var snapshot = MemoryReader.Read();
        var text = DisplayFormatter.IconText(snapshot, _mode);
        var color = DisplayFormatter.ColorFor(DisplayFormatter.LevelFor(snapshot.LoadPercent), IsLightTaskbar());
        var size = SystemInformation.SmallIconSize.Width;

        var iconKey = $"{text}|{color.ToArgb()}|{size}";
        if (iconKey != _currentIconKey)
        {
            using var bitmap = IconRenderer.Render(text, color, size);
            var icon = Icon.FromHandle(bitmap.GetHicon());
            _notifyIcon.Icon = icon;
            ReleaseIcon(_currentIcon);
            _currentIcon = icon;
            _currentIconKey = iconKey;
        }

        _notifyIcon.Text = DisplayFormatter.Tooltip(snapshot);
    }

    private void SetMode(DisplayMode mode)
    {
        _mode = mode;
        AppSettings.DisplayMode = mode;
        UpdateModeChecks();
        RefreshIcon();
    }

    private void UpdateModeChecks()
    {
        _percentItem.Checked = _mode == DisplayMode.Percent;
        _gigabytesItem.Checked = _mode == DisplayMode.Gigabytes;
    }

    private void OnAutoStartClick(object? sender, EventArgs e)
    {
        if (AutoStart.IsEnabled) AutoStart.Disable();
        else AutoStart.Enable();

        _autoStartItem.Checked = AutoStart.IsEnabled;
    }

    private static void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        try
        {
            Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            // Elevation prompt dismissed
        }
    }

    // Explorer creates the icon's NotifyIconSettings entry a few seconds after it first appears
    private void OnPromotionTick(object? sender, EventArgs e)
    {
        if (TrayPromoter.TryPromote() || --_promotionAttemptsLeft == 0) _promotionTimer.Stop();
    }

    private static bool IsLightTaskbar()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("SystemUsesLightTheme") is 1;
    }

    // Icon.FromHandle does not own the HICON from GetHicon, so it has to be destroyed explicitly
    private static void ReleaseIcon(Icon? icon)
    {
        if (icon is null) return;

        DestroyIcon(icon.Handle);
        icon.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
