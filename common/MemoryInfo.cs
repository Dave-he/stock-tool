using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace stock_tool.common;
[StructLayout(LayoutKind.Sequential)]
public struct MEMORYSTATUSEX
{
    public uint dwLength;
    public uint dwMemoryLoad;
    public ulong ullTotalPhys;
    public ulong ullAvailPhys;
    public ulong ullTotalPageFile;
    public ulong ullAvailPageFile;
    public ulong ullTotalVirtual;
    public ulong ullAvailVirtual;
    public ulong ullAvailExtendedVirtual;
}
class MemoryInfo
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public static ulong GetAvailableMemoryInMB()
    {
        MEMORYSTATUSEX memInfo = new MEMORYSTATUSEX();
        memInfo.dwLength = (uint)Marshal.SizeOf(memInfo);
        GlobalMemoryStatusEx(ref memInfo);
        return memInfo.ullAvailPhys / (1024 * 1024); // 转换为 MB
    }
}
