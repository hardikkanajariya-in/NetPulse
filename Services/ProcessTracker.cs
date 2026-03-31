using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpeedoMeter.Services;

public sealed class ProcessTracker
{
    private const int AF_INET = 2;
    private const int TCP_TABLE_OWNER_PID_ALL = 5;
    private const int UDP_TABLE_OWNER_PID = 1;
    private const uint ERROR_SUCCESS = 0;

    private static readonly HashSet<string> SystemProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "svchost", "lsass", "csrss", "smss", "wininit",
        "services", "Registry", "MemCompression", "fontdrvhost", "dwm",
        "conhost", "sihost", "taskhostw", "ctfmon", "dllhost",
        "WmiPrvSE", "SearchHost", "StartMenuExperienceHost",
        "RuntimeBroker", "ShellExperienceHost", "TextInputHost"
    };

    private readonly Dictionary<int, CachedProcess> _cache = new();
    private DateTime _lastClean = DateTime.UtcNow;

    public List<ProcessNetworkInfo> Poll()
    {
        var pidConns = new Dictionary<uint, int>();

        try { CollectTcpPids(pidConns); } catch { }
        try { CollectUdpPids(pidConns); } catch { }

        pidConns.Remove(0);
        pidConns.Remove(4);

        if ((DateTime.UtcNow - _lastClean).TotalSeconds > 60)
        {
            var stale = _cache.Where(kv => (DateTime.UtcNow - kv.Value.Timestamp).TotalMinutes > 5)
                .Select(kv => kv.Key).ToList();
            foreach (var k in stale) _cache.Remove(k);
            _lastClean = DateTime.UtcNow;
        }

        var grouped = new Dictionary<string, ProcessNetworkInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in pidConns)
        {
            var proc = ResolveProcess((int)kv.Key);
            if (proc == null || SystemProcesses.Contains(proc.Name)) continue;

            if (!grouped.TryGetValue(proc.Name, out var info))
            {
                info = new ProcessNetworkInfo
                {
                    ProcessName = proc.Name,
                    ProcessPath = proc.Path ?? string.Empty
                };
                grouped[proc.Name] = info;
            }
            info.ConnectionCount += kv.Value;
        }

        return grouped.Values
            .OrderByDescending(p => p.ConnectionCount)
            .ToList();
    }

    private CachedProcess? ResolveProcess(int pid)
    {
        if (_cache.TryGetValue(pid, out var cached) &&
            (DateTime.UtcNow - cached.Timestamp).TotalSeconds < 30)
            return cached;

        try
        {
            using var process = Process.GetProcessById(pid);
            string name = process.ProcessName;
            string? path = null;
            try { path = process.MainModule?.FileName; } catch { }

            var entry = new CachedProcess(name, path, DateTime.UtcNow);
            _cache[pid] = entry;
            return entry;
        }
        catch
        {
            _cache.Remove(pid);
            return null;
        }
    }

    private static void CollectTcpPids(Dictionary<uint, int> pidConns)
    {
        int size = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref size, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
        if (size == 0) return;

        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedTcpTable(buffer, ref size, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0) != ERROR_SUCCESS)
                return;

            int count = Marshal.ReadInt32(buffer);
            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(buffer + 4 + i * rowSize);
                if (row.dwOwningPid != 0 && row.dwRemoteAddr != 0)
                {
                    pidConns.TryGetValue(row.dwOwningPid, out int existing);
                    pidConns[row.dwOwningPid] = existing + 1;
                }
            }
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void CollectUdpPids(Dictionary<uint, int> pidConns)
    {
        int size = 0;
        GetExtendedUdpTable(IntPtr.Zero, ref size, false, AF_INET, UDP_TABLE_OWNER_PID, 0);
        if (size == 0) return;

        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedUdpTable(buffer, ref size, false, AF_INET, UDP_TABLE_OWNER_PID, 0) != ERROR_SUCCESS)
                return;

            int count = Marshal.ReadInt32(buffer);
            int rowSize = Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();

            for (int i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(buffer + 4 + i * rowSize);
                if (row.dwOwningPid != 0)
                {
                    pidConns.TryGetValue(row.dwOwningPid, out int existing);
                    pidConns[row.dwOwningPid] = existing + 1;
                }
            }
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize,
        bool bOrder, int ulAf, int TableClass, uint Reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize,
        bool bOrder, int ulAf, int TableClass, uint Reserved);

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint dwState;
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwRemoteAddr;
        public uint dwRemotePort;
        public uint dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDPROW_OWNER_PID
    {
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwOwningPid;
    }

    private sealed record CachedProcess(string Name, string? Path, DateTime Timestamp);
}
