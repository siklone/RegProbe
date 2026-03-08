using Microsoft.Win32;

namespace WindowsOptimizer.Core.Registry;

public sealed record RegistryValueReference(
    RegistryHive Hive,
    RegistryView View,
    string KeyPath,
    string ValueName);
