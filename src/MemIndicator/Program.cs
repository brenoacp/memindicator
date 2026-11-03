namespace MemIndicator;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var instance = new Mutex(initiallyOwned: true, @"Local\MemIndicator", out var isFirstInstance);
        if (!isFirstInstance) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplication());
    }
}
