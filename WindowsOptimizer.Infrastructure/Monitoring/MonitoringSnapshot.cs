using System;
using System.Collections.Generic;

namespace WindowsOptimizer.Infrastructure.Monitoring;

public sealed record MonitoringSnapshot(
    IReadOnlyList<SensorReading> Readings,
    DateTimeOffset CapturedAt,
    string Source);
