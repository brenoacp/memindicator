using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemIndicator;

internal static class MemoryReader
{
    public static MemorySnapshot Read()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status)) throw new Win32Exception();

        return new MemorySnapshot(status.TotalPhys, status.AvailPhys, (int)status.MemoryLoad);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
