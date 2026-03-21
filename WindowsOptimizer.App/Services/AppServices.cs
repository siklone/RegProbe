using System;
using WindowsOptimizer.App.Services.OsDetection;
using WindowsOptimizer.App.Services.Hardware;

namespace WindowsOptimizer.App.Services;

public static class AppServices
{
    private static OsDetectionService? _osDetectionService;
    private static MotherboardWmiProvider? _motherboardProvider;

    // Expose hardware provider instances via AppServices so callers can resolve
    // consistent instances without directly using 'new' throughout the codebase.
    public static IOsDetectionService OsDetectionService => _osDetectionService ??= new OsDetectionService();

    public static IMotherboardProvider MotherboardProvider => _motherboardProvider ??= new MotherboardWmiProvider();

    public static void Dispose()
    {
        // Clear provider singletons if present
        _osDetectionService = null;
        _motherboardProvider = null;
    }
}
