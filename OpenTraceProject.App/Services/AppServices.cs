using System;
using OpenTraceProject.App.Services.OsDetection;
using OpenTraceProject.App.Services.Hardware;

namespace OpenTraceProject.App.Services;

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
