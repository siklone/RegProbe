using System;

namespace WindowsOptimizer.Infrastructure.Monitoring;

public sealed record SensorReading(
    string Name,
    SensorType Type,
    double Value,
    string Unit,
    DateTimeOffset Timestamp,
    string Source);
