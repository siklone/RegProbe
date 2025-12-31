using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace WindowsOptimizer.Infrastructure.Metrics;

internal sealed class NetworkEtwSampler
{
    private readonly ConcurrentDictionary<int, ulong> _bytesByPid = new();
    private readonly object _sync = new();
    private TraceEventSession? _session;
    private Task? _processingTask;
    private bool _isRunning;
    private bool _failed;

    public bool IsRunning => _isRunning;

    public bool TryStart()
    {
        lock (_sync)
        {
            if (_failed)
            {
                return false;
            }

            if (_isRunning)
            {
                return true;
            }

            try
            {
                var sessionName = $"WindowsOptimizerNetwork-{Guid.NewGuid()}";
                _session = new TraceEventSession(sessionName)
                {
                    StopOnDispose = true
                };

                _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                var source = _session.Source;
                source.Kernel.TcpIpSend += data => AddBytes(data.ProcessID, data.size);
                source.Kernel.TcpIpRecv += data => AddBytes(data.ProcessID, data.size);
                source.Kernel.TcpIpSendIPV6 += data => AddBytes(data.ProcessID, data.size);
                source.Kernel.TcpIpRecvIPV6 += data => AddBytes(data.ProcessID, data.size);
                source.Kernel.UdpIpSend += data => AddBytes(data.ProcessID, data.size);
                source.Kernel.UdpIpRecv += data => AddBytes(data.ProcessID, data.size);
                source.Kernel.UdpIpSendIPV6 += data => AddBytes(data.ProcessID, data.size);
                source.Kernel.UdpIpRecvIPV6 += data => AddBytes(data.ProcessID, data.size);

                _processingTask = Task.Run(() =>
                {
                    try
                    {
                        source.Process();
                    }
                    catch
                    {
                        // Swallow ETW shutdown exceptions; fallback will handle.
                    }
                });

                _isRunning = true;
                return true;
            }
            catch
            {
                _failed = true;
                try
                {
                    _session?.Dispose();
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _session = null;
                }

                return false;
            }
        }
    }

    public Dictionary<int, ulong> SnapshotBytes()
    {
        return _bytesByPid.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    public void PruneToAlivePids(HashSet<int> alivePids)
    {
        foreach (var pid in _bytesByPid.Keys)
        {
            if (!alivePids.Contains(pid))
            {
                _bytesByPid.TryRemove(pid, out _);
            }
        }
    }

    private void AddBytes(int pid, int bytes)
    {
        if (pid <= 0 || bytes <= 0)
        {
            return;
        }

        var delta = (ulong)bytes;
        _bytesByPid.AddOrUpdate(pid, delta, (_, existing) => existing + delta);
    }
}
