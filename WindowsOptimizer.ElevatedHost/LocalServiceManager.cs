using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.Core.Services;

namespace WindowsOptimizer.ElevatedHost;

internal sealed class LocalServiceManager : IServiceManager
{
    private static readonly TimeSpan ServiceTimeout = TimeSpan.FromSeconds(30);

    public Task<ServiceInfo> QueryAsync(string serviceName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name is required.", nameof(serviceName));
        }

        var exists = false;
        var startMode = Infrastructure.Services.ServiceStartMode.Unknown;
        using (var key = Registry.LocalMachine.OpenSubKey(BuildServiceKeyPath(serviceName), false))
        {
            if (key != null)
            {
                exists = true;
                startMode = MapStartMode(key.GetValue("Start"));
            }
        }

        var status = ServiceStatus.Unknown;
        if (exists)
        {
            try
            {
                using var controller = new ServiceController(serviceName);
                status = MapStatus(controller.Status);
            }
            catch (InvalidOperationException)
            {
                status = ServiceStatus.Unknown;
            }
        }

        return Task.FromResult(new ServiceInfo(exists, startMode, status));
    }

    public Task SetStartModeAsync(string serviceName, Infrastructure.Services.ServiceStartMode startMode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name is required.", nameof(serviceName));
        }

        if (startMode is Infrastructure.Services.ServiceStartMode.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(startMode), startMode, "Start mode must be a concrete value.");
        }

        using var key = Registry.LocalMachine.OpenSubKey(BuildServiceKeyPath(serviceName), true);
        if (key is null)
        {
            throw new InvalidOperationException($"Service '{serviceName}' was not found.");
        }

        key.SetValue("Start", (int)startMode, RegistryValueKind.DWord);
        return Task.CompletedTask;
    }

    public Task StartAsync(string serviceName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name is required.", nameof(serviceName));
        }

        using var controller = new ServiceController(serviceName);
        if (controller.Status == ServiceControllerStatus.Running)
        {
            return Task.CompletedTask;
        }

        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running, ServiceTimeout);
        return Task.CompletedTask;
    }

    public Task StopAsync(string serviceName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name is required.", nameof(serviceName));
        }

        using var controller = new ServiceController(serviceName);
        if (controller.Status == ServiceControllerStatus.Stopped)
        {
            return Task.CompletedTask;
        }

        if (!controller.CanStop)
        {
            return Task.CompletedTask;
        }

        controller.Stop();
        controller.WaitForStatus(ServiceControllerStatus.Stopped, ServiceTimeout);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListServiceNamesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var service in ServiceController.GetServices())
        {
            names.Add(service.ServiceName);
        }

        foreach (var device in ServiceController.GetDevices())
        {
            names.Add(device.ServiceName);
        }

        return Task.FromResult((IReadOnlyList<string>)names.ToList());
    }

    private static string BuildServiceKeyPath(string serviceName)
    {
        return $"SYSTEM\\CurrentControlSet\\Services\\{serviceName}";
    }

    private static Infrastructure.Services.ServiceStartMode MapStartMode(object? startValue)
    {
        return startValue switch
        {
            int start => start switch
            {
                0 => Infrastructure.Services.ServiceStartMode.Boot,
                1 => Infrastructure.Services.ServiceStartMode.System,
                2 => Infrastructure.Services.ServiceStartMode.Automatic,
                3 => Infrastructure.Services.ServiceStartMode.Manual,
                4 => Infrastructure.Services.ServiceStartMode.Disabled,
                _ => Infrastructure.Services.ServiceStartMode.Unknown
            },
            _ => Infrastructure.Services.ServiceStartMode.Unknown
        };
    }

    private static ServiceStatus MapStatus(ServiceControllerStatus status)
    {
        return status switch
        {
            ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
            ServiceControllerStatus.StartPending => ServiceStatus.StartPending,
            ServiceControllerStatus.StopPending => ServiceStatus.StopPending,
            ServiceControllerStatus.Running => ServiceStatus.Running,
            ServiceControllerStatus.ContinuePending => ServiceStatus.ContinuePending,
            ServiceControllerStatus.PausePending => ServiceStatus.PausePending,
            ServiceControllerStatus.Paused => ServiceStatus.Paused,
            _ => ServiceStatus.Unknown
        };
    }
}
