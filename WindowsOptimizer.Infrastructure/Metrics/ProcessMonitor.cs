using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed class ProcessMonitor
{
    private Dictionary<int, (DateTime Time, TimeSpan TotalProcessorTime)> _previousCpuUsage = new();
    private Dictionary<int, (DateTime Time, ulong TotalBytes)> _previousIoUsage = new();
    private Dictionary<int, (DateTime Time, ulong TotalBytes)> _previousNetworkUsage = new();

    public bool IsNetworkApproximate { get; private set; } = true;

    public List<ProcessInfo> GetTopProcessesByCpu(int count = 10)
    {
        var processes = new List<ProcessInfo>();
        var currentTime = DateTime.UtcNow;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var cpuUsage = CalculateCpuUsage(process, currentTime);
                var ramMb = process.WorkingSet64 / (1024.0 * 1024.0);

                processes.Add(new ProcessInfo
                {
                    Name = process.ProcessName,
                    Pid = process.Id,
                    CpuPercent = cpuUsage,
                    RamMb = ramMb,
                    Threads = process.Threads.Count,
                    Handles = process.HandleCount
                });
            }
            catch
            {
                // Process may have exited, skip
            }
        }

        return processes.OrderByDescending(p => p.CpuPercent).Take(count).ToList();
    }

    public List<ProcessInfo> GetTopProcessesByRam(int count = 10)
    {
        var processes = new List<ProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var ramMb = process.WorkingSet64 / (1024.0 * 1024.0);

                processes.Add(new ProcessInfo
                {
                    Name = process.ProcessName,
                    Pid = process.Id,
                    RamMb = ramMb,
                    CpuPercent = 0, // Not needed for RAM sort
                    Threads = process.Threads.Count,
                    Handles = process.HandleCount
                });
            }
            catch { }
        }

        return processes.OrderByDescending(p => p.RamMb).Take(count).ToList();
    }

    public List<ProcessInfo> GetTopProcessesByIo(int count = 10)
    {
        var processes = new List<ProcessInfo>();
        var currentTime = DateTime.UtcNow;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (!TryGetIoBytes(process, out var totalBytes))
                {
                    continue;
                }

                var ioMbps = CalculateIoMbps(process.Id, totalBytes, currentTime);

                processes.Add(new ProcessInfo
                {
                    Name = process.ProcessName,
                    Pid = process.Id,
                    IoMbps = ioMbps,
                    RamMb = process.WorkingSet64 / (1024.0 * 1024.0),
                    Threads = process.Threads.Count,
                    Handles = process.HandleCount
                });
            }
            catch
            {
                // Process may have exited or is not accessible
            }
        }

        return processes.OrderByDescending(p => p.IoMbps).Take(count).ToList();
    }

    public List<ProcessInfo> GetTopProcessesByNetwork(int count = 10)
    {
        var currentTime = DateTime.UtcNow;

        if (TryGetTcpBytesByPid(out var bytesByPid))
        {
            IsNetworkApproximate = false;
            var processes = new List<ProcessInfo>();

            foreach (var entry in bytesByPid)
            {
                try
                {
                    var process = Process.GetProcessById(entry.Key);
                    var networkMbps = CalculateNetworkMbps(entry.Key, entry.Value, currentTime);

                    processes.Add(new ProcessInfo
                    {
                        Name = process.ProcessName,
                        Pid = process.Id,
                        IoMbps = networkMbps,
                        RamMb = process.WorkingSet64 / (1024.0 * 1024.0),
                        Threads = process.Threads.Count,
                        Handles = process.HandleCount
                    });
                }
                catch
                {
                    // Process may have exited or is not accessible
                }
            }

            return processes.OrderByDescending(p => p.IoMbps).Take(count).ToList();
        }

        IsNetworkApproximate = true;
        return GetTopProcessesByIo(count);
    }

    private double CalculateCpuUsage(Process process, DateTime currentTime)
    {
        var pid = process.Id;
        var currentTotalTime = process.TotalProcessorTime;

        if (_previousCpuUsage.TryGetValue(pid, out var previous))
        {
            var timeDiff = (currentTime - previous.Time).TotalMilliseconds;
            var cpuDiff = (currentTotalTime - previous.TotalProcessorTime).TotalMilliseconds;

            if (timeDiff > 0)
            {
                var cpuUsage = (cpuDiff / (timeDiff * Environment.ProcessorCount)) * 100.0;
                _previousCpuUsage[pid] = (currentTime, currentTotalTime);
                return Math.Min(cpuUsage, 100.0); // Cap at 100%
            }
        }

        _previousCpuUsage[pid] = (currentTime, currentTotalTime);
        return 0;
    }

    private double CalculateIoMbps(int pid, ulong currentTotalBytes, DateTime currentTime)
    {
        if (_previousIoUsage.TryGetValue(pid, out var previous))
        {
            var seconds = (currentTime - previous.Time).TotalSeconds;
            var diffBytes = currentTotalBytes >= previous.TotalBytes ? currentTotalBytes - previous.TotalBytes : 0;

            _previousIoUsage[pid] = (currentTime, currentTotalBytes);

            if (seconds > 0)
            {
                var bytesPerSecond = diffBytes / seconds;
                return (bytesPerSecond * 8.0) / (1024.0 * 1024.0);
            }

            return 0;
        }

        _previousIoUsage[pid] = (currentTime, currentTotalBytes);
        return 0;
    }

    private double CalculateNetworkMbps(int pid, ulong currentTotalBytes, DateTime currentTime)
    {
        if (_previousNetworkUsage.TryGetValue(pid, out var previous))
        {
            var seconds = (currentTime - previous.Time).TotalSeconds;
            var diffBytes = currentTotalBytes >= previous.TotalBytes ? currentTotalBytes - previous.TotalBytes : 0;

            _previousNetworkUsage[pid] = (currentTime, currentTotalBytes);

            if (seconds > 0)
            {
                var bytesPerSecond = diffBytes / seconds;
                return (bytesPerSecond * 8.0) / (1024.0 * 1024.0);
            }

            return 0;
        }

        _previousNetworkUsage[pid] = (currentTime, currentTotalBytes);
        return 0;
    }

    private static bool TryGetIoBytes(Process process, out ulong totalBytes)
    {
        totalBytes = 0;

        if (process.HasExited)
        {
            return false;
        }

        if (!GetProcessIoCounters(process.Handle, out var counters))
        {
            return false;
        }

        totalBytes = counters.ReadTransferCount + counters.WriteTransferCount + counters.OtherTransferCount;
        return true;
    }

    public void Cleanup()
    {
        // Remove entries for processes that no longer exist
        var currentPids = Process.GetProcesses().Select(p => p.Id).ToHashSet();
        var deadPids = _previousCpuUsage.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
        foreach (var pid in deadPids)
        {
            _previousCpuUsage.Remove(pid);
        }

        var deadIoPids = _previousIoUsage.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
        foreach (var pid in deadIoPids)
        {
            _previousIoUsage.Remove(pid);
        }

        var deadNetworkPids = _previousNetworkUsage.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
        foreach (var pid in deadNetworkPids)
        {
            _previousNetworkUsage.Remove(pid);
        }
    }

    public bool KillProcess(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            process.Kill();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool SuspendProcess(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            foreach (System.Diagnostics.ProcessThread thread in process.Threads)
            {
                var threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (threadHandle != IntPtr.Zero)
                {
                    SuspendThread(threadHandle);
                    CloseHandle(threadHandle);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ResumeProcess(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            foreach (System.Diagnostics.ProcessThread thread in process.Threads)
            {
                var threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (threadHandle != IntPtr.Zero)
                {
                    ResumeThread(threadHandle);
                    CloseHandle(threadHandle);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    // P/Invoke for thread suspension
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint SuspendThread(IntPtr hThread);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern int ResumeThread(IntPtr hThread);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [System.Flags]
    private enum ThreadAccess : int
    {
        SUSPEND_RESUME = 0x0002
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessIoCounters(IntPtr hProcess, out IoCounters ioCounters);

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    private const uint ErrorInsufficientBuffer = 122;
    private const int AfInet = 2;
    private const int AfInet6 = 23;

    private enum TcpTableClass
    {
        TcpTableOwnerPidAll = 5
    }

    private enum TcpEstatsType
    {
        TcpConnectionEstatsSynOpts,
        TcpConnectionEstatsData,
        TcpConnectionEstatsSndCong,
        TcpConnectionEstatsPath,
        TcpConnectionEstatsSendBuff,
        TcpConnectionEstatsRec,
        TcpConnectionEstatsObsRec,
        TcpConnectionEstatsBandwidth,
        TcpConnectionEstatsFineRtt,
        TcpConnectionEstatsMaximum
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRow
    {
        public uint State;
        public uint LocalAddr;
        public uint LocalPort;
        public uint RemoteAddr;
        public uint RemotePort;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        public uint State;
        public uint LocalAddr;
        public uint LocalPort;
        public uint RemoteAddr;
        public uint RemotePort;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp6Row
    {
        public uint State;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] LocalAddr;
        public uint LocalScopeId;
        public uint LocalPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] RemoteAddr;
        public uint RemoteScopeId;
        public uint RemotePort;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] LocalAddr;
        public uint LocalScopeId;
        public uint LocalPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] RemoteAddr;
        public uint RemoteScopeId;
        public uint RemotePort;
        public uint State;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TcpEstatsDataRodV0
    {
        public ulong DataBytesOut;
        public ulong DataSegsOut;
        public ulong DataBytesIn;
        public ulong DataSegsIn;
        public ulong SegsOut;
        public ulong SegsIn;
        public uint SoftErrors;
        public uint SoftErrorReason;
        public uint SndUna;
        public uint SndNxt;
        public uint SndMax;
        public ulong ThruBytesAcked;
        public uint RcvNxt;
        public ulong ThruBytesReceived;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TcpEstatsDataRwV0
    {
        public byte EnableCollection;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        TcpTableClass tableClass,
        uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetPerTcpConnectionEStats(
        ref MibTcpRow row,
        TcpEstatsType estatsType,
        IntPtr rw,
        uint rwVersion,
        uint rwSize,
        IntPtr ros,
        uint rosVersion,
        uint rosSize,
        out TcpEstatsDataRodV0 rod,
        uint rodVersion,
        uint rodSize);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint SetPerTcpConnectionEStats(
        ref MibTcpRow row,
        TcpEstatsType estatsType,
        ref TcpEstatsDataRwV0 rw,
        uint rwVersion,
        uint rwSize,
        uint offset);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetPerTcp6ConnectionEStats(
        ref MibTcp6Row row,
        TcpEstatsType estatsType,
        IntPtr rw,
        uint rwVersion,
        uint rwSize,
        IntPtr ros,
        uint rosVersion,
        uint rosSize,
        out TcpEstatsDataRodV0 rod,
        uint rodVersion,
        uint rodSize);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint SetPerTcp6ConnectionEStats(
        ref MibTcp6Row row,
        TcpEstatsType estatsType,
        ref TcpEstatsDataRwV0 rw,
        uint rwVersion,
        uint rwSize,
        uint offset);

    private static bool TryGetTcpBytesByPid(out Dictionary<int, ulong> bytesByPid)
    {
        bytesByPid = new Dictionary<int, ulong>();
        var anySuccess = false;

        TryCollectTcpBytes(bytesByPid, ref anySuccess);
        TryCollectTcp6Bytes(bytesByPid, ref anySuccess);

        return anySuccess;
    }

    private static void TryCollectTcpBytes(Dictionary<int, ulong> bytesByPid, ref bool anySuccess)
    {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AfInet, TcpTableClass.TcpTableOwnerPidAll, 0);
        if (result != ErrorInsufficientBuffer || bufferSize <= 0)
        {
            return;
        }

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(buffer, ref bufferSize, true, AfInet, TcpTableClass.TcpTableOwnerPidAll, 0);
            if (result != 0)
            {
                return;
            }

            var rowCount = Marshal.ReadInt32(buffer);
            if (rowCount == 0)
            {
                anySuccess = true;
                return;
            }
            var rowPtr = IntPtr.Add(buffer, sizeof(int));
            var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();

            for (var i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPtr);
                if (TryGetTcpRowBytes(row, out var bytes, ref anySuccess))
                {
                    var pid = (int)row.OwningPid;
                    bytesByPid[pid] = bytesByPid.TryGetValue(pid, out var total) ? total + bytes : bytes;
                }

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void TryCollectTcp6Bytes(Dictionary<int, ulong> bytesByPid, ref bool anySuccess)
    {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AfInet6, TcpTableClass.TcpTableOwnerPidAll, 0);
        if (result != ErrorInsufficientBuffer || bufferSize <= 0)
        {
            return;
        }

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(buffer, ref bufferSize, true, AfInet6, TcpTableClass.TcpTableOwnerPidAll, 0);
            if (result != 0)
            {
                return;
            }

            var rowCount = Marshal.ReadInt32(buffer);
            if (rowCount == 0)
            {
                anySuccess = true;
                return;
            }
            var rowPtr = IntPtr.Add(buffer, sizeof(int));
            var rowSize = Marshal.SizeOf<MibTcp6RowOwnerPid>();

            for (var i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MibTcp6RowOwnerPid>(rowPtr);
                if (TryGetTcp6RowBytes(row, out var bytes, ref anySuccess))
                {
                    var pid = (int)row.OwningPid;
                    bytesByPid[pid] = bytesByPid.TryGetValue(pid, out var total) ? total + bytes : bytes;
                }

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryGetTcpRowBytes(MibTcpRowOwnerPid row, out ulong bytes, ref bool anySuccess)
    {
        bytes = 0;
        var tcpRow = new MibTcpRow
        {
            State = row.State,
            LocalAddr = row.LocalAddr,
            LocalPort = row.LocalPort,
            RemoteAddr = row.RemoteAddr,
            RemotePort = row.RemotePort
        };

        var enable = new TcpEstatsDataRwV0 { EnableCollection = 1 };
        _ = SetPerTcpConnectionEStats(ref tcpRow, TcpEstatsType.TcpConnectionEstatsData, ref enable, 0, (uint)Marshal.SizeOf<TcpEstatsDataRwV0>(), 0);

        var result = GetPerTcpConnectionEStats(ref tcpRow,
            TcpEstatsType.TcpConnectionEstatsData,
            IntPtr.Zero, 0, 0,
            IntPtr.Zero, 0, 0,
            out var rod, 0, (uint)Marshal.SizeOf<TcpEstatsDataRodV0>());

        if (result != 0)
        {
            return false;
        }

        anySuccess = true;
        bytes = rod.DataBytesIn + rod.DataBytesOut;
        return true;
    }

    private static bool TryGetTcp6RowBytes(MibTcp6RowOwnerPid row, out ulong bytes, ref bool anySuccess)
    {
        bytes = 0;
        var tcpRow = new MibTcp6Row
        {
            State = row.State,
            LocalAddr = row.LocalAddr,
            LocalScopeId = row.LocalScopeId,
            LocalPort = row.LocalPort,
            RemoteAddr = row.RemoteAddr,
            RemoteScopeId = row.RemoteScopeId,
            RemotePort = row.RemotePort
        };

        var enable = new TcpEstatsDataRwV0 { EnableCollection = 1 };
        _ = SetPerTcp6ConnectionEStats(ref tcpRow, TcpEstatsType.TcpConnectionEstatsData, ref enable, 0, (uint)Marshal.SizeOf<TcpEstatsDataRwV0>(), 0);

        var result = GetPerTcp6ConnectionEStats(ref tcpRow,
            TcpEstatsType.TcpConnectionEstatsData,
            IntPtr.Zero, 0, 0,
            IntPtr.Zero, 0, 0,
            out var rod, 0, (uint)Marshal.SizeOf<TcpEstatsDataRodV0>());

        if (result != 0)
        {
            return false;
        }

        anySuccess = true;
        bytes = rod.DataBytesIn + rod.DataBytesOut;
        return true;
    }
}

public sealed class ProcessInfo
{
    public string Name { get; set; } = string.Empty;
    public int Pid { get; set; }
    public double CpuPercent { get; set; }
    public double RamMb { get; set; }
    public double IoMbps { get; set; }
    public int Threads { get; set; }
    public int Handles { get; set; }
    public string Status { get; set; } = "Running";

    public string RamFormatted => $"{RamMb:F1} MB";
    public string IoFormatted => $"{IoMbps:F2} Mbps";
}
