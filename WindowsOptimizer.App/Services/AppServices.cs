using System;
using WindowsOptimizer.Infrastructure.Threading;
using WindowsOptimizer.App.Services.OsDetection;
using WindowsOptimizer.App.Services.Hardware;

namespace WindowsOptimizer.App.Services;

public static class AppServices
{
    private static MetricWorkerPool? _metricWorkerPool;
    private static OsDetectionService? _osDetectionService;
    private static MotherboardWmiProvider? _motherboardProvider;

    public static MetricWorkerPool? MetricWorkerPool => _metricWorkerPool;

    public static MetricDataBus? MetricBus => _metricWorkerPool?.Bus;

    // Expose hardware provider instances via AppServices so callers can resolve
    // consistent instances without directly using 'new' throughout the codebase.
    public static IOsDetectionService OsDetectionService => _osDetectionService ??= new OsDetectionService();

    public static IMotherboardProvider MotherboardProvider => _motherboardProvider ??= new MotherboardWmiProvider();

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
        // Clear provider singletons if present
        _osDetectionService = null;
        _motherboardProvider = null;
    }
}
