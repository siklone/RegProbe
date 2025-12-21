using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Infrastructure.Services;
using Xunit;

public sealed class ServiceStartModeBatchTweakTests
{
    [Fact]
    public async Task ApplyRollback_DisablesAndRestoresServices()
    {
        var manager = new FakeServiceManager();
        manager.AddService("TestService", ServiceStartMode.Automatic, ServiceStatus.Running);
        manager.AddService("UserService_123", ServiceStartMode.Manual, ServiceStatus.Stopped);

        var tweak = new ServiceStartModeBatchTweak(
            "test.services",
            "Test service tweak",
            "Disables services in bulk.",
            TweakRiskLevel.Advanced,
            new[]
            {
                new ServiceStartModeEntry("TestService", ServiceStartMode.Disabled),
                new ServiceStartModeEntry("UserService", ServiceStartMode.Disabled)
            },
            manager,
            stopRunning: true,
            requiresElevation: true);

        var detect = await tweak.DetectAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Detected, detect.Status);

        var apply = await tweak.ApplyAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Applied, apply.Status);

        var verify = await tweak.VerifyAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Verified, verify.Status);

        var rollback = await tweak.RollbackAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.RolledBack, rollback.Status);

        var restoredTest = await manager.QueryAsync("TestService", CancellationToken.None);
        Assert.Equal(ServiceStartMode.Automatic, restoredTest.StartMode);
        Assert.Equal(ServiceStatus.Running, restoredTest.Status);

        var restoredUser = await manager.QueryAsync("UserService_123", CancellationToken.None);
        Assert.Equal(ServiceStartMode.Manual, restoredUser.StartMode);
        Assert.Equal(ServiceStatus.Stopped, restoredUser.Status);
    }

    private sealed class FakeServiceManager : IServiceManager
    {
        private readonly Dictionary<string, ServiceInfo> _services = new(StringComparer.OrdinalIgnoreCase);

        public void AddService(string name, ServiceStartMode startMode, ServiceStatus status)
        {
            _services[name] = new ServiceInfo(true, startMode, status);
        }

        public Task<ServiceInfo> QueryAsync(string serviceName, CancellationToken ct)
        {
            if (_services.TryGetValue(serviceName, out var info))
            {
                return Task.FromResult(info);
            }

            return Task.FromResult(new ServiceInfo(false, ServiceStartMode.Unknown, ServiceStatus.Unknown));
        }

        public Task SetStartModeAsync(string serviceName, ServiceStartMode startMode, CancellationToken ct)
        {
            var info = _services[serviceName];
            _services[serviceName] = info with { StartMode = startMode };
            return Task.CompletedTask;
        }

        public Task StartAsync(string serviceName, CancellationToken ct)
        {
            if (_services.TryGetValue(serviceName, out var info))
            {
                _services[serviceName] = info with { Status = ServiceStatus.Running };
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(string serviceName, CancellationToken ct)
        {
            if (_services.TryGetValue(serviceName, out var info))
            {
                _services[serviceName] = info with { Status = ServiceStatus.Stopped };
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> ListServiceNamesAsync(CancellationToken ct)
        {
            return Task.FromResult((IReadOnlyList<string>)_services.Keys.ToList());
        }
    }
}
