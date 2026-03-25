using Microsoft.Win32;

namespace OpenTraceProject.Core.Registry;

public sealed record RegistryValueReference(
    RegistryHive Hive,
    RegistryView View,
    string KeyPath,
    string ValueName);
