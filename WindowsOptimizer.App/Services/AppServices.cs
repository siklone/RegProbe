using System;
using WindowsOptimizer.Infrastructure.Threading;

namespace WindowsOptimizer.App.Services;

public static class AppServices
{
    private static MetricWorkerPool? _metricWorkerPool;

    public static MetricWorkerPool? MetricWorkerPool => _metricWorkerPool;

    public static MetricDataBus? MetricBus => _metricWorkerPool?.Bus;

    public static void InitializeMetricThreading(Action<Action> uiDispatcher, int workerCount = 4)
    {
        if (_metricWorkerPool != null)
        {
            return;
        }

        var bus = new MetricDataBus(uiDispatcher);
        _metricWorkerPool = new MetricWorkerPool(bus, workerCount);
    }

    public static void Dispose()
    {
        _metricWorkerPool?.Dispose();
        _metricWorkerPool = null;
    }
}
